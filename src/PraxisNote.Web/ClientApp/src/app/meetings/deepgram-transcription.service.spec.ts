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
    vi.useRealTimers();
    vi.unstubAllGlobals();
  });

  describe('sendAudio', () => {
    it('sendAudio_WebSocketClosesDuringConversion_BuffersAudioChunks', async () => {
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

      // Wait for the blob.arrayBuffer() promise to resolve via microtask flush
      await new Promise(resolve => setTimeout(resolve, 50));

      // Audio should be in pending buffer (not sent to WS since it was closed)
      expect(service['pendingAudioChunks'].length).toBeGreaterThan(0);
    });
  });

  describe('flushPendingAudio', () => {
    it('flushPendingAudio_MoreThan10Chunks_SendsAllChunks', () => {
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

      // The flush throttles sends via setTimeout, so only the first chunk
      // is sent synchronously. Verify all chunks are flushed (no truncation).
      expect(service['pendingAudioChunks'].length).toBe(0); // buffer was drained
      const sentSizes = ws2.sentData.map(d => (d as ArrayBuffer).byteLength);
      expect(sentSizes.length).toBeGreaterThanOrEqual(1);
      expect(sentSizes[0]).toBe(1); // oldest chunk (no truncation)
    });
  });

  describe('isReconnecting', () => {
    it('isReconnecting_OnError_SetsTrueImmediately', () => {
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

  describe('reconnect', () => {
    it('reconnect_After20Attempts_StopsAndSetsError', () => {
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
    });
  });

  describe('speaker diarization routing', () => {
    it('singleChannel_MicOnly_RoutesToSingleChannelHandler', () => {
      // Start with 1 channel (mic-only mode)
      service.start(1, 'TestUser');
      const ws = MockWebSocket.instances[0];
      ws.simulateOpen();

      // Send a single-channel diarized result (no channel_index)
      ws.simulateMessage(JSON.stringify({
        type: 'Results',
        is_final: true,
        channel: {
          alternatives: [{
            transcript: 'Hello everyone',
            words: [{ word: 'Hello', speaker: 0 }, { word: 'everyone', speaker: 0 }],
          }],
        },
      }));

      const segments = service.segments();
      expect(segments.length).toBe(1);
      expect(segments[0].speaker).toBe('Speaker 0');
      expect(segments[0].text).toBe('Hello everyone');
    });

    it('multichannelRequestedButFellBack_SessionConfigOverrides_RoutesToSingleChannel', () => {
      // Start with 2 channels (system audio mode) — but backend will fall back
      service.start(2, 'LocalUser');
      const ws = MockWebSocket.instances[0];
      ws.simulateOpen();

      // Backend sends SessionConfig indicating single-channel fallback
      ws.simulateMessage(JSON.stringify({
        type: 'SessionConfig',
        multichannel: false,
        diarize: true,
      }));

      // Send a single-channel diarized result (no channel_index)
      ws.simulateMessage(JSON.stringify({
        type: 'Results',
        is_final: true,
        channel: {
          alternatives: [{
            transcript: 'Hello from remote',
            words: [{ word: 'Hello', speaker: 1 }, { word: 'from', speaker: 1 }, { word: 'remote', speaker: 1 }],
          }],
        },
      }));

      const segments = service.segments();
      expect(segments.length).toBe(1);
      // Should be labeled as Speaker 1 (from diarization), NOT LocalUser
      expect(segments[0].speaker).toBe('Speaker 1');
      expect(segments[0].text).toBe('Hello from remote');
    });

    it('multichannelRequestedButFellBack_NoSessionConfig_SafetyNetAutoCorrects', () => {
      // Start with 2 channels but no SessionConfig received
      service.start(2, 'LocalUser');
      const ws = MockWebSocket.instances[0];
      ws.simulateOpen();

      // Do NOT send SessionConfig — simulate the case where the server doesn't send one

      // Send a result without channel_index (Deepgram single-channel diarized)
      ws.simulateMessage(JSON.stringify({
        type: 'Results',
        is_final: true,
        channel: {
          alternatives: [{
            transcript: 'Testing safety net',
            words: [{ word: 'Testing', speaker: 2 }, { word: 'safety', speaker: 2 }, { word: 'net', speaker: 2 }],
          }],
        },
      }));

      const segments = service.segments();
      expect(segments.length).toBe(1);
      // Safety net should auto-correct to single-channel: speaker from diarization
      expect(segments[0].speaker).toBe('Speaker 2');
      expect(segments[0].text).toBe('Testing safety net');

      // Verify actualMultichannel was auto-corrected
      expect(service['actualMultichannel']).toBe(false);
    });

    it('trueMultichannel_WithChannelIndex_RoutesToMultichannelHandler', () => {
      // Start with 2 channels — true multichannel (raw audio)
      service.start(2, 'LocalUser');
      const ws = MockWebSocket.instances[0];
      ws.simulateOpen();

      // Backend confirms multichannel mode
      ws.simulateMessage(JSON.stringify({
        type: 'SessionConfig',
        multichannel: true,
        diarize: true,
      }));

      // Send a multichannel result with channel_index for channel 0 (mic/local)
      ws.simulateMessage(JSON.stringify({
        type: 'Results',
        is_final: true,
        channel_index: [0, 2],
        channel: {
          alternatives: [{
            transcript: 'Hello from mic',
            words: [{ word: 'Hello', speaker: 0 }, { word: 'from', speaker: 0 }, { word: 'mic', speaker: 0 }],
          }],
        },
      }));

      const segments = service.segments();
      expect(segments.length).toBe(1);
      // Channel 0 = local user
      expect(segments[0].speaker).toBe('LocalUser');
      expect(segments[0].text).toBe('Hello from mic');
    });

    it('trueMultichannel_Channel1_LabelsAsParticipant', () => {
      // Start with 2 channels — true multichannel
      service.start(2, 'LocalUser');
      const ws = MockWebSocket.instances[0];
      ws.simulateOpen();

      // Backend confirms multichannel mode
      ws.simulateMessage(JSON.stringify({
        type: 'SessionConfig',
        multichannel: true,
        diarize: true,
      }));

      // Send a multichannel result for channel 1 (system audio/remote)
      ws.simulateMessage(JSON.stringify({
        type: 'Results',
        is_final: true,
        channel_index: [1, 2],
        channel: {
          alternatives: [{
            transcript: 'Hello from remote',
            words: [{ word: 'Hello', speaker: 0 }, { word: 'from', speaker: 0 }, { word: 'remote', speaker: 0 }],
          }],
        },
      }));

      const segments = service.segments();
      expect(segments.length).toBe(1);
      // Channel 1 = remote participant
      expect(segments[0].speaker).toBe('Participant 0');
      expect(segments[0].text).toBe('Hello from remote');
    });

    it('sessionConfigResetsOnNewStart', () => {
      // Start with multichannel
      service.start(2, 'User1');
      const ws1 = MockWebSocket.instances[0];
      ws1.simulateOpen();

      // Receive SessionConfig
      ws1.simulateMessage(JSON.stringify({
        type: 'SessionConfig',
        multichannel: false,
        diarize: true,
      }));

      expect(service['actualMultichannel']).toBe(false);

      // Start a new session — actualMultichannel should reset
      service.start(1, 'User2');
      expect(service['actualMultichannel']).toBeNull();
    });
  });

  describe('sendAudio dropped chunks', () => {
    it('sendAudio_NotConnectedAfter10Drops_SetsConnectionLostError', () => {
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
