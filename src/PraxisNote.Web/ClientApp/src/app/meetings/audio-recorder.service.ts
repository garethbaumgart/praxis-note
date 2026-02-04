import { Injectable, signal, computed, OnDestroy } from '@angular/core';

export type RecordingState = 'idle' | 'recording' | 'paused';
export type AudioCaptureMode = 'microphone' | 'both';
// Note: 'system' mode (system audio only, no mic) was considered but isn't exposed
// since online meetings always need the user's voice too.

@Injectable({ providedIn: 'root' })
export class AudioRecorderService implements OnDestroy {
  private mediaRecorder: MediaRecorder | null = null;
  private micStream: MediaStream | null = null;
  private systemStream: MediaStream | null = null;
  private mixedStream: MediaStream | null = null;
  private audioContext: AudioContext | null = null;
  private mixingContext: AudioContext | null = null; // Separate context for mixing
  private analyserNode: AnalyserNode | null = null;
  private chunks: Blob[] = [];
  private timerInterval: ReturnType<typeof setInterval> | null = null;
  private levelAnimationId: number | null = null;
  private isStarting = false;
  private startToken = 0;
  private recoveryAttempts = 0;
  private static readonly MAX_RECOVERY_ATTEMPTS = 5;
  private static readonly RECOVERY_ATTEMPT_WINDOW_MS = 60_000;
  private lastRecoveryTime = 0;
  private isRecovering = false;
  private contextKeepAliveInterval: ReturnType<typeof setInterval> | null = null;

  readonly state = signal<RecordingState>('idle');
  readonly elapsedSeconds = signal(0);
  readonly error = signal<string | null>(null);
  readonly audioLevels = signal<number[]>(new Array(16).fill(0));
  readonly captureMode = signal<AudioCaptureMode>('microphone');

  readonly onAudioChunk = signal<((blob: Blob) => void) | null>(null);

  readonly isRecording = computed(() => this.state() === 'recording');
  readonly isPaused = computed(() => this.state() === 'paused');
  readonly isActive = computed(() => this.state() !== 'idle');
  readonly hasSystemAudio = computed(() => this.captureMode() === 'both');

  readonly formattedTime = computed(() => {
    const total = this.elapsedSeconds();
    const minutes = Math.floor(total / 60);
    const seconds = total % 60;
    return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
  });

  ngOnDestroy(): void {
    this.discard();
  }

  /**
   * Start recording with microphone only (original behavior).
   * Use this for in-person meetings or when system audio is not needed.
   */
  async start(): Promise<void> {
    return this.startRecording('microphone');
  }

  /**
   * Start recording with system audio capture (for online meetings).
   * This will prompt the user to share a browser tab to capture participant audio.
   * Also captures microphone for the user's own voice.
   */
  async startWithSystemAudio(): Promise<void> {
    return this.startRecording('both');
  }

