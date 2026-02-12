import { Component, ChangeDetectionStrategy, input, output, computed } from '@angular/core';
import { BlindSpotNudge } from './insights.model';

@Component({
  selector: 'app-nudge-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="bg-surface-subtle border border-border rounded-xl p-4 flex flex-col gap-3">
      <!-- Header: Avatar + Dimension -->
      <div class="flex items-center gap-3">
        <div class="w-9 h-9 rounded-full bg-accent-bg flex items-center justify-center shrink-0">
          <i class="pi pi-sparkles text-sm text-accent-foreground"></i>
        </div>
        <span class="text-xs font-semibold uppercase tracking-wide text-inprogress-foreground">
          {{ nudge().dimension }}
        </span>
      </div>

      <!-- Blind spot description (quote) -->
      <p class="text-sm italic text-foreground-muted">
        "{{ nudge().blindSpotDescription }}"
      </p>

      <!-- Actionable suggestion -->
      <p class="text-sm text-foreground">
        {{ nudge().suggestion }}
      </p>

      <!-- Footer: actions -->
      <div class="flex items-center justify-between mt-1">
        <button type="button"
                class="text-xs text-foreground-muted hover:text-foreground transition-colors"
                (click)="onDismiss.emit(nudge().id)"
                aria-label="Dismiss nudge for {{ nudge().dimension }}">
          Dismiss
        </button>
        <button type="button"
                class="px-3 py-1.5 text-xs font-medium text-white bg-accent-solid rounded-md hover:opacity-90 transition-opacity"
                (click)="onAccept.emit(nudge().id)"
                aria-label="Set goal from {{ nudge().dimension }} nudge">
          Set Goal
        </button>
      </div>
    </div>
  `,
})
export class NudgeCardComponent {
  readonly nudge = input.required<BlindSpotNudge>();
  readonly onDismiss = output<string>();
  readonly onAccept = output<string>();

  readonly dimensionIcon = computed(() => {
    const icons: Record<string, string> = {
      'Talk Time': 'pi pi-chart-pie',
      Engagement: 'pi pi-users',
      Tone: 'pi pi-comments',
      Interruptions: 'pi pi-ban',
    };
    return icons[this.nudge().dimension] ?? 'pi pi-lightbulb';
  });
}
