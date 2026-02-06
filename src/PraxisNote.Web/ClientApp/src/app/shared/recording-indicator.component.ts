import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { Router } from '@angular/router';
import { AudioRecorderService } from '../meetings/audio-recorder.service';

@Component({
  selector: 'app-recording-indicator',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (recorder.isActive()) {
      <button
        type="button"
        class="recording-pill fixed bottom-6 right-6 z-60 flex items-center gap-2 py-2 px-4 rounded-full text-sm font-medium cursor-pointer shadow-lg transition-all duration-200 hover:scale-[1.02] lg:hidden"
        (click)="returnToMeeting()"
        aria-label="Return to active recording"
      >
        <span class="pulse-dot w-2 h-2 rounded-full"></span>
        <span>Recording</span>
        <span>{{ recorder.formattedTime() }}</span>
        <span class="return-hint flex items-center gap-1 opacity-70 border-l border-current pl-2 ml-1">Return <i class="pi pi-arrow-right text-xs"></i></span>
      </button>
    }
  `,
  styles: [`
    .recording-pill {
      background: var(--color-danger-bg);
      border: 1px solid var(--color-danger-base);
      color: var(--color-danger-base);
    }

    .recording-pill:hover {
      background: var(--color-danger-base);
      color: var(--color-surface);
    }

    .recording-pill:hover .pulse-dot {
      background: var(--color-surface);
    }

    .pulse-dot {
      background: var(--color-danger-base);
      animation: pulse 1.5s ease-in-out infinite;
    }

    @keyframes pulse {
      0%, 100% { opacity: 1; }
      50% { opacity: 0.3; }
    }
  `],
})
export class RecordingIndicatorComponent {
  protected readonly recorder = inject(AudioRecorderService);
  private readonly router = inject(Router);

  returnToMeeting(): void {
    const meetingId = this.recorder.activeMeetingId();
    if (meetingId) {
      this.router.navigate(['/meetings', meetingId]);
    }
  }
}
