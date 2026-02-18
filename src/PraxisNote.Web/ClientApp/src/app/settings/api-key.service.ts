import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ApiKeyDto, ApiKeyCreateResponse } from './api-key.model';
import { ToastService } from '../shared/services/toast.service';

@Injectable({ providedIn: 'root' })
export class ApiKeyService {
  private readonly http = inject(HttpClient);
  private readonly toast = inject(ToastService);

  private readonly _keys = signal<ApiKeyDto[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _creating = signal(false);
  private readonly _lastCreatedRawKey = signal<string | null>(null);

  readonly keys = this._keys.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly creating = this._creating.asReadonly();
  readonly lastCreatedRawKey = this._lastCreatedRawKey.asReadonly();

  loadKeys(): void {
    this._loading.set(true);
    this._error.set(null);

    this.http.get<ApiKeyDto[]>('/api/api-keys').subscribe({
      next: (keys) => {
        this._keys.set(keys);
        this._loading.set(false);
      },
      error: () => {
        this._error.set('Failed to load API keys');
        this._loading.set(false);
      },
    });
  }

  createKey(name: string, expiresAt?: string): void {
    this._creating.set(true);

    const body: { name: string; expiresAt?: string } = { name };
    if (expiresAt) {
      body.expiresAt = expiresAt;
    }

    this.http.post<ApiKeyCreateResponse>('/api/api-keys', body).subscribe({
      next: (result) => {
        this._lastCreatedRawKey.set(result.rawKey);
        this._creating.set(false);
        this.loadKeys();
      },
      error: (err) => {
        const message = err.error?.error ?? 'Failed to create API key';
        this.toast.error(message);
        this._creating.set(false);
      },
    });
  }

  revokeKey(id: string): void {
    const previousKeys = this._keys();
    this._keys.update(keys => keys.filter(k => k.id !== id));

    this.http.delete(`/api/api-keys/${id}`).subscribe({
      next: () => {
        this.toast.success({ summary: 'API key revoked' });
      },
      error: () => {
        this._keys.set(previousKeys);
        this.toast.error('Failed to revoke API key');
      },
    });
  }

  clearLastCreatedKey(): void {
    this._lastCreatedRawKey.set(null);
  }
}
