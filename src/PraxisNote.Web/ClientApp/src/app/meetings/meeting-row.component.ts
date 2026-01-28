import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { Meeting, MeetingStatus } from './meeting.model';

@Component({
  selector: 'app-meeting-row',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="meeting-row group cursor-pointer flex items-center gap-4 p-3 bg-surface-subtle border border-border rounded-lg hover:shadow-md transition-shadow"
      [class.recording]="meeting().status === 'Processing'"
      (click)="onOpen.emit()"
    >
      <!-- Time -->
      <div class="text-center w-14 flex-shrink-0">
        <div class="text-lg font-semibold text-foreground">{{ formatTime(meeting().meetingDate) }}</div>
        <div class="text-xs text-foreground-muted">{{ formatAmPm(meeting().meetingDate) }}</div>
      </div>

      <!-- Content -->
      <div class="flex-1 min-w-0">
        <div class="flex items-center gap-2">
          <h4 class="font-medium text-foreground text-sm truncate">
            {{ meeting().title || 'Untitled Meeting' }}
          </h4>
          @if (meeting().transcriptContent) {
            <i class="pi pi-file-edit text-xs text-foreground-muted" title="Has transcript"></i>
          }
          <!-- Tags -->
          @if (meeting().tags.length > 0) {
            <div class="flex items-center gap-1">
              @for (tag of meeting().tags.slice(0, 3); track tag.id) {
                <span class="tag-badge">{{ tag.name }}</span>
              }
              @if (meeting().tags.length > 3) {
                <span class="text-xs text-foreground-muted">+{{ meeting().tags.length - 3 }}</span>
              }
            </div>
          }
        </div>
        <p class="text-xs text-foreground-muted truncate">
          {{ formatAttendees(meeting().attendees) }}
        </p>
      </div>

      <!-- Status & Actions -->
      <div class="flex items-center gap-2">
        <span class="status-badge {{ getStatusClass(meeting().status) }}">
          @if (meeting().status === 'Processing') {
            <span class="w-2 h-2 bg-current rounded-full animate-pulse mr-1"></span>
          }
          {{ getStatusLabel(meeting().status) }}
        </span>

        <!-- Delete button (hover reveal) -->
        <button
          type="button"
          class="opacity-0 group-hover:opacity-100 p-1.5 text-foreground-muted hover:text-danger hover:bg-danger/10 rounded transition-all"
          (click)="handleDelete($event)"
          aria-label="Delete meeting"
        >
          <i class="pi pi-trash text-sm"></i>
        </button>
      </div>
    </div>
  `,
  styles: [`
    .meeting-row.recording {
      border: 2px dashed var(--color-inprogress-text);
      background: var(--color-inprogress-bg);
    }

    .status-badge {
      font-size: 10px;
      padding: 2px 8px;
      border-radius: 9999px;
      font-weight: 500;
      display: flex;
      align-items: center;
    }

    .status-draft {
      background: var(--color-bg-muted);
      color: var(--color-text-muted);
    }

    .status-processing {
      background: var(--color-inprogress-bg);
      color: var(--color-inprogress-text);
    }

    .status-ready {
      background: var(--color-primary-bg);
      color: var(--color-primary-text);
    }

    .status-reviewed {
      background: var(--color-done-bg);
      color: var(--color-done-text);
    }

    .status-failed {
      background: var(--color-danger-bg, rgba(191, 97, 106, 0.1));
      color: var(--color-danger-base);
    }
  `],
})
export class MeetingRowComponent {
  readonly meeting = input.required<Meeting>();
  readonly onOpen = output<void>();
  readonly onDelete = output<void>();

  formatTime(dateStr: string | null): string {
    if (!dateStr) return '--:--';
    const date = new Date(dateStr);
    const hours = date.getHours();
    const minutes = date.getMinutes();
    const displayHours = hours % 12 || 12;
    return `${displayHours}:${minutes.toString().padStart(2, '0')}`;
  }

  formatAmPm(dateStr: string | null): string {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    return date.getHours() >= 12 ? 'PM' : 'AM';
  }

  formatAttendees(attendees: string | null): string {
    if (!attendees) return 'No attendees';
    return attendees;
  }

  getStatusClass(status: MeetingStatus): string {
    const classes: Record<MeetingStatus, string> = {
      Draft: 'status-draft',
      Processing: 'status-processing',
      Ready: 'status-ready',
      Reviewed: 'status-reviewed',
      Failed: 'status-failed',
    };
    return classes[status];
  }

  getStatusLabel(status: MeetingStatus): string {
    const labels: Record<MeetingStatus, string> = {
      Draft: 'Draft',
      Processing: 'Recording',
      Ready: 'Ready',
      Reviewed: 'Reviewed',
      Failed: 'Failed',
    };
    return labels[status];
  }

  handleDelete(event: Event): void {
    event.stopPropagation();
    this.onDelete.emit();
  }
}
