import { Component, ChangeDetectionStrategy, inject, OnInit, signal } from '@angular/core';
import { Skeleton } from 'primeng/skeleton';
import { Tooltip } from 'primeng/tooltip';
import { GoalsService } from './goals.service';
import { GoalCardComponent } from './goal-card.component';
import { AddGoalDialogComponent } from './add-goal-dialog.component';

@Component({
  selector: 'app-goals-section',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Skeleton, Tooltip, GoalCardComponent, AddGoalDialogComponent],
  template: `
    <section class="mb-6">
      <div class="flex items-center justify-between mb-3">
        <div class="flex items-center gap-2">
          <h2 class="text-lg font-semibold text-foreground">Goals</h2>
          <i class="pi pi-info-circle text-foreground-muted text-sm cursor-help"
             pTooltip="Track behavioral goals based on your meeting patterns. Set targets for communication habits you want to improve and monitor progress over time."
             tooltipPosition="top"
             role="img"
             aria-label="Goals info"></i>
        </div>
        <button type="button"
                class="flex items-center gap-1 text-sm text-accent-foreground hover:opacity-80 transition"
                (click)="showAddDialog.set(true)"
                aria-label="Add goal">
          <i class="pi pi-plus text-xs"></i>
          <span>Add Goal</span>
        </button>
      </div>

      @if (goalsService.loading()) {
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          @for (i of skeletonItems; track i) {
            <div class="bg-surface-subtle border border-border rounded-xl p-4">
              <div class="flex items-center gap-3">
                <p-skeleton shape="circle" width="44px" height="44px" />
                <div class="flex-1">
                  <p-skeleton width="70%" height="14px" styleClass="mb-2" />
                  <p-skeleton width="50%" height="10px" />
                </div>
              </div>
              <p-skeleton width="40%" height="10px" styleClass="mt-3" />
            </div>
          }
        </div>
      } @else if (goalsService.error()) {
        <div class="bg-surface-subtle border border-border rounded-xl p-6 text-center">
          <i class="pi pi-exclamation-triangle text-3xl text-danger mb-2"></i>
          <p class="text-sm text-danger">{{ goalsService.error() }}</p>
        </div>
      } @else if (goalsService.progress().length === 0) {
        <div class="bg-surface-subtle border border-border rounded-xl p-6 text-center">
          <i class="pi pi-flag text-3xl text-foreground-muted mb-2"></i>
          <p class="text-sm text-foreground-muted">
            Set behavioral goals to track your communication habits across meetings.
          </p>
          <button type="button"
                  class="mt-3 px-4 py-2 bg-accent-solid text-white rounded-lg text-sm font-medium hover:opacity-90 transition"
                  (click)="showAddDialog.set(true)">
            Add Your First Goal
          </button>
        </div>
      } @else {
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          @for (goal of goalsService.progress(); track goal.goalId) {
            <app-goal-card [goal]="goal" (onDelete)="deleteGoal($event)" />
          }
        </div>
      }
    </section>

    <app-add-goal-dialog
      [visible]="showAddDialog()"
      (onClose)="showAddDialog.set(false)"
      (onAdd)="addGoal($event)" />
  `,
})
export class GoalsSectionComponent implements OnInit {
  protected readonly goalsService = inject(GoalsService);
  protected readonly showAddDialog = signal(false);
  protected readonly skeletonItems = [0, 1, 2];

  ngOnInit(): void {
    this.goalsService.loadGoalsAndProgress();
  }

  protected addGoal(goal: {
    metricType: string;
    operator: string;
    targetValue: number;
    targetValueUpper: number | null;
    title: string;
  }): void {
    this.goalsService.createGoal(goal);
    this.showAddDialog.set(false);
  }

  protected deleteGoal(goalId: string): void {
    this.goalsService.deleteGoal(goalId);
  }
}
