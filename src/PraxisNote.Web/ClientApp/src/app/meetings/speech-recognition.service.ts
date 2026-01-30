import { Injectable, signal, computed, OnDestroy } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class SpeechRecognitionService implements OnDestroy {
  private recognition: SpeechRecognition | null = null;
  private shouldBeListening = false;

  readonly transcript = signal('');
  readonly interimText = signal('');
  readonly isListening = signal(false);
  readonly error = signal<string | null>(null);

  readonly isSupported = computed(
    () =>
      typeof window !== 'undefined' &&
      ('SpeechRecognition' in window || 'webkitSpeechRecognition' in window),
  );

  ngOnDestroy(): void {
    this.stop();
  }

  start(): void {
    if (!this.isSupported()) {
      this.error.set(
        'Speech recognition is not supported in this browser. Please use Chrome or Edge.',
      );
      return;
    }

    this.transcript.set('');
    this.interimText.set('');
    this.error.set(null);
    this.shouldBeListening = true;

    this.createAndStart();
  }

  stop(): void {
    this.shouldBeListening = false;
    this.interimText.set('');
    if (this.recognition) {
      this.recognition.onend = null;
      this.recognition.stop();
      this.recognition = null;
    }
    this.isListening.set(false);
  }

  reset(): void {
    this.stop();
    this.transcript.set('');
    this.error.set(null);
  }

  private createAndStart(): void {
    const SpeechRecognitionCtor = window.SpeechRecognition ?? window.webkitSpeechRecognition;
    if (!SpeechRecognitionCtor) return;

    this.recognition = new SpeechRecognitionCtor();
    this.recognition.continuous = true;
    this.recognition.interimResults = true;
    this.recognition.lang = 'en-US';

    this.recognition.onstart = () => {
      this.isListening.set(true);
    };

    this.recognition.onresult = (event: SpeechRecognitionEvent) => {
      let interim = '';
      let finalTranscript = '';

      for (let i = event.resultIndex; i < event.results.length; i++) {
        const result = event.results[i];
        if (result.isFinal) {
          finalTranscript += result[0].transcript;
        } else {
          interim += result[0].transcript;
        }
      }

      if (finalTranscript) {
        this.transcript.update(prev => {
          const separator = prev ? ' ' : '';
          return prev + separator + finalTranscript.trim();
        });
      }

      this.interimText.set(interim);
    };

    this.recognition.onerror = (event: SpeechRecognitionErrorEvent) => {
      // 'no-speech' and 'aborted' are expected during normal use
      if (event.error === 'no-speech' || event.error === 'aborted') return;

      if (event.error === 'not-allowed') {
        this.error.set('Microphone access denied for speech recognition.');
        this.shouldBeListening = false;
      } else {
        this.error.set(`Speech recognition error: ${event.error}`);
      }
    };

    this.recognition.onend = () => {
      this.isListening.set(false);
      // Auto-restart: Web Speech API stops after silence gaps
      if (this.shouldBeListening) {
        this.createAndStart();
      }
    };

    try {
      this.recognition.start();
    } catch {
      // Already started — ignore
    }
  }
}
