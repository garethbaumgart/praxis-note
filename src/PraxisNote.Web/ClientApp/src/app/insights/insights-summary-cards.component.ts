import { Component, computed, input, ChangeDetectionStrategy } from '@angular/core';
import { Tooltip } from 'primeng/tooltip';
import { TrendSummary } from './insights.model';

interface MetricCard {
  label: string;
  value: string;
  change: number;
  changeLabel: string;
  infoText: string;
  invertTrend?: boolean;
}

@Component({
  selector: 'app-insights-summary-cards',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Tooltip],
  template: `
    <div class="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-4">
      @for (card of cards(); track card.label) {
        <div class="bg-surface-subtle border border-border rounded-xl p-4">
          <div class="flex items-center gap-1.5 mb-1">
            <p class="text-xs text-foreground-muted font-medium uppercase tracking-wide">{{ card.label }}</p>
            <i class="pi pi-info-circle text-foreground-muted text-xs cursor-help"
               [pTooltip]="card.infoText"
               tooltipPosition="top"
               role="img"
               [attr.aria-label]="card.infoText"></i>
          </div>
          <div class="flex items-baseline gap-2 mt-1">
            <span class="text-2xl font-bold text-foreground">{{ card.value }}</span>
            @if (card.change !== 0) {
              <span class="text-sm font-medium flex items-center gap-0.5"
                    [class.text-done-foreground]="isTrendPositive(card)"
                    [class.text-danger]="!isTrendPositive(card)">
                @if (card.change > 0) {
                  <i class="pi pi-arrow-up text-xs"></i>
                } @else {
                  <i class="pi pi-arrow-down text-xs"></i>
                }
                {{ formatChange(card.change) }}
              </span>
            }
          </div>
          <p class="text-xs text-foreground-muted mt-1">{{ card.changeLabel }}</p>
        </div>
      }
    </div>
  `,
})
export class InsightsSummaryCardsComponent {
  readonly summary = input.required<TrendSummary>();

  protected readonly cards = computed<MetricCard[]>(() => {
    const s = this.summary();
    return [
      {
        label: 'Talk Time',
        value: `${s.averageTalkTimePercent}%`,
        change: s.talkTimeChange,
        changeLabel: 'vs previous period',
        infoText: 'Percentage of meeting time you spent speaking. Aim for 30-50% in group meetings to ensure balanced participation.',
      },
      {
        label: 'Question Ratio',
        value: s.averageQuestionRatio.toFixed(2),
        change: s.questionRatioChange,
        changeLabel: 'questions per statement',
        infoText: 'Ratio of questions asked to statements made. Higher values indicate more curiosity and engagement. A ratio of 0.3-0.5 suggests active listening.',
      },
      {
        label: 'Sentiment',
        value: s.averageSentimentScore.toFixed(2),
        change: s.sentimentChange,
        changeLabel: 'average score (0-1)',
        infoText: 'Average emotional tone across meetings on a 0-1 scale. Values above 0.6 indicate generally positive communication. Scores below 0.4 may suggest tension.',
      },
      {
        label: 'Red Flags',
        value: `${s.totalRedFlags}`,
        change: s.redFlagChange,
        changeLabel: 'this period',
        infoText: 'Count of detected communication red flags (evasive language, hedging, defensiveness, inconsistency). Fewer is better — a downward trend indicates improving directness.',
        invertTrend: true,
      },
      {
        label: 'Engagement',
        value: s.dominantEngagementLevel,
        change: 0,
        changeLabel: 'dominant level',
        infoText: 'Your most common engagement level across meetings: high (active contributor), medium (participates when prompted), or low (mostly observing).',
      },
    ];
  });

  protected isTrendPositive(card: MetricCard): boolean {
    if (card.invertTrend) {
      return card.change < 0;
    }
    return card.change > 0;
  }

  protected formatChange(change: number): string {
    return `${Math.abs(change)}%`;
  }
}
