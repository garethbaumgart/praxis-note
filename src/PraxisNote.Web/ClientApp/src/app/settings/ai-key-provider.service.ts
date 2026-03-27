import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
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

  keyForProvider(provider: AiProvider) {
    return computed(() => this._keys().find(k => k.provider === provider) ?? null);
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

  upsertKey(
    provider: AiProvider,
    apiKey: string,
    preferredModel?: string,
  ): Promise<ValidateKeyResult> {
    this._saving.set(true);

    return new Promise((resolve, reject) => {
      this.http
        .put<ValidateKeyResult>(
          `/api/ai-keys/${provider}`,
          { apiKey, preferredModel },
        )
        .subscribe({
          next: (result) => {
            this._saving.set(false);
            this.loadKeys();
            this.toast.success({ summary: `${provider} key saved` });
            resolve(result);
          },
          error: (err: HttpErrorResponse) => {
            this._saving.set(false);
            if (err.status === 422) {
              const code = err.error?.error;
              const message = code === 'ai_key_invalid'
                ? 'This API key was rejected by the provider. Please check the key and try again.'
                : (code ?? 'Invalid API key');
              reject(message);
            } else {
              const message = err.error?.error ?? `Failed to save ${provider} key`;
              this.toast.error(message);
              reject(message);
            }
          },
        });
    });
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
