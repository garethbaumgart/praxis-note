import { Component, computed, input, ChangeDetectionStrategy, inject, PLATFORM_ID } from '@angular/core';
import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import { UIChart } from 'primeng/chart';
import { Tooltip } from 'primeng/tooltip';
import { TrendDataPoint } from './insights.model';
import { ThemeService } from '../shared/theme.service';

@Component({
  selector: 'app-insights-trend-chart',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [UIChart, Tooltip],
  template: `
    @if (compact()) {
      <div class="bg-surface-subtle border border-border rounded-xl p-4" [attr.aria-label]="title() + ' sparkline card'">
        <!-- Header: title + info + trend badge -->
        <div class="flex items-center justify-between mb-1">
          <div class="flex items-center gap-1.5">
            <h3 class="text-sm font-semibold text-foreground">{{ title() }}</h3>
            <i class="pi pi-info-circle text-foreground-muted text-xs cursor-help"
               [pTooltip]="infoText()"
               tooltipPosition="top"
               role="img"
               [attr.aria-label]="infoText()"></i>
          </div>
          @if (trendBadge(); as badge) {
            <span class="text-xs font-medium px-2 py-0.5 rounded-full"
                  [class]="badge.classes">
              {{ badge.icon }} {{ badge.label }}
            </span>
          }
        </div>
        <!-- Current value -->
        <div class="flex items-baseline gap-1.5 mb-2">
          <span class="text-xl font-bold" [style.color]="accentColor()">{{ currentValue() }}</span>
          <span class="text-xs text-foreground-muted">{{ valueSubtitle() }}</span>
        </div>
        <!-- Sparkline -->
        <p-chart [type]="chartType()" [data]="chartData()" [options]="chartOptions()" [height]="'64px'"
                 [attr.aria-label]="title() + ' sparkline'" role="img" />
      </div>
    } @else {
      <div class="bg-surface-subtle border border-border rounded-xl p-4">
        <div class="flex items-center gap-2 mb-3">
          <h3 class="text-sm font-semibold text-foreground">{{ title() }}</h3>
          <i class="pi pi-info-circle text-foreground-muted text-sm cursor-help"
             [pTooltip]="infoText()"
             tooltipPosition="top"
             role="img"
             [attr.aria-label]="infoText()"></i>
        </div>
        <p-chart [type]="chartType()" [data]="chartData()" [options]="chartOptions()" [height]="'250px'"
                 [attr.aria-label]="title() + ' chart'" role="img" />
      </div>
    }
  `,
})
export class InsightsTrendChartComponent {
  private readonly themeService = inject(ThemeService);
  private readonly document = inject(DOCUMENT);
  private readonly platformId = inject(PLATFORM_ID);

  readonly title = input.required<string>();
  readonly infoText = input.required<string>();
  readonly chartType = input<'line' | 'bar' | 'scatter' | 'bubble' | 'pie' | 'doughnut' | 'polarArea' | 'radar'>('line');
  readonly dataPoints = input.required<TrendDataPoint[]>();
  readonly colorVar = input<string>('--color-primary-solid');
  readonly fillColorVar = input<string>('--color-primary-bg');
  readonly compact = input<boolean>(false);

  readonly accentColor = computed(() => this.getThemeColor(this.colorVar()));

  readonly currentValue = computed(() => {
    const points = this.dataPoints();
    if (points.length === 0) return '-';
    const last = points[points.length - 1].value;
    // Format based on chart type/title
    if (this.title().toLowerCase().includes('%')) return `${Math.round(last)}%`;
    if (last < 1 && last > 0) return last.toFixed(2);
    return String(Math.round(last * 10) / 10);
  });

  readonly valueSubtitle = computed(() => {
    const title = this.title().toLowerCase();
    if (title.includes('per meeting') || title.includes('interruption') || title.includes('red flag')) return 'per meeting';
    if (title.includes('engagement')) return this.getEngagementLabel();
    return 'latest';
  });

