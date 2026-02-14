import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { Router } from '@angular/router';
import { Skeleton } from 'primeng/skeleton';
import { Tooltip } from 'primeng/tooltip';
import { HomeDashboardService } from './home-dashboard.service';
import { ErrorStateComponent } from '../shared/components/error-state.component';

@Component({
  selector: 'app-outstanding-action-items',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Skeleton, Tooltip, ErrorStateComponent],
  template: `
    @if (dashboard.actionItemsLoading()) {
      <div class="widget-card" role="status" aria-label="Loading action items">
        <span class="sr-only">Loading action items...</span>
        <div class="flex items-center gap-2 mb-3">
          <p-skeleton width="20px" height="20px" shape="square" styleClass="rounded" />
          <p-skeleton width="180px" height="14px" />
        </div>
        <p-skeleton width="100%" height="12px" styleClass="mb-2" />
        <p-skeleton width="90%" height="12px" styleClass="mb-2" />
        <p-skeleton width="80%" height="12px" />
      </div>
    } @else if (dashboard.actionItemsError()) {
      <app-error-state
        title="Couldn't load action items"
        [message]="dashboard.actionItemsError()!"
        size="sm"
        (retry)="dashboard.loadActionItems()"
      />
    } @else if (dashboard.hasActionItems()) {
      <div class="widget-card">
        <div class="flex items-center gap-2 mb-3">
          <i class="pi pi-exclamation-circle text-sm text-overdue-foreground" aria-hidden="true"></i>
          <h2 class="text-sm font-semibold text-foreground">Outstanding Action Items</h2>
          <span class="text-xs text-foreground-muted">({{ dashboard.actionItems().length }})</span>
        </div>

        @for (item of dashboard.actionItems(); track item.actionItemId) {
          <button
            type="button"
            class="action-item-row"
            [attr.aria-label]="'Action item: ' + item.description + ' from ' + (item.meetingTitle ?? 'Untitled Meeting')"
            (click)="goToMeeting(item.meetingId)">
            <div class="w-3.5 h-3.5 border-2 border-border rounded mt-0.5 shrink-0" aria-hidden="true"></div>
            <div class="flex-1 min-w-0">
              <div class="text-sm text-foreground text-left">{{ item.description }}</div>
              <div class="text-xs text-foreground-muted mt-0.5 text-left">
                {{ item.meetingTitle ?? 'Untitled Meeting' }}
                @if (item.assignee) {
                  <span> &middot; {{ item.assignee }}</span>
                }
                @if (item.isLinkedToTask) {
                  <span
                    class="ml-1 text-accent-foreground"
                    pTooltip="Linked to task"
                    tooltipPosition="top">
                    <i class="pi pi-link text-xs" aria-hidden="true"></i>
                    {{ item.linkedTaskStatus }}
                  </span>
                }
              </div>
            </div>
          </button>
        }
      </div>
    }
  `,
  styles: [`
    .action-item-row {
      display: flex;
      align-items: flex-start;
      gap: 0.5rem;
      padding: 0.375rem 0.25rem;
      border-radius: 0.375rem;
      cursor: pointer;
      width: 100%;
      text-align: left;
      border: none;
      background: none;
      font: inherit;
      transition: background 0.1s;
    }
    .action-item-row:hover {
      background: var(--color-bg-muted);
    }
  `],
})
export class OutstandingActionItemsComponent {
  protected readonly dashboard = inject(HomeDashboardService);
  private readonly router = inject(Router);

  protected goToMeeting(meetingId: string): void {
    this.router.navigate(['/meetings', meetingId], {
      state: { breadcrumbSource: { label: 'Home', route: '/home' } },
    });
  }
}