  private async startRecording(mode: AudioCaptureMode): Promise<void> {
    if (this.state() !== 'idle' || this.isStarting) return;
    this.isStarting = true;
    const token = ++this.startToken;

    this.error.set(null);
    this.captureMode.set(mode);
    this.recoveryAttempts = 0;
    this.lastRecoveryTime = 0;

    try {
      // Always get microphone for user's voice
      this.micStream = await this.getMicrophoneStream();

      // If cancelled while waiting
      if (token !== this.startToken) {
        this.releaseStream(this.micStream);
        this.micStream = null;
        this.isStarting = false;
        return;
      }

      // Get system audio if requested
      if (mode === 'both') {
        try {
          this.systemStream = await this.getSystemAudioStream();

          // If cancelled while waiting
          if (token !== this.startToken) {
            this.releaseStream(this.micStream);
            this.releaseStream(this.systemStream);
            this.micStream = null;
            this.systemStream = null;
            this.isStarting = false;
            return;
          }
        } catch (err) {
          // If user cancels tab sharing, fall back to mic-only
          console.warn('System audio capture cancelled, falling back to microphone only:', err);
          this.captureMode.set('microphone');
          // Continue with mic only
        }
      }

      // Create the recording stream (mixed or mic-only)
      const recordingStream = this.createRecordingStream();
      if (!recordingStream) {
        throw new Error('No audio stream available');
      }

      // Set up Web Audio API for level metering
      this.audioContext = new AudioContext();
      if (this.audioContext.state === 'suspended') {
        await this.audioContext.resume();
      }
      const source = this.audioContext.createMediaStreamSource(recordingStream);
      this.analyserNode = this.audioContext.createAnalyser();
      this.analyserNode.fftSize = 64;
      source.connect(this.analyserNode);

      // Determine best supported mime type
      const mimeType = this.getSupportedMimeType();
      this.chunks = [];

      this.initMediaRecorder(recordingStream, mimeType);
      this.monitorAudioTracks();

      // Collect data every 1 second for chunked access
      this.mediaRecorder!.start(1000);
      this.state.set('recording');
      this.elapsedSeconds.set(0);
      this.startTimer();
      this.startLevelMetering();
      this.startContextKeepAlive();
    } catch (err) {
      this.cleanup();
      if (err instanceof DOMException && err.name === 'NotAllowedError') {
        this.error.set('Microphone access denied. Please allow microphone permissions and try again.');
      } else if (err instanceof DOMException) {
        this.error.set(err.message);
      } else if (err instanceof Error) {
        this.error.set(err.message);
      } else {
        this.error.set('Failed to start recording. Please check your audio settings.');
      }
    } finally {
      this.isStarting = false;
    }
  }

  private initMediaRecorder(stream: MediaStream, mimeType: string | undefined): void {
    this.mediaRecorder = new MediaRecorder(
      stream,
      mimeType ? { mimeType } : undefined
    );

    this.mediaRecorder.ondataavailable = (e) => {
      if (e.data.size > 0) {
        this.chunks.push(e.data);
        this.onAudioChunk()?.(e.data);
      }
    };

    this.mediaRecorder.onerror = (e) => {
      console.error('MediaRecorder error:', e);
      this.attemptRecovery('MediaRecorder error');
    };
  }

  private monitorAudioTracks(): void {
    // Handle case where user stops sharing the tab
    if (this.systemStream) {
      const videoTrack = this.systemStream.getVideoTracks()[0];
      if (videoTrack) {
        videoTrack.onended = () => {
          // Tab sharing stopped - update mode indicator
          // Recording continues with the mixed stream that was created,
          // but system audio track will be silent
          console.log('Tab sharing stopped, system audio will be silent');
          this.captureMode.set('microphone');
        };
      }
    }

    // Monitor mic audio track for unexpected end (e.g. device disconnected)
    if (this.micStream) {
      for (const track of this.micStream.getAudioTracks()) {
        track.onended = () => {
          console.warn('Microphone audio track ended unexpectedly');
          if (this.state() !== 'idle') {
            this.attemptRecovery('microphone track ended');
          }
        };
      }
    }

    // Monitor system audio track for unexpected end
    if (this.systemStream) {
      for (const track of this.systemStream.getAudioTracks()) {
        track.onended = () => {
          console.warn('System audio track ended unexpectedly');
          if (this.state() !== 'idle') {
            // System audio lost — fall back to mic-only rather than failing
            this.captureMode.set('microphone');
            this.attemptRecovery('system audio track ended');
          }
        };
      }
    }
  }

