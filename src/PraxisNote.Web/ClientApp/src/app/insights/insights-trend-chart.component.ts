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
    <div class="bg-surface-subtle border border-border rounded-xl p-4">
      <div class="flex items-center gap-2 mb-3">
        <h3 class="text-sm font-semibold text-foreground">{{ title() }}</h3>
        <i class="pi pi-info-circle text-foreground-muted text-sm cursor-help"
           [pTooltip]="infoText()"
           tooltipPosition="top"
           role="img"
           [attr.aria-label]="infoText()"></i>
      </div>
      <p-chart [type]="chartType()" [data]="chartData()" [options]="chartOptions()" [height]="'250px'" />
    </div>
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

  readonly chartData = computed(() => {
    // Access theme signal so chart recomputes on theme change
    this.themeService.theme();

    const points = this.dataPoints();
    const type = this.chartType();
    const color = this.getThemeColor(this.colorVar());
    const fillColor = this.getThemeColor(this.fillColorVar());

    const dataset: Record<string, unknown> = {
      data: points.map(p => p.value),
      borderColor: color,
      backgroundColor: fillColor,
      borderWidth: 2,
    };

    if (type === 'line') {
      dataset['fill'] = true;
      dataset['tension'] = 0.3;
      dataset['pointRadius'] = 3;
      dataset['pointHoverRadius'] = 5;
      dataset['pointBackgroundColor'] = color;
    }

    return {
      labels: points.map(p => this.formatDate(p.date)),
      datasets: [dataset],
    };
  });

  readonly chartOptions = computed(() => {
    this.themeService.theme();

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
