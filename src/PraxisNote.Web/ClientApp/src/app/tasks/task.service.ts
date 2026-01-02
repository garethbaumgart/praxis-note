import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Task } from './task.model';

@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly http = inject(HttpClient);

  private readonly _tasks = signal<Task[]>([]);
  private readonly _loading = signal(false);

  readonly tasks = this._tasks.asReadonly();
  readonly loading = this._loading.asReadonly();

  readonly todoTasks = computed(() =>
    this._tasks().filter(t => t.status === 'Todo')
  );

  readonly inProgressTasks = computed(() =>
    this._tasks().filter(t => t.status === 'InProgress')
  );

  readonly doneTasks = computed(() =>
    this._tasks().filter(t => t.status === 'Done')
  );

  loadTasks(): void {
    this._loading.set(true);
    this.http.get<Task[]>('/api/tasks').subscribe({
      next: (tasks) => {
        this._tasks.set(tasks);
        this._loading.set(false);
      },
      error: () => {
        this._loading.set(false);
      },
    });
  }

  createTask(title: string): void {
    this.http.post<{ id: string }>('/api/tasks', { title }).subscribe({
      next: (result) => {
        const newTask: Task = {
          id: result.id,
          title,
          status: 'Todo',
          createdAt: new Date().toISOString(),
          completedAt: null,
        };
        this._tasks.update(tasks => [newTask, ...tasks]);
      },
    });
  }

  updateTask(id: string, title: string): void {
    this.http.put(`/api/tasks/${id}`, { title }).subscribe({
      next: () => {
        this._tasks.update(tasks =>
          tasks.map(t => (t.id === id ? { ...t, title } : t))
        );
      },
      error: () => this.loadTasks(), // Reload to restore consistent state
    });
  }

  changeStatus(id: string, status: 'Todo' | 'InProgress' | 'Done'): void {
    this.http.put(`/api/tasks/${id}/status`, { status }).subscribe({
      next: () => {
        this._tasks.update(tasks =>
          tasks.map(t =>
            t.id === id
              ? {
                  ...t,
                  status,
                  completedAt: status === 'Done' ? new Date().toISOString() : null,
                }
              : t
          )
        );
      },
      error: () => this.loadTasks(), // Reload to restore consistent state
    });
  }

  deleteTask(id: string): void {
    this.http.delete(`/api/tasks/${id}`).subscribe({
      next: () => {
        this._tasks.update(tasks => tasks.filter(t => t.id !== id));
      },
      error: () => this.loadTasks(), // Reload to restore consistent state
    });
  }
}
