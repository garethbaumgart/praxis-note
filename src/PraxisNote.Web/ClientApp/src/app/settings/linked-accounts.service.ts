import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { LinkedIdentity } from './linked-accounts.model';
import { ToastService } from '../shared/services/toast.service';

@Injectable({ providedIn: 'root' })
export class LinkedAccountsService {
  private readonly http = inject(HttpClient);
  private readonly toast = inject(ToastService);

  private readonly _identities = signal<LinkedIdentity[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  readonly identities = this._identities.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  loadIdentities(): void {
    this._loading.set(true);
    this._error.set(null);
    this.http.get<LinkedIdentity[]>('/api/account/linked-identities').subscribe({
      next: (identities) => {
        this._identities.set(identities);
        this._loading.set(false);
      },
      error: () => {
        this._error.set('Failed to load linked accounts');
        this._loading.set(false);
      },
    });
  }

  unlinkIdentity(identityId: string): void {
    this.http.delete(`/api/account/linked-identities/${identityId}`).subscribe({
      next: () => {
        this._identities.update(ids => ids.filter(i => i.id !== identityId));
        this.toast.success({ summary: 'Account unlinked' });
      },
      error: (err) => {
        const message = err.error?.error ?? 'Failed to unlink account';
        this.toast.error(message);
      },
    });
  }

  setDefaultProfile(identityId: string, profileId: string | null): void {
    this.http.put(`/api/account/linked-identities/${identityId}/default-profile`, { profileId }).subscribe({
      next: () => {
        this._identities.update(ids =>
          ids.map(i => i.id === identityId ? { ...i, defaultProfileId: profileId } : i)
        );
        this.toast.success({ summary: 'Default profile updated' });
      },
      error: () => {
        this.toast.error('Failed to update default profile');
      },
    });
  }

  /**
   * Generate a link code for account linking.
   */
  generateLinkCode(): Promise<{ code: string; expiresAt: string }> {
    return new Promise((resolve, reject) => {
      this.http.post<{ code: string; expiresAt: string }>('/api/account/link-code', {}).subscribe({
        next: (result) => resolve(result),
        error: (err) => reject(err),
      });
    });
  }

  /**
   * Redeem a link code to link accounts. Always creates a new profile with the given name.
   */
  redeemLinkCode(code: string, profileName: string): Promise<{ targetUserId: string }> {
    return new Promise((resolve, reject) => {
      this.http.post<{ targetUserId: string }>('/api/account/link', {
        code,
        profileName,
      }).subscribe({
        next: (result) => resolve(result),
        error: (err) => reject(err),
      });
    });
  }
}
