import { Component, ChangeDetectionStrategy } from '@angular/core';
import { Skeleton } from 'primeng/skeleton';

@Component({
  selector: 'app-tag-list-skeleton',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Skeleton],
  template: `
    <div role="status" aria-label="Loading tags">
      <span class="sr-only">Loading tags...</span>
      <!-- Selector skeleton -->
      <div class="mb-6">
        <p-skeleton width="280px" height="2.5rem" styleClass="rounded-lg" />
      </div>
      <!-- Tag cards skeleton -->
      <div class="space-y-4">
        <div class="flex items-center gap-2 mb-3">
          <p-skeleton width="5rem" height="1.5rem" styleClass="rounded-full" />
          <div class="flex-1 h-px bg-border-muted"></div>
        </div>
        @for (i of skeletonRows; track i) {
          <div class="flex items-center gap-3 px-3 py-2.5">
            <p-skeleton width="1.75rem" height="1.75rem" styleClass="rounded-md" />
            <div class="flex-1 min-w-0 space-y-1.5">
              <p-skeleton width="55%" height="0.875rem" />
              <p-skeleton width="30%" height="0.75rem" />
            </div>
            <p-skeleton width="3rem" height="0.75rem" />
          </div>
        }
      </div>
    </div>
  `,
})
export class TagListSkeletonComponent {
  readonly skeletonRows = [0, 1, 2, 3];
}
