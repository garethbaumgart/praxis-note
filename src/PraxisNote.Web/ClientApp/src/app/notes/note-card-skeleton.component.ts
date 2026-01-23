import { Component, ChangeDetectionStrategy } from '@angular/core';
import { Skeleton } from 'primeng/skeleton';

@Component({
  selector: 'app-note-card-skeleton',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Skeleton],
  template: `
    <div class="bg-surface-subtle rounded-md p-3 border border-border" role="status" aria-live="polite">
      <span class="sr-only">Loading note...</span>
      <!-- Content skeleton -->
      <p-skeleton width="100%" height="1rem" styleClass="mb-2" />
      <p-skeleton width="85%" height="1rem" styleClass="mb-2" />
      <p-skeleton width="60%" height="1rem" styleClass="mb-3" />
      <!-- Checkbox skeleton -->
      <div class="flex items-center gap-2 mb-2">
        <p-skeleton width="14px" height="14px" />
        <p-skeleton width="70%" height="0.875rem" />
      </div>
      <div class="flex items-center gap-2 mb-3">
        <p-skeleton width="14px" height="14px" />
        <p-skeleton width="55%" height="0.875rem" />
      </div>
      <!-- Tags skeleton -->
      <div class="flex items-center gap-1">
        <p-skeleton width="3rem" height="18px" borderRadius="9999px" />
        <p-skeleton width="4rem" height="18px" borderRadius="9999px" />
      </div>
    </div>
  `,
})
export class NoteCardSkeletonComponent {}
