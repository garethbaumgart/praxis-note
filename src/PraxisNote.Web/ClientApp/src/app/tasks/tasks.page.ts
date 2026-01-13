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
        <i class="pi pi-search absolute left-3 top-1/2 -translate-y-1/2 text-xs text-foreground-secondary"></i>
        <input
          #searchInput
          type="text"
          placeholder="Search"
          [value]="searchQuery()"
          (input)="searchQuery.set(asInput($event).value)"
          (keydown.escape)="clearSearch()"
          class="w-full h-9 pl-9 pr-16 text-sm text-foreground-secondary placeholder-foreground-secondary bg-surface-muted hover:bg-surface-muted/80 focus:bg-surface-muted/80 rounded-lg focus:outline-none transition-colors duration-150"
          aria-label="Search tasks"
        >
        @if (searchQuery()) {
          <button
            type="button"
            class="absolute right-9 top-1/2 -translate-y-1/2 text-foreground-muted hover:text-foreground transition-colors"
            (click)="clearSearch()"
            aria-label="Clear search"
          >
            <i class="pi pi-times text-xs"></i>
          </button>
        }
        @if (!searchQuery()) {
          <kbd class="absolute right-3 top-1/2 -translate-y-1/2 hidden md:inline px-1.5 py-0.5 text-xs text-foreground-muted bg-surface border border-border rounded font-sans">/</kbd>
        }
      </div>

      <!-- Kanban Board -->
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4 md:gap-6">
        <app-column
          #todoColumn
          status="Todo"
          label="Todo"
          [showSkeleton]="!taskService.initialLoadComplete()"
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
          (onSortModeChange)="todoSortMode.set($event)"
          [showSortMenu]="activeSortMenu() === 'Todo'"
          (onSortMenuToggle)="toggleSortMenu('Todo')"
        />

        <app-column
          #inProgressColumn
          status="InProgress"
          label="In Progress"
          [showSkeleton]="!taskService.initialLoadComplete()"
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
          (onSortModeChange)="inProgressSortMode.set($event)"
          [showSortMenu]="activeSortMenu() === 'InProgress'"
          (onSortMenuToggle)="toggleSortMenu('InProgress')"
        />

        <app-column
          #doneColumn
          status="Done"
          [label]="showArchive() ? 'Archive' : 'Done'"
          [showSkeleton]="!taskService.initialLoadComplete()"
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
          (onSortModeChange)="doneSortMode.set($event)"
          [showSortMenu]="activeSortMenu() === 'Done'"
          (onSortMenuToggle)="toggleSortMenu('Done')"
        />
      </div>
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
  readonly todoSortMode = signal<'manual' | 'dueDate'>('manual');
  readonly inProgressSortMode = signal<'manual' | 'dueDate'>('manual');
  readonly doneSortMode = signal<'manual' | 'dueDate'>('manual');
  readonly activeSortMenu = signal<'Todo' | 'InProgress' | 'Done' | null>(null);

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
    if (this.searchQuery() || this.todoSortMode() !== 'manual') return [];
    const inProgress = this.inProgressColumn()?.dropList();
    const done = this.doneColumn()?.dropList();
    return [inProgress, done].filter((list): list is CdkDropList => !!list);
  });

  readonly inProgressConnectedTo = computed(() => {
    if (this.searchQuery() || this.inProgressSortMode() !== 'manual') return [];
    const todo = this.todoColumn()?.dropList();
    const done = this.doneColumn()?.dropList();
    return [todo, done].filter((list): list is CdkDropList => !!list);
  });

  readonly doneConnectedTo = computed(() => {
    if (this.searchQuery() || this.doneSortMode() !== 'manual') return [];
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

  @HostListener('document:click')
  onDocumentClick(): void {
    this.activeSortMenu.set(null);
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

  toggleSortMenu(column: 'Todo' | 'InProgress' | 'Done'): void {
    this.activeSortMenu.update(current => current === column ? null : column);
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
