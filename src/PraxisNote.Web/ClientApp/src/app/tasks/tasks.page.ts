import { Component, HostListener, inject, OnInit, AfterViewInit, OnDestroy, viewChild, viewChildren, ChangeDetectionStrategy, computed, signal, ElementRef, PLATFORM_ID, WritableSignal, DestroyRef, effect } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CdkDragDrop, CdkDropList } from '@angular/cdk/drag-drop';
import { TaskService } from './task.service';
import { TagService } from '../tags/tag.service';
import { ColumnComponent } from './column.component';
import { Task, TaskStatus, SortMode } from './task.model';
import { TaskTag } from '../tags/tag.model';
import { ToastService } from '../shared/services/toast.service';
import { ContextualHeaderService } from '../shared/services/contextual-header.service';
import { ErrorStateComponent } from '../shared/components/error-state.component';
import { PageContentComponent } from '../shared/components/page-content.component';

interface ColumnConfig {
  status: TaskStatus;
  label: string;
  tasks: Task[];
  showAddButton: boolean;
  emptyMessage: string;
  mobileEmptyMessage: string;
  archiveCount?: number;
  doneCount?: number;
  showArchive?: boolean;
  sortModeSignal: WritableSignal<SortMode>;
}

@Component({
  selector: 'app-tasks-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ColumnComponent, ErrorStateComponent, PageContentComponent],
  template: `
    <app-page-content>
      <h1 class="sr-only">Tasks</h1>
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
          class="w-full h-9 pl-9 pr-28 text-sm text-foreground-secondary placeholder-foreground-secondary bg-surface-muted hover:bg-surface-muted/80 focus:bg-surface-muted/80 rounded-lg focus:outline-none transition-colors duration-150"
          aria-label="Search tasks"
        >
        @if (searchQuery().trim()) {
          <span
            class="absolute right-12 top-1/2 -translate-y-1/2 text-xs text-foreground-muted"
            role="status"
            aria-live="polite"
          >
            {{ getSearchResultLabel(searchResultCount()) }}
          </span>
        }
        @if (searchQuery()) {
          <button
            type="button"
            class="absolute right-3 top-1/2 -translate-y-1/2 text-foreground-muted hover:text-foreground transition-colors"
            (click)="clearSearch()"
            aria-label="Clear search"
          >
            <i class="pi pi-times text-xs"></i>
          </button>
        } @else {
          <kbd class="absolute right-3 top-1/2 -translate-y-1/2 hidden md:inline px-1.5 py-0.5 text-xs text-foreground-muted bg-surface border border-border rounded font-sans">/</kbd>
        }
      </div>

      @if (taskService.error()) {
        <app-error-state
          title="Something went wrong"
          [message]="taskService.error()!"
          (retry)="taskService.loadTasks()"
        />
      } @else {
      <!-- Mobile: Segmented column indicator -->
      <div class="flex md:hidden gap-1 py-1" role="tablist" aria-label="Column navigation">
        <button
          type="button"
          class="flex-1 py-3"
          (click)="scrollToColumn(0)"
          role="tab"
          [attr.aria-selected]="activeColumn() === 0"
          [attr.tabindex]="activeColumn() === 0 ? 0 : -1"
          aria-label="Go to Todo column"
        >
          <span
            class="block w-full rounded-full transition-all"
            [class.h-2]="activeColumn() === 0"
            [class.h-1]="activeColumn() !== 0"
            [class.bg-indicator-todo-active]="activeColumn() === 0"
            [class.bg-indicator-todo-inactive]="activeColumn() !== 0"
          ></span>
        </button>
        <button
          type="button"
          class="flex-1 py-3"
          (click)="scrollToColumn(1)"
          role="tab"
          [attr.aria-selected]="activeColumn() === 1"
          [attr.tabindex]="activeColumn() === 1 ? 0 : -1"
          aria-label="Go to In Progress column"
        >
          <span
            class="block w-full rounded-full transition-all"
            [class.h-2]="activeColumn() === 1"
            [class.h-1]="activeColumn() !== 1"
            [class.bg-indicator-inprogress-active]="activeColumn() === 1"
            [class.bg-indicator-inprogress-inactive]="activeColumn() !== 1"
          ></span>
        </button>
        <button
          type="button"
          class="flex-1 py-3"
          (click)="scrollToColumn(2)"
          role="tab"
          [attr.aria-selected]="activeColumn() === 2"
          [attr.tabindex]="activeColumn() === 2 ? 0 : -1"
          aria-label="Go to Done column"
        >
          <span
            class="block w-full rounded-full transition-all"
            [class.h-2]="activeColumn() === 2"
            [class.h-1]="activeColumn() !== 2"
            [class.bg-indicator-done-active]="activeColumn() === 2"
            [class.bg-indicator-done-inactive]="activeColumn() !== 2"
          ></span>
        </button>
      </div>

      <!-- Mobile: Horizontal swipe navigation -->
      <div #mobileScrollContainer role="region" aria-label="Task columns" class="flex md:hidden overflow-x-auto snap-x snap-mandatory scrollbar-hide -mx-4">
        @for (col of columnConfigs(); track col.status) {
          <app-column
            class="flex-none w-screen px-4 snap-center"
            [status]="col.status"
            [label]="col.label"
            [showSkeleton]="!taskService.initialLoadComplete()"
            [tasks]="col.tasks"
            [connectedTo]="[]"
            [showAddButton]="col.showAddButton"
            [showKbdHint]="false"
            [searchQuery]="searchQuery()"
            [highlightedTaskId]="highlightedTaskId()"
            [emptyMessage]="col.mobileEmptyMessage"
            [archiveCount]="col.archiveCount ?? 0"
            [doneCount]="col.doneCount ?? 0"
            [showArchive]="col.showArchive ?? false"
            [allTags]="tagService.tags()"
            (onArchiveToggle)="toggleArchive()"
            (onDrop)="drop($event, col.status)"
            (onEditTask)="updateTask($event.id, $event.title)"
            (onDeleteTask)="deleteTask($event)"
            (onTaskCreated)="createTask($event, col.status)"
            (onAddComment)="addComment($event.taskId, $event.content)"
            (onEditComment)="editComment($event.taskId, $event.commentId, $event.content)"
            (onDeleteComment)="deleteComment($event.taskId, $event.commentId)"
            (onSetDueDate)="setDueDate($event.taskId, $event.date)"
            (onClearDueDate)="clearDueDate($event.taskId)"
            (onTogglePriority)="togglePriority($event.taskId)"
            (onStatusChange)="changeStatus($event.taskId, $event.status)"
            (onAddTag)="addTagToTask($event.taskId, $event.tag)"
            (onRemoveTag)="removeTagFromTask($event.taskId, $event.tagId)"
            (onCreateTag)="createAndAddTag($event.taskId, $event.name)"
            (onSortModeChange)="col.sortModeSignal.set($event)"
          />
        }
      </div>

      <!-- Desktop: Grid layout -->
      <div class="hidden md:grid md:grid-cols-3 gap-6">
        @for (col of columnConfigs(); track col.status) {
          <app-column
            #desktopColumn
            [status]="col.status"
            [label]="col.label"
            [showSkeleton]="!taskService.initialLoadComplete()"
            [tasks]="col.tasks"
            [connectedTo]="getConnectedTo(col.status)"
            [showAddButton]="col.showAddButton"
            [showKbdHint]="col.status === 'Todo' && !searchQuery()"
            [searchQuery]="searchQuery()"
            [highlightedTaskId]="highlightedTaskId()"
            [emptyMessage]="col.emptyMessage"
            [archiveCount]="col.archiveCount ?? 0"
            [doneCount]="col.doneCount ?? 0"
            [showArchive]="col.showArchive ?? false"
            [allTags]="tagService.tags()"
            (onArchiveToggle)="toggleArchive()"
            (onDrop)="drop($event, col.status)"
            (onEditTask)="updateTask($event.id, $event.title)"
            (onDeleteTask)="deleteTask($event)"
            (onTaskCreated)="createTask($event, col.status)"
            (onAddComment)="addComment($event.taskId, $event.content)"
            (onEditComment)="editComment($event.taskId, $event.commentId, $event.content)"
            (onDeleteComment)="deleteComment($event.taskId, $event.commentId)"
            (onSetDueDate)="setDueDate($event.taskId, $event.date)"
            (onClearDueDate)="clearDueDate($event.taskId)"
            (onTogglePriority)="togglePriority($event.taskId)"
            (onStatusChange)="changeStatus($event.taskId, $event.status)"
            (onAddTag)="addTagToTask($event.taskId, $event.tag)"
            (onRemoveTag)="removeTagFromTask($event.taskId, $event.tagId)"
            (onCreateTag)="createAndAddTag($event.taskId, $event.name)"
            (onSortModeChange)="col.sortModeSignal.set($event)"
          />
        }
      </div>
      }
    </app-page-content>
  `,
})
export class TasksPage implements OnInit, AfterViewInit, OnDestroy {
  readonly taskService = inject(TaskService);
  readonly tagService = inject(TagService);
  private readonly toastService = inject(ToastService);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly headerService = inject(ContextualHeaderService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  // Desktop column refs for drag-drop connectedTo and keyboard shortcuts
  readonly desktopColumns = viewChildren<ColumnComponent>('desktopColumn');
  readonly searchInput = viewChild<ElementRef<HTMLInputElement>>('searchInput');
  readonly mobileScrollContainer = viewChild<ElementRef<HTMLElement>>('mobileScrollContainer');

  readonly showArchive = signal(false);
  readonly searchQuery = signal('');
  readonly activeColumn = signal(0);
  readonly columnLabels = ['Todo', 'In Progress', 'Done'] as const;
  readonly highlightedTaskId = signal('');
  private highlightTimeout: ReturnType<typeof setTimeout> | null = null;

  private scrollListener: (() => void) | null = null;
  readonly todoSortMode = signal<SortMode>('manual');
  readonly inProgressSortMode = signal<SortMode>('manual');
  readonly doneSortMode = signal<SortMode>('manual');
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

  readonly searchResultCount = computed(() => {
    if (!this.searchQuery().trim()) return 0;
    return this.filteredTodoTasks().length +
           this.filteredInProgressTasks().length +
           this.filteredDoneColumnTasks().length;
  });

  /** Column configurations for DRY template rendering */
  readonly columnConfigs = computed<ColumnConfig[]>(() => {
    const searching = !!this.searchQuery();
    return [
      {
        status: 'Todo' as const,
        label: 'Todo',
        tasks: this.filteredTodoTasks(),
        showAddButton: true,
        emptyMessage: searching ? 'No matching tasks' : 'Press N to add your first task',
        mobileEmptyMessage: searching ? 'No matching tasks' : 'Tap + to add your first task',
        sortModeSignal: this.todoSortMode,
      },
      {
        status: 'InProgress' as const,
        label: 'In Progress',
        tasks: this.filteredInProgressTasks(),
        showAddButton: true,
        emptyMessage: searching ? 'No matching tasks' : 'Nothing in progress',
        mobileEmptyMessage: searching ? 'No matching tasks' : 'Nothing in progress',
        sortModeSignal: this.inProgressSortMode,
      },
      {
        status: 'Done' as const,
        label: this.showArchive() ? 'Archive' : 'Done',
        tasks: this.filteredDoneColumnTasks(),
        showAddButton: false,
        emptyMessage: searching ? 'No matching tasks' : (this.showArchive() ? 'No archived tasks' : 'Complete some tasks!'),
        mobileEmptyMessage: searching ? 'No matching tasks' : (this.showArchive() ? 'No archived tasks' : 'Complete some tasks!'),
        archiveCount: this.taskService.archivedCount(),
        doneCount: this.taskService.doneTasks().length,
        showArchive: this.showArchive(),
        sortModeSignal: this.doneSortMode,
      },
    ];
  });

  private filterTasks(tasks: Task[]): Task[] {
    const query = this.searchQuery().toLowerCase().trim();
    if (!query) {
      return tasks;
    }
    return tasks.filter(task =>
      task.title.toLowerCase().includes(query)
    );
  }

  /** Get connected drop lists for a column (excludes self, returns empty when searching) */
  getConnectedTo(status: TaskStatus): CdkDropList[] {
    if (this.searchQuery()) return [];
    const columns = this.desktopColumns();
    // Columns are ordered: 0=Todo, 1=InProgress, 2=Done
    const excludeIndex = status === 'Todo' ? 0 : status === 'InProgress' ? 1 : 2;
    return columns
      .filter((_, i) => i !== excludeIndex)
      .map(col => col.dropList())
      .filter((list): list is CdkDropList => !!list);
  }

  /** Pending highlight task ID from query param, waiting for data to load */
  private pendingHighlightId = '';

  constructor() {
    // Watch for initial load to complete, then trigger pending highlight
    effect(() => {
      const loaded = this.taskService.initialLoadComplete();
      if (loaded && this.pendingHighlightId) {
        const taskId = this.pendingHighlightId;
        this.pendingHighlightId = '';
        // Use setTimeout to let the DOM render the task cards first
        setTimeout(() => this.scrollAndHighlight(taskId), 100);
      }
    });

    this.destroyRef.onDestroy(() => {
      if (this.highlightTimeout) {
        clearTimeout(this.highlightTimeout);
      }
    });
  }

  ngOnInit(): void {
    this.headerService.breadcrumb.set([{ label: 'Tasks' }]);
    this.taskService.loadTasks();
    this.taskService.loadArchivedCount();
    this.tagService.loadTags();

    // Read highlight query param
    this.route.queryParams
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(params => {
        const highlightId = params['highlight'];
        if (highlightId) {
          if (this.taskService.initialLoadComplete()) {
            // Data already loaded, highlight immediately
            setTimeout(() => this.scrollAndHighlight(highlightId), 100);
          } else {
            // Store pending highlight for when data loads
            this.pendingHighlightId = highlightId;
          }
        }
      });
  }

  ngAfterViewInit(): void {
    if (isPlatformBrowser(this.platformId)) {
      this.setupScrollObserver();
    }
  }

  ngOnDestroy(): void {
    this.headerService.clearContext();
    if (this.scrollListener) {
      const container = this.mobileScrollContainer()?.nativeElement;
      container?.removeEventListener('scroll', this.scrollListener);
      this.scrollListener = null;
    }
  }

  /**
   * Scroll to a task card and apply the highlight glow effect.
   * Handles both desktop and mobile layouts.
   */
  private scrollAndHighlight(taskId: string): void {
    // Clear any existing highlight
    if (this.highlightTimeout) {
      clearTimeout(this.highlightTimeout);
      this.highlightTimeout = null;
    }

    // Determine which column the task is in
    const columnIndex = this.getColumnIndexForTask(taskId);
    if (columnIndex === -1) {
      // Task not found - clean up query param and bail
      this.cleanupQueryParam();
      return;
    }

    const isMobile = !isPlatformBrowser(this.platformId) ? false :
      window.matchMedia('(max-width: 767px)').matches;

    if (isMobile) {
      // Mobile: scroll to the correct column first, then scroll to the task card
      this.scrollToColumn(columnIndex);
      // Wait for column scroll to settle, then scroll to card
      setTimeout(() => {
        this.applyHighlightAndScroll(taskId);
      }, 350);
    } else {
      // Desktop: scroll to the card directly
      this.applyHighlightAndScroll(taskId);
    }
  }

  /** Apply highlight signal and scroll the task card into view */
  private applyHighlightAndScroll(taskId: string): void {
    this.highlightedTaskId.set(taskId);

    // Find the task card element in the DOM
    const cardEl = document.querySelector(`[data-task-id="${taskId}"]`);
    if (cardEl) {
      cardEl.scrollIntoView({ behavior: 'smooth', block: 'center' });
    }

    // Remove highlight after 2.5s (CSS transition handles fade-out)
    this.highlightTimeout = setTimeout(() => {
      this.highlightedTaskId.set('');
      this.highlightTimeout = null;
    }, 2500);

    // Clean up the query param from the URL
    this.cleanupQueryParam();
  }

  /** Determine which column (0=Todo, 1=InProgress, 2=Done) a task belongs to */
  private getColumnIndexForTask(taskId: string): number {
    if (this.taskService.todoTasks().some(t => t.id === taskId)) return 0;
    if (this.taskService.inProgressTasks().some(t => t.id === taskId)) return 1;
    if (this.taskService.doneTasks().some(t => t.id === taskId)) return 2;
    return -1;
  }

  /** Remove the highlight query parameter from the URL without triggering navigation */
  private cleanupQueryParam(): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {},
      replaceUrl: true,
    });
  }

  private setupScrollObserver(): void {
    const container = this.mobileScrollContainer()?.nativeElement;
    if (!container) return;

    // Calculate active column based on scroll position
    this.scrollListener = () => {
      const scrollLeft = container.scrollLeft;
      const columnWidth = container.children[0]?.clientWidth || container.clientWidth;
      const newIndex = Math.round(scrollLeft / columnWidth);
      const clampedIndex = Math.max(0, Math.min(newIndex, 2));

      if (this.activeColumn() !== clampedIndex) {
        this.activeColumn.set(clampedIndex);
      }
    };

    container.addEventListener('scroll', this.scrollListener, { passive: true });
  }

  scrollToColumn(index: number): void {
    const container = this.mobileScrollContainer()?.nativeElement;
    if (!container) return;

    const column = container.children[index] as HTMLElement;
    column?.scrollIntoView({ behavior: 'smooth', inline: 'center', block: 'nearest' });
  }

  /** Type-safe helper for accessing input value from events */
  asInput(event: Event): HTMLInputElement {
    return event.target as HTMLInputElement;
  }

  /** Format search result count for display */
  getSearchResultLabel(count: number): string {
    if (count === 0) return 'No results';
    return count === 1 ? '1 result' : `${count} results`;
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
      const todoCol = this.desktopColumns()[0]; // First column is Todo
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
    const deletedTask = this.taskService.deleteTaskWithUndo(id);
    if (deletedTask) {
      this.toastService.success({
        summary: 'Task deleted',
        action: {
          label: 'Undo',
          callback: () => {
            this.taskService.undoDelete(id);
            this.toastService.clear(); // Dismiss toast immediately after undo
          },
        },
        life: 5000,
      });
    }
  }

  addComment(taskId: string, content: string): void {
    this.taskService.addComment(taskId, content);
  }

  editComment(taskId: string, commentId: string, content: string): void {
    this.taskService.updateComment(taskId, commentId, content);
  }

  deleteComment(taskId: string, commentId: string): void {
    const deletedComment = this.taskService.deleteCommentWithUndo(taskId, commentId);
    if (deletedComment) {
      this.toastService.success({
        summary: 'Comment deleted',
        action: {
          label: 'Undo',
          callback: () => {
            this.taskService.undoCommentDelete(commentId);
            this.toastService.clear();
          },
        },
        life: 5000,
      });
    }
  }

  setDueDate(taskId: string, date: string): void {
    this.taskService.setDueDate(taskId, date);
  }

  clearDueDate(taskId: string): void {
    this.taskService.clearDueDate(taskId);
  }

  togglePriority(taskId: string): void {
    this.taskService.togglePriority(taskId);
  }

  changeStatus(taskId: string, status: TaskStatus): void {
    // Place at bottom of target column for mobile quick status changes
    const targetIndex =
      status === 'Todo'
        ? this.taskService.todoTasks().length
        : status === 'InProgress'
          ? this.taskService.inProgressTasks().length
          : this.taskService.doneTasks().length;
    this.taskService.changeStatus(taskId, status, targetIndex);
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

  addTagToTask(taskId: string, tag: TaskTag): void {
    this.taskService.addTagToTask(
      taskId,
      tag,
      () => this.tagService.incrementUsageCount(tag.id, 'task'),
      () => this.tagService.decrementUsageCount(tag.id, 'task')
    );
  }

  removeTagFromTask(taskId: string, tagId: string): void {
    this.taskService.removeTagFromTask(
      taskId,
      tagId,
      () => this.tagService.decrementUsageCount(tagId, 'task'),
      () => this.tagService.incrementUsageCount(tagId, 'task')
    );
  }

  createAndAddTag(taskId: string, name: string): void {
    this.tagService.createTag(name, (createdTag) => {
      // After tag is created, add it to the task
      const taskTag: TaskTag = { id: createdTag.id, name: createdTag.name };
      this.taskService.addTagToTask(
        taskId,
        taskTag,
        () => this.tagService.incrementUsageCount(createdTag.id, 'task')
      );
    });
  }
}