  private attemptRecovery(reason: string): void {
    if (this.isRecovering || this.state() === 'idle') return;
    this.isRecovering = true;

    const now = Date.now();
    // Reset the attempt counter if it's been long enough since the last recovery
    if (now - this.lastRecoveryTime > AudioRecorderService.RECOVERY_ATTEMPT_WINDOW_MS) {
      this.recoveryAttempts = 0;
    }

    this.recoveryAttempts++;
    this.lastRecoveryTime = now;

    if (this.recoveryAttempts > AudioRecorderService.MAX_RECOVERY_ATTEMPTS) {
      console.error(`Recording recovery failed after ${AudioRecorderService.MAX_RECOVERY_ATTEMPTS} attempts, giving up`);
      this.error.set('Recording failed repeatedly and could not recover. Please start a new recording.');
      this.isRecovering = false;
      this.cleanup();
      return;
    }

    console.warn(`Attempting recording recovery (attempt ${this.recoveryAttempts}/${AudioRecorderService.MAX_RECOVERY_ATTEMPTS}), reason: ${reason}`);

    this.recoverRecording().then(recovered => {
      this.isRecovering = false;
      if (!recovered) {
        // If recovery failed, try again after a short delay
        // (the mic stream re-acquisition might succeed on retry)
        setTimeout(() => {
          if (this.state() !== 'idle') {
            this.attemptRecovery('retry after failed recovery');
          }
        }, 2000);
      }
    }).catch(err => {
      console.error('Unexpected error during recording recovery:', err);
      this.isRecovering = false;
    });
  }

  private async recoverRecording(): Promise<boolean> {
    const wasPaused = this.state() === 'paused';

    // Stop the current MediaRecorder if it's still active
    if (this.mediaRecorder && this.mediaRecorder.state !== 'inactive') {
      try {
        this.mediaRecorder.onerror = null;
        this.mediaRecorder.ondataavailable = null;
        this.mediaRecorder.stop();
      } catch {
        // Already stopped or in bad state — that's fine
      }
    }
    this.mediaRecorder = null;

    // Check if mic stream is still alive; re-acquire if needed
    const micAlive = this.micStream?.getAudioTracks().some(t => t.readyState === 'live') ?? false;
    if (!micAlive) {
      // Release the dead stream
      this.releaseStream(this.micStream);
      this.micStream = null;

      try {
        this.micStream = await this.getMicrophoneStream();
      } catch (err) {
        console.error('Failed to re-acquire microphone during recovery:', err);
        return false;
      }
    }

    // Rebuild the mixed stream if we had system audio
    // (system audio can't be re-acquired without user gesture, so we just use mic if it's gone)
    const systemAlive = this.systemStream?.getAudioTracks().some(t => t.readyState === 'live') ?? false;
    if (!systemAlive && this.systemStream) {
      this.releaseStream(this.systemStream);
      this.systemStream = null;
      this.captureMode.set('microphone');
    }

    // Tear down old mixing context and rebuild
    if (this.mixingContext) {
      try { this.mixingContext.close(); } catch { /* ignore */ }
      this.mixingContext = null;
    }
    this.mixedStream = null;

    const recordingStream = this.createRecordingStream();
    if (!recordingStream) {
      console.error('No recording stream available during recovery');
      return false;
    }

    // Rebuild level metering on the new stream
    this.stopLevelMetering();
    if (this.audioContext) {
      try { this.audioContext.close(); } catch { /* ignore */ }
    }
    this.audioContext = new AudioContext();
    if (this.audioContext.state === 'suspended') {
      await this.audioContext.resume();
    }
    const source = this.audioContext.createMediaStreamSource(recordingStream);
    this.analyserNode = this.audioContext.createAnalyser();
    this.analyserNode.fftSize = 64;
    source.connect(this.analyserNode);

    // Create a new MediaRecorder on the fresh stream
    const mimeType = this.getSupportedMimeType();
    this.initMediaRecorder(recordingStream, mimeType);
    this.monitorAudioTracks();

    try {
      this.mediaRecorder!.start(1000);
    } catch (err) {
      console.error('Failed to start MediaRecorder during recovery:', err);
      return false;
    }

    // Restore the correct state
    if (wasPaused) {
      this.mediaRecorder!.pause();
      this.state.set('paused');
    } else {
      this.state.set('recording');
    }

    this.startLevelMetering();
    this.error.set(null);
    console.log('Recording recovered successfully');
    return true;
  }

  private async getMicrophoneStream(): Promise<MediaStream> {
    try {
      return await navigator.mediaDevices.getUserMedia({ audio: true });
    } catch (err) {
      if (err instanceof DOMException && err.name === 'NotAllowedError') {
        // Preserve DOMException type so outer catch can handle it consistently
        throw new DOMException(
          'Microphone access denied. Please allow microphone permissions and try again.',
          'NotAllowedError'
        );
      }
      // Wrap other errors as DOMException for consistent handling
      throw new DOMException(
        'Could not access microphone. Please check your audio settings.',
        err instanceof DOMException ? err.name : 'NotReadableError'
      );
    }
  }

