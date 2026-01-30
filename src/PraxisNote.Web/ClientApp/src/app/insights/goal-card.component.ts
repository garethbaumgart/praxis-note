import { Component, ChangeDetectionStrategy, input, output, computed } from '@angular/core';
import { GoalProgress } from './insights.model';

@Component({
  selector: 'app-goal-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="bg-surface-subtle border border-border rounded-xl p-4 flex flex-col gap-3 relative group">
      <!-- Delete button -->
      <button
        type="button"
        class="absolute top-2 right-2 opacity-0 group-hover:opacity-100 transition-opacity text-foreground-muted hover:text-danger p-1"
        (click)="onDelete.emit(goal().goalId)"
        aria-label="Delete goal">
        <i class="pi pi-trash text-xs"></i>
      </button>

      <!-- Ring + Value -->
      <div class="flex items-center gap-3">
        <svg viewBox="0 0 40 40" class="w-11 h-11 shrink-0"
             role="img" [attr.aria-label]="goal().isMet ? 'Goal met' : 'Goal progress: ' + progressPercent() + '%'">
          <!-- Background ring -->
          <circle cx="20" cy="20" r="16" fill="none"
                  stroke="var(--color-border)" stroke-width="3" />
          <!-- Progress ring -->
          <circle cx="20" cy="20" r="16" fill="none"
                  [attr.stroke]="ringColor()"
                  stroke-width="3"
                  stroke-linecap="round"
                  [attr.stroke-dasharray]="circumference"
                  [attr.stroke-dashoffset]="dashOffset()"
                  transform="rotate(-90 20 20)" />
          <!-- Center icon -->
          @if (goal().isMet) {
            <text x="20" y="21" text-anchor="middle" dominant-baseline="central"
                  fill="var(--color-done-text)" font-size="14">&#10003;</text>
          } @else {
            <text x="20" y="21" text-anchor="middle" dominant-baseline="central"
                  fill="var(--color-foreground-muted)" font-size="11">
              {{ progressPercent() }}%
            </text>
          }
        </svg>
        <div class="min-w-0">
          <p class="text-sm font-medium text-foreground truncate">{{ goal().title }}</p>
          <p class="text-xs text-foreground-muted">
            {{ metricLabel() }} {{ operatorLabel() }} {{ targetLabel() }}
          </p>
        </div>
      </div>

      <!-- Streak badge -->
      @if (goal().streak > 0) {
        <div class="flex items-center gap-1 text-xs text-done-foreground">
          <i class="pi pi-bolt text-[10px]"></i>
          <span>{{ goal().streak }} meeting streak</span>
        </div>
      }

      <!-- Dot track -->
      @if (goal().recentResults.length > 0) {
        <div class="flex items-center gap-1" role="img"
             [attr.aria-label]="'Recent results: ' + passedCount() + ' of ' + goal().recentResults.length + ' meetings passed'">
          @for (passed of goal().recentResults; track $index) {
            <span class="w-2 h-2 rounded-full"
                  [class.bg-done-foreground]="passed"
                  [class.bg-border]="!passed"></span>
          }
          <span class="text-[10px] text-foreground-muted ml-1">
            {{ goal().meetingsEvaluated }} meetings
          </span>
        </div>
      } @else {
        <p class="text-xs text-foreground-muted">No meetings evaluated yet</p>
      }
    </div>
  `,
})
export class GoalCardComponent {
  readonly goal = input.required<GoalProgress>();
  readonly onDelete = output<string>();

  protected readonly circumference = 2 * Math.PI * 16;

  protected readonly progressPercent = computed(() => {
    const g = this.goal();
    if (g.currentValue === null || g.meetingsEvaluated === 0) return 0;
    // Calculate how close to the target
    const ratio = this.calculateRatio(g);
    return Math.min(100, Math.max(0, Math.round(ratio * 100)));
  });

  protected readonly dashOffset = computed(() => {
    const pct = this.progressPercent();
    return this.circumference * (1 - pct / 100);
  });

  protected readonly ringColor = computed(() => {
    if (this.goal().isMet) return 'var(--color-done-text)';
    const pct = this.progressPercent();
    if (pct >= 70) return 'var(--color-inprogress-text)';
    return 'var(--color-danger-base)';
  });

  protected readonly passedCount = computed(() =>
    this.goal().recentResults.filter(r => r).length,
  );

  protected readonly metricLabel = computed(() => {
    const labels: Record<string, string> = {
      TalkTimePercentage: 'Talk time',
      QuestionRatio: 'Questions',
      InterruptionCount: 'Interruptions',
      SentimentScore: 'Sentiment',
      RedFlagCount: 'Red flags',
    };
    return labels[this.goal().metricType] ?? this.goal().metricType;
  });

  protected readonly operatorLabel = computed(() => {
    const labels: Record<string, string> = {
      LessThan: '<',
      LessThanOrEqual: '≤',
      GreaterThan: '>',
      GreaterThanOrEqual: '≥',
      Between: 'between',
    };
    return labels[this.goal().operator] ?? this.goal().operator;
  });

  protected readonly targetLabel = computed(() => {
    const g = this.goal();
    if (g.operator === 'Between' && g.targetValueUpper !== null) {
      return `${g.targetValue}–${g.targetValueUpper}`;
    }
    return `${g.targetValue}`;
  });

  private calculateRatio(g: GoalProgress): number {
    if (g.currentValue === null) return 0;
    const current = g.currentValue;
    const target = g.targetValue;

    switch (g.operator) {
      case 'LessThan':
      case 'LessThanOrEqual':
        if (target === 0) return current === 0 ? 1 : 0;
        return current <= target ? 1 : Math.max(0, 1 - (current - target) / target);
      case 'GreaterThan':
      case 'GreaterThanOrEqual':
        if (target === 0) return current > 0 ? 1 : 0;
        return current >= target ? 1 : current / target;
      case 'Between': {
        const upper = g.targetValueUpper ?? target;
        const mid = (target + upper) / 2;
        const range = (upper - target) / 2;
        if (range === 0) return current === target ? 1 : 0;
        const dist = Math.abs(current - mid);
        return dist <= range ? 1 : Math.max(0, 1 - (dist - range) / range);
      }
      default:
        return 0;
    }
  }
}
