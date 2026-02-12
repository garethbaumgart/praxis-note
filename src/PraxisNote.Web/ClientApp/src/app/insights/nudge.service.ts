import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BlindSpotNudge } from './insights.model';
import { ToastService } from '../shared/services/toast.service';

@Injectable({ providedIn: 'root' })
export class NudgeService {
  private readonly http = inject(HttpClient);
  private readonly toast = inject(ToastService);

  private readonly _nudges = signal<BlindSpotNudge[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  readonly nudges = this._nudges.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  loadNudges(range: string): void {
    this._loading.set(true);
    this._error.set(null);

    this.http.get<BlindSpotNudge[]>(`/api/insights/nudges?range=${range}`).subscribe({
      next: data => {
        this._nudges.set(data);
        this._loading.set(false);
      },
      error: () => {
        this._error.set('Failed to load nudges');
        this._loading.set(false);
      },
    });
  }

  dismissNudge(id: string): void {
    this.http.post(`/api/insights/nudges/${id}/dismiss`, {}).subscribe({
      next: () => {
        this._nudges.update(nudges => nudges.filter(n => n.id !== id));
      },
      error: () => this.toast.error('Failed to dismiss nudge'),
    });
  }

  acceptNudge(id: string, onSuccess?: () => void): void {
    this.http.post<{ goalId: string }>(`/api/insights/nudges/${id}/accept`, {}).subscribe({
      next: () => {
        this._nudges.update(nudges => nudges.filter(n => n.id !== id));
        onSuccess?.();
      },
      error: () => this.toast.error('Failed to create goal from nudge'),
    });
  }
}
