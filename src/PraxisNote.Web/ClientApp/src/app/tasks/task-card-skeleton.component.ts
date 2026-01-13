import { Component, ChangeDetectionStrategy } from '@angular/core';
import { Skeleton } from 'primeng/skeleton';

@Component({
  selector: 'app-task-card-skeleton',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Skeleton],
  template: `
    <div class="bg-surface rounded-md py-2 px-3 border border-border" role="status" aria-live="polite">
      <span class="sr-only">Loading task...</span>
      <!-- Title skeleton -->
      <p-skeleton width="75%" height="1rem" styleClass="mb-2" />
      <!-- Second line -->
      <p-skeleton width="50%" height="1rem" styleClass="mb-3" />
      <!-- Icon row skeleton -->
      <div class="flex items-center gap-2">
        <p-skeleton width="4rem" height="1.25rem" />
        <p-skeleton width="1.25rem" height="1.25rem" />
      </div>
    </div>
  `,
})
export class TaskCardSkeletonComponent {}
