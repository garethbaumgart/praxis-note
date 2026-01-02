import { Component, ElementRef, HostListener, inject, OnInit, signal, viewChild } from '@angular/core';
import { Dialog } from 'primeng/dialog';
import { Draggable, Droppable } from 'primeng/dragdrop';
import { TaskService } from './task.service';
import { TaskCardComponent } from './task-card.component';
import { Task } from './task.model';

@Component({
  selector: 'app-tasks-page',
  standalone: true,
  imports: [Dialog, TaskCardComponent, Draggable, Droppable],
  template: `
    <div class="max-w-7xl mx-auto px-6 py-8">
      <!-- Header -->
      <div class="flex items-center justify-between mb-6">
        <h1 class="text-xl font-semibold text-gray-800">Tasks</h1>
        <button
          class="flex items-center gap-2 h-9 px-3 text-sm font-medium text-gray-900 bg-violet-100 hover:bg-violet-200 rounded-lg transition-colors"
          (click)="showDialog.set(true)"
        >
          <i class="pi pi-plus text-violet-600"></i>
          <span>Add Task</span>
          <kbd class="ml-1 px-1.5 py-0.5 text-xs text-violet-500 bg-violet-50 rounded font-sans">&#8963;N</kbd>
        </button>
      </div>

      <!-- Loading state -->
      @if (taskService.loading()) {
        <div class="flex items-center justify-center py-20">
          <i class="pi pi-spin pi-spinner text-3xl text-primary"></i>
        </div>
      } @else {
        <!-- Kanban Board -->
        <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
          <!-- Todo Column -->
          <div
            class="rounded-lg p-3 min-h-48 transition-all bg-slate-100"
            [class.bg-slate-200]="dragOverColumn() === 'Todo'"
            pDroppable="tasks"
            (onDrop)="onDrop('Todo')"
            (onDragEnter)="dragOverColumn.set('Todo')"
            (onDragLeave)="dragOverColumn.set(null)"
          >
            <div class="flex items-center gap-2 mb-3">
              <span class="text-xs font-medium text-slate-600 uppercase tracking-wide">Todo</span>
              <span class="text-xs text-slate-400">{{ taskService.todoTasks().length }}</span>
            </div>
            <div class="space-y-2">
              @for (task of taskService.todoTasks(); track task.id; let idx = $index) {
                <div
                  pDraggable="tasks"
                  pDroppable="tasks"
                  (onDragStart)="onDragStart(task)"
                  (onDragEnd)="onDragEnd()"
                  (onDrop)="onDropOnTask('Todo', idx)"
                  class="cursor-grab active:cursor-grabbing"
                >
                  <app-task-card
                    [task]="task"
                    (onEdit)="updateTask(task.id, $event)"
                    (onDelete)="deleteTask(task.id)"
                  />
                </div>
              } @empty {
                <p class="text-sm text-gray-400 text-center py-8">No tasks yet</p>
              }
            </div>
          </div>

          <!-- In Progress Column -->
          <div
            class="rounded-lg p-3 min-h-48 transition-all bg-sky-100"
            [class.bg-sky-200]="dragOverColumn() === 'InProgress'"
            pDroppable="tasks"
            (onDrop)="onDrop('InProgress')"
            (onDragEnter)="dragOverColumn.set('InProgress')"
            (onDragLeave)="dragOverColumn.set(null)"
          >
            <div class="flex items-center gap-2 mb-3">
              <span class="text-xs font-medium text-sky-700 uppercase tracking-wide">In Progress</span>
              <span class="text-xs text-sky-500">{{ taskService.inProgressTasks().length }}</span>
            </div>
            <div class="space-y-2">
              @for (task of taskService.inProgressTasks(); track task.id; let idx = $index) {
                <div
                  pDraggable="tasks"
                  pDroppable="tasks"
                  (onDragStart)="onDragStart(task)"
                  (onDragEnd)="onDragEnd()"
                  (onDrop)="onDropOnTask('InProgress', idx)"
                  class="cursor-grab active:cursor-grabbing"
                >
                  <app-task-card
                    [task]="task"
                    (onEdit)="updateTask(task.id, $event)"
                    (onDelete)="deleteTask(task.id)"
                  />
                </div>
              } @empty {
                <p class="text-sm text-gray-400 text-center py-8">Nothing in progress</p>
              }
            </div>
          </div>

          <!-- Done Column -->
          <div
            class="rounded-lg p-3 min-h-48 transition-all bg-emerald-100"
            [class.bg-emerald-200]="dragOverColumn() === 'Done'"
            pDroppable="tasks"
            (onDrop)="onDrop('Done')"
            (onDragEnter)="dragOverColumn.set('Done')"
            (onDragLeave)="dragOverColumn.set(null)"
          >
            <div class="flex items-center gap-2 mb-3">
              <span class="text-xs font-medium text-emerald-700 uppercase tracking-wide">Done</span>
              <span class="text-xs text-emerald-500">{{ taskService.doneTasks().length }}</span>
            </div>
            <div class="space-y-2">
              @for (task of taskService.doneTasks(); track task.id; let idx = $index) {
                <div
                  pDraggable="tasks"
                  pDroppable="tasks"
                  (onDragStart)="onDragStart(task)"
                  (onDragEnd)="onDragEnd()"
                  (onDrop)="onDropOnTask('Done', idx)"
                  class="cursor-grab active:cursor-grabbing"
                >
                  <app-task-card
                    [task]="task"
                    (onEdit)="updateTask(task.id, $event)"
                    (onDelete)="deleteTask(task.id)"
                  />
                </div>
              } @empty {
                <p class="text-sm text-gray-400 text-center py-8">Complete some tasks!</p>
              }
            </div>
          </div>
        </div>
      }
    </div>

    <!-- Add Task Dialog -->
    <p-dialog
      [visible]="showDialog()"
      (visibleChange)="showDialog.set($event)"
      (onShow)="onDialogShow()"
      [modal]="true"
      [style]="{ width: '420px' }"
      [draggable]="false"
      [showHeader]="false"
      [contentStyle]="{ padding: 0 }"
    >
      <div class="p-5">
        <input
          #taskInput
          type="text"
          [value]="newTaskTitle()"
          (input)="newTaskTitle.set($any($event.target).value)"
          placeholder="Task name..."
          class="w-full text-lg font-medium text-gray-900 placeholder-gray-400 border-0 focus:outline-none focus:ring-0 p-0 bg-transparent"
          (keydown.enter)="createTask(newTaskTitle())"
          (keydown.escape)="showDialog.set(false)"
        />
        <div class="flex items-center justify-between mt-4 pt-4 border-t border-gray-100">
          <div class="flex items-center gap-2 text-sm text-gray-500">
            <i class="pi pi-inbox"></i>
            <span>Todo</span>
          </div>
          <div class="flex gap-2">
            <button
              type="button"
              class="px-3 py-1.5 text-sm text-gray-600 hover:bg-gray-100 rounded-md transition-colors"
              (click)="showDialog.set(false)"
            >
              Cancel
            </button>
            <button
              type="button"
              class="px-3 py-1.5 text-sm font-medium text-white bg-violet-600 hover:bg-violet-700 rounded-md transition-colors"
              (click)="createTask(newTaskTitle())"
            >
              Add Task
            </button>
          </div>
        </div>
      </div>
    </p-dialog>
  `,
})
export class TasksPage implements OnInit {
  readonly taskService = inject(TaskService);
  readonly showDialog = signal(false);
  readonly newTaskTitle = signal('');
  readonly draggedTask = signal<Task | null>(null);
  readonly dragOverColumn = signal<'Todo' | 'InProgress' | 'Done' | null>(null);

