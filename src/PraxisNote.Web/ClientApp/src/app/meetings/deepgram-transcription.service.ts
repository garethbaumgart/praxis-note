import { Injectable, signal, computed, inject, OnDestroy, isDevMode } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { MockAuthService } from '../auth/mock-auth.service';

export interface TranscriptSegment {
  speaker: string;
  text: string;
}

@Injectable({ providedIn: 'root' })
export class DeepgramTranscriptionService implements OnDestroy {
  private readonly mockAuth = inject(MockAuthService);
  private readonly http = inject(HttpClient);
  private ws: WebSocket | null = null;
  private channels = 1;
  private localUserName = 'You';
  private encoding = '';

  // Reconnection state
  private intentionallyStopped = false;
  private hasEverConnected = false;
  private reconnectAttempts = 0;
  private reconnectTimeoutId: ReturnType<typeof setTimeout> | null = null;
  private pendingAudioChunks: ArrayBuffer[] = [];
  private droppedAudioChunks = 0;
  private static readonly MAX_RECONNECT_ATTEMPTS = 10;
  private static readonly MAX_DROPPED_CHUNKS_BEFORE_ERROR = 10;
  private static readonly INITIAL_RECONNECT_DELAY_MS = 500;
  private static readonly MAX_RECONNECT_DELAY_MS = 15000;
  private static readonly MAX_PENDING_CHUNKS = 30;

  readonly transcript = signal('');
  readonly segments = signal<TranscriptSegment[]>([]);
  readonly interimText = signal('');
  readonly interimSpeaker = signal('');
  readonly isListening = signal(false);
  readonly isReconnecting = signal(false);
  readonly error = signal<string | null>(null);

  readonly isSupported = computed(() => true);

  readonly labeledTranscript = computed(() => {
    const segs = this.segments();
    if (segs.length === 0) return this.transcript();
    return segs.map(s => `[${s.speaker}]: ${s.text}`).join('\n');
  });

  ngOnDestroy(): void {
    this.stop();
  }

  /**
   * Pre-flight check: verifies the transcription service is configured and reachable.
   * Returns true if available, false otherwise (and sets the error signal).
   */
  async checkAvailability(): Promise<boolean> {
    try {
      const response = await firstValueFrom(
        this.http.get<{ available: boolean }>('/api/transcription/status')
      );
      if (!response.available) {
        this.error.set('Transcription service is not configured. Please contact your administrator.');
        return false;
      }
      return true;
    } catch (err) {
      if (err instanceof HttpErrorResponse && (err.status === 401 || err.status === 403)) {
        this.error.set('Session expired. Please refresh the page and try again.');
      } else {
        this.error.set('Transcription service is unreachable. Please check your connection and try again.');
      }
      return false;
    }
  }

  start(channelCount = 1, userName = 'You', mimeType = ''): void {
    this.transcript.set('');
    this.segments.set([]);
    this.interimText.set('');
    this.interimSpeaker.set('');
    this.error.set(null);
    this.channels = channelCount;
    this.localUserName = userName;
    this.encoding = mimeType;
    this.intentionallyStopped = false;
    this.hasEverConnected = false;
    this.reconnectAttempts = 0;
    this.isReconnecting.set(false);
    this.pendingAudioChunks = [];
    this.droppedAudioChunks = 0;
    if (this.reconnectTimeoutId !== null) {
      clearTimeout(this.reconnectTimeoutId);
      this.reconnectTimeoutId = null;
    }

    this.connectWebSocket();
  }

  private buildWsUrl(): string {
    const protocol = location.protocol === 'https:' ? 'wss:' : 'ws:';
    let wsUrl = `${protocol}//${location.host}/api/transcription/stream`;

    const params = new URLSearchParams();

    // In dev mode, WebSocket can't send custom headers, so pass mock auth via query param
    if (isDevMode()) {
      const mockHeader = this.mockAuth.getMockHeader();
      if (mockHeader) {
        params.set('mockAuth', mockHeader);
      }
    }

    if (this.channels > 1) {
      params.set('channels', String(this.channels));
    }

    if (this.encoding) {
      params.set('mimeType', this.encoding);
    }

    const qs = params.toString();
    if (qs) {
      wsUrl += `?${qs}`;
    }

    return wsUrl;
  }

