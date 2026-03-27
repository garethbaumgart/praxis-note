import { Component, ChangeDetectionStrategy, input, output, signal, inject } from '@angular/core';
import { AiKeyDto, AiProvider } from './ai-key-provider.model';
import { AiKeyProviderService } from './ai-key-provider.service';

interface ProviderMeta {
  label: string;
  colorClass: string;
  placeholder: string;
  keyUrl: string;
  keyUrlLabel: string;
  freeTier?: boolean;
}

const PROVIDER_META: Record<AiProvider, ProviderMeta> = {
  Anthropic: {
    label: 'Anthropic',
    colorClass: 'text-[#d97757]',
    placeholder: 'sk-ant-...',
    keyUrl: 'https://console.anthropic.com/settings/keys',
    keyUrlLabel: 'Get key from console.anthropic.com',
  },
  OpenAI: {
    label: 'OpenAI',
    colorClass: 'text-[#10a37f]',
    placeholder: 'sk-...',
    keyUrl: 'https://platform.openai.com/api-keys',
    keyUrlLabel: 'Get key from platform.openai.com',
  },
  Gemini: {
    label: 'Google Gemini',
    colorClass: 'text-[#4285f4]',
    placeholder: 'AIza...',
    keyUrl: 'https://aistudio.google.com/apikey',
    keyUrlLabel: 'Get key from aistudio.google.com',
    freeTier: true,
  },
};

@Component({
  selector: 'app-ai-key-provider-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="border border-border rounded-lg overflow-hidden">
      <!-- Collapsed row -->
      <button
        type="button"
        class="w-full flex items-center gap-3 px-4 py-3 hover:bg-surface-subtle transition-colors text-left"
        (click)="toggleDrawer()"
        [attr.aria-expanded]="drawerOpen()"
        [attr.aria-label]="'Configure ' + meta.label + ' API key'"
      >
        <span class="text-lg font-semibold" [class]="meta.colorClass" aria-hidden="true">
          @switch (provider()) {
            @case ('Anthropic') { A }
            @case ('OpenAI') { O }
            @case ('Gemini') { G }
          }
        </span>
        <span class="flex-1 text-sm font-medium text-foreground">{{ meta.label }}</span>

        @if (key()?.hasKey) {
          <span class="flex items-center gap-1.5 text-xs text-done-foreground bg-done/30 px-2 py-0.5 rounded-full">
            <i class="pi pi-check-circle text-[10px]" aria-hidden="true"></i>
            Connected
          </span>
        }
        @if (provider() === 'Gemini' && key()?.hasKey && meta.freeTier) {
          <span class="text-xs text-foreground-muted bg-surface-muted px-2 py-0.5 rounded-full">Free tier</span>
        }

        <i class="pi text-xs text-foreground-muted" [class.pi-chevron-down]="!drawerOpen()" [class.pi-chevron-up]="drawerOpen()" aria-hidden="true"></i>
      </button>

      <!-- Expandable drawer -->
      @if (drawerOpen()) {
        <div class="border-t border-border px-4 py-4 space-y-3 bg-surface-subtle">
          @if (key()?.hasKey) {
            <!-- Connected state -->
            <div class="flex items-center justify-between">
              <div class="text-sm">
                <span class="text-foreground-muted">Key:</span>
                <code class="ml-1 text-xs text-foreground font-mono bg-surface-muted px-1.5 py-0.5 rounded">{{ key()!.keyHint }}</code>
              </div>
              <button
                type="button"
                class="p-2 text-foreground-muted hover:text-danger transition-colors rounded-md"
                (click)="confirmRemove()"
                aria-label="Remove API key"
              >
                <i class="pi pi-trash text-sm" aria-hidden="true"></i>
              </button>
            </div>
            @if (key()!.preferredModel) {
              <p class="text-xs text-foreground-muted">Model: {{ key()!.preferredModel }}</p>
            }
          } @else {
            <!-- Input state -->
            <div>
              <label [for]="'ai-key-' + provider()" class="block text-sm font-medium text-foreground mb-1">API Key</label>
              <input
                [id]="'ai-key-' + provider()"
                type="password"
                class="w-full px-3 py-2 text-sm border border-border rounded-lg bg-surface text-foreground placeholder:text-foreground-muted focus:outline-none focus:ring-2 focus:ring-accent/50 font-mono"
                [placeholder]="meta.placeholder"
                [value]="inputKey()"
                (input)="inputKey.set($any($event.target).value)"
                (keydown.enter)="validateAndSave()"
              />
            </div>

            <a
              [href]="meta.keyUrl"
              target="_blank"
              rel="noopener noreferrer"
              class="inline-flex items-center gap-1 text-xs text-accent-solid hover:underline"
            >
              <i class="pi pi-external-link text-[10px]" aria-hidden="true"></i>
              {{ meta.keyUrlLabel }}
            </a>

            @if (validationError()) {
              <div class="py-2 px-3 bg-danger/10 border border-danger/30 rounded-lg">
                <p class="text-xs text-danger">{{ validationError() }}</p>
              </div>
            }

            @if (rateLimitWarning()) {
              <div class="py-2 px-3 bg-accent/20 border border-accent/30 rounded-lg">
                <p class="text-xs text-foreground">Key saved but validation was rate-limited. It will be verified on first use.</p>
              </div>
            }

            <div class="flex justify-end">
              <button
                type="button"
                class="px-4 py-1.5 text-sm text-white bg-accent-solid hover:opacity-90 rounded-md transition-opacity disabled:opacity-50"
                [disabled]="!inputKey().trim() || aiKeyService.saving()"
                (click)="validateAndSave()"
              >
                @if (aiKeyService.saving()) {
                  <i class="pi pi-spin pi-spinner text-xs mr-1" aria-hidden="true"></i>
                }
                Validate & Save
              </button>
            </div>
          }
        </div>
      }
    </div>
  `,
})
export class AiKeyProviderCardComponent {
  readonly provider = input.required<AiProvider>();
  readonly key = input<AiKeyDto | null>(null);
  readonly onRemove = output<AiProvider>();
  readonly onSaved = output<void>();

  readonly aiKeyService = inject(AiKeyProviderService);

  readonly drawerOpen = signal(false);
  readonly inputKey = signal('');
  readonly validationError = signal<string | null>(null);
  readonly rateLimitWarning = signal(false);

  get meta(): ProviderMeta {
    return PROVIDER_META[this.provider()];
  }

  toggleDrawer(): void {
    this.drawerOpen.update(v => !v);
    if (this.drawerOpen()) {
      this.inputKey.set('');
      this.validationError.set(null);
      this.rateLimitWarning.set(false);
    }
  }

  async validateAndSave(): Promise<void> {
    if (this.aiKeyService.saving()) return;
    const key = this.inputKey().trim();
    if (!key) return;

    this.validationError.set(null);
    this.rateLimitWarning.set(false);

    try {
      const result = await this.aiKeyService.upsertKey(this.provider(), key);
      if (result?.rateLimited) {
        this.rateLimitWarning.set(true);
      }
      this.inputKey.set('');
      this.onSaved.emit();
    } catch (err) {
      this.validationError.set(typeof err === 'string' ? err : 'Invalid API key');
    }
  }

  confirmRemove(): void {
    this.onRemove.emit(this.provider());
  }
}
