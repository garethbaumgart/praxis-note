import { Injectable, inject, signal, computed, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { Subject, debounceTime } from 'rxjs';
import { Task } from './task.model';

@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);

  private readonly reorderSubject = new Subject<{ status: string; taskIds: string[] }>();

  private readonly _tasks = signal<Task[]>([]);
  private readonly _loading = signal(false);

  constructor() {
    // Debounce reorder API calls to avoid excessive requests during rapid drag operations
    this.reorderSubject
      .pipe(debounceTime(300), takeUntilDestroyed(this.destroyRef))
      .subscribe(({ status, taskIds }) => {
        this.http.put('/api/tasks/reorder', { status, taskIds }).subscribe({
          error: () => this.loadTasks(),
        });
      });
  }

  readonly tasks = this._tasks.asReadonly();
  readonly loading = this._loading.asReadonly();

  readonly todoTasks = computed(() =>
    this._tasks()
      .filter(t => t.status === 'Todo')
      .sort((a, b) => a.position - b.position)
  );

  readonly inProgressTasks = computed(() =>
    this._tasks()
      .filter(t => t.status === 'InProgress')
      .sort((a, b) => a.position - b.position)
  );

  readonly doneTasks = computed(() =>
    this._tasks()
      .filter(t => t.status === 'Done')
      .sort((a, b) => a.position - b.position)
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
          position: 0,
          createdAt: new Date().toISOString(),
          completedAt: null,
        };
        // Push down existing Todo tasks and add new one at position 0
        this._tasks.update(tasks =>
          tasks.map(t =>
            t.status === 'Todo' ? { ...t, position: t.position + 1 } : t
          ).concat(newTask)
        );
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
        this._tasks.update(tasks => {
          // Push down tasks in target column
          const updated = tasks.map(t => {
            if (t.id === id) {
              return {
                ...t,
                status,
                position: 0,
                completedAt: status === 'Done' ? new Date().toISOString() : null,
              };
            }
            if (t.status === status) {
              return { ...t, position: t.position + 1 };
            }
            return t;
          });
          return updated;
        });
      },
      error: () => this.loadTasks(),
    });
  }

  reorderTasks(status: 'Todo' | 'InProgress' | 'Done', taskIds: string[]): void {
    // Update positions locally immediately (optimistic update)
    this._tasks.update(tasks =>
      tasks.map(t => {
        if (t.status === status) {
          const newPosition = taskIds.indexOf(t.id);
          return newPosition >= 0 ? { ...t, position: newPosition } : t;
        }
        return t;
      })
    );

    // Debounce the API call
    this.reorderSubject.next({ status, taskIds });
  }

  deleteTask(id: string): void {
    this.http.delete(`/api/tasks/${id}`).subscribe({
      next: () => {
        this._tasks.update(tasks => tasks.filter(t => t.id !== id));
      },
      error: () => this.loadTasks(),
    });
  }
}
