import { Component, HostListener, inject, OnInit, viewChild, ChangeDetectionStrategy, computed, signal, ElementRef } from '@angular/core';
import { CdkDragDrop, CdkDropList } from '@angular/cdk/drag-drop';
import { TaskService } from './task.service';
import { ColumnComponent } from './column.component';
import { Task } from './task.model';

type TaskStatus = 'Todo' | 'InProgress' | 'Done';

@Component({
  selector: 'app-tasks-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ColumnComponent],
  template: `
    <div class="max-w-7xl mx-auto px-4 md:px-6 py-6 md:py-8">
      <!-- Header -->
      <div class="flex items-center justify-between mb-4">
        <h1 class="text-xl font-semibold text-foreground">Tasks</h1>
      </div>

      <!-- Search -->
      <div class="relative mb-6">
        <i class="pi pi-search absolute left-3 top-1/2 -translate-y-1/2 text-sm"
           [class.text-foreground-muted]="!searchQuery()"
           [class.text-primary]="searchQuery()"></i>
        <input
          #searchInput
          type="text"
          placeholder="Search tasks..."
          [value]="searchQuery()"
          (input)="searchQuery.set(asInput($event).value)"
          (keydown.escape)="clearSearch()"
          class="w-full pl-9 pr-16 py-2 bg-surface rounded-md text-sm text-foreground placeholder-foreground-muted border border-black/10 dark:border-white/15 focus:border-black/30 dark:focus:border-white/30 focus:outline-none transition-colors"
          aria-label="Search tasks"
        >
        @if (searchQuery()) {
          <button
            type="button"
            class="absolute right-9 top-1/2 -translate-y-1/2 text-foreground-muted hover:text-foreground transition-colors"
            (click)="clearSearch()"
            aria-label="Clear search"
          >
            <i class="pi pi-times text-sm"></i>
          </button>
        }
        <kbd class="absolute right-3 top-1/2 -translate-y-1/2 hidden md:inline px-1.5 py-0.5 text-xs text-foreground-muted bg-surface-hover border border-surface-border rounded font-sans">/</kbd>
      </div>

      <!-- Loading state -->
      @if (taskService.loading()) {
        <div class="flex items-center justify-center py-20">
          <i class="pi pi-spin pi-spinner text-3xl text-primary"></i>
        </div>
      } @else {
        <!-- Kanban Board -->
        <div class="grid grid-cols-1 md:grid-cols-3 gap-4 md:gap-6">
          <app-column
            #todoColumn
            status="Todo"
            label="Todo"
            [tasks]="filteredTodoTasks()"
            [connectedTo]="todoConnectedTo()"
            [showAddButton]="true"
            [showKbdHint]="!searchQuery()"
            [emptyMessage]="searchQuery() ? 'No matching tasks' : 'Press N to add your first task'"
            (onDrop)="drop($event, 'Todo')"
            (onEditTask)="updateTask($event.id, $event.title)"
            (onDeleteTask)="deleteTask($event)"
            (onTaskCreated)="createTask($event, 'Todo')"
            (onAddComment)="addComment($event.taskId, $event.content)"
            (onEditComment)="editComment($event.taskId, $event.commentId, $event.content)"
            (onDeleteComment)="deleteComment($event.taskId, $event.commentId)"
            (onSetDueDate)="setDueDate($event.taskId, $event.date)"
            (onClearDueDate)="clearDueDate($event.taskId)"
          />

          <app-column
            #inProgressColumn
            status="InProgress"
            label="In Progress"
            [tasks]="filteredInProgressTasks()"
            [connectedTo]="inProgressConnectedTo()"
            [showAddButton]="true"
            [emptyMessage]="searchQuery() ? 'No matching tasks' : 'Nothing in progress'"
            (onDrop)="drop($event, 'InProgress')"
            (onEditTask)="updateTask($event.id, $event.title)"
            (onDeleteTask)="deleteTask($event)"
            (onTaskCreated)="createTask($event, 'InProgress')"
            (onAddComment)="addComment($event.taskId, $event.content)"
            (onEditComment)="editComment($event.taskId, $event.commentId, $event.content)"
            (onDeleteComment)="deleteComment($event.taskId, $event.commentId)"
            (onSetDueDate)="setDueDate($event.taskId, $event.date)"
            (onClearDueDate)="clearDueDate($event.taskId)"
          />

          <app-column
            #doneColumn
            status="Done"
            [label]="showArchive() ? 'Archive' : 'Done'"
            [tasks]="filteredDoneColumnTasks()"
            [connectedTo]="doneConnectedTo()"
            [showAddButton]="false"
            [emptyMessage]="searchQuery() ? 'No matching tasks' : (showArchive() ? 'No archived tasks' : 'Complete some tasks!')"
            [archiveCount]="taskService.archivedCount()"
            [doneCount]="taskService.doneTasks().length"
            [showArchive]="showArchive()"
            (onArchiveToggle)="toggleArchive()"
            (onDrop)="drop($event, 'Done')"
            (onEditTask)="updateTask($event.id, $event.title)"
            (onDeleteTask)="deleteTask($event)"
            (onTaskCreated)="createTask($event, 'Done')"
            (onAddComment)="addComment($event.taskId, $event.content)"
            (onEditComment)="editComment($event.taskId, $event.commentId, $event.content)"
            (onDeleteComment)="deleteComment($event.taskId, $event.commentId)"
            (onSetDueDate)="setDueDate($event.taskId, $event.date)"
            (onClearDueDate)="clearDueDate($event.taskId)"
          />
        </div>
      }
    </div>
  `,
})
export class TasksPage implements OnInit {
  readonly taskService = inject(TaskService);

