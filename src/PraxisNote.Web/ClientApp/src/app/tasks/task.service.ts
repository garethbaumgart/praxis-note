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
  private readonly _archivedTasks = signal<Task[]>([]);
  private readonly _archivedCount = signal(0);
  private readonly _loading = signal(false);
  private readonly _initialLoadComplete = signal(false);

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
  readonly archivedTasks = this._archivedTasks.asReadonly();
  readonly archivedCount = this._archivedCount.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly initialLoadComplete = this._initialLoadComplete.asReadonly();

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
        this._initialLoadComplete.set(true);
      },
      error: () => {
        this._loading.set(false);
        this._initialLoadComplete.set(true); // Mark complete even on error to stop showing skeletons
      },
    });
  }

  loadArchivedCount(): void {
    this.http.get<{ count: number }>('/api/tasks/archived/count').subscribe({
      next: (result) => {
        this._archivedCount.set(result.count);
      },
      error: () => {
        // Keep existing count on error
      },
    });
  }

  loadArchivedTasks(): void {
    this._loading.set(true);
    this.http.get<Task[]>('/api/tasks?includeArchived=true').subscribe({
      next: (tasks) => {
        this._archivedTasks.set(tasks);
        this._loading.set(false);
      },
      error: () => {
        this._loading.set(false);
      },
    });
  }

  clearArchivedTasks(): void {
    this._archivedTasks.set([]);
  }

  createTask(title: string): void {
    this.createTaskInColumn(title, 'Todo');
  }

  createTaskInColumn(title: string, status: 'Todo' | 'InProgress' | 'Done'): void {
    const tempId = crypto.randomUUID();
    const now = new Date().toISOString();
    const newTask: Task = {
      id: tempId,
      title,
      status: status,
      position: 0,
      createdAt: now,
      startedAt: status === 'Todo' ? null : now,
      completedAt: status === 'Done' ? now : null,
      comments: [],
      dueDate: null,
    };

    // Optimistic update - add task immediately
    this._tasks.update(tasks =>
      tasks.map(t =>
        t.status === status ? { ...t, position: t.position + 1 } : t
      ).concat(newTask)
    );

    // Make HTTP call in background
    this.http.post<{ id: string }>('/api/tasks', { title }).subscribe({
      next: (result) => {
        // Replace temp ID with real ID
        this._tasks.update(tasks =>
          tasks.map(t => (t.id === tempId ? { ...t, id: result.id } : t))
        );

        // If not Todo, also call the status change API
        if (status !== 'Todo') {
          this.http.put(`/api/tasks/${result.id}/status`, { status }).subscribe({
            error: () => this.loadTasks(),
          });
        }
      },
      error: () => {
        // Remove optimistic task and reload
        this._tasks.update(tasks => tasks.filter(t => t.id !== tempId));
        this.loadTasks();
      },
    });
  }

  updateTask(id: string, title: string): void {
    // Optimistic update - update immediately
    this._tasks.update(tasks =>
      tasks.map(t => (t.id === id ? { ...t, title } : t))
    );

    // Make HTTP call in background
    this.http.put(`/api/tasks/${id}`, { title }).subscribe({
      error: () => this.loadTasks(), // Reload to restore if failed
    });
  }

  changeStatus(id: string, status: 'Todo' | 'InProgress' | 'Done', targetPosition?: number): void {
    const position = targetPosition ?? 0;
    const now = new Date().toISOString();

    // Remove from archived tasks if present (unarchiving via drag)
    const wasArchived = this._archivedTasks().some(t => t.id === id);
    if (wasArchived) {
      this._archivedTasks.update(tasks => tasks.filter(t => t.id !== id));
      this._archivedCount.update(count => Math.max(0, count - 1));
    }

    // Optimistic update - update UI immediately
    this._tasks.update(tasks => {
      // Get tasks in target column (excluding the moved task)
      const targetColumnTasks = tasks
        .filter(t => t.status === status && t.id !== id)
        .sort((a, b) => a.position - b.position);

      // Clamp position to valid range
      const clampedPosition = Math.max(0, Math.min(position, targetColumnTasks.length));

      // Build new positions for target column
      const newPositions = new Map<string, number>();
      newPositions.set(id, clampedPosition);
      for (let i = 0; i < targetColumnTasks.length; i++) {
        const newPos = i >= clampedPosition ? i + 1 : i;
        newPositions.set(targetColumnTasks[i].id, newPos);
      }

      return tasks.map(t => {
        if (t.id === id) {
          return {
            ...t,
            status,
            position: clampedPosition,
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

    // Then make API call - reload on error to revert
    this.http.put(`/api/tasks/${id}/status`, { status, position }).subscribe({
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
    // Optimistic update - remove immediately
    this._tasks.update(tasks => tasks.filter(t => t.id !== id));

    // Make HTTP call in background
    this.http.delete(`/api/tasks/${id}`).subscribe({
      error: () => this.loadTasks(), // Reload to restore if failed
    });
  }

  addComment(taskId: string, content: string): void {
    const now = new Date().toISOString();
    const tempId = crypto.randomUUID();

    // Optimistic update
    this._tasks.update(tasks =>
      tasks.map(t =>
        t.id === taskId
          ? {
              ...t,
              comments: [
                { id: tempId, content, createdAt: now, updatedAt: now },
                ...t.comments,
              ],
            }
          : t
      )
    );

    this.http.post<{ id: string }>(`/api/tasks/${taskId}/comments`, { content }).subscribe({
      next: (result) => {
        // Update the temp ID with the real ID
        this._tasks.update(tasks =>
          tasks.map(t =>
            t.id === taskId
              ? {
                  ...t,
                  comments: t.comments.map(c =>
                    c.id === tempId ? { ...c, id: result.id } : c
                  ),
                }
              : t
          )
        );
      },
      error: () => this.loadTasks(),
    });
  }

  updateComment(taskId: string, commentId: string, content: string): void {
    const now = new Date().toISOString();

    // Optimistic update
    this._tasks.update(tasks =>
      tasks.map(t =>
        t.id === taskId
          ? {
              ...t,
              comments: t.comments.map(c =>
                c.id === commentId ? { ...c, content, updatedAt: now } : c
              ),
            }
          : t
      )
    );

    this.http.put(`/api/tasks/${taskId}/comments/${commentId}`, { content }).subscribe({
      error: () => this.loadTasks(),
    });
  }

  deleteComment(taskId: string, commentId: string): void {
    // Optimistic update
    this._tasks.update(tasks =>
      tasks.map(t =>
        t.id === taskId
          ? { ...t, comments: t.comments.filter(c => c.id !== commentId) }
          : t
      )
    );

    this.http.delete(`/api/tasks/${taskId}/comments/${commentId}`).subscribe({
      error: () => this.loadTasks(),
    });
  }

  setDueDate(taskId: string, date: string): void {
    // Optimistic update
    this._tasks.update(tasks =>
      tasks.map(t => (t.id === taskId ? { ...t, dueDate: date } : t))
    );

    this.http.put(`/api/tasks/${taskId}/due-date`, { date }).subscribe({
      error: () => this.loadTasks(),
    });
  }

  clearDueDate(taskId: string): void {
    // Optimistic update
    this._tasks.update(tasks =>
      tasks.map(t => (t.id === taskId ? { ...t, dueDate: null } : t))
    );

    this.http.delete(`/api/tasks/${taskId}/due-date`).subscribe({
      error: () => this.loadTasks(),
    });
  }
}
