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
    this.createTaskInColumn(title, 'Todo');
  }

  createTaskInColumn(title: string, status: 'Todo' | 'InProgress' | 'Done'): void {
    this.http.post<{ id: string }>('/api/tasks', { title }).subscribe({
      next: (result) => {
        const now = new Date().toISOString();
        const newTask: Task = {
          id: result.id,
          title,
          status: status,
          position: 0,
          createdAt: now,
          startedAt: status === 'Todo' ? null : now,
          completedAt: status === 'Done' ? now : null,
        };

        // Push down existing tasks in target column and add new one at position 0
        this._tasks.update(tasks =>
          tasks.map(t =>
            t.status === status ? { ...t, position: t.position + 1 } : t
          ).concat(newTask)
        );

        // If not Todo, also call the status change API
        if (status !== 'Todo') {
          this.http.put(`/api/tasks/${result.id}/status`, { status }).subscribe({
            error: () => {
              // Revert optimistic status/timestamps before reloading
              this._tasks.update(tasks =>
                tasks.map(t =>
                  t.id === result.id
                    ? { ...t, status: 'Todo', startedAt: null, completedAt: null, position: 0 }
                    : t
                )
              );
              this.loadTasks();
            },
          });
        }
      },
      error: () => this.loadTasks(), // Reload to restore consistent state
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

  changeStatus(id: string, status: 'Todo' | 'InProgress' | 'Done', targetPosition?: number): void {
    this.http.put(`/api/tasks/${id}/status`, { status }).subscribe({
      next: () => {
        const now = new Date().toISOString();
        const position = targetPosition ?? 0;

        this._tasks.update(tasks => {
          // Get tasks in target column (excluding the moved task)
          const targetColumnTasks = tasks
            .filter(t => t.status === status && t.id !== id)
            .sort((a, b) => a.position - b.position);

          // Build new positions for target column
          const newPositions = new Map<string, number>();
          let pos = 0;
          for (let i = 0; i <= targetColumnTasks.length; i++) {
            if (i === position) {
              newPositions.set(id, pos++);
            }
            if (i < targetColumnTasks.length) {
              newPositions.set(targetColumnTasks[i].id, pos++);
            }
          }

          return tasks.map(t => {
            if (t.id === id) {
              return {
                ...t,
                status,
                position: newPositions.get(id) ?? 0,
                startedAt: status === 'Todo' ? null : (t.startedAt ?? now),
                completedAt: status === 'Done' ? now : null,
              };
            }
            if (t.status === status && newPositions.has(t.id)) {
              return { ...t, position: newPositions.get(t.id)! };
            }
            return t;
          });
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
