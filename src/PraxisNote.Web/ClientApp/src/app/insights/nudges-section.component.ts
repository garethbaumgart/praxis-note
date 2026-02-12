import { Component, ChangeDetectionStrategy, inject, OnInit } from '@angular/core';
import { Skeleton } from 'primeng/skeleton';
import { Tooltip } from 'primeng/tooltip';
import { NudgeService } from './nudge.service';
import { NudgeCardComponent } from './nudge-card.component';
import { GoalsService } from './goals.service';
import { InsightsService } from './insights.service';
import { ErrorStateComponent } from '../shared/components/error-state.component';

@Component({
  selector: 'app-nudges-section',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Skeleton, Tooltip, NudgeCardComponent, ErrorStateComponent],
  template: `
    @if (nudgeService.loading()) {
      <section class="mb-6">
        <div class="flex items-center gap-2 mb-3">
          <h2 class="text-lg font-semibold text-foreground">Blind Spot Nudges</h2>
        </div>
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4" role="status" aria-label="Loading nudges">
          <span class="sr-only">Loading nudges...</span>
          @for (i of skeletonItems; track i) {
            <div class="bg-surface-subtle border border-border rounded-xl p-4">
              <div class="flex items-center gap-3 mb-3">
                <p-skeleton shape="circle" width="36px" height="36px" />
                <p-skeleton width="40%" height="10px" />
              </div>
              <p-skeleton width="90%" height="12px" styleClass="mb-2" />
              <p-skeleton width="100%" height="12px" styleClass="mb-2" />
              <p-skeleton width="70%" height="12px" />
            </div>
          }
        </div>
      </section>
    } @else if (nudgeService.error()) {
      <section class="mb-6">
        <div class="flex items-center gap-2 mb-3">
          <h2 class="text-lg font-semibold text-foreground">Blind Spot Nudges</h2>
        </div>
        <app-error-state
          size="sm"
          title="Something went wrong"
          [message]="nudgeService.error()!"
          (retry)="reload()"
        />
      </section>
    } @else if (nudgeService.nudges().length > 0) {
      <section class="mb-6">
        <div class="flex items-center gap-2 mb-3">
          <h2 class="text-lg font-semibold text-foreground">Blind Spot Nudges</h2>
          <i class="pi pi-info-circle text-foreground-muted text-sm cursor-help"
             pTooltip="AI-generated coaching suggestions based on gaps between your self-assessment and AI analysis. Try these micro-experiments in your next meeting."
             tooltipPosition="top"
             role="img"
             aria-label="Blind spot nudges info"></i>
        </div>
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          @for (nudge of nudgeService.nudges(); track nudge.id) {
            <app-nudge-card
              [nudge]="nudge"
              (onDismiss)="dismiss($event)"
              (onAccept)="accept($event)"
            />
          }
        </div>
      </section>
    }
  `,
})
export class NudgesSectionComponent implements OnInit {
  protected readonly nudgeService = inject(NudgeService);
  private readonly goalsService = inject(GoalsService);
  private readonly insightsService = inject(InsightsService);
  protected readonly skeletonItems = [0, 1, 2];

  ngOnInit(): void {
    this.nudgeService.loadNudges(this.insightsService.dateRange());
  }

  protected reload(): void {
    this.nudgeService.loadNudges(this.insightsService.dateRange());
  }

  protected dismiss(id: string): void {
    this.nudgeService.dismissNudge(id);
  }

  protected accept(id: string): void {
    this.nudgeService.acceptNudge(id, () => {
      // Reload goals after the accept call completes to avoid stale data
      this.goalsService.loadGoalsAndProgress();
    });
  }
}
