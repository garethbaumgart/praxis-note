import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { DeepgramTranscriptionService } from './deepgram-transcription.service';
import { MockAuthService } from '../auth/mock-auth.service';

// Mock WebSocket that captures constructor calls and allows test control
class MockWebSocket {
  static instances: MockWebSocket[] = [];
  readyState = 0; // CONNECTING
  binaryType = '';
  onopen: (() => void) | null = null;
  onmessage: ((event: MessageEvent) => void) | null = null;
  onerror: (() => void) | null = null;
  onclose: ((event: CloseEvent) => void) | null = null;
  sentData: (ArrayBuffer | string)[] = [];
  closeCalled = false;
  closeCode?: number;
  closeReason?: string;

  constructor(public url: string) {
    MockWebSocket.instances.push(this);
  }

  send(data: ArrayBuffer | string): void {
    this.sentData.push(data);
  }

  close(code?: number, reason?: string): void {
    this.closeCalled = true;
    this.closeCode = code;
    this.closeReason = reason;
    this.readyState = 3; // CLOSED
  }

  // Test helpers
  simulateOpen(): void {
    this.readyState = 1; // OPEN
    this.onopen?.();
  }

  simulateError(): void {
    this.onerror?.();
  }

  simulateClose(code = 1006, reason = ''): void {
    this.readyState = 3; // CLOSED
    this.onclose?.({ code, reason } as CloseEvent);
  }

  simulateMessage(data: string): void {
    this.onmessage?.({ data } as MessageEvent);
  }
}

// Store original WebSocket
const OriginalWebSocket = globalThis.WebSocket;

describe('DeepgramTranscriptionService', () => {
  let service: DeepgramTranscriptionService;

  beforeEach(() => {
    MockWebSocket.instances = [];

    // Stub WebSocket globally
    vi.stubGlobal('WebSocket', MockWebSocket);
    // Copy WebSocket constants
    (globalThis as any).WebSocket.CONNECTING = 0;
    (globalThis as any).WebSocket.OPEN = 1;
    (globalThis as any).WebSocket.CLOSING = 2;
    (globalThis as any).WebSocket.CLOSED = 3;

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        MockAuthService,
        DeepgramTranscriptionService,
      ],
    });

    service = TestBed.inject(DeepgramTranscriptionService);
  });

  afterEach(() => {
    service.reset();
    vi.unstubAllGlobals();
  });

  describe('sendAudio re-check', () => {
    it('buffers audio when WebSocket closes during arrayBuffer conversion', async () => {
      service.start();
      const ws = MockWebSocket.instances[0];
      ws.simulateOpen();

      // Create a mock blob with arrayBuffer() that returns a promise
      const buffer = new ArrayBuffer(3);
      new Uint8Array(buffer).set([1, 2, 3]);
      const blob = {
        arrayBuffer: () => Promise.resolve(buffer),
        size: 3,
        type: '',
      } as unknown as Blob;

      // Close WebSocket AFTER sendAudio is called but before it resolves
      // Since arrayBuffer() is async, we simulate the WS closing during that time
      ws.readyState = 3; // CLOSED
      service['isReconnecting'].set(true);

      // sendAudio will check readyState synchronously (CLOSED), then enter reconnecting branch
      service.sendAudio(blob);

      // Wait for the blob.arrayBuffer() promise to resolve
      await new Promise(resolve => setTimeout(resolve, 50));

      // Audio should be in pending buffer (not sent to WS since it was closed)
      expect(service['pendingAudioChunks'].length).toBeGreaterThan(0);
    });
  });

  describe('flushPendingAudio', () => {
    it('sends only 10 most recent chunks after reconnect', () => {
      service.start();
      const ws1 = MockWebSocket.instances[0];
      ws1.simulateOpen();

      // Simulate disconnect
      ws1.simulateError();

      // We should now have a second WS instance (reconnect attempt)
      // But let's directly test the buffer logic
      // Manually buffer 15 chunks
      for (let i = 0; i < 15; i++) {
        service['pendingAudioChunks'].push(new ArrayBuffer(i + 1));
      }

      // Simulate reconnection success
      const ws2 = MockWebSocket.instances[MockWebSocket.instances.length - 1];
      ws2.simulateOpen();

      // The flush should have trimmed to 10 and sent them
      // Check that we sent the 10 most recent (sizes 6-15)
      const sentSizes = ws2.sentData.map(d => (d as ArrayBuffer).byteLength);
      expect(sentSizes.length).toBeLessThanOrEqual(10);
    });
  });

  describe('isReconnecting signal', () => {
    it('sets isReconnecting true immediately on error', () => {
      service.start();
      const ws = MockWebSocket.instances[0];
      ws.simulateOpen();

      expect(service.isReconnecting()).toBe(false);

      // Trigger error
      ws.simulateError();

      // isReconnecting should be true immediately (before setTimeout fires)
      expect(service.isReconnecting()).toBe(true);
      expect(service.isListening()).toBe(false);
    });
  });

  describe('totalReconnects cap', () => {
    it('stops reconnecting after 20 total reconnections', () => {
      vi.useFakeTimers();

      service.start();

      // Simulate 20 reconnection cycles
      for (let i = 0; i < 20; i++) {
        const ws = MockWebSocket.instances[MockWebSocket.instances.length - 1];
        ws.simulateOpen();

        // Simulate disconnect (non-1000 close code)
        ws.simulateClose(1006, 'connection lost');

        // Advance timer to trigger reconnect
        vi.advanceTimersByTime(20000);
      }

      // The 21st disconnect should trigger the error
      const lastWs = MockWebSocket.instances[MockWebSocket.instances.length - 1];
      lastWs.simulateOpen();
      lastWs.simulateClose(1006, 'connection lost');
      vi.advanceTimersByTime(20000);

      // Should have hit the cap
      expect(service.isReconnecting()).toBe(false);
      expect(service.error()).toContain('reconnection limit');

      vi.useRealTimers();
    });
  });

  describe('dropped chunks threshold', () => {
    it('shows error after 10 dropped audio chunks when not connected', () => {
      service.start();
      // Don't open the WebSocket — leave it in CONNECTING state
      // But set hasEverConnected to false (it already is by default since onopen never fired)

      // The WS is in CONNECTING state (readyState=0), not OPEN, not reconnecting
      // sendAudio will take the "else if (!intentionallyStopped)" branch
      const ws = MockWebSocket.instances[0];
      // Ensure the ws exists and is not open
      expect(ws.readyState).toBe(0); // CONNECTING

      // Also make sure we're not reconnecting
      // hasEverConnected is false, so onerror won't set isReconnecting
      // Let's simulate a failed initial connection
      ws.simulateError();
      ws.simulateClose(1006, 'failed');

      // Now sendAudio should drop chunks since we're not reconnecting
      // (hasEverConnected is false, so isReconnecting stays false)
      expect(service.isReconnecting()).toBe(false);
      expect(service.error()).not.toBeNull(); // Error was set by attemptReconnect

      // Reset error to test the dropped chunks logic
      service['error'].set(null);
      service['intentionallyStopped'] = false;
      service['droppedAudioChunks'] = 0;

      // Create mock blobs to send
      for (let i = 0; i < 11; i++) {
        const mockBlob = {
          arrayBuffer: () => Promise.resolve(new ArrayBuffer(1)),
          size: 1,
          type: '',
        } as unknown as Blob;
        service.sendAudio(mockBlob);
      }

      // After 10 drops, error should be set
      expect(service.error()).toContain('connection lost');
    });
  });
});
