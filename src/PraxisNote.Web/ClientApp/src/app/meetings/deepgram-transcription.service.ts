import { Injectable, signal, computed, inject, OnDestroy, isDevMode } from '@angular/core';
import { MockAuthService } from '../auth/mock-auth.service';

export interface TranscriptSegment {
  speaker: string;
  text: string;
}

@Injectable({ providedIn: 'root' })
export class DeepgramTranscriptionService implements OnDestroy {
  private readonly mockAuth = inject(MockAuthService);
  private ws: WebSocket | null = null;
  private channels = 1;
  private localUserName = 'You';

  readonly transcript = signal('');
  readonly segments = signal<TranscriptSegment[]>([]);
  readonly interimText = signal('');
  readonly interimSpeaker = signal('');
  readonly isListening = signal(false);
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

  start(channelCount = 1, userName = 'You'): void {
    this.transcript.set('');
    this.segments.set([]);
    this.interimText.set('');
    this.interimSpeaker.set('');
    this.error.set(null);
    this.channels = channelCount;
    this.localUserName = userName;

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

    if (channelCount > 1) {
      params.set('channels', String(channelCount));
    }

    const qs = params.toString();
    if (qs) {
      wsUrl += `?${qs}`;
    }

    this.ws = new WebSocket(wsUrl);
    this.ws.binaryType = 'arraybuffer';

    this.ws.onopen = () => {
      this.isListening.set(true);
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
      this.error.set('Transcription connection error. Check your network.');
      this.isListening.set(false);
    };

    this.ws.onclose = (event: CloseEvent) => {
      this.isListening.set(false);
      if (event.code !== 1000 && !this.error()) {
        this.error.set('Transcription disconnected unexpectedly.');
      }
    };
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
      blob.arrayBuffer().then(buffer => {
        this.ws?.send(buffer);
      }).catch(() => {
        // Blob may have been invalidated (e.g. tab backgrounded). Non-fatal — skip this chunk.
      });
    }
  }

  stop(): void {
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
