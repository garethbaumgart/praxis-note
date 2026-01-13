import { Component, ChangeDetectionStrategy } from '@angular/core';

@Component({
  selector: 'app-task-card-skeleton',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="bg-surface rounded-md py-2 px-3 border border-border animate-pulse">
      <!-- Title skeleton -->
      <div class="h-4 bg-foreground-muted/20 rounded w-3/4 mb-2"></div>
      <!-- Second line (sometimes) -->
      <div class="h-4 bg-foreground-muted/20 rounded w-1/2 mb-3"></div>
      <!-- Icon row skeleton -->
      <div class="flex items-center gap-2">
        <div class="h-5 w-16 bg-foreground-muted/10 rounded"></div>
        <div class="h-5 w-5 bg-foreground-muted/10 rounded"></div>
      </div>
    </div>
  `,
})
export class TaskCardSkeletonComponent {}
