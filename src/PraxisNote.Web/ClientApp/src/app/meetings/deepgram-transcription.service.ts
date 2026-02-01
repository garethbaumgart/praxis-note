import { Injectable, signal, computed, inject, OnDestroy, isDevMode } from '@angular/core';
import { MockAuthService } from '../auth/mock-auth.service';

@Injectable({ providedIn: 'root' })
export class DeepgramTranscriptionService implements OnDestroy {
  private readonly mockAuth = inject(MockAuthService);
  private ws: WebSocket | null = null;

  readonly transcript = signal('');
  readonly interimText = signal('');
  readonly isListening = signal(false);
  readonly error = signal<string | null>(null);

  readonly isSupported = computed(() => true);

  ngOnDestroy(): void {
    this.stop();
  }

  start(): void {
    this.transcript.set('');
    this.interimText.set('');
    this.error.set(null);

    const protocol = location.protocol === 'https:' ? 'wss:' : 'ws:';
    let wsUrl = `${protocol}//${location.host}/api/transcription/stream`;

    // In dev mode, WebSocket can't send custom headers, so pass mock auth via query param
    if (isDevMode()) {
      const mockHeader = this.mockAuth.getMockHeader();
      if (mockHeader) {
        wsUrl += `?mockAuth=${encodeURIComponent(mockHeader)}`;
      }
    }

    this.ws = new WebSocket(wsUrl);
    this.ws.binaryType = 'arraybuffer';

    this.ws.onopen = () => {
      this.isListening.set(true);
    };

    this.ws.onmessage = (event: MessageEvent) => {
      try {
        const data = JSON.parse(event.data);

        if (data.type === 'Results') {
          const alt = data.channel?.alternatives?.[0];
          if (!alt) return;

          if (data.is_final) {
            const text = alt.transcript?.trim();
            if (text) {
              this.transcript.update(prev => {
                const separator = prev ? ' ' : '';
                return prev + separator + text;
              });
            }
            this.interimText.set('');
          } else {
            this.interimText.set(alt.transcript ?? '');
          }
        }
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
    this.isListening.set(false);
  }

  reset(): void {
    this.stop();
    this.transcript.set('');
    this.error.set(null);
  }
}
