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
        class="recording-pill"
        (click)="returnToMeeting()"
        aria-label="Return to active recording"
      >
        <span class="pulse-dot"></span>
        <span class="label">Recording</span>
        <span class="timer">{{ recorder.formattedTime() }}</span>
        <span class="return-hint">Return <i class="pi pi-arrow-right text-xs"></i></span>
      </button>
    }
  `,
  styles: [`
    .recording-pill {
      position: fixed;
      bottom: 1.5rem;
      right: 1.5rem;
      z-index: 50;
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.5rem 1rem;
      background: var(--color-danger-bg, rgba(191, 97, 106, 0.15));
      border: 1px solid var(--color-danger-base);
      border-radius: 9999px;
      color: var(--color-danger-base);
      font-size: 0.8125rem;
      font-weight: 500;
      cursor: pointer;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
      transition: background 0.2s, transform 0.2s;
    }

    .recording-pill:hover {
      background: var(--color-danger-base);
      color: white;
      transform: scale(1.02);
    }

    .recording-pill:hover .pulse-dot {
      background: white;
    }

    .pulse-dot {
      width: 0.5rem;
      height: 0.5rem;
      border-radius: 50%;
      background: var(--color-danger-base);
      animation: pulse 1.5s ease-in-out infinite;
    }

    .return-hint {
      display: flex;
      align-items: center;
      gap: 0.25rem;
      opacity: 0.7;
      border-left: 1px solid currentColor;
      padding-left: 0.5rem;
      margin-left: 0.25rem;
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