  private connectWebSocket(): void {
    // Clean up any existing WebSocket before creating a new one
    if (this.ws) {
      this.ws.onopen = null;
      this.ws.onmessage = null;
      this.ws.onerror = null;
      this.ws.onclose = null;
      if (this.ws.readyState === WebSocket.OPEN || this.ws.readyState === WebSocket.CONNECTING) {
        this.ws.close();
      }
      this.ws = null;
    }

    const wsUrl = this.buildWsUrl();

    this.ws = new WebSocket(wsUrl);
    this.ws.binaryType = 'arraybuffer';

    this.ws.onopen = () => {
      this.isListening.set(true);
      this.error.set(null);
      this.hasEverConnected = true;
      this.reconnectTimeoutId = null;
      this.droppedAudioChunks = 0;

      // If reconnecting, flush buffered audio and reset state
      if (this.isReconnecting()) {
        this.flushPendingAudio();
        this.isReconnecting.set(false);
        this.reconnectAttempts = 0;
      }
    };

    this.ws.onmessage = (event: MessageEvent) => {
      try {
        const data = JSON.parse(event.data);
        this.handleDeepgramResult(data);
      } catch {
        // Ignore non-JSON messages (metadata, etc.)
      }
    };

    this.ws.onerror = () => {
      this.isListening.set(false);
      if (!this.intentionallyStopped) {
        this.attemptReconnect();
      }
    };

    this.ws.onclose = (event: CloseEvent) => {
      this.isListening.set(false);
      if (event.code !== 1000 && !this.intentionallyStopped) {
        this.attemptReconnect(event.reason);
      }
    };
  }

  private attemptReconnect(closeReason?: string): void {
    if (this.intentionallyStopped) return;
    // Guard against duplicate calls (onerror + onclose can both fire for the same failure)
    if (this.isReconnecting() && this.reconnectTimeoutId !== null) return;

    // Fail immediately if the connection was never successfully established.
    // No point retrying with exponential backoff if the initial connection failed.
    if (!this.hasEverConnected) {
      this.isReconnecting.set(false);
      this.pendingAudioChunks = [];
      const reason = closeReason
        ? `Transcription service unavailable: ${closeReason}`
        : 'Could not connect to transcription service. Please try again.';
      this.error.set(reason);
      return;
    }

    if (this.reconnectAttempts >= DeepgramTranscriptionService.MAX_RECONNECT_ATTEMPTS) {
      this.isReconnecting.set(false);
      this.pendingAudioChunks = [];
      this.error.set('Transcription connection lost after multiple retries.');
      return;
    }

    this.isReconnecting.set(true);
    this.reconnectAttempts++;

    const delay = Math.min(
      DeepgramTranscriptionService.INITIAL_RECONNECT_DELAY_MS * Math.pow(2, this.reconnectAttempts - 1),
      DeepgramTranscriptionService.MAX_RECONNECT_DELAY_MS,
    );

    this.reconnectTimeoutId = setTimeout(() => {
      this.reconnectTimeoutId = null;
      if (!this.intentionallyStopped) {
        this.connectWebSocket();
      }
    }, delay);
  }

  private flushPendingAudio(): void {
    for (const chunk of this.pendingAudioChunks) {
      if (this.ws?.readyState === WebSocket.OPEN) {
        this.ws.send(chunk);
      }
    }
    this.pendingAudioChunks = [];
  }

  private handleDeepgramResult(data: Record<string, unknown>): void {
    if (data['type'] !== 'Results') return;

    const isFinal = data['is_final'] as boolean;

    if (this.channels > 1) {
      this.handleMultichannelResult(data, isFinal);
    } else {
      this.handleSingleChannelResult(data, isFinal);
    }
  }

  private handleSingleChannelResult(data: Record<string, unknown>, isFinal: boolean): void {
    const channel = data['channel'] as Record<string, unknown> | undefined;
    const alt = (channel?.['alternatives'] as Record<string, unknown>[])?.[0];
    if (!alt) return;

    const transcriptText = (alt['transcript'] as string)?.trim();
    if (!transcriptText) {
      if (!isFinal) this.interimText.set('');
      return;
    }

    // Extract speaker from word-level diarization
    const words = alt['words'] as Array<Record<string, unknown>> | undefined;
    const speaker = this.getSpeakerFromWords(words);

    if (isFinal) {
      this.transcript.update(prev => {
        const separator = prev ? ' ' : '';
        return prev + separator + transcriptText;
      });
      this.segments.update(prev => [...prev, { speaker, text: transcriptText }]);
      this.interimText.set('');
      this.interimSpeaker.set('');
    } else {
      this.interimText.set(transcriptText);
      this.interimSpeaker.set(speaker);
    }
  }

