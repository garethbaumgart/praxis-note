import { Component, ChangeDetectionStrategy, input, output, computed } from '@angular/core';
import { RouterLink } from '@angular/router';

export type AiErrorCode = 'no_ai_key' | 'ai_key_invalid' | 'ai_rate_limited' | 'ai_provider_error';

export interface AiErrorState {
  code: AiErrorCode;
  message: string;
  settingsUrl?: string;
  retryAfterSeconds?: number;
}

@Component({
  selector: 'app-ai-error-banner',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink],
  template: `
    @if (error()) {
      <div [class]="bannerClass()">
        <i [class]="iconClass()" aria-hidden="true"></i>
        <div class="flex-1 min-w-0">
          <p class="text-sm font-medium">{{ error()!.message }}</p>
          @if (error()!.settingsUrl && isPersistent()) {
            <a [routerLink]="error()!.settingsUrl" class="text-xs underline mt-1 block">
              Go to Settings → AI Keys
            </a>
          }
        </div>
        <button type="button" class="text-current opacity-50 hover:opacity-100 flex-shrink-0"
                (click)="onDismiss.emit()" aria-label="Dismiss">
          <i class="pi pi-times text-xs"></i>
        </button>
      </div>
    }
  `,
})
export class AiErrorBannerComponent {
  readonly error = input<AiErrorState | null>(null);
  readonly onDismiss = output<void>();

  protected readonly isPersistent = computed(() => {
    const code = this.error()?.code;
    return code === 'no_ai_key' || code === 'ai_key_invalid';
  });

  protected readonly bannerClass = computed(() => {
    const code = this.error()?.code;
    switch (code) {
      case 'no_ai_key':
      case 'ai_key_invalid':
        return 'rounded-lg px-4 py-3 flex items-start gap-3 bg-warning-bg text-warning';
      case 'ai_rate_limited':
        return 'rounded-lg px-4 py-3 flex items-start gap-3 bg-info-bg text-info';
      case 'ai_provider_error':
        return 'rounded-lg px-4 py-3 flex items-start gap-3 bg-danger-bg text-danger';
      default:
        return 'rounded-lg px-4 py-3 flex items-start gap-3 bg-danger-bg text-danger';
    }
  });

  protected readonly iconClass = computed(() => {
    const code = this.error()?.code;
    switch (code) {
      case 'no_ai_key':
        return 'pi pi-key flex-shrink-0 mt-0.5 text-sm';
      case 'ai_key_invalid':
        return 'pi pi-shield flex-shrink-0 mt-0.5 text-sm';
      case 'ai_rate_limited':
        return 'pi pi-clock flex-shrink-0 mt-0.5 text-sm';
      case 'ai_provider_error':
        return 'pi pi-wifi flex-shrink-0 mt-0.5 text-sm';
      default:
        return 'pi pi-exclamation-triangle flex-shrink-0 mt-0.5 text-sm';
    }
  });
}
