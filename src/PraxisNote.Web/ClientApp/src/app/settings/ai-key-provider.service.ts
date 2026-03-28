import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { AiKeyDto, AiProvider, ValidateKeyResult } from './ai-key-provider.model';
import { ToastService } from '../shared/services/toast.service';

@Injectable({ providedIn: 'root' })
export class AiKeyProviderService {
  private readonly http = inject(HttpClient);
  private readonly toast = inject(ToastService);

  private readonly _keys = signal<AiKeyDto[]>([]);
  private readonly _loading = signal(false);
  private readonly _saving = signal(false);
  private readonly _error = signal<string | null>(null);

  readonly keys = this._keys.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly saving = this._saving.asReadonly();
  readonly error = this._error.asReadonly();

  private readonly _providerComputedCache = new Map<AiProvider, ReturnType<typeof computed<AiKeyDto | null>>>();

  keyForProvider(provider: AiProvider) {
    let cached = this._providerComputedCache.get(provider);
    if (!cached) {
      cached = computed(() => this._keys().find(k => k.provider === provider) ?? null);
      this._providerComputedCache.set(provider, cached);
    }
    return cached;
  }

  loadKeys(): void {
    this._loading.set(true);
    this._error.set(null);

    this.http.get<AiKeyDto[]>('/api/ai-keys').subscribe({
      next: (keys) => {
        this._keys.set(keys);
        this._loading.set(false);
      },
      error: () => {
        this._error.set('Failed to load AI keys');
        this._loading.set(false);
      },
    });
  }

  async upsertKey(
    provider: AiProvider,
    apiKey: string,
    preferredModel?: string,
  ): Promise<ValidateKeyResult> {
    this._saving.set(true);

    try {
      const result = await firstValueFrom(
        this.http.put<ValidateKeyResult>(
          `/api/ai-keys/${provider}`,
          { apiKey, preferredModel },
        ),
      );
      this.loadKeys();
      const summary = apiKey ? `${provider} key saved` : `${provider} model updated`;
      this.toast.success({ summary });
      return result;
    } catch (err) {
      const httpErr = err as HttpErrorResponse;
      if (httpErr.status === 422) {
        const code = httpErr.error?.error;
        let message: string;
        if (code === 'ai_key_invalid') {
          message = 'This API key was rejected by the provider. Please check the key and try again.';
        } else if (code === 'invalid_model') {
          message = httpErr.error?.message ?? 'Unknown model selected';
        } else {
          message = code ?? 'Invalid API key';
        }
        throw message;
      } else {
        const message = httpErr.error?.error ?? `Failed to save ${provider} key`;
        this.toast.error(message);
        throw message;
      }
    } finally {
      this._saving.set(false);
    }
  }

  removeKey(provider: AiProvider): void {
    const previousKeys = this._keys();
    this._keys.update(keys => keys.filter(k => k.provider !== provider));

    this.http.delete(`/api/ai-keys/${provider}`).subscribe({
      next: () => {
        this.toast.success({ summary: `${provider} key removed` });
      },
      error: () => {
        this._keys.set(previousKeys);
        this.toast.error(`Failed to remove ${provider} key`);
      },
    });
  }
}