  private async getSystemAudioStream(): Promise<MediaStream> {
    // getDisplayMedia captures the audio from a shared tab/screen
    // We request video too (required by most browsers) but only use the audio
    const audioConstraints: MediaTrackConstraints = {
      // Request high quality audio capture
      echoCancellation: false,
      noiseSuppression: false,
      autoGainControl: false,
    };

    const stream = await navigator.mediaDevices.getDisplayMedia({
      video: true, // Required, but we won't use it
      audio: audioConstraints,
    });

    // Check if audio track was actually captured
    const audioTracks = stream.getAudioTracks();
    if (audioTracks.length === 0) {
      // User may have shared a screen/window without audio
      // This happens if they didn't check "Share audio" or shared a window instead of tab
      this.releaseStream(stream);
      throw new Error('No audio captured. Please share a browser tab and enable "Share audio".');
    }

    return stream;
  }

  private createRecordingStream(): MediaStream | null {
    // If we only have mic, return it directly
    if (!this.systemStream) {
      this.mixedStream = this.micStream;
      return this.micStream;
    }

    // Need mic stream to mix
    if (!this.micStream) {
      return null;
    }

    // Mix both streams using Web Audio API
    this.mixingContext = new AudioContext();
    if (this.mixingContext.state === 'suspended') {
      // Resume the context in case the browser started it suspended due to autoplay policies
      this.mixingContext.resume().catch(() => {
        // Swallow errors to avoid breaking recording flow
      });
    }

    // Create sources for both streams
    const micSource = this.mixingContext.createMediaStreamSource(this.micStream);
    const systemSource = this.mixingContext.createMediaStreamSource(this.systemStream);

    // Create a destination to mix into
    const destination = this.mixingContext.createMediaStreamDestination();

    // Create gain nodes for volume control if needed
    const micGain = this.mixingContext.createGain();
    const systemGain = this.mixingContext.createGain();

    // Set gains (can be adjusted if one is too loud/quiet)
    micGain.gain.value = 1.0;
    systemGain.gain.value = 1.0;

    // Connect: source -> gain -> destination
    micSource.connect(micGain);
    systemSource.connect(systemGain);
    micGain.connect(destination);
    systemGain.connect(destination);

    this.mixedStream = destination.stream;
    return destination.stream;
  }

  private releaseStream(stream: MediaStream | null): void {
    if (stream) {
      for (const track of stream.getTracks()) {
        track.onended = null;
        track.stop();
      }
    }
  }

  pause(): void {
    if (this.mediaRecorder?.state === 'recording') {
      this.mediaRecorder.pause();
      this.state.set('paused');
      this.stopTimer();
    }
  }

  resume(): void {
    if (this.mediaRecorder?.state === 'paused') {
      this.mediaRecorder.resume();
      this.state.set('recording');
      this.startTimer();
    }
  }

  stop(): Promise<File | null> {
    return new Promise((resolve) => {
      if (!this.mediaRecorder || this.mediaRecorder.state === 'inactive') {
        const file = this.buildFileFromChunks();
        this.cleanup();
        resolve(file);
        return;
      }

      this.mediaRecorder.onstop = () => {
        const file = this.buildFileFromChunks();
        this.cleanup();
        resolve(file);
      };

      this.mediaRecorder.stop();
    });
  }

  private buildFileFromChunks(): File | null {
    if (this.chunks.length === 0) return null;
    const mimeType = this.mediaRecorder?.mimeType || 'audio/webm';
    const baseMime = mimeType.split(';')[0].toLowerCase();
    const extension = baseMime.includes('ogg') ? 'ogg' : baseMime.includes('mp4') ? 'mp4' : 'webm';
    const blob = new Blob(this.chunks, { type: mimeType });
    return new File([blob], `recording-${Date.now()}.${extension}`, { type: mimeType });
  }

