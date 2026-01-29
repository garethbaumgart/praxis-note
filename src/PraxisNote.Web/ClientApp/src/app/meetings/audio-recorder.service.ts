import { Injectable, signal, computed, OnDestroy } from '@angular/core';

export type RecordingState = 'idle' | 'recording' | 'paused';

@Injectable({ providedIn: 'root' })
export class AudioRecorderService implements OnDestroy {
  private mediaRecorder: MediaRecorder | null = null;
  private audioStream: MediaStream | null = null;
  private audioContext: AudioContext | null = null;
  private analyserNode: AnalyserNode | null = null;
  private chunks: Blob[] = [];
  private timerInterval: ReturnType<typeof setInterval> | null = null;
  private levelAnimationId: number | null = null;
  private isStarting = false;
  private startToken = 0;

  readonly state = signal<RecordingState>('idle');
  readonly elapsedSeconds = signal(0);
  readonly error = signal<string | null>(null);
  readonly audioLevels = signal<number[]>(new Array(16).fill(0));

  readonly isRecording = computed(() => this.state() === 'recording');
  readonly isPaused = computed(() => this.state() === 'paused');
  readonly isActive = computed(() => this.state() !== 'idle');

  readonly formattedTime = computed(() => {
    const total = this.elapsedSeconds();
    const minutes = Math.floor(total / 60);
    const seconds = total % 60;
    return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
  });

  ngOnDestroy(): void {
    this.discard();
  }

  async start(): Promise<void> {
    if (this.state() !== 'idle' || this.isStarting) return;
    this.isStarting = true;
    const token = ++this.startToken;

    this.error.set(null);

    let stream: MediaStream;
    try {
      stream = await navigator.mediaDevices.getUserMedia({ audio: true });
    } catch (err) {
      if (err instanceof DOMException && err.name === 'NotAllowedError') {
        this.error.set('Microphone access denied. Please allow microphone permissions and try again.');
      } else {
        this.error.set('Could not access microphone. Please check your audio settings.');
      }
      this.isStarting = false;
      return;
    }

    // If discard() was called while waiting for permission, release the stream
    if (token !== this.startToken) {
      for (const track of stream.getTracks()) {
        track.stop();
      }
      this.isStarting = false;
      return;
    }

    this.audioStream = stream;

    try {
      // Set up Web Audio API for level metering
      this.audioContext = new AudioContext();
      if (this.audioContext.state === 'suspended') {
        await this.audioContext.resume();
      }
      const source = this.audioContext.createMediaStreamSource(this.audioStream);
      this.analyserNode = this.audioContext.createAnalyser();
      this.analyserNode.fftSize = 64;
      source.connect(this.analyserNode);

      // Determine best supported mime type; omit option to let browser use default
      const mimeType = this.getSupportedMimeType();
      this.chunks = [];

      this.mediaRecorder = new MediaRecorder(this.audioStream,
        mimeType ? { mimeType } : undefined,
      );

      this.mediaRecorder.ondataavailable = (e) => {
        if (e.data.size > 0) {
          this.chunks.push(e.data);
        }
      };

      // Collect data every 1 second for chunked access
      this.mediaRecorder.start(1000);
      this.state.set('recording');
      this.elapsedSeconds.set(0);
      this.startTimer();
      this.startLevelMetering();
    } catch (err) {
      this.cleanup();
      this.error.set('Failed to start recording. Your browser may not support audio recording.');
    } finally {
      this.isStarting = false;
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
        this.cleanup();
        resolve(null);
        return;
      }

      this.mediaRecorder.onstop = () => {
        const mimeType = this.mediaRecorder?.mimeType || 'audio/webm';
        const baseMime = mimeType.split(';')[0].toLowerCase();
        const extension = baseMime.includes('ogg') ? 'ogg' : baseMime.includes('mp4') ? 'mp4' : 'webm';
        const blob = new Blob(this.chunks, { type: mimeType });
        const file = new File([blob], `recording-${Date.now()}.${extension}`, { type: mimeType });

        this.cleanup();
        resolve(file);
      };

      this.mediaRecorder.stop();
    });
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
      this.elapsedSeconds.update(s => s + 1);
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

  private cleanup(): void {
    this.stopTimer();
    this.stopLevelMetering();

    if (this.audioStream) {
      for (const track of this.audioStream.getTracks()) {
        track.stop();
      }
      this.audioStream = null;
    }

    if (this.audioContext) {
      this.audioContext.close();
      this.audioContext = null;
    }

    this.analyserNode = null;
    this.mediaRecorder = null;
    this.chunks = [];
    this.state.set('idle');
    this.elapsedSeconds.set(0);
    this.audioLevels.set(new Array(16).fill(0));
  }
}
