import { Component, inject, computed, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { Router } from '@angular/router';
import { Skeleton } from 'primeng/skeleton';
import { InsightsSummaryService } from './insights-summary.service';

@Component({
  selector: 'app-insights-widget',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Skeleton],
  template: `
    @if (service.loading()) {
      <!-- Loading skeleton -->
      <div class="p-5 bg-surface border border-border rounded-xl">
        <div class="flex items-center gap-2 mb-4">
          <p-skeleton width="32px" height="32px" shape="square" styleClass="rounded-lg" />
          <p-skeleton width="120px" height="18px" />
        </div>
        <div class="flex flex-col sm:flex-row gap-5">
          <div class="flex-1">
            <p-skeleton width="100px" height="12px" styleClass="mb-2" />
            <p-skeleton width="80px" height="28px" />
          </div>
          <div class="flex gap-6">
            <div>
              <p-skeleton width="60px" height="12px" styleClass="mb-1" />
              <p-skeleton width="40px" height="22px" />
            </div>
            <div>
              <p-skeleton width="60px" height="12px" styleClass="mb-1" />
              <p-skeleton width="40px" height="22px" />
            </div>
          </div>
        </div>
      </div>
    } @else if (summary()) {
      <!-- With data -->
      <button
        class="w-full group p-5 bg-surface border border-border rounded-xl hover:border-accent-foreground hover:shadow-md transition-all duration-200 text-left"
        aria-label="View insights dashboard"
        (click)="goToInsights()">
        <div class="flex items-center justify-between mb-4">
          <div class="flex items-center gap-2">
            <div class="w-8 h-8 rounded-lg bg-accent flex items-center justify-center">
              <i class="pi pi-chart-line text-accent-foreground text-sm" aria-hidden="true"></i>
            </div>
            <h3 class="font-semibold text-foreground">Your Insights</h3>
          </div>
          <span class="text-sm text-accent-foreground font-medium group-hover:underline">
            View all <i class="pi pi-arrow-right text-xs ml-1" aria-hidden="true"></i>
          </span>
        </div>

        <div class="flex flex-col sm:flex-row gap-5">
          <!-- Featured metric with sparkline -->
          <div class="flex-1">
            <p class="text-xs text-foreground-muted font-medium uppercase tracking-wide">
              {{ summary()!.headline.label }} Trend
            </p>
            <div class="flex items-end gap-3 mt-1">
              <div>
                <div class="flex items-baseline gap-1.5">
                  <span class="text-2xl font-bold text-foreground">
                    {{ summary()!.headline.value }}{{ summary()!.headline.unit }}
                  </span>
                  @if (summary()!.headline.change !== 0) {
                    <span class="text-sm font-medium flex items-center gap-0.5"
                          [class.text-done-foreground]="summary()!.headline.change < 0"
                          [class.text-danger]="summary()!.headline.change > 0">
                      <i class="pi text-xs"
                         [class.pi-arrow-down]="summary()!.headline.change < 0"
                         [class.pi-arrow-up]="summary()!.headline.change > 0"
                         aria-hidden="true"></i>
                      {{ absChange(summary()!.headline.change) }}%
                    </span>
                  }
                </div>
                <p class="text-xs text-foreground-muted mt-0.5">30-day average</p>
              </div>
              <!-- SVG Sparkline -->
              @if (sparklinePath()) {
                <svg width="100" height="32" viewBox="0 0 100 32" class="mb-1" aria-hidden="true">
                  <path [attr.d]="sparklineFillPath()" class="fill-accent stroke-none" />
                  <path [attr.d]="sparklinePath()" class="stroke-accent-foreground fill-none" style="stroke-width: 2; stroke-linecap: round; stroke-linejoin: round;" />
                </svg>
              }
            </div>
          </div>

          <!-- Secondary stats -->
          <div class="flex gap-6 sm:gap-8">
            <div>
              <p class="text-xs text-foreground-muted">{{ summary()!.questionRatio.label }}</p>
              <div class="flex items-baseline gap-1 mt-0.5">
                <span class="text-lg font-bold text-foreground">
                  {{ summary()!.questionRatio.value.toFixed(2) }}
                </span>
                @if (summary()!.questionRatio.change !== 0) {
                  <span class="text-xs font-medium"
                        [class.text-done-foreground]="summary()!.questionRatio.change > 0"
                        [class.text-danger]="summary()!.questionRatio.change < 0">
                    <i class="pi text-[10px]"
                       [class.pi-arrow-up]="summary()!.questionRatio.change > 0"
                       [class.pi-arrow-down]="summary()!.questionRatio.change < 0"
                       aria-hidden="true"></i>
                  </span>
                }
              </div>
            </div>
            <div>
              <p class="text-xs text-foreground-muted">{{ summary()!.redFlags.label }}</p>
              <div class="flex items-baseline gap-1 mt-0.5">
                <span class="text-lg font-bold text-foreground">
                  {{ summary()!.redFlags.value }}
                </span>
                @if (summary()!.redFlags.change !== 0) {
                  <span class="text-xs font-medium"
                        [class.text-done-foreground]="summary()!.redFlags.change < 0"
                        [class.text-danger]="summary()!.redFlags.change > 0">
                    <i class="pi text-[10px]"
                       [class.pi-arrow-up]="summary()!.redFlags.change > 0"
                       [class.pi-arrow-down]="summary()!.redFlags.change < 0"
                       aria-hidden="true"></i>
                  </span>
                }
              </div>
            </div>
          </div>
        </div>

        <!-- Nudge line -->
        @if (summary()!.nudgeText) {
          <div class="mt-4 pt-3 border-t border-border">
            <p class="text-sm text-foreground-secondary">
              <i class="pi pi-lightbulb text-inprogress-foreground text-xs mr-1.5" aria-hidden="true"></i>
              {{ summary()!.nudgeText }}
            </p>
          </div>
        }
      </button>
    } @else if (service.error()) {
      <!-- Error state: API failed -->
      <div class="p-5 bg-surface border border-border rounded-xl">
        <div class="flex items-start gap-3">
          <div class="w-8 h-8 rounded-lg bg-accent flex items-center justify-center flex-shrink-0 mt-0.5">
            <i class="pi pi-chart-line text-accent-foreground text-sm" aria-hidden="true"></i>
          </div>
          <div>
            <h3 class="font-semibold text-foreground mb-1">Insights</h3>
            <p class="text-sm text-foreground-muted">
              Unable to load insights right now. Try refreshing the page.
            </p>
          </div>
        </div>
      </div>
    } @else if (service.loaded()) {
      <!-- Empty state: no insights data -->
      <div class="p-5 bg-surface border border-border rounded-xl">
        <div class="flex items-start gap-3">
          <div class="w-8 h-8 rounded-lg bg-accent flex items-center justify-center flex-shrink-0 mt-0.5">
            <i class="pi pi-chart-line text-accent-foreground text-sm" aria-hidden="true"></i>
          </div>
          <div>
            <h3 class="font-semibold text-foreground mb-1">Insights</h3>
            <p class="text-sm text-foreground-muted">
              Analyze meetings to unlock personal communication insights and track your growth over time.
            </p>
            <button
              class="text-sm text-accent-foreground font-medium mt-2 inline-block hover:underline"
              aria-label="Go to meetings"
              (click)="goToMeetings()">
              Go to Meetings <i class="pi pi-arrow-right text-xs ml-1" aria-hidden="true"></i>
            </button>
          </div>
        </div>
      </div>
    }
  `,
})
export class InsightsWidgetComponent implements OnInit {
  protected readonly service = inject(InsightsSummaryService);
  private readonly router = inject(Router);

  readonly summary = this.service.summary;

  readonly sparklinePath = computed(() => {
    const values = this.summary()?.sparklineValues;
    if (!values || values.length < 2) return null;

    const max = Math.max(...values);
    const min = Math.min(...values);
    const range = max - min || 1;
    const step = 100 / (values.length - 1);

    return values
      .map((v, i) => {
        const x = Math.round(i * step);
        const y = Math.round(30 - ((v - min) / range) * 26 + 2);
        return `${i === 0 ? 'M' : 'L'}${x},${y}`;
      })
      .join(' ');
  });

  readonly sparklineFillPath = computed(() => {
    const linePath = this.sparklinePath();
    if (!linePath) return null;

    const values = this.summary()?.sparklineValues;
    if (!values || values.length < 2) return null;

    const step = 100 / (values.length - 1);
    const lastX = Math.round((values.length - 1) * step);

    return `${linePath} L${lastX},32 L0,32 Z`;
  });

  ngOnInit(): void {
    this.service.load();
  }

  protected absChange(change: number): string {
    return Math.abs(change).toFixed(1);
  }

  protected goToInsights(): void {
    this.router.navigate(['/insights']);
  }

  protected goToMeetings(): void {
    this.router.navigate(['/meetings']);
  }
}
