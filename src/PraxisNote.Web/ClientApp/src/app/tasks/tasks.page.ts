import { Component, ElementRef, HostListener, inject, OnInit, signal, viewChild, viewChildren, AfterViewChecked } from '@angular/core';
import { CdkDragDrop, CdkDrag, CdkDropList, CdkDragPlaceholder } from '@angular/cdk/drag-drop';
import { Dialog } from 'primeng/dialog';
import { TaskService } from './task.service';
import { TaskCardComponent } from './task-card.component';
import { Task } from './task.model';

type TaskStatus = 'Todo' | 'InProgress' | 'Done';

@Component({
  selector: 'app-tasks-page',
  standalone: true,
  imports: [Dialog, TaskCardComponent, CdkDropList, CdkDrag, CdkDragPlaceholder],
  template: `
    <div class="max-w-7xl mx-auto px-4 md:px-6 py-6 md:py-8">
      <!-- Header -->
      <div class="flex items-center justify-between mb-6">
        <h1 class="text-xl font-semibold text-foreground">Tasks</h1>
        <button
          class="flex items-center gap-2 h-9 px-3 text-sm font-medium text-foreground bg-accent hover:bg-accent-hover rounded-lg transition-colors"
          (click)="showDialog.set(true)"
        >
          <i class="pi pi-plus text-accent-foreground"></i>
          <span>Add Task</span>
          <kbd class="hidden md:inline ml-1 px-1.5 py-0.5 text-xs text-accent-foreground bg-accent rounded font-sans">&#8984;&#8679;N</kbd>
        </button>
      </div>

      <!-- Loading state -->
      @if (taskService.loading()) {
        <div class="flex items-center justify-center py-20">
          <i class="pi pi-spin pi-spinner text-3xl text-primary"></i>
        </div>
      } @else {
        <!-- Kanban Board -->
        <div class="grid grid-cols-1 md:grid-cols-3 gap-4 md:gap-6">
          <!-- Todo Column -->
          <div class="rounded-lg p-3 min-h-48 transition-all bg-todo">
            <div class="flex items-center justify-between mb-3">
              <div class="flex items-center gap-2">
                <span class="text-xs font-medium text-todo-foreground uppercase tracking-wide">Todo</span>
                <span class="text-xs text-todo-foreground-muted">{{ taskService.todoTasks().length }}</span>
              </div>
              @if (inlineCreatingColumn() !== 'Todo') {
                <button
                  class="flex items-center justify-center w-6 h-6 text-todo-foreground-muted hover:text-todo-foreground hover:bg-todo-hover rounded transition-colors"
                  (click)="startInlineCreate('Todo')"
                  aria-label="Add task to Todo"
                >
                  <i class="pi pi-plus text-xs"></i>
                </button>
              }
            </div>
            <div
              cdkDropList
              #todoList="cdkDropList"
              [cdkDropListData]="taskService.todoTasks()"
              [cdkDropListConnectedTo]="[inProgressList, doneList]"
              (cdkDropListDropped)="drop($event, 'Todo')"
              class="space-y-2 min-h-12"
            >
              <!-- Inline task creation -->
              @if (inlineCreatingColumn() === 'Todo') {
                <div class="bg-surface border border-todo-border rounded-md p-3 shadow-sm">
                  <input
                    #inlineInput
                    type="text"
                    [value]="inlineTaskTitle()"
                    (input)="inlineTaskTitle.set($any($event.target).value)"
                    (keydown.enter)="submitInlineCreate()"
                    (keydown.escape)="cancelInlineCreate()"
                    (blur)="onInlineBlur()"
                    placeholder="Task name..."
                    class="w-full text-sm font-medium text-foreground placeholder-foreground-muted border-0 focus:outline-none focus:ring-0 p-0 bg-transparent"
                  />
                </div>
              }
              @for (task of taskService.todoTasks(); track task.id) {
                <div cdkDrag [cdkDragData]="task" class="cursor-grab active:cursor-grabbing touch-manipulation">
                  <app-task-card
                    [task]="task"
                    (onEdit)="updateTask(task.id, $event)"
                    (onDelete)="deleteTask(task.id)"
                  />
                  <!-- Drag placeholder -->
                  <div *cdkDragPlaceholder class="bg-todo-hover border-2 border-dashed border-todo-border rounded-md h-16"></div>
                </div>
              } @empty {
                @if (inlineCreatingColumn() !== 'Todo') {
                  <p class="text-sm text-foreground-muted text-center py-8">No tasks yet</p>
                }
              }
            </div>
          </div>

          <!-- In Progress Column -->
          <div class="rounded-lg p-3 min-h-48 transition-all bg-inprogress">
            <div class="flex items-center justify-between mb-3">
              <div class="flex items-center gap-2">
                <span class="text-xs font-medium text-inprogress-foreground uppercase tracking-wide">In Progress</span>
                <span class="text-xs text-inprogress-foreground-muted">{{ taskService.inProgressTasks().length }}</span>
              </div>
              @if (inlineCreatingColumn() !== 'InProgress') {
                <button
                  class="flex items-center justify-center w-6 h-6 text-inprogress-foreground-muted hover:text-inprogress-foreground hover:bg-inprogress-hover rounded transition-colors"
                  (click)="startInlineCreate('InProgress')"
                  aria-label="Add task to In Progress"
                >
                  <i class="pi pi-plus text-xs"></i>
                </button>
              }
            </div>
            <div
              cdkDropList
              #inProgressList="cdkDropList"
              [cdkDropListData]="taskService.inProgressTasks()"
              [cdkDropListConnectedTo]="[todoList, doneList]"
              (cdkDropListDropped)="drop($event, 'InProgress')"
              class="space-y-2 min-h-12"
            >
              <!-- Inline task creation -->
              @if (inlineCreatingColumn() === 'InProgress') {
                <div class="bg-surface border border-inprogress-border rounded-md p-3 shadow-sm">
                  <input
                    #inlineInput
                    type="text"
                    [value]="inlineTaskTitle()"
                    (input)="inlineTaskTitle.set($any($event.target).value)"
                    (keydown.enter)="submitInlineCreate()"
                    (keydown.escape)="cancelInlineCreate()"
                    (blur)="onInlineBlur()"
                    placeholder="Task name..."
                    class="w-full text-sm font-medium text-foreground placeholder-foreground-muted border-0 focus:outline-none focus:ring-0 p-0 bg-transparent"
                  />
                </div>
              }
              @for (task of taskService.inProgressTasks(); track task.id) {
                <div cdkDrag [cdkDragData]="task" class="cursor-grab active:cursor-grabbing touch-manipulation">
                  <app-task-card
                    [task]="task"
                    (onEdit)="updateTask(task.id, $event)"
                    (onDelete)="deleteTask(task.id)"
                  />
                  <div *cdkDragPlaceholder class="bg-inprogress-hover border-2 border-dashed border-inprogress-border rounded-md h-16"></div>
                </div>
              } @empty {
                @if (inlineCreatingColumn() !== 'InProgress') {
                  <p class="text-sm text-foreground-muted text-center py-8">Nothing in progress</p>
                }
              }
            </div>
          </div>

          <!-- Done Column -->
          <div class="rounded-lg p-3 min-h-48 transition-all bg-done">
            <div class="flex items-center justify-between mb-3">
              <div class="flex items-center gap-2">
                <span class="text-xs font-medium text-done-foreground uppercase tracking-wide">Done</span>
                <span class="text-xs text-done-foreground-muted">{{ taskService.doneTasks().length }}</span>
              </div>
              @if (inlineCreatingColumn() !== 'Done') {
                <button
                  class="flex items-center justify-center w-6 h-6 text-done-foreground-muted hover:text-done-foreground hover:bg-done-hover rounded transition-colors"
                  (click)="startInlineCreate('Done')"
                  aria-label="Add task to Done"
                >
                  <i class="pi pi-plus text-xs"></i>
                </button>
              }
            </div>
            <div
              cdkDropList
              #doneList="cdkDropList"
              [cdkDropListData]="taskService.doneTasks()"
              [cdkDropListConnectedTo]="[todoList, inProgressList]"
              (cdkDropListDropped)="drop($event, 'Done')"
              class="space-y-2 min-h-12"
            >
              <!-- Inline task creation -->
              @if (inlineCreatingColumn() === 'Done') {
                <div class="bg-surface border border-done-border rounded-md p-3 shadow-sm">
                  <input
                    #inlineInput
                    type="text"
                    [value]="inlineTaskTitle()"
                    (input)="inlineTaskTitle.set($any($event.target).value)"
                    (keydown.enter)="submitInlineCreate()"
                    (keydown.escape)="cancelInlineCreate()"
                    (blur)="onInlineBlur()"
                    placeholder="Task name..."
                    class="w-full text-sm font-medium text-foreground placeholder-foreground-muted border-0 focus:outline-none focus:ring-0 p-0 bg-transparent"
                  />
                </div>
              }
              @for (task of taskService.doneTasks(); track task.id) {
                <div cdkDrag [cdkDragData]="task" class="cursor-grab active:cursor-grabbing touch-manipulation">
                  <app-task-card
                    [task]="task"
                    (onEdit)="updateTask(task.id, $event)"
                    (onDelete)="deleteTask(task.id)"
                  />
                  <div *cdkDragPlaceholder class="bg-done-hover border-2 border-dashed border-done-border rounded-md h-16"></div>
                </div>
              } @empty {
                @if (inlineCreatingColumn() !== 'Done') {
                  <p class="text-sm text-foreground-muted text-center py-8">Complete some tasks!</p>
                }
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
          class="w-full text-lg font-medium text-foreground placeholder-foreground-muted border-0 focus:outline-none focus:ring-0 p-0 bg-transparent"
          (keydown.enter)="createTask(newTaskTitle())"
          (keydown.escape)="showDialog.set(false)"
        />
        <div class="flex items-center justify-between mt-4 pt-4 border-t border-border-muted">
          <div class="flex items-center gap-2 text-sm text-foreground-secondary">
            <i class="pi pi-inbox"></i>
            <span>Todo</span>
          </div>
          <div class="flex gap-2">
            <button
              type="button"
              class="px-3 py-1.5 text-sm text-foreground-secondary hover:bg-surface-muted rounded-md transition-colors"
              (click)="showDialog.set(false)"
            >
              Cancel
            </button>
            <button
              type="button"
              class="px-3 py-1.5 text-sm font-medium text-white bg-accent-solid hover:bg-accent-solid-hover rounded-md transition-colors"
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
export class TasksPage implements OnInit, AfterViewChecked {
  readonly taskService = inject(TaskService);
  readonly showDialog = signal(false);
  readonly newTaskTitle = signal('');

  // Inline task creation state
  readonly inlineCreatingColumn = signal<TaskStatus | null>(null);
  readonly inlineTaskTitle = signal('');
  private shouldFocusInlineInput = false;

  readonly taskInput = viewChild<ElementRef<HTMLInputElement>>('taskInput');
  readonly inlineInputs = viewChildren<ElementRef<HTMLInputElement>>('inlineInput');

  ngOnInit(): void {
    this.taskService.loadTasks();
  }

  ngAfterViewChecked(): void {
    if (this.shouldFocusInlineInput) {
      const inputs = this.inlineInputs();
      if (inputs.length > 0) {
        inputs[0].nativeElement.focus();
        this.shouldFocusInlineInput = false;
      }
    }
  }

  onDialogShow(): void {
    // Focus the input after a brief delay to ensure the dialog is fully rendered
    setTimeout(() => this.taskInput()?.nativeElement.focus(), 0);
  }

  startInlineCreate(column: TaskStatus): void {
    this.inlineCreatingColumn.set(column);
    this.inlineTaskTitle.set('');
    this.shouldFocusInlineInput = true;
  }

  cancelInlineCreate(): void {
    this.inlineCreatingColumn.set(null);
    this.inlineTaskTitle.set('');
  }

  onInlineBlur(): void {
    // Small delay to allow click events to fire first
    setTimeout(() => {
      if (this.inlineTaskTitle().trim()) {
        this.submitInlineCreate();
      } else {
        this.cancelInlineCreate();
      }
    }, 100);
  }

  submitInlineCreate(): void {
    const title = this.inlineTaskTitle().trim();
    const column = this.inlineCreatingColumn();

    if (!title || !column) {
      this.cancelInlineCreate();
      return;
    }

    this.taskService.createTaskInColumn(title, column);
    this.cancelInlineCreate();
  }

  @HostListener('document:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    // Ignore if user is typing in an input field
    const target = event.target as HTMLElement;
    if (target.tagName === 'INPUT' || target.tagName === 'TEXTAREA' || target.isContentEditable) {
      return;
    }

    // Cmd+Shift+N to open add task dialog
    if (event.key.toLowerCase() === 'n' && event.metaKey && event.shiftKey && !this.showDialog()) {
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

  drop(event: CdkDragDrop<Task[]>, targetStatus: 'Todo' | 'InProgress' | 'Done'): void {
    const task = event.item.data as Task;

    if (event.previousContainer === event.container) {
      // Same column reorder
      if (event.previousIndex !== event.currentIndex) {
        const tasks = event.container.data;
        const taskIds = tasks.map(t => t.id);
        // Simulate the move to get new order
        taskIds.splice(event.previousIndex, 1);
        taskIds.splice(event.currentIndex, 0, task.id);
        this.taskService.reorderTasks(targetStatus, taskIds);
      }
    } else {
      // Cross-column move
      this.taskService.changeStatus(task.id, targetStatus);

      // If dropping at a specific position, also reorder
      if (event.currentIndex !== event.container.data.length) {
        const targetTasks = event.container.data;
        const taskIds = targetTasks.map(t => t.id);

        // Remove existing occurrence if present (avoids duplicates)
        const existingIndex = taskIds.indexOf(task.id);
        if (existingIndex !== -1) {
          taskIds.splice(existingIndex, 1);
        }

        // Insert at the desired drop index
        taskIds.splice(event.currentIndex, 0, task.id);
        this.taskService.reorderTasks(targetStatus, taskIds);
      }
    }
  }
}