  readonly trendBadge = computed(() => {
    const points = this.dataPoints();
    if (points.length < 2) return null;

    const recent = points[points.length - 1].value;
    const previous = points[points.length - 2].value;
    const diff = recent - previous;
    const percentChange = previous !== 0 ? Math.round(Math.abs(diff / previous) * 100) : (diff !== 0 ? 100 : 0);

    if (Math.abs(diff) < 0.01) {
      return { icon: '\u2192', label: 'stable', classes: 'bg-primary-bg text-primary-solid' };
    }

    const title = this.title().toLowerCase();
    const lowerIsBetter = title.includes('interruption') || title.includes('red flag');
    const isImproving = lowerIsBetter ? diff < 0 : diff > 0;

    if (isImproving) {
      return { icon: '\u2191', label: `${percentChange}%`, classes: 'bg-done-bg text-done-text' };
    }
    return { icon: '\u2193', label: `${percentChange}%`, classes: 'bg-danger-bg text-danger-base' };
  });

  readonly chartData = computed(() => {
    this.themeService.theme();

    const points = this.dataPoints();
    const type = this.chartType();
    const color = this.getThemeColor(this.colorVar());
    const fillColor = this.getThemeColor(this.fillColorVar());
    const isCompact = this.compact();

    const dataset: Record<string, unknown> = {
      data: points.map(p => p.value),
      borderColor: color,
      backgroundColor: fillColor,
      borderWidth: isCompact ? 1.5 : 2,
    };

    if (type === 'line') {
      dataset['fill'] = true;
      dataset['tension'] = 0.3;
      if (isCompact) {
        dataset['pointRadius'] = points.map((_, i) => i === points.length - 1 ? 3 : 0);
        dataset['pointHoverRadius'] = 4;
      } else {
        dataset['pointRadius'] = 3;
        dataset['pointHoverRadius'] = 5;
      }
      dataset['pointBackgroundColor'] = color;
    }

    if (type === 'bar' && isCompact) {
      dataset['borderRadius'] = 3;
      dataset['borderSkipped'] = false;
    }

    return {
      labels: points.map(p => this.formatDate(p.date)),
      datasets: [dataset],
    };
  });

  readonly chartOptions = computed(() => {
    this.themeService.theme();

    const isCompact = this.compact();

    if (isCompact) {
      return {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { display: false },
          tooltip: {
            enabled: true,
            callbacks: {
              title: (items: { dataIndex: number }[]) => {
                const points = this.dataPoints();
                const idx = items[0]?.dataIndex;
                return points[idx]?.label || this.formatDate(points[idx]?.date);
              },
            },
          },
        },
        scales: {
          x: { display: false },
          y: { display: false, beginAtZero: true },
        },
        elements: {
          point: { hoverRadius: 4 },
        },
      };
    }

    const textMuted = this.getThemeColor('--color-text-muted');
    const gridColor = this.getThemeColor('--color-border-muted');

    return {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: { display: false },
        tooltip: {
          callbacks: {
            title: (items: { dataIndex: number }[]) => {
              const points = this.dataPoints();
              const idx = items[0]?.dataIndex;
              return points[idx]?.label || this.formatDate(points[idx]?.date);
            },
          },
        },
      },
      scales: {
        x: {
          grid: { display: false },
          ticks: { font: { size: 10 }, color: textMuted },
        },
        y: {
          grid: { color: gridColor },
          ticks: { font: { size: 10 }, color: textMuted },
          beginAtZero: true,
        },
      },
    };
  });

  private getEngagementLabel(): string {
    const points = this.dataPoints();
    if (points.length === 0) return 'latest';
    const last = points[points.length - 1].value;
    if (last >= 2.5) return 'high';
    if (last >= 1.5) return 'medium';
    return 'low';
  }

  private getThemeColor(varName: string): string {
    if (!isPlatformBrowser(this.platformId)) return '#5e81ac';
    return getComputedStyle(this.document.documentElement).getPropertyValue(varName).trim() || '#5e81ac';
  }

  private formatDate(dateStr: string): string {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    const locale = typeof navigator !== 'undefined' && navigator.language ? navigator.language : undefined;
    return date.toLocaleDateString(locale, { month: 'short', day: 'numeric' });
  }
}
