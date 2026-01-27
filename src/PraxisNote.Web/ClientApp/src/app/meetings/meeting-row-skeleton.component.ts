import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-meeting-row-skeleton',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex items-center gap-4 p-3 bg-surface-subtle border border-border rounded-lg animate-pulse">
      <!-- Time skeleton -->
      <div class="w-14 flex-shrink-0 flex flex-col items-center gap-1">
        <div class="h-5 w-10 bg-surface-muted rounded"></div>
        <div class="h-3 w-6 bg-surface-muted rounded"></div>
      </div>

      <!-- Content skeleton -->
      <div class="flex-1 space-y-2">
        <div class="h-4 w-40 bg-surface-muted rounded"></div>
        <div class="h-3 w-24 bg-surface-muted rounded"></div>
      </div>

      <!-- Status skeleton -->
      <div class="h-5 w-16 bg-surface-muted rounded-full"></div>
    </div>
  `,
})
export class MeetingRowSkeletonComponent {}
