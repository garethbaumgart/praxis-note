import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { DailySummary } from './summary.model';

@Injectable({ providedIn: 'root' })
export class SummaryService {
  private readonly http = inject(HttpClient);

  readonly summary = signal<DailySummary | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly selectedDate = signal<string>(this.todayDateString());

  loadSummary(date?: string): void {
    const targetDate = date ?? this.selectedDate();
    this.selectedDate.set(targetDate);
    this.loading.set(true);
    this.error.set(null);

    let params = new HttpParams();
    if (targetDate) {
      params = params.set('date', targetDate);
    }

    this.http.get<DailySummary>('/api/summary', { params }).subscribe({
      next: data => {
        this.summary.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load daily summary');
        this.loading.set(false);
      },
    });
  }

  navigateDate(offset: number): void {
    const current = new Date(this.selectedDate() + 'T00:00:00');
    current.setDate(current.getDate() + offset);
    const newDate = this.formatDate(current);
    this.loadSummary(newDate);
  }

  goToToday(): void {
    this.loadSummary(this.todayDateString());
  }

  readonly isToday = computed(() => this.selectedDate() === this.todayDateString());

  private todayDateString(): string {
    return this.formatDate(new Date());
  }

  private formatDate(d: Date): string {
    return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
  }
}
