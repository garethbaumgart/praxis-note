import { Injectable, inject, signal, computed, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { Subject, debounceTime } from 'rxjs';
import { Task } from './task.model';
import { ToastService } from '../shared/services/toast.service';

interface PendingDeletion {
  task: Task;
  timeoutId: ReturnType<typeof setTimeout>;
  /** Original index in the tasks array for restoring at the same position */
  index: number;
}

interface PendingCommentDeletion {
  taskId: string;
  comment: { id: string; content: string; createdAt: string; updatedAt: string };
  timeoutId: ReturnType<typeof setTimeout>;
  /** Original index in the comments array for restoring at the same position */
  index: number;
}

@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  private readonly toast = inject(ToastService);

  private readonly reorderSubject = new Subject<{ status: string; taskIds: string[] }>();
  private readonly pendingDeletions = new Map<string, PendingDeletion>();
  private readonly pendingCommentDeletions = new Map<string, PendingCommentDeletion>();

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
          error: () => {
            this.toast.error('Failed to reorder tasks');
            this.loadTasks();
          },
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
      isPriority: false,
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
            error: () => {
              this.toast.error('Failed to create task');
              this.loadTasks();
            },
          });
        }
      },
      error: () => {
        // Remove optimistic task and reload
        this._tasks.update(tasks => tasks.filter(t => t.id !== tempId));
        this.toast.error('Failed to create task');
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
      error: () => {
        this.toast.error('Failed to update task');
        this.loadTasks();
      },
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
      error: () => {
        this.toast.error('Failed to move task');
        this.loadTasks();
      },
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
      error: () => {
        this.toast.error('Failed to delete task');
        this.loadTasks();
      },
    });
  }

  /**
   * Delete a task with undo capability.
   * Returns the deleted task for display purposes, or null if task not found.
   * The actual API deletion is delayed to allow undo.
   */
  deleteTaskWithUndo(id: string, undoTimeoutMs = 5000): Task | null {
    const tasks = this._tasks();
    const index = tasks.findIndex(t => t.id === id);
    if (index === -1) return null;

    const task = tasks[index];

    // Cancel any existing pending deletion for this task
    this.cancelPendingDeletion(id);

    // Remove from UI immediately (optimistic update)
    this._tasks.update(tasks => tasks.filter(t => t.id !== id));

    // Schedule actual deletion after timeout
    const timeoutId = setTimeout(() => {
      this.commitDeletion(id);
    }, undoTimeoutMs);

    // Store for potential undo (including original index for position restoration)
    this.pendingDeletions.set(id, { task, timeoutId, index });

    return task;
  }

  /**
   * Undo a pending deletion, restoring the task to the UI at its original position.
   * Returns true if the undo was successful, false if the task was already deleted.
   */
  undoDelete(id: string): boolean {
    const pending = this.pendingDeletions.get(id);
    if (!pending) return false;

    // Cancel the scheduled deletion
    clearTimeout(pending.timeoutId);
    this.pendingDeletions.delete(id);

    // Restore task to UI at original position
    this._tasks.update(tasks => {
      const clampedIndex = Math.min(pending.index, tasks.length);
      return [
        ...tasks.slice(0, clampedIndex),
        pending.task,
        ...tasks.slice(clampedIndex),
      ];
    });

    return true;
  }

  private commitDeletion(id: string): void {
    const pending = this.pendingDeletions.get(id);
    if (!pending) return;

    this.pendingDeletions.delete(id);

    // Actually delete from backend
    this.http.delete(`/api/tasks/${id}`).subscribe({
      error: () => {
        this.toast.error('Failed to delete task');
        // Restore task at original position on error
        this._tasks.update(tasks => {
          const clampedIndex = Math.min(pending.index, tasks.length);
          return [
            ...tasks.slice(0, clampedIndex),
            pending.task,
            ...tasks.slice(clampedIndex),
          ];
        });
      },
    });
  }

  private cancelPendingDeletion(id: string): void {
    const pending = this.pendingDeletions.get(id);
    if (pending) {
      clearTimeout(pending.timeoutId);
      this.pendingDeletions.delete(id);
    }
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
      error: () => {
        this.toast.error('Failed to add comment');
        this.loadTasks();
      },
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
      error: () => {
        this.toast.error('Failed to update comment');
        this.loadTasks();
      },
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
      error: () => {
        this.toast.error('Failed to delete comment');
        this.loadTasks();
      },
    });
  }

  /**
   * Delete a comment with undo capability.
   * Returns the deleted comment for display purposes, or null if not found.
   * The actual API deletion is delayed to allow undo.
   */
  deleteCommentWithUndo(taskId: string, commentId: string, undoTimeoutMs = 5000): { id: string; content: string } | null {
    const task = this._tasks().find(t => t.id === taskId);
    if (!task) return null;

    const index = task.comments.findIndex(c => c.id === commentId);
    if (index === -1) return null;

    const comment = task.comments[index];

    // Cancel any existing pending deletion for this comment
    this.cancelPendingCommentDeletion(commentId);

    // Remove from UI immediately (optimistic update)
    this._tasks.update(tasks =>
      tasks.map(t =>
        t.id === taskId
          ? { ...t, comments: t.comments.filter(c => c.id !== commentId) }
          : t
      )
    );

    // Schedule actual deletion after timeout
    const timeoutId = setTimeout(() => {
      this.commitCommentDeletion(taskId, commentId);
    }, undoTimeoutMs);

    // Store for potential undo
    this.pendingCommentDeletions.set(commentId, { taskId, comment, timeoutId, index });

    return { id: comment.id, content: comment.content };
  }

  /**
   * Undo a pending comment deletion, restoring the comment to the UI.
   * Returns true if the undo was successful, false if the comment was already deleted.
   */
  undoCommentDelete(commentId: string): boolean {
    const pending = this.pendingCommentDeletions.get(commentId);
    if (!pending) return false;

    // Cancel the scheduled deletion
    clearTimeout(pending.timeoutId);
    this.pendingCommentDeletions.delete(commentId);

    // Restore comment to UI at original position
    this._tasks.update(tasks =>
      tasks.map(t => {
        if (t.id === pending.taskId) {
          const comments = [...t.comments];
          const clampedIndex = Math.min(pending.index, comments.length);
          comments.splice(clampedIndex, 0, pending.comment);
          return { ...t, comments };
        }
        return t;
      })
    );

    return true;
  }

  private commitCommentDeletion(taskId: string, commentId: string): void {
    const pending = this.pendingCommentDeletions.get(commentId);
    if (!pending) return;

    this.pendingCommentDeletions.delete(commentId);

    // Actually delete from backend
    this.http.delete(`/api/tasks/${taskId}/comments/${commentId}`).subscribe({
      error: () => {
        this.toast.error('Failed to delete comment');
        // Restore comment at original position on error
        this._tasks.update(tasks =>
          tasks.map(t => {
            if (t.id === pending.taskId) {
              const comments = [...t.comments];
              const clampedIndex = Math.min(pending.index, comments.length);
              comments.splice(clampedIndex, 0, pending.comment);
              return { ...t, comments };
            }
            return t;
          })
        );
      },
    });
  }

  private cancelPendingCommentDeletion(commentId: string): void {
    const pending = this.pendingCommentDeletions.get(commentId);
    if (pending) {
      clearTimeout(pending.timeoutId);
      this.pendingCommentDeletions.delete(commentId);
    }
  }

  setDueDate(taskId: string, date: string): void {
    // Optimistic update
    this._tasks.update(tasks =>
      tasks.map(t => (t.id === taskId ? { ...t, dueDate: date } : t))
    );

    this.http.put(`/api/tasks/${taskId}/due-date`, { date }).subscribe({
      error: () => {
        this.toast.error('Failed to set due date');
        this.loadTasks();
      },
    });
  }

  clearDueDate(taskId: string): void {
    // Optimistic update
    this._tasks.update(tasks =>
      tasks.map(t => (t.id === taskId ? { ...t, dueDate: null } : t))
    );

    this.http.delete(`/api/tasks/${taskId}/due-date`).subscribe({
      error: () => {
        this.toast.error('Failed to clear due date');
        this.loadTasks();
      },
    });
  }

  togglePriority(taskId: string): void {
    // Optimistic update
    this._tasks.update(tasks =>
      tasks.map(t => (t.id === taskId ? { ...t, isPriority: !t.isPriority } : t))
    );

    this.http.patch(`/api/tasks/${taskId}/priority`, {}).subscribe({
      error: () => {
        this.toast.error('Failed to update priority');
        this.loadTasks();
      },
    });
  }
}
