import { Component, inject, OnInit, signal } from '@angular/core';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { TaskService } from './task.service';
import { TaskCardComponent } from './task-card.component';

@Component({
  selector: 'app-tasks-page',
  standalone: true,
  imports: [Button, Dialog, InputText, TaskCardComponent],
  template: `
    <div class="max-w-7xl mx-auto px-6 py-8">
      <!-- Header -->
      <div class="flex items-center justify-between mb-8">
        <div>
          <h1 class="text-2xl font-bold text-gray-900">Tasks</h1>
          <p class="text-gray-500 mt-1">Manage your tasks across the board</p>
        </div>
        <p-button
          label="Add Task"
          icon="pi pi-plus"
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
          <div class="bg-gray-50 rounded-xl p-4">
            <div class="flex items-center gap-2 mb-4">
              <div class="w-3 h-3 rounded-full bg-gray-400"></div>
              <h2 class="font-semibold text-gray-700">Todo</h2>
              <span class="text-sm text-gray-400">({{ taskService.todoTasks().length }})</span>
            </div>
            <div class="space-y-3">
              @for (task of taskService.todoTasks(); track task.id) {
                <app-task-card
                  [task]="task"
                  (onStatusChange)="changeStatus(task.id, $event)"
                  (onEdit)="updateTask(task.id, $event)"
                  (onDelete)="deleteTask(task.id)"
                />
              } @empty {
                <p class="text-sm text-gray-400 text-center py-8">No tasks yet</p>
              }
            </div>
          </div>

          <!-- In Progress Column -->
          <div class="bg-blue-50 rounded-xl p-4">
            <div class="flex items-center gap-2 mb-4">
              <div class="w-3 h-3 rounded-full bg-blue-500"></div>
              <h2 class="font-semibold text-blue-700">In Progress</h2>
              <span class="text-sm text-blue-400">({{ taskService.inProgressTasks().length }})</span>
            </div>
            <div class="space-y-3">
              @for (task of taskService.inProgressTasks(); track task.id) {
                <app-task-card
                  [task]="task"
                  (onStatusChange)="changeStatus(task.id, $event)"
                  (onEdit)="updateTask(task.id, $event)"
                  (onDelete)="deleteTask(task.id)"
                />
              } @empty {
                <p class="text-sm text-blue-400 text-center py-8">Nothing in progress</p>
              }
            </div>
          </div>

          <!-- Done Column -->
          <div class="bg-green-50 rounded-xl p-4">
            <div class="flex items-center gap-2 mb-4">
              <div class="w-3 h-3 rounded-full bg-green-500"></div>
              <h2 class="font-semibold text-green-700">Done</h2>
              <span class="text-sm text-green-400">({{ taskService.doneTasks().length }})</span>
            </div>
            <div class="space-y-3">
              @for (task of taskService.doneTasks(); track task.id) {
                <app-task-card
                  [task]="task"
                  (onStatusChange)="changeStatus(task.id, $event)"
                  (onEdit)="updateTask(task.id, $event)"
                  (onDelete)="deleteTask(task.id)"
                />
              } @empty {
                <p class="text-sm text-green-400 text-center py-8">Complete some tasks!</p>
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

  ngOnInit(): void {
    this.taskService.loadTasks();
  }

  createTask(title: string): void {
    console.log('createTask called with:', title);
    if (title.trim()) {
      console.log('Calling taskService.createTask');
      this.taskService.createTask(title.trim());
      this.newTaskTitle.set('');
      this.showDialog.set(false);
    } else {
      console.log('Title was empty or whitespace');
    }
  }

  updateTask(id: string, title: string): void {
    this.taskService.updateTask(id, title);
  }

  changeStatus(id: string, status: 'Todo' | 'InProgress' | 'Done'): void {
    this.taskService.changeStatus(id, status);
  }

  deleteTask(id: string): void {
    this.taskService.deleteTask(id);
  }
}
