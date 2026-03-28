import { Component, ChangeDetectionStrategy, input, output, inject, signal, computed } from '@angular/core';
import { AiKeyDto, AiProvider } from './ai-key-provider.model';
import { AiKeyProviderService } from './ai-key-provider.service';
import { AI_MODEL_CATALOGUE, AiModelOption, AiModelTag } from './ai-model-catalogue';

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

const TAG_STYLES: Record<AiModelTag, { label: string; class: string }> = {
  fast: { label: 'Fast', class: 'bg-done/20 text-done-foreground' },
  balanced: { label: 'Balanced', class: 'bg-accent/20 text-accent-solid' },
  powerful: { label: 'Powerful', class: 'bg-in-progress/20 text-in-progress-foreground' },
  cheap: { label: 'Cheap', class: 'bg-surface-muted text-foreground-muted' },
  'free-tier': { label: 'Free tier', class: 'bg-surface-muted text-foreground-muted' },
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
        [attr.aria-expanded]="isOpen()"
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

        <i class="pi text-xs text-foreground-muted" [class.pi-chevron-down]="!isOpen()" [class.pi-chevron-up]="isOpen()" aria-hidden="true"></i>
      </button>

      <!-- Expandable drawer -->
      @if (isOpen()) {
        <div class="border-t border-border px-4 py-4 space-y-3 bg-surface-subtle">
          @if (key()?.hasKey && !replacing()) {
            <!-- Connected state -->
            <div class="flex items-center justify-between">
              <div class="text-sm">
                <span class="text-foreground-muted">Key:</span>
                <code class="ml-1 text-xs text-foreground font-mono bg-surface-muted px-1.5 py-0.5 rounded">{{ key()!.keyHint }}</code>
              </div>
              <div class="flex items-center gap-1">
                <button
                  type="button"
                  class="p-2 text-foreground-muted hover:text-foreground transition-colors rounded-md"
                  (click)="replacing.set(true)"
                  aria-label="Replace API key"
                >
                  <i class="pi pi-pencil text-sm" aria-hidden="true"></i>
                </button>
                @if (!confirmingRemove()) {
                  <button
                    type="button"
                    class="p-2 text-foreground-muted hover:text-danger transition-colors rounded-md"
                    (click)="confirmingRemove.set(true)"
                    aria-label="Remove API key"
                  >
                    <i class="pi pi-trash text-sm" aria-hidden="true"></i>
                  </button>
                } @else {
                  <button
                    type="button"
                    class="px-2 py-1 text-xs text-danger hover:bg-danger/10 rounded transition-colors"
                    (click)="doRemove()"
                    aria-label="Confirm remove API key"
                  >
                    Remove?
                  </button>
                  <button
                    type="button"
                    class="px-2 py-1 text-xs text-foreground-muted hover:text-foreground rounded transition-colors"
                    (click)="confirmingRemove.set(false)"
                    aria-label="Cancel remove"
                  >
                    Cancel
                  </button>
                }
              </div>
            </div>

            <!-- Model selector -->
            @if (modelOptions().length > 0) {
              <div class="pt-2 border-t border-border">
                <p [id]="'preferred-model-label-' + provider()" class="text-xs font-medium text-foreground-muted mb-2">Preferred model</p>
                <div class="space-y-1.5" role="radiogroup" [attr.aria-labelledby]="'preferred-model-label-' + provider()">
                  @for (model of modelOptions(); track model.value) {
                    <button
                      type="button"
                      role="radio"
                      [attr.aria-checked]="selectedModel() === model.value"
                      [attr.tabindex]="selectedModel() === model.value ? 0 : -1"
                      class="w-full flex items-center gap-3 px-3 py-2 rounded-lg border transition-colors text-left"
                      [class.border-accent-solid]="selectedModel() === model.value"
                      [class.bg-accent/10]="selectedModel() === model.value"
                      [class.border-border]="selectedModel() !== model.value"
                      [class.hover:border-foreground-muted]="selectedModel() !== model.value"
                      [disabled]="savingModel() === model.value"
                      (click)="selectModel(model.value)"
                      (keydown)="handleModelKeyDown($event, $index)"
                    >
                      <span class="flex-1 min-w-0">
                        <span class="text-sm font-medium text-foreground">{{ model.label }}</span>
                        <span class="block text-xs text-foreground-muted">{{ model.description }}</span>
                      </span>
                      <span class="flex items-center gap-1 shrink-0">
                        @for (tag of model.tags; track tag) {
                          <span class="text-[10px] px-1.5 py-0.5 rounded-full" [class]="tagClass(tag)">{{ tagLabel(tag) }}</span>
                        }
                      </span>
                      @if (savingModel() === model.value) {
                        <span role="status" aria-label="Saving model">
                          <i class="pi pi-spin pi-spinner text-xs text-accent-solid" aria-hidden="true"></i>
                          <span class="sr-only">Saving model...</span>
                        </span>
                      } @else if (selectedModel() === model.value) {
                        <i class="pi pi-check text-xs text-accent-solid" aria-hidden="true"></i>
                      }
                    </button>
                  }
                </div>
              </div>
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
  readonly isOpen = input(false);
  readonly onRemove = output<AiProvider>();
  readonly onSaved = output<void>();
  readonly onToggle = output<AiProvider>();

  readonly aiKeyService = inject(AiKeyProviderService);

  readonly inputKey = signal('');
  readonly validationError = signal<string | null>(null);
  readonly rateLimitWarning = signal(false);
  readonly replacing = signal(false);
  readonly confirmingRemove = signal(false);
  readonly savingModel = signal<string | null>(null);
  readonly pendingModel = signal<string | null>(null);

  readonly modelOptions = computed<AiModelOption[]>(() => AI_MODEL_CATALOGUE[this.provider()] ?? []);

  readonly selectedModel = computed(() => {
    const pending = this.pendingModel();
    if (pending) return pending;
    const options = this.modelOptions();
    if (!options.length) return '';
    const current = this.key()?.preferredModel;
    if (current && options.some(m => m.value === current)) return current;
    return options.find(m => m.isDefault)?.value ?? options[0].value;
  });

  get meta(): ProviderMeta {
    return PROVIDER_META[this.provider()];
  }

  tagClass(tag: AiModelTag): string {
    return TAG_STYLES[tag]?.class ?? 'bg-surface-muted text-foreground-muted';
  }

  tagLabel(tag: AiModelTag): string {
    return TAG_STYLES[tag]?.label ?? tag;
  }

  toggleDrawer(): void {
    if (!this.isOpen()) {
      this.inputKey.set('');
      this.validationError.set(null);
      this.rateLimitWarning.set(false);
      this.replacing.set(false);
      this.confirmingRemove.set(false);
    }
    this.onToggle.emit(this.provider());
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
      this.replacing.set(false);
      this.onSaved.emit();
    } catch (err) {
      this.validationError.set(typeof err === 'string' ? err : 'Invalid API key');
    }
  }

  async selectModel(modelValue: string): Promise<void> {
    const persistedModel = this.key()?.preferredModel;
    if (this.savingModel() || modelValue === persistedModel) return;

    this.pendingModel.set(modelValue);
    this.savingModel.set(modelValue);
    try {
      await this.aiKeyService.upsertKey(this.provider(), '', modelValue);
    } catch (err) {
      this.pendingModel.set(null);
      this.aiKeyService.showModelError(typeof err === 'string' ? err : 'Failed to update model');
    } finally {
      this.savingModel.set(null);
      this.pendingModel.set(null);
    }
  }

  handleModelKeyDown(event: KeyboardEvent, currentIndex: number): void {
    const options = this.modelOptions();
    let newIndex = currentIndex;

    if (event.key === 'ArrowDown' || event.key === 'ArrowRight') {
      newIndex = (currentIndex + 1) % options.length;
    } else if (event.key === 'ArrowUp' || event.key === 'ArrowLeft') {
      newIndex = (currentIndex - 1 + options.length) % options.length;
    } else {
      return;
    }

    event.preventDefault();
    const container = (event.target as HTMLElement).closest('[role="radiogroup"]');
    const buttons = container?.querySelectorAll<HTMLElement>('[role="radio"]');
    buttons?.[newIndex]?.focus();
    this.selectModel(options[newIndex].value);
  }

  doRemove(): void {
    this.confirmingRemove.set(false);
    this.onRemove.emit(this.provider());
  }
}
