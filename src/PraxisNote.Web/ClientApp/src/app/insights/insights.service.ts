import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehavioralTrends, DateRange } from './insights.model';

@Injectable({ providedIn: 'root' })
export class InsightsService {
  private readonly http = inject(HttpClient);

  private readonly _trends = signal<BehavioralTrends | null>(null);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _dateRange = signal<DateRange>('30d');
  private readonly _participant = signal<string | null>(null);

  readonly trends = this._trends.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly dateRange = this._dateRange.asReadonly();
  readonly participant = this._participant.asReadonly();

  setDateRange(range: DateRange): void {
    this._dateRange.set(range);
    this.loadTrends();
  }

  setParticipant(name: string | null): void {
    this._participant.set(name);
    this.loadTrends();
  }

  loadTrends(): void {
    this._loading.set(true);
    this._error.set(null);

    let url = `/api/insights/behavioral-trends?range=${this._dateRange()}`;
    const participant = this._participant();
    if (participant) {
      url += `&participant=${encodeURIComponent(participant)}`;
    }

    this.http.get<BehavioralTrends>(url).subscribe({
      next: data => {
        this._trends.set(data);
        this._loading.set(false);
      },
      error: () => {
        this._error.set('Failed to load insights data');
        this._loading.set(false);
      },
    });
  }
}
