import { Component, ChangeDetectionStrategy } from '@angular/core';
import { Skeleton } from 'primeng/skeleton';

@Component({
  selector: 'app-meeting-row-skeleton',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Skeleton],
  template: `
    <div class="flex items-center gap-4 p-3 bg-surface-subtle border border-border rounded-lg" role="status" aria-label="Loading meeting">
      <span class="sr-only">Loading meeting...</span>
      <!-- Time skeleton -->
      <div class="w-14 flex-shrink-0 flex flex-col items-center gap-1">
        <p-skeleton width="2.5rem" height="1.25rem" />
        <p-skeleton width="1.5rem" height="0.75rem" />
      </div>

      <!-- Content skeleton -->
      <div class="flex-1 space-y-2">
        <p-skeleton width="10rem" height="1rem" />
        <p-skeleton width="6rem" height="0.75rem" />
      </div>

      <!-- Status skeleton -->
      <p-skeleton width="4rem" height="1.25rem" styleClass="rounded-full" />
    </div>
  `,
})
export class MeetingRowSkeletonComponent {}
