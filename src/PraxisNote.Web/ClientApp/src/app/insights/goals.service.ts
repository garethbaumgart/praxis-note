import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehavioralGoal, GoalProgress } from './insights.model';

@Injectable({ providedIn: 'root' })
export class GoalsService {
  private readonly http = inject(HttpClient);

  private readonly _goals = signal<BehavioralGoal[]>([]);
  private readonly _progress = signal<GoalProgress[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  readonly goals = this._goals.asReadonly();
  readonly progress = this._progress.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  loadGoalsAndProgress(): void {
    this._loading.set(true);
    this._error.set(null);

    this.http.get<GoalProgress[]>('/api/insights/goals/progress').subscribe({
      next: data => {
        this._progress.set(data);
        this._loading.set(false);
      },
      error: () => {
        this._error.set('Failed to load goals');
        this._loading.set(false);
      },
    });
  }

  createGoal(goal: {
    metricType: string;
    operator: string;
    targetValue: number;
    targetValueUpper: number | null;
    title: string;
  }): void {
    this.http.post<{ id: string }>('/api/insights/goals', goal).subscribe({
      next: () => this.loadGoalsAndProgress(),
      error: () => this._error.set('Failed to create goal'),
    });
  }

  deleteGoal(goalId: string): void {
    this.http.delete(`/api/insights/goals/${goalId}`).subscribe({
      next: () => this.loadGoalsAndProgress(),
      error: () => this._error.set('Failed to delete goal'),
    });
  }

  updateGoal(
    goalId: string,
    goal: {
      metricType: string;
      operator: string;
      targetValue: number;
      targetValueUpper: number | null;
      title: string;
      isActive: boolean;
    },
  ): void {
    this.http.put(`/api/insights/goals/${goalId}`, goal).subscribe({
      next: () => this.loadGoalsAndProgress(),
      error: () => this._error.set('Failed to update goal'),
    });
  }
}
