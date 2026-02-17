import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  signal,
  inject,
  viewChild,
  ElementRef,
  effect,
  Injector,
  afterNextRender,
} from '@angular/core';
import { AudioRecorderService } from './audio-recorder.service';
import { DeepgramTranscriptionService } from './deepgram-transcription.service';

@Component({
  selector: 'app-meeting-transcript-section',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <!-- Hero Record CTA (when not recording and no/little transcript) -->
    @if (!recorder.isActive() && !hasSubstantialTranscript()) {
      <div class="record-hero">
        <div class="record-hero-buttons">
          <button
            type="button"
            class="record-hero-btn"
            (click)="onStartRecording.emit('microphone')"
            aria-label="Record with microphone"
          >
            <i class="pi pi-microphone"></i>
            <span>Microphone</span>
            <span class="record-hero-hint">In-person meetings</span>
          </button>
          <button
            type="button"
            class="record-hero-btn"
            (click)="onStartRecording.emit('both')"
            aria-label="Record online meeting"
          >
            <i class="pi pi-desktop"></i>
            <span>Online Meeting</span>
            <span class="record-hero-hint">Zoom, Teams, etc.</span>
          </button>
        </div>
        <p class="record-hero-or">or paste transcript below</p>
      </div>
    }

    <!-- Compact Record buttons (when transcript already has content) -->
    @if (!recorder.isActive() && hasSubstantialTranscript()) {
      <div class="record-compact">
        <button
          type="button"
          class="record-compact-btn"
          (click)="onStartRecording.emit('microphone')"
          aria-label="Record with microphone"
        >
          <i class="pi pi-microphone"></i> Record More
        </button>
        <button
          type="button"
          class="record-compact-btn"
          (click)="onStartRecording.emit('both')"
          aria-label="Record online meeting"
        >
          <i class="pi pi-desktop"></i> Online Meeting
        </button>
      </div>
    }

    <!-- Audio Recording UI -->
    @if (recorder.isActive()) {
      <div class="recording-area">
        <div class="flex items-center justify-between mb-3">
          <div class="flex items-center gap-2">
            <span class="w-3 h-3 bg-danger rounded-full recording-pulse" aria-hidden="true"></span>
            <span class="text-sm font-medium text-foreground">
              {{ recorder.isPaused() ? 'Paused' : 'Recording' }}
            </span>
            @if (recorder.hasSystemAudio()) {
              <span class="text-xs px-2 py-0.5 bg-accent-solid/20 text-accent-solid rounded-full">
                <i class="pi pi-desktop mr-1"></i>Tab Audio
              </span>
            }
          </div>
          <span class="text-sm text-foreground-muted font-mono" aria-label="Recording duration">{{ recorder.formattedTime() }}</span>
        </div>
        <!-- Audio level bars -->
        <div class="flex items-end gap-0.5 h-8 mb-3" aria-hidden="true">
          @for (level of recorder.audioLevels(); track $index) {
            <div
              class="audio-bar flex-1 rounded-sm"
              [class.bg-accent-solid]="level > 0.05"
              [class.bg-surface-muted]="level <= 0.05"
              [style.height.%]="Math.max(level * 100, 10)"
            ></div>
          }
        </div>
        <div class="flex justify-center gap-2">
          @if (recorder.isRecording()) {
            <button
              type="button"
              class="px-3 py-1.5 text-xs bg-surface-muted text-foreground-secondary rounded-md hover:bg-surface-muted/80 transition-colors"
              (click)="recorder.pause()"
              aria-label="Pause recording"
            >
              <i class="pi pi-pause mr-1"></i>Pause
            </button>
          } @else {
            <button
              type="button"
              class="px-3 py-1.5 text-xs bg-surface-muted text-foreground-secondary rounded-md hover:bg-surface-muted/80 transition-colors"
              (click)="recorder.resume()"
              aria-label="Resume recording"
            >
              <i class="pi pi-play mr-1"></i>Resume
            </button>
          }
          <button
            type="button"
            class="px-3 py-1.5 text-xs bg-danger text-white rounded-md hover:opacity-90 transition-opacity"
            (click)="onStopRecording.emit()"
            aria-label="Stop recording"
          >
            <i class="pi pi-stop-circle mr-1"></i>Stop
          </button>
        </div>
      </div>
    }

    @if (recorder.error()) {
      <p class="text-xs text-danger mt-2">{{ recorder.error() }}</p>
    }
    @if (transcription.isReconnecting()) {
      <div class="flex items-center gap-2 text-xs text-foreground-muted bg-surface-muted rounded px-3 py-1.5 mt-2">
        <i class="pi pi-spin pi-spinner text-xs"></i>
        <span>Reconnecting transcription...</span>
      </div>
    } @else if (transcription.error()) {
      <p class="text-xs text-danger mt-2">{{ transcription.error() }}</p>
    }

    @if (showTabWarning()) {
      <div class="flex items-center gap-2 text-xs text-foreground-muted bg-surface-muted rounded px-3 py-1.5 mt-2">
        <i class="pi pi-info-circle text-xs"></i>
        <span>Keep this tab active for best recording quality.</span>
      </div>
    }

    <!-- Live transcript preview while recording -->
    @if (recorder.isActive() && (transcription.transcript() || transcription.interimText())) {
      <div class="live-transcript" #liveTranscriptContainer>
        <div class="flex items-center gap-1.5 mb-2">
          <i class="pi pi-volume-up text-xs text-accent-solid"></i>
          <span class="text-xs font-medium text-foreground-secondary">Live Transcript</span>
        </div>
        @if (transcription.segments().length > 0) {
          <div class="text-sm leading-relaxed space-y-1">
            @for (seg of transcription.segments(); track $index) {
              <p>
                <span class="font-semibold text-accent-solid text-xs">[{{ seg.speaker }}]</span>
                <span class="text-foreground ml-1">{{ seg.text }}</span>
              </p>
            }
            @if (transcription.interimText()) {
              <p>
                @if (transcription.interimSpeaker()) {
                  <span class="font-semibold text-foreground-muted text-xs">[{{ transcription.interimSpeaker() }}]</span>
                }
                <span class="text-foreground-muted italic ml-1">{{ transcription.interimText() }}</span>
              </p>
            }
          </div>
        } @else {
          <p class="text-sm text-foreground leading-relaxed">
            {{ transcription.transcript() }}
            @if (transcription.interimText()) {
              <span class="text-foreground-muted italic">{{ transcription.interimText() }}</span>
            }
          </p>
        }
      </div>
    }

    <!-- Transcript textarea -->
    <textarea
      #transcriptArea
      class="transcript-textarea"
      placeholder="Paste transcript here..."
      [value]="transcript()"
      (input)="onTranscriptInput($event)"
      aria-label="Meeting transcript"
      rows="3"
    ></textarea>
    <div class="flex justify-end mt-1">
      <span class="text-xs text-foreground-muted">{{ transcript().length }} characters</span>
    </div>
  `,
  styles: [`
    :host { display: block; --transcript-max-height: 300px; }

    .record-hero {
      text-align: center;
      padding: 16px 0;
      margin-bottom: 12px;
    }

    .record-hero-buttons {
      display: flex;
      justify-content: center;
      gap: 12px;
      margin-bottom: 12px;
    }

    .record-hero-btn {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 4px;
      padding: 16px 24px;
      border-radius: 12px;
      border: 2px dashed var(--color-border-default);
      background: var(--color-bg-muted);
      color: var(--color-text-primary);
      font-size: 13px;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.15s;
      min-width: 140px;
    }

    .record-hero-btn:hover {
      border-color: var(--color-primary-solid);
      background: var(--color-primary-bg);
      color: var(--color-primary-text);
    }

    .record-hero-btn .pi {
      font-size: 20px;
      margin-bottom: 4px;
    }

    .record-hero-hint {
      font-size: 10px;
      font-weight: 400;
      color: var(--color-text-muted);
    }

    .record-hero-btn:hover .record-hero-hint {
      color: var(--color-primary-text);
    }

    .record-hero-or {
      font-size: 11px;
      color: var(--color-text-muted);
    }

    .record-compact {
      display: flex;
      gap: 6px;
      margin-bottom: 8px;
    }

    .record-compact-btn {
      background: var(--color-todo-bg);
      color: var(--color-todo-text);
      border: 1px solid var(--color-todo-border);
      border-radius: 5px;
      padding: 4px 10px;
      font-size: 11px;
      cursor: pointer;
      transition: all .15s;
    }

    .record-compact-btn:hover {
      background: var(--color-todo-bg-hover);
    }

    .recording-area { background: var(--color-bg-muted); border-radius: 8px; padding: 16px; margin-bottom: 12px; }
    @keyframes pulse-recording { 0%, 100% { opacity: 1; } 50% { opacity: 0.4; } }
    .recording-pulse { animation: pulse-recording 1.5s ease-in-out infinite; }
    .audio-bar { transition: height .1s ease-out; }

    .live-transcript {
      background: var(--color-bg-muted); border-radius: 6px; padding: 12px;
      margin-bottom: 12px; max-height: 200px; overflow-y: auto;
    }

    .transcript-textarea {
      width: 100%; background: var(--color-bg-muted); border: none; border-radius: 6px;
      padding: 12px; font-size: 13px; line-height: 1.7; color: var(--color-text-primary);
      resize: none; overflow-y: auto; outline: none; box-sizing: border-box; min-height: 80px; max-height: var(--transcript-max-height);
    }
    .transcript-textarea::placeholder { color: var(--color-text-muted); }
    .transcript-textarea:focus-visible {
      outline: 2px solid var(--color-primary-solid);
      outline-offset: -2px;
    }
  `],
})
export class MeetingTranscriptSectionComponent {
  private static readonly MAX_TEXTAREA_HEIGHT = 300;

  readonly recorder = inject(AudioRecorderService);
  readonly transcription = inject(DeepgramTranscriptionService);
  private readonly injector = inject(Injector);

  readonly Math = Math;

  // Inputs from parent
  readonly transcript = input.required<string>();
  readonly showTabWarning = input.required<boolean>();

  // Outputs to parent
  readonly onTranscriptChange = output<string>();
  readonly onStartRecording = output<'microphone' | 'both'>();
  readonly onStopRecording = output<void>();

  readonly transcriptArea = viewChild<ElementRef<HTMLTextAreaElement>>('transcriptArea');
  readonly liveTranscriptContainer = viewChild<ElementRef<HTMLDivElement>>('liveTranscriptContainer');

  /** Whether the transcript has enough content to show compact record buttons instead of hero CTA */
  readonly hasSubstantialTranscript = signal(false);

  constructor() {
    // Track whether transcript has content for switching between hero and compact record CTAs
    effect(() => {
      const t = this.transcript();
      this.hasSubstantialTranscript.set(t.length > 0);
    });

    // Auto-resize transcript on any change
    effect(() => {
      this.transcript();
      afterNextRender(() => {
        const ta = this.transcriptArea()?.nativeElement;
        if (ta) {
          // Only auto-scroll if the user is already near the bottom before the update
          const distanceFromBottom = ta.scrollHeight - (ta.scrollTop + ta.clientHeight);
          const isNearBottom = distanceFromBottom < 10;

          this.autoResizeTextarea(ta);

          if (isNearBottom) {
            ta.scrollTop = ta.scrollHeight;
          }
        }
      }, { injector: this.injector });
    });

    // Auto-scroll live transcript when new segments arrive
    effect(() => {
      this.transcription.segments();
      this.transcription.interimText();
      afterNextRender(() => {
        const container = this.liveTranscriptContainer()?.nativeElement;
        if (container) {
          const distanceFromBottom = container.scrollHeight - (container.scrollTop + container.clientHeight);
          const isNearBottom = distanceFromBottom < 30;
          if (isNearBottom) {
            container.scrollTop = container.scrollHeight;
          }
        }
      }, { injector: this.injector });
    });
  }

  onTranscriptInput(event: Event): void {
    const textarea = event.target as HTMLTextAreaElement;
    this.onTranscriptChange.emit(textarea.value);
    this.autoResizeTextarea(textarea);
  }

  private autoResizeTextarea(textarea: HTMLTextAreaElement): void {
    textarea.style.height = 'auto';
    const maxHeight = parseInt(getComputedStyle(textarea).maxHeight, 10) || MeetingTranscriptSectionComponent.MAX_TEXTAREA_HEIGHT;
    textarea.style.height = Math.min(textarea.scrollHeight, maxHeight) + 'px';
  }
}
