import { Component, ChangeDetectionStrategy, inject, computed, effect } from '@angular/core';
import { Skeleton } from 'primeng/skeleton';
import { Tooltip } from 'primeng/tooltip';
import { JohariWindowService } from './johari-window.service';
import { InsightsService } from './insights.service';

@Component({
  selector: 'app-johari-window',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Skeleton, Tooltip],
  template: `
    <section class="mb-6">
      <h2 class="text-lg font-semibold text-foreground mb-3">Self-Awareness (Johari Window)</h2>

      @if (johariService.loading()) {
        <!-- Skeleton loading -->
        <div class="bg-surface-subtle border border-border rounded-xl p-6">
          <div class="grid grid-cols-2 gap-3 mb-4" style="height: 200px;">
            <p-skeleton width="100%" height="100%" />
            <p-skeleton width="100%" height="100%" />
            <p-skeleton width="100%" height="100%" />
            <p-skeleton width="100%" height="100%" />
          </div>
          <p-skeleton width="60%" height="14px" styleClass="mb-2" />
          <p-skeleton width="80%" height="12px" />
        </div>
      } @else if (johariService.error()) {
        <div class="bg-surface-subtle border border-border rounded-xl p-6 text-center">
          <i class="pi pi-exclamation-triangle text-3xl text-danger mb-2" aria-hidden="true"></i>
          <p class="text-sm text-danger">{{ johariService.error() }}</p>
        </div>
      } @else if (!johariService.johariWindow()?.hasEnoughData) {
        <!-- Empty state: not enough meetings with reflections -->
        <div class="bg-surface-subtle border border-border rounded-xl p-8 text-center">
          <div class="w-14 h-14 rounded-2xl bg-surface-muted flex items-center justify-center mx-auto mb-4">
            <i class="pi pi-th-large text-2xl text-foreground-muted" aria-hidden="true"></i>
          </div>
          <h3 class="text-base font-semibold text-foreground mb-2">Johari Window building...</h3>
          <p class="text-sm text-foreground-muted max-w-md mx-auto mb-4">
            Complete reflections on
            <strong class="text-foreground">{{ meetingsRemaining() }} more meeting{{ meetingsRemaining() === 1 ? '' : 's' }}</strong>
            to unlock your self-awareness profile.
            We need at least {{ johariService.johariWindow()?.minimumMeetings ?? 3 }} meetings with both analysis and reflection.
          </p>
          <div class="flex items-center justify-center gap-1.5" role="img" [attr.aria-label]="progressAriaLabel()">
            @for (dot of progressDots(); track $index) {
              <span class="w-3 h-3 rounded-full" [class.bg-accent-solid]="dot" [class.bg-border]="!dot"></span>
            }
            <span class="text-xs text-foreground-muted ml-2">
              {{ johariService.johariWindow()?.meetingCount ?? 0 }} of {{ johariService.johariWindow()?.minimumMeetings ?? 3 }} meetings
            </span>
          </div>
        </div>
      } @else {
        @let jw = johariService.johariWindow()!;
        <!-- Full Johari Window -->
        <div class="bg-surface-subtle border border-border rounded-xl overflow-hidden">
          <!-- Grid + Legend -->
          <div class="p-6">
            <!-- Trend banner -->
            @if (jw.openTrend !== null) {
              <div class="flex items-center gap-2 mb-4 text-sm">
                @if (jw.openTrend! > 0) {
                  <span class="inline-flex items-center gap-1 px-2.5 py-1 rounded-full bg-done text-done-foreground text-xs font-medium">
                    <i class="pi pi-arrow-up text-[10px]" aria-hidden="true"></i>
                    +{{ jw.openTrend }}% self-awareness growth
                  </span>
                } @else if (jw.openTrend! < 0) {
                  <span class="inline-flex items-center gap-1 px-2.5 py-1 rounded-full bg-inprogress text-inprogress-foreground text-xs font-medium">
                    <i class="pi pi-arrow-down text-[10px]" aria-hidden="true"></i>
                    {{ jw.openTrend }}% self-awareness change
                  </span>
                } @else {
                  <span class="inline-flex items-center gap-1 px-2.5 py-1 rounded-full bg-surface-muted text-foreground-muted text-xs font-medium">
                    <i class="pi pi-minus text-[10px]" aria-hidden="true"></i>
                    Stable self-awareness
                  </span>
                }
              </div>
            }

            <!-- 2x2 Grid -->
            <div class="grid gap-1.5 mb-5" role="img" [attr.aria-label]="gridAriaLabel()"
                 style="height: 160px;"
                 [style.grid-template-columns]="gridCols()"
                 [style.grid-template-rows]="gridRows()">

              <!-- Open (top-left) -->
              <div class="rounded-lg p-2 flex flex-col items-center justify-center text-center"
                   style="background: var(--color-done-bg); border: 1px solid var(--color-done-border, var(--color-done-bg));">
                <span class="text-xl font-bold" style="color: var(--color-done-text);">{{ jw.openPercentage }}%</span>
                <span class="text-[10px] font-medium mt-0.5" style="color: var(--color-done-text);">Open</span>
              </div>

              <!-- Blind Spot (top-right) -->
              <div class="rounded-lg p-2 flex flex-col items-center justify-center text-center"
                   style="background: var(--color-inprogress-bg); border: 1px solid var(--color-inprogress-border, var(--color-inprogress-bg));">
                <span class="text-base font-bold" style="color: var(--color-inprogress-text);">{{ jw.blindSpotPercentage }}%</span>
                <span class="text-[9px] font-medium mt-0.5" style="color: var(--color-inprogress-text);">Blind Spot</span>
              </div>

              <!-- Hidden (bottom-left) -->
              <div class="rounded-lg p-2 flex flex-col items-center justify-center text-center"
                   style="background: var(--color-primary-bg); border: 1px solid var(--color-primary-border, var(--color-primary-bg));">
                <span class="text-base font-bold" style="color: var(--color-primary-text);">{{ jw.hiddenPercentage }}%</span>
                <span class="text-[9px] font-medium mt-0.5" style="color: var(--color-primary-text);">Hidden</span>
              </div>

              <!-- Unknown (bottom-right) -->
              <div class="rounded-lg p-2 flex flex-col items-center justify-center text-center bg-surface-muted border border-border">
                <span class="text-base font-bold text-foreground-muted">{{ jw.unknownPercentage }}%</span>
                <span class="text-[9px] font-medium mt-0.5 text-foreground-muted">Unknown</span>
              </div>
            </div>

            <!-- Legend -->
            <div class="flex flex-wrap gap-3 text-xs text-foreground-muted">
              <span class="flex items-center gap-1.5">
                <span class="w-2.5 h-2.5 rounded-sm" style="background: var(--color-done-text);"></span> Open
              </span>
              <span class="flex items-center gap-1.5">
                <span class="w-2.5 h-2.5 rounded-sm" style="background: var(--color-inprogress-text);"></span> Blind Spot
              </span>
              <span class="flex items-center gap-1.5">
                <span class="w-2.5 h-2.5 rounded-sm" style="background: var(--color-primary-text);"></span> Hidden
              </span>
              <span class="flex items-center gap-1.5">
                <span class="w-2.5 h-2.5 rounded-sm bg-surface-muted border border-border"></span> Unknown
              </span>
            </div>
          </div>

          <!-- Dimensions breakdown -->
          @if (jw.dimensions.length > 0) {
            <div class="border-t border-border p-5">
              <h4 class="text-xs font-semibold text-foreground-muted uppercase tracking-wider mb-3">Dimension Breakdown</h4>
              <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
                @for (dim of jw.dimensions; track dim.name) {
                  <div class="flex items-start gap-3 p-3 rounded-lg bg-surface border border-border">
                    <span class="w-7 h-7 rounded-full flex items-center justify-center shrink-0 mt-0.5"
                          [class.bg-done]="dim.quadrant === 'Open'"
                          [class.bg-inprogress]="dim.quadrant === 'BlindSpot'"
                          [class.bg-accent]="dim.quadrant === 'Hidden' || dim.quadrant === 'Unknown'">
                      <i class="pi text-[11px]" aria-hidden="true"
                         [class.pi-check]="dim.quadrant === 'Open'"
                         [class.pi-eye]="dim.quadrant === 'BlindSpot'"
                         [class.pi-eye-slash]="dim.quadrant === 'Hidden' || dim.quadrant === 'Unknown'"
                         [class.text-done-foreground]="dim.quadrant === 'Open'"
                         [class.text-inprogress-foreground]="dim.quadrant === 'BlindSpot'"
                         [class.text-accent-foreground]="dim.quadrant === 'Hidden' || dim.quadrant === 'Unknown'"></i>
                    </span>
                    <div class="flex-1 min-w-0">
                      <div class="flex items-center justify-between mb-0.5">
                        <span class="text-sm font-medium text-foreground">{{ dim.name }}</span>
                        <span class="text-[10px] px-1.5 py-0.5 rounded-full font-medium"
                              [class.bg-done]="dim.quadrant === 'Open'"
                              [class.text-done-foreground]="dim.quadrant === 'Open'"
                              [class.bg-inprogress]="dim.quadrant === 'BlindSpot'"
                              [class.text-inprogress-foreground]="dim.quadrant === 'BlindSpot'"
                              [class.bg-accent]="dim.quadrant === 'Hidden' || dim.quadrant === 'Unknown'"
                              [class.text-accent-foreground]="dim.quadrant === 'Hidden' || dim.quadrant === 'Unknown'">
                          {{ dim.quadrant === 'BlindSpot' ? 'Blind Spot' : dim.quadrant }}
                        </span>
                      </div>
                      <div class="flex items-center gap-3 text-xs text-foreground-muted mb-1">
                        <span>You: <strong class="text-foreground">{{ dim.selfValue }}</strong></span>
                        <span>AI: <strong class="text-foreground">{{ dim.aiValue }}</strong></span>
                      </div>
                      @if (dim.explanation) {
                        <p class="text-[11px] text-foreground-muted leading-relaxed">{{ dim.explanation }}</p>
                      }
                    </div>
                  </div>
                }
              </div>
            </div>
          }

          <!-- Blind spot details -->
          @if (jw.blindSpots.length > 0) {
            <div class="border-t border-border p-5">
              <h4 class="text-xs font-semibold text-inprogress-foreground uppercase tracking-wider mb-3">
                <i class="pi pi-eye mr-1" aria-hidden="true"></i> Blind Spots to Explore
              </h4>
              <div class="space-y-2">
                @for (spot of jw.blindSpots; track spot.dimension) {
                  <div class="flex items-start gap-2 p-2.5 rounded-lg bg-inprogress/5 border border-inprogress/20">
                    <i class="pi pi-info-circle text-inprogress-foreground text-xs mt-0.5 shrink-0" aria-hidden="true"></i>
                    <div>
                      <p class="text-sm text-foreground">{{ spot.description }}</p>
                      <p class="text-[10px] text-foreground-muted mt-0.5">
                        Detected in {{ spot.meetingCount }} meeting{{ spot.meetingCount === 1 ? '' : 's' }}
                      </p>
                    </div>
                  </div>
                }
              </div>
            </div>
          }

          <!-- Footer -->
          <div class="border-t border-border px-5 py-3 flex items-center justify-between text-xs text-foreground-muted">
            <span>
              <i class="pi pi-chart-bar text-accent-foreground mr-1" aria-hidden="true"></i>
              Based on <strong class="text-foreground">{{ jw.meetingCount }}</strong>
              meeting{{ jw.meetingCount === 1 ? '' : 's' }} with reflections
            </span>
            <span pTooltip="Compares your self-assessment against AI behavioral analysis across talk time, engagement, tone, and interruptions."
                  tooltipPosition="left" class="cursor-help">
              <i class="pi pi-question-circle" aria-hidden="true"></i> How it works
            </span>
          </div>
        </div>
      }
    </section>
  `,
})
export class JohariWindowComponent {
  /** Minimum fr value for grid axes — prevents tiny quadrants from becoming unreadable */
  private static readonly MIN_FR = 25;

