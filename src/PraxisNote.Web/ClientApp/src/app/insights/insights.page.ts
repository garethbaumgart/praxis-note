import { Component, inject, OnInit, OnDestroy, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SelectButton } from 'primeng/selectbutton';
import { Skeleton } from 'primeng/skeleton';
import { InsightsService } from './insights.service';
import { InsightsSummaryCardsComponent } from './insights-summary-cards.component';
import { InsightsTrendChartComponent } from './insights-trend-chart.component';
import { GoalsSectionComponent } from './goals-section.component';
import { CommunicationProfileComponent } from './communication-profile.component';
import { JohariWindowComponent } from './johari-window.component';
import { DateRange } from './insights.model';
import { ContextualHeaderService } from '../shared/services/contextual-header.service';

@Component({
  selector: 'app-insights-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, SelectButton, Skeleton, InsightsSummaryCardsComponent, InsightsTrendChartComponent, GoalsSectionComponent, CommunicationProfileComponent, JohariWindowComponent],
  template: `
    <div class="max-w-7xl mx-auto px-4 md:px-6 py-6 md:py-8">
      <h1 class="sr-only">Insights</h1>
      <!-- Date range selector -->
      <div class="flex items-center justify-end gap-2 mb-6">
        <p-selectButton
            [options]="dateRangeOptions"
            [ngModel]="insightsService.dateRange()"
            (ngModelChange)="onDateRangeChange($event)"
            optionLabel="label"
            optionValue="value"
            [allowEmpty]="false"
            size="small" />
      </div>

      @if (insightsService.loading()) {
        <!-- Skeleton loading state -->
        <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-4 mb-6">
          @for (i of skeletonCards; track i) {
            <div class="bg-surface-subtle border border-border rounded-xl p-4">
              <p-skeleton width="60%" height="12px" styleClass="mb-3" />
              <p-skeleton width="40%" height="28px" styleClass="mb-2" />
              <p-skeleton width="80%" height="10px" />
            </div>
          }
        </div>
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
          @for (i of skeletonCharts; track i) {
            <div class="bg-surface-subtle border border-border rounded-xl p-4">
              <p-skeleton width="50%" height="12px" styleClass="mb-2" />
              <p-skeleton width="30%" height="24px" styleClass="mb-3" />
              <p-skeleton width="100%" height="64px" />
            </div>
          }
        </div>
      } @else if (insightsService.error()) {
        <div class="bg-surface-subtle border border-border rounded-xl p-8 text-center">
          <i class="pi pi-exclamation-triangle text-4xl text-danger mb-3"></i>
          <p class="text-danger font-medium">{{ insightsService.error() }}</p>
          <button class="mt-4 px-4 py-2 bg-accent-solid text-white rounded-lg text-sm font-medium hover:opacity-90 transition"
                  (click)="insightsService.loadTrends()">
            Try Again
          </button>
        </div>
      } @else if (!insightsService.trends() || insightsService.trends()!.meetingCount === 0) {
        <!-- Empty state -->
        <div class="bg-surface-subtle border border-border rounded-xl p-12 text-center mb-6">
          <i class="pi pi-chart-line text-5xl text-foreground-muted mb-4"></i>
          <h2 class="text-lg font-semibold text-foreground mb-2">No insights yet</h2>
          <p class="text-foreground-muted text-sm max-w-md mx-auto">
            Insights appear after you analyze meetings with behavioral analysis. Record and analyze a few meetings to start seeing your communication trends.
          </p>
        </div>

        <!-- Communication Profile (always visible) -->
        <app-communication-profile />

        <!-- Johari Window (always visible) -->
        <app-johari-window />

        <!-- Goals Section (always visible) -->
        <app-goals-section />
      } @else {
        <!-- Summary Cards -->
        <app-insights-summary-cards [summary]="insightsService.trends()!.summary" />

        <!-- Communication Profile -->
        <div class="mt-6">
          <app-communication-profile />
        </div>

        <!-- Johari Window -->
        <div class="mt-6">
          <app-johari-window />
        </div>

        <!-- Goals Section -->
        <div class="mt-6">
          <app-goals-section />
        </div>

        <!-- Participant info -->
        @if (insightsService.trends()!.availableParticipants.length > 1) {
          <p class="text-xs text-foreground-muted mt-4">
            Showing data for <strong class="text-foreground">{{ insightsService.trends()!.participantName }}</strong>
            across {{ insightsService.trends()!.meetingCount }} meetings
          </p>
        } @else {
          <p class="text-xs text-foreground-muted mt-4">
            {{ insightsService.trends()!.meetingCount }} meetings analyzed
          </p>
        }

        <!-- Charts - compact 2-column grid -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-3 mt-6">
          <app-insights-trend-chart
            title="Talk-Time %"
            infoText="Percentage of meeting time you spent speaking per meeting. A consistent range of 30-50% in group settings indicates balanced participation."
            [compact]="true"
            [dataPoints]="insightsService.trends()!.talkTimeTrend.dataPoints"
            colorVar="--color-primary-solid"
            fillColorVar="--color-primary-bg" />

          <app-insights-trend-chart
            title="Question Ratio"
            infoText="Ratio of questions you asked to statements you made in each meeting. Higher values indicate more curiosity and collaborative exploration."
            [compact]="true"
            [dataPoints]="insightsService.trends()!.questionRatioTrend.dataPoints"
            colorVar="--color-done-text"
            fillColorVar="--color-done-bg" />

          <app-insights-trend-chart
            title="Interruptions"
            infoText="Number of times you interrupted others per meeting. Fewer interruptions generally indicate better listening skills. Some contexts (brainstorming) may warrant more."
            chartType="bar"
            [compact]="true"
            [dataPoints]="insightsService.trends()!.interruptionTrend.dataPoints"
            colorVar="--color-inprogress-text"
            fillColorVar="--color-inprogress-bg" />

          <app-insights-trend-chart
            title="Sentiment"
            infoText="Your communication sentiment score (0-1) per meeting. Higher values indicate more positive tone. Consistent scores above 0.6 suggest constructive communication."
            [compact]="true"
            [dataPoints]="insightsService.trends()!.sentimentTrend.dataPoints"
            colorVar="--color-done-text"
            fillColorVar="--color-done-bg" />

          <app-insights-trend-chart
            title="Red Flags"
            infoText="Number of detected communication red flags (evasive language, hedging, defensiveness, inconsistency) per meeting. A downward trend indicates improving directness."
            chartType="bar"
            [compact]="true"
            [dataPoints]="insightsService.trends()!.redFlagTrend.totalByMeeting"
            colorVar="--color-danger-base"
            fillColorVar="--color-danger-base" />

          <app-insights-trend-chart
            title="Engagement"
            infoText="Your engagement level per meeting: 3 = high (active contributor), 2 = medium (participates when prompted), 1 = low (mostly observing). Higher is generally better."
            [compact]="true"
            [dataPoints]="insightsService.trends()!.engagementTrend.dataPoints"
            colorVar="--color-primary-solid"
            fillColorVar="--color-primary-bg" />
        </div>
      }
    </div>
  `,
})
export class InsightsPage implements OnInit, OnDestroy {
  protected readonly insightsService = inject(InsightsService);
  private readonly headerService = inject(ContextualHeaderService);

  protected readonly dateRangeOptions = [
    { label: '7d', value: '7d' },
    { label: '30d', value: '30d' },
    { label: '90d', value: '90d' },
    { label: 'All', value: 'all' },
  ];

  protected readonly skeletonCards = [0, 1, 2, 3, 4];
  protected readonly skeletonCharts = [0, 1, 2, 3, 4, 5];

  ngOnInit(): void {
    this.headerService.breadcrumb.set([{ label: 'Insights' }]);
    this.insightsService.loadTrends();
  }

  ngOnDestroy(): void {
    this.headerService.clearContext();
  }

  protected onDateRangeChange(range: DateRange): void {
    this.insightsService.setDateRange(range);
  }
}