  readonly taskInput = viewChild<ElementRef<HTMLInputElement>>('taskInput');

  ngOnInit(): void {
    this.taskService.loadTasks();
  }

  onDialogShow(): void {
    // Focus the input after a brief delay to ensure the dialog is fully rendered
    setTimeout(() => this.taskInput()?.nativeElement.focus(), 0);
  }

  @HostListener('document:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    // Ctrl+N to open add task dialog
    if (event.key.toLowerCase() === 'n' && event.ctrlKey && !this.showDialog()) {
      event.preventDefault();
      this.showDialog.set(true);
    }
  }

  createTask(title: string): void {
    if (title.trim()) {
      this.taskService.createTask(title.trim());
      this.newTaskTitle.set('');
      this.showDialog.set(false);
    }
  }

  updateTask(id: string, title: string): void {
    this.taskService.updateTask(id, title);
  }

  deleteTask(id: string): void {
    this.taskService.deleteTask(id);
  }

  onDragStart(task: Task): void {
    this.draggedTask.set(task);
  }

  onDragEnd(): void {
    this.draggedTask.set(null);
    this.dragOverColumn.set(null);
  }

  onDrop(targetStatus: 'Todo' | 'InProgress' | 'Done'): void {
    const task = this.draggedTask();
    if (task && task.status !== targetStatus) {
      this.taskService.changeStatus(task.id, targetStatus);
    }
    this.draggedTask.set(null);
    this.dragOverColumn.set(null);
  }

  onDropOnTask(targetStatus: 'Todo' | 'InProgress' | 'Done', targetIndex: number): void {
    const task = this.draggedTask();
    if (!task) return;

    // Get tasks in target column
    const tasksInColumn =
      targetStatus === 'Todo'
        ? this.taskService.todoTasks()
        : targetStatus === 'InProgress'
          ? this.taskService.inProgressTasks()
          : this.taskService.doneTasks();

    if (task.status === targetStatus) {
      // Same column reorder
      const currentIndex = tasksInColumn.findIndex(t => t.id === task.id);
      if (currentIndex === targetIndex) return; // No change

      // Build new order
      const taskIds = tasksInColumn.map(t => t.id);
      taskIds.splice(currentIndex, 1); // Remove from current position
      taskIds.splice(targetIndex, 0, task.id); // Insert at target position

      this.taskService.reorderTasks(targetStatus, taskIds);
    } else {
      // Cross-column move to specific position
      this.taskService.changeStatus(task.id, targetStatus);
    }

    this.draggedTask.set(null);
    this.dragOverColumn.set(null);
  }
}