  protected readonly johariService = inject(JohariWindowService);
  private readonly insightsService = inject(InsightsService);

  protected readonly meetingsRemaining = computed(() => {
    const data = this.johariService.johariWindow();
    if (!data) return 3;
    return Math.max(0, data.minimumMeetings - data.meetingCount);
  });

  protected readonly progressDots = computed(() => {
    const data = this.johariService.johariWindow();
    if (!data) return [false, false, false];
    return Array.from({ length: data.minimumMeetings }, (_, i) => i < data.meetingCount);
  });

  protected readonly progressAriaLabel = computed(() => {
    const data = this.johariService.johariWindow();
    if (!data) return 'Progress: 0 of 3 meetings with reflections';
    return `Progress: ${data.meetingCount} of ${data.minimumMeetings} meetings with reflections`;
  });

  protected readonly gridAriaLabel = computed(() => {
    const data = this.johariService.johariWindow();
    if (!data?.hasEnoughData) return 'Johari Window grid';
    return `Johari Window: Open ${data.openPercentage}%, Blind Spot ${data.blindSpotPercentage}%, Hidden ${data.hiddenPercentage}%, Unknown ${data.unknownPercentage}%`;
  });

  // Proportional grid sizing — MIN_FR ensures small quadrants remain readable
  protected readonly gridCols = computed(() => {
    const data = this.johariService.johariWindow();
    if (!data?.hasEnoughData) return '1fr 1fr';
    const left = Math.max(JohariWindowComponent.MIN_FR, data.openPercentage + data.hiddenPercentage);
    const right = Math.max(JohariWindowComponent.MIN_FR, data.blindSpotPercentage + data.unknownPercentage);
    return `${left}fr ${right}fr`;
  });

  protected readonly gridRows = computed(() => {
    const data = this.johariService.johariWindow();
    if (!data?.hasEnoughData) return '1fr 1fr';
    const top = Math.max(JohariWindowComponent.MIN_FR, data.openPercentage + data.blindSpotPercentage);
    const bottom = Math.max(JohariWindowComponent.MIN_FR, data.hiddenPercentage + data.unknownPercentage);
    return `${top}fr ${bottom}fr`;
  });

  constructor() {
    // Reload when date range changes
    effect(() => {
      const range = this.insightsService.dateRange();
      this.johariService.loadJohariWindow(range);
    });
  }
}