  readonly todoColumn = viewChild<ColumnComponent>('todoColumn');
  readonly inProgressColumn = viewChild<ColumnComponent>('inProgressColumn');
  readonly doneColumn = viewChild<ColumnComponent>('doneColumn');
  readonly searchInput = viewChild<ElementRef<HTMLInputElement>>('searchInput');

  readonly showArchive = signal(false);
  readonly searchQuery = signal('');

  readonly doneColumnTasks = computed(() =>
    this.showArchive()
      ? this.taskService.archivedTasks()
      : this.taskService.doneTasks()
  );

  // Filtered task lists based on search query
  readonly filteredTodoTasks = computed(() =>
    this.filterTasks(this.taskService.todoTasks())
  );

  readonly filteredInProgressTasks = computed(() =>
    this.filterTasks(this.taskService.inProgressTasks())
  );

  readonly filteredDoneColumnTasks = computed(() =>
    this.filterTasks(this.doneColumnTasks())
  );

  private filterTasks(tasks: Task[]): Task[] {
    const query = this.searchQuery().toLowerCase().trim();
    if (!query) {
      return tasks;
    }
    return tasks.filter(task =>
      task.title.toLowerCase().includes(query)
    );
  }

  readonly todoConnectedTo = computed(() => {
    const inProgress = this.inProgressColumn()?.dropList();
    const done = this.doneColumn()?.dropList();
    return [inProgress, done].filter((list): list is CdkDropList => !!list);
  });

  readonly inProgressConnectedTo = computed(() => {
    const todo = this.todoColumn()?.dropList();
    const done = this.doneColumn()?.dropList();
    return [todo, done].filter((list): list is CdkDropList => !!list);
  });

  readonly doneConnectedTo = computed(() => {
    const todo = this.todoColumn()?.dropList();
    const inProgress = this.inProgressColumn()?.dropList();
    return [todo, inProgress].filter((list): list is CdkDropList => !!list);
  });

  ngOnInit(): void {
    this.taskService.loadTasks();
    this.taskService.loadArchivedCount();
  }

  /** Type-safe helper for accessing input value from events */
  asInput(event: Event): HTMLInputElement {
    return event.target as HTMLInputElement;
  }

  clearSearch(): void {
    this.searchQuery.set('');
    this.searchInput()?.nativeElement.blur();
  }

  focusSearch(): void {
    this.searchInput()?.nativeElement.focus();
  }

  toggleArchive(): void {
    const newValue = !this.showArchive();
    this.showArchive.set(newValue);

    if (newValue) {
      this.taskService.loadArchivedTasks();
    } else {
      this.taskService.clearArchivedTasks();
    }
  }

  @HostListener('document:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    const target = event.target as HTMLElement;
    const isInInput = target.tagName === 'INPUT' || target.tagName === 'TEXTAREA' || target.isContentEditable;

    // '/' to focus search (only when not in an input)
    if (event.key === '/' && !isInInput && !event.metaKey && !event.ctrlKey) {
      event.preventDefault();
      this.focusSearch();
      return;
    }

    if (isInInput) {
      return;
    }

    // N to start inline task creation in Todo column (only when not searching)
    if (event.key.toLowerCase() === 'n' && !event.metaKey && !event.ctrlKey && !this.searchQuery()) {
      const todoCol = this.todoColumn();
      if (todoCol && !todoCol.isCreating()) {
        event.preventDefault();
        todoCol.startCreate();
      }
    }
  }

  createTask(title: string, status: TaskStatus): void {
    this.taskService.createTaskInColumn(title, status);
  }

  updateTask(id: string, title: string): void {
    this.taskService.updateTask(id, title);
  }

  deleteTask(id: string): void {
    this.taskService.deleteTask(id);
  }

  addComment(taskId: string, content: string): void {
    this.taskService.addComment(taskId, content);
  }

  editComment(taskId: string, commentId: string, content: string): void {
    this.taskService.updateComment(taskId, commentId, content);
  }

  deleteComment(taskId: string, commentId: string): void {
    this.taskService.deleteComment(taskId, commentId);
  }

  setDueDate(taskId: string, date: string): void {
    this.taskService.setDueDate(taskId, date);
  }

  clearDueDate(taskId: string): void {
    this.taskService.clearDueDate(taskId);
  }

  drop(event: CdkDragDrop<Task[]>, targetStatus: TaskStatus): void {
    const task = event.item.data as Task;

    if (event.previousContainer === event.container) {
      if (event.previousIndex !== event.currentIndex) {
        const tasks = event.container.data;
        const taskIds = tasks.map(t => t.id);
        taskIds.splice(event.previousIndex, 1);
        taskIds.splice(event.currentIndex, 0, task.id);
        this.taskService.reorderTasks(targetStatus, taskIds);
      }
    } else {
      this.taskService.changeStatus(task.id, targetStatus, event.currentIndex);
    }
  }
}