  /** Discard the recording without producing a file */
  discard(): void {
    this.startToken++;
    if (this.mediaRecorder && this.mediaRecorder.state !== 'inactive') {
      // Override onstop to prevent file creation
      this.mediaRecorder.onstop = () => {};
      this.mediaRecorder.stop();
    }
    this.cleanup();
  }

  /** Check if system audio capture is supported by the browser */
  static isSystemAudioSupported(): boolean {
    return (
      typeof navigator !== 'undefined' &&
      typeof navigator.mediaDevices !== 'undefined' &&
      typeof navigator.mediaDevices.getDisplayMedia === 'function'
    );
  }

  private getSupportedMimeType(): string | undefined {
    const types = [
      'audio/webm;codecs=opus',
      'audio/webm',
      'audio/mp4',
      'audio/ogg;codecs=opus',
    ];
    for (const type of types) {
      if (MediaRecorder.isTypeSupported(type)) {
        return type;
      }
    }
    return undefined;
  }

  private startTimer(): void {
    this.stopTimer();
    this.timerInterval = setInterval(() => {
      this.elapsedSeconds.update((s) => s + 1);
    }, 1000);
  }

  private stopTimer(): void {
    if (this.timerInterval !== null) {
      clearInterval(this.timerInterval);
      this.timerInterval = null;
    }
  }

  private startLevelMetering(): void {
    if (!this.analyserNode) return;

    const bufferLength = this.analyserNode.frequencyBinCount;
    const dataArray = new Uint8Array(bufferLength);
    const barCount = 16;

    const update = () => {
      if (!this.analyserNode || this.state() === 'idle') return;

      this.analyserNode.getByteFrequencyData(dataArray);

      const levels: number[] = [];
      const binsPerBar = Math.floor(bufferLength / barCount);

      for (let i = 0; i < barCount; i++) {
        let sum = 0;
        for (let j = 0; j < binsPerBar; j++) {
          sum += dataArray[i * binsPerBar + j];
        }
        // Normalize to 0-1
        levels.push(sum / (binsPerBar * 255));
      }

      this.audioLevels.set(levels);
      this.levelAnimationId = requestAnimationFrame(update);
    };

    this.levelAnimationId = requestAnimationFrame(update);
  }

  private stopLevelMetering(): void {
    if (this.levelAnimationId !== null) {
      cancelAnimationFrame(this.levelAnimationId);
      this.levelAnimationId = null;
    }
  }

  private startContextKeepAlive(): void {
    this.stopContextKeepAlive();
    // Periodically check and resume AudioContexts that browsers may suspend
    // when the tab is backgrounded or after prolonged inactivity
    this.contextKeepAliveInterval = setInterval(() => {
      if (this.state() === 'idle') return;

      if (this.audioContext?.state === 'suspended') {
        this.audioContext.resume().catch(() => {});
      }
      if (this.mixingContext?.state === 'suspended') {
        this.mixingContext.resume().catch(() => {});
      }
    }, 5000);
  }

  private stopContextKeepAlive(): void {
    if (this.contextKeepAliveInterval !== null) {
      clearInterval(this.contextKeepAliveInterval);
      this.contextKeepAliveInterval = null;
    }
  }

  private cleanup(): void {
    this.stopTimer();
    this.stopLevelMetering();
    this.stopContextKeepAlive();

    this.releaseStream(this.micStream);
    this.releaseStream(this.systemStream);
    // Don't release mixedStream separately - it's derived from the others

    this.micStream = null;
    this.systemStream = null;
    this.mixedStream = null;

    if (this.audioContext) {
      this.audioContext.close();
      this.audioContext = null;
    }

    if (this.mixingContext) {
      this.mixingContext.close();
      this.mixingContext = null;
    }

    this.analyserNode = null;
    this.mediaRecorder = null;
    this.chunks = [];
    this.state.set('idle');
    this.elapsedSeconds.set(0);
    this.audioLevels.set(new Array(16).fill(0));
    this.captureMode.set('microphone');
    this.recoveryAttempts = 0;
    this.isRecovering = false;
  }
}
