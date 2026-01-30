import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Subscription } from 'rxjs';
import { CommunicationProfile } from './insights.model';

@Injectable({ providedIn: 'root' })
export class CommunicationProfileService {
  private readonly http = inject(HttpClient);
  private pendingRequest?: Subscription;

  private readonly _profile = signal<CommunicationProfile | null>(null);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  readonly profile = this._profile.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  loadProfile(range: string = '90d'): void {
    this.pendingRequest?.unsubscribe();
    this._loading.set(true);
    this._error.set(null);

    this.pendingRequest = this.http
      .get<CommunicationProfile>(`/api/insights/communication-profile?range=${range}`)
      .subscribe({
        next: data => {
          this._profile.set(data);
          this._loading.set(false);
        },
        error: () => {
          this._error.set('Failed to load communication profile');
          this._loading.set(false);
        },
      });
  }
}
