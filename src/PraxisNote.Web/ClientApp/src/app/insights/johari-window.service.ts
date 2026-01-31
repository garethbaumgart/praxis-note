import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Subscription } from 'rxjs';
import { JohariWindow } from './insights.model';

@Injectable({ providedIn: 'root' })
export class JohariWindowService {
  private readonly http = inject(HttpClient);
  private pendingRequest?: Subscription;

  private readonly _johariWindow = signal<JohariWindow | null>(null);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  readonly johariWindow = this._johariWindow.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  loadJohariWindow(range: string = '90d'): void {
    this.pendingRequest?.unsubscribe();
    this._loading.set(true);
    this._error.set(null);

    this.pendingRequest = this.http
      .get<JohariWindow>(`/api/insights/johari-window?range=${encodeURIComponent(range)}`)
      .subscribe({
        next: data => {
          this._johariWindow.set(data);
          this._loading.set(false);
        },
        error: () => {
          this._error.set('Failed to load Johari Window data');
          this._loading.set(false);
        },
      });
  }
}
