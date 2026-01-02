import { Component, inject, OnInit, signal } from '@angular/core';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { Draggable, Droppable } from 'primeng/dragdrop';
import { TaskService } from './task.service';
import { TaskCardComponent } from './task-card.component';
import { Task } from './task.model';

@Component({
  selector: 'app-tasks-page',
  standalone: true,
  imports: [Button, Dialog, InputText, TaskCardComponent, Draggable, Droppable],
  template: `
    <div class="max-w-7xl mx-auto px-6 py-8">
      <!-- Header -->
      <div class="flex items-center justify-between mb-6">
        <h1 class="text-xl font-semibold text-gray-800">Tasks</h1>
        <p-button
          label="New"
          icon="pi pi-plus"
          [text]="true"
          severity="secondary"
          (onClick)="showDialog.set(true)"
        />
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
            class="rounded-lg p-3 min-h-48 transition-all bg-slate-50/50"
            [class.bg-slate-100]="dragOverColumn() === 'Todo'"
            pDroppable="tasks"
            (onDrop)="onDrop('Todo')"
            (onDragEnter)="dragOverColumn.set('Todo')"
            (onDragLeave)="dragOverColumn.set(null)"
          >
            <div class="flex items-center gap-2 mb-3">
              <div class="w-2 h-2 rounded-full bg-slate-500"></div>
              <span class="text-xs font-medium text-slate-600 uppercase tracking-wide">Todo</span>
              <span class="text-xs text-slate-400">{{ taskService.todoTasks().length }}</span>
            </div>
            <div class="space-y-2">
              @for (task of taskService.todoTasks(); track task.id) {
                <div
                  pDraggable="tasks"
                  (onDragStart)="onDragStart(task)"
                  (onDragEnd)="onDragEnd()"
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
            class="rounded-lg p-3 min-h-48 transition-all bg-sky-50/50"
            [class.bg-sky-100]="dragOverColumn() === 'InProgress'"
            pDroppable="tasks"
            (onDrop)="onDrop('InProgress')"
            (onDragEnter)="dragOverColumn.set('InProgress')"
            (onDragLeave)="dragOverColumn.set(null)"
          >
            <div class="flex items-center gap-2 mb-3">
              <div class="w-2 h-2 rounded-full bg-sky-600"></div>
              <span class="text-xs font-medium text-sky-700 uppercase tracking-wide">In Progress</span>
              <span class="text-xs text-sky-500">{{ taskService.inProgressTasks().length }}</span>
            </div>
            <div class="space-y-2">
              @for (task of taskService.inProgressTasks(); track task.id) {
                <div
                  pDraggable="tasks"
                  (onDragStart)="onDragStart(task)"
                  (onDragEnd)="onDragEnd()"
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
            class="rounded-lg p-3 min-h-48 transition-all bg-teal-50/50"
            [class.bg-teal-100]="dragOverColumn() === 'Done'"
            pDroppable="tasks"
            (onDrop)="onDrop('Done')"
            (onDragEnter)="dragOverColumn.set('Done')"
            (onDragLeave)="dragOverColumn.set(null)"
          >
            <div class="flex items-center gap-2 mb-3">
              <div class="w-2 h-2 rounded-full bg-teal-600"></div>
              <span class="text-xs font-medium text-teal-700 uppercase tracking-wide">Done</span>
              <span class="text-xs text-teal-500">{{ taskService.doneTasks().length }}</span>
            </div>
            <div class="space-y-2">
              @for (task of taskService.doneTasks(); track task.id) {
                <div
                  pDraggable="tasks"
                  (onDragStart)="onDragStart(task)"
                  (onDragEnd)="onDragEnd()"
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
      [modal]="true"
      [style]="{ width: '450px' }"
      [draggable]="false"
      [showHeader]="false"
      [contentStyle]="{ padding: 0 }"
    >
      <div class="p-6">
        <!-- Header with icon -->
        <div class="flex items-center gap-3 mb-6">
          <div class="w-12 h-12 rounded-xl bg-violet-100 flex items-center justify-center">
            <i class="pi pi-plus text-xl text-violet-600"></i>
          </div>
          <div>
            <h2 class="text-xl font-semibold text-gray-900">New Task</h2>
            <p class="text-sm text-gray-500">Add a task to your board</p>
          </div>
        </div>

        <!-- Input -->
        <div class="flex flex-col gap-2">
          <label for="taskTitle" class="text-sm font-medium text-gray-700">Task title</label>
          <input
            pInputText
            id="taskTitle"
            [value]="newTaskTitle()"
            (input)="newTaskTitle.set($any($event.target).value)"
            placeholder="e.g., Review pull request, Update documentation..."
            class="w-full"
            (keydown.enter)="createTask(newTaskTitle())"
          />
        </div>
      </div>

      <!-- Footer -->
      <div class="flex justify-end gap-2 px-6 py-4 bg-gray-50 border-t border-gray-100">
        <p-button
          label="Cancel"
          [text]="true"
          severity="secondary"
          (onClick)="showDialog.set(false)"
        />
        <p-button
          label="Create Task"
          icon="pi pi-check"
          (onClick)="createTask(newTaskTitle())"
        />
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

  ngOnInit(): void {
    this.taskService.loadTasks();
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
}