  private handleMultichannelResult(data: Record<string, unknown>, isFinal: boolean): void {
    const channelObj = data['channel'] as Record<string, unknown> | undefined;
    const channelIndex = (data['channel_index'] as number[])?.[0] ?? 0;
    const alt = (channelObj?.['alternatives'] as Record<string, unknown>[])?.[0];
    if (!alt) return;

    const transcriptText = (alt['transcript'] as string)?.trim();
    if (!transcriptText) {
      if (!isFinal) this.interimText.set('');
      return;
    }

    let speaker: string;
    if (channelIndex === 0) {
      // Channel 0 = mic = local user
      speaker = this.localUserName;
    } else {
      // Channel 1 = system = remote participants, use diarization to distinguish
      const words = alt['words'] as Array<Record<string, unknown>> | undefined;
      const speakerNum = this.getSpeakerNumberFromWords(words);
      speaker = speakerNum !== null ? `Participant ${speakerNum}` : 'Remote';
    }

    if (isFinal) {
      this.transcript.update(prev => {
        const separator = prev ? ' ' : '';
        return prev + separator + transcriptText;
      });
      this.segments.update(prev => [...prev, { speaker, text: transcriptText }]);
      this.interimText.set('');
      this.interimSpeaker.set('');
    } else {
      this.interimText.set(transcriptText);
      this.interimSpeaker.set(speaker);
    }
  }

  private getSpeakerFromWords(words: Array<Record<string, unknown>> | undefined): string {
    if (!words || words.length === 0) return 'Speaker';
    const speakerNum = words[0]?.['speaker'] as number | undefined;
    if (speakerNum === undefined || speakerNum === null) return 'Speaker';
    return `Speaker ${speakerNum}`;
  }

  private getSpeakerNumberFromWords(words: Array<Record<string, unknown>> | undefined): number | null {
    if (!words || words.length === 0) return null;
    const speakerNum = words[0]?.['speaker'] as number | undefined;
    return speakerNum ?? null;
  }

  sendAudio(blob: Blob): void {
    if (this.ws?.readyState === WebSocket.OPEN) {
      this.droppedAudioChunks = 0;
      blob.arrayBuffer().then(buffer => {
        this.ws?.send(buffer);
      }).catch(() => {
        // Blob may have been invalidated (e.g. tab backgrounded). Non-fatal — skip this chunk.
      });
    } else if (this.isReconnecting()) {
      // Buffer audio during reconnect so it can be flushed when the connection is restored
      blob.arrayBuffer().then(buffer => {
        // Re-check: connection may have been restored while arrayBuffer() resolved
        if (!this.isReconnecting() && this.ws?.readyState === WebSocket.OPEN) {
          this.ws.send(buffer);
          return;
        }
        if (this.pendingAudioChunks.length < DeepgramTranscriptionService.MAX_PENDING_CHUNKS) {
          this.pendingAudioChunks.push(buffer);
        }
      }).catch(() => {
        // Non-fatal
      });
    } else if (!this.intentionallyStopped) {
      // WebSocket is not open and not reconnecting — audio is being silently dropped.
      // Surface an error after a threshold of dropped chunks so the user knows.
      this.droppedAudioChunks++;
      if (this.droppedAudioChunks >= DeepgramTranscriptionService.MAX_DROPPED_CHUNKS_BEFORE_ERROR && !this.error()) {
        this.error.set('Transcription connection lost. Audio is not being transcribed.');
      }
    }
  }

  stop(): void {
    this.intentionallyStopped = true;
    if (this.reconnectTimeoutId !== null) {
      clearTimeout(this.reconnectTimeoutId);
      this.reconnectTimeoutId = null;
    }
    this.isReconnecting.set(false);
    this.pendingAudioChunks = [];
    this.reconnectAttempts = 0;

    if (this.ws) {
      if (this.ws.readyState === WebSocket.OPEN || this.ws.readyState === WebSocket.CONNECTING) {
        this.ws.close(1000, 'Recording stopped');
      }
      this.ws = null;
    }
    this.interimText.set('');
    this.interimSpeaker.set('');
    this.isListening.set(false);
  }

  reset(): void {
    this.stop();
    this.transcript.set('');
    this.segments.set([]);
    this.error.set(null);
  }
}
