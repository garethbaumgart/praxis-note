import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { InsightsSummary } from './insights-summary.model';

@Injectable({ providedIn: 'root' })
export class InsightsSummaryService {
  private readonly http = inject(HttpClient);

  private readonly _summary = signal<InsightsSummary | null>(null);
  private readonly _loading = signal(false);
  private readonly _loaded = signal(false);
  private readonly _error = signal(false);

  readonly summary = this._summary.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly loaded = this._loaded.asReadonly();
  readonly error = this._error.asReadonly();

  load(): void {
    if (this._loaded()) return;

    this._loading.set(true);

    this.http.get<InsightsSummary | null>('/api/insights/summary').subscribe({
      next: data => {
        this._summary.set(data);
        this._loading.set(false);
        this._loaded.set(true);
      },
      error: () => {
        this._summary.set(null);
        this._loading.set(false);
        this._loaded.set(true);
        this._error.set(true);
      },
    });
  }
}
