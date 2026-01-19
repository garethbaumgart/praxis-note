import { Component, computed, ElementRef, input, output, signal, viewChild, inject, Injector, afterNextRender, ChangeDetectionStrategy, DestroyRef, HostListener } from '@angular/core';
import { NgClass } from '@angular/common';
import { Task, Comment } from './task.model';
import { AutoResizeDirective } from '../shared/directives/auto-resize.directive';
import { StatusColorPipe } from '../shared/pipes/status-color.pipe';
import { LinkifyPipe } from '../shared/pipes/linkify.pipe';
import { HighlightPipe } from '../shared/pipes/highlight.pipe';
import { DeleteConfirmationService } from '../shared/services/delete-confirmation.service';
import { DeleteConfirmButtonComponent } from '../shared/components/delete-confirm-button.component';
import { DatePickerPopoverComponent } from './date-picker-popover.component';

@Component({
  selector: 'app-task-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgClass, AutoResizeDirective, StatusColorPipe, LinkifyPipe, HighlightPipe, DeleteConfirmButtonComponent, DatePickerPopoverComponent],
  template: `
    <div
      class="bg-surface rounded-md py-2 px-3 border transition-colors group"
      [ngClass]="task().status | statusColor:'border'"
    >
      <!-- Task content -->
      <div class="flex items-start gap-2">
        <!-- Priority flag -->
        <button
          type="button"
          class="shrink-0 w-5 h-5 flex items-center justify-center rounded transition-colors"
          [class.text-danger]="task().isPriority"
          [class.text-foreground-muted/30]="!task().isPriority"
          [class.hover:text-danger-hover]="!task().isPriority"
          (click)="onTogglePriority.emit(); $event.stopPropagation()"
          [attr.aria-label]="task().isPriority ? 'Remove priority' : 'Mark as priority'"
        >
          <i class="pi text-sm" [class.pi-flag-fill]="task().isPriority" [class.pi-flag]="!task().isPriority"></i>
        </button>
        <div class="flex-1 min-w-0">
          @if (editing()) {
            <textarea
              #editInput
              appAutoResize
              [value]="editTitle()"
              (input)="editTitle.set(asTextArea($event).value)"
              (keydown.enter)="onEnterKey(asKeyboardEvent($event))"
              (keydown.escape)="cancelEdit()"
              (blur)="saveEdit()"
              rows="1"
              aria-label="Edit task title. Press Enter to save, Escape to cancel."
              class="w-full text-sm text-foreground bg-transparent border-0 outline-none resize-none p-0 leading-normal"
            ></textarea>
          } @else {
            <!-- Clickable title for inline editing -->
            <p
              class="text-sm text-foreground whitespace-pre-wrap cursor-pointer hover:bg-surface-hover rounded px-1 -mx-1 transition-colors"
              [class.line-through]="task().status === 'Done'"
              [class.text-foreground-muted]="task().status === 'Done'"
              (click)="startEdit(); $event.stopPropagation()"
              [innerHTML]="task().title | highlight: searchQuery()"
            ></p>
          }
        </div>
        <!-- Time (visible) / Delete button (on hover) -->
        <div class="flex items-center shrink-0">
            @if (confirmingTaskDelete()) {
              <app-delete-confirm-button
                ariaLabel="Confirm delete task"
                (onConfirm)="confirmTaskDelete()"
              />
            } @else {
              @if (relativeTime(); as time) {
                <!-- With time: mobile shows both, desktop swaps on hover -->
                <div class="relative flex items-center gap-2">
                  <span
                    class="text-xs transition-opacity md:group-hover:opacity-0"
                    [class.text-inprogress-foreground-muted]="task().status === 'InProgress'"
                    [class.text-done-foreground-muted]="task().status === 'Done'"
                  >{{ time }}</span>
                  <!-- Mobile: always visible delete button -->
                  <button
                    type="button"
                    class="flex md:hidden text-foreground-muted/30 hover:text-danger text-xs transition-colors"
                    (click)="startTaskDeleteConfirm(); $event.stopPropagation()"
                    aria-label="Delete task"
                  >
                    <i class="pi pi-trash"></i>
                  </button>
                  <!-- Desktop: hover-reveal delete button (overlays time) -->
                  <button
                    type="button"
                    class="hidden md:group-hover:flex absolute right-0 text-foreground-muted/40 hover:text-danger text-xs"
                    (click)="startTaskDeleteConfirm(); $event.stopPropagation()"
                    aria-label="Delete task"
                  >
                    <i class="pi pi-trash"></i>
                  </button>
                </div>
              } @else {
                <!-- No time (Todo): mobile shows always, desktop on hover -->
                <button
                  type="button"
                  class="flex text-foreground-muted/30 hover:text-danger text-xs transition-colors md:opacity-0 md:pointer-events-none md:group-hover:opacity-100 md:group-hover:pointer-events-auto"
                  (click)="startTaskDeleteConfirm(); $event.stopPropagation()"
                  aria-label="Delete task"
                >
                  <i class="pi pi-trash"></i>
                </button>
              }
            }
          </div>
        </div>

        <!-- Tab bar - Google Home style -->
        <div class="mt-2 flex items-center gap-1.5 relative">
          <!-- Due Date tab -->
          <button
            type="button"
            [ngClass]="dueDateTabClass()"
            (click)="toggleTab('dueDate'); $event.stopPropagation()"
            [attr.aria-label]="task().dueDate ? (dueDateExpanded() ? 'Collapse due date' : 'Expand due date') : 'Set due date'"
            [attr.aria-expanded]="dueDateExpanded()"
          >
            <i [ngClass]="isOverdue() ? 'pi pi-exclamation-circle' : 'pi pi-calendar'"></i>
            @if (dueDateExpanded() || task().dueDate) {
              <span>{{ dueDateDisplayText() ?? 'Due Date' }}</span>
            }
          </button>

          <!-- Comments tab -->
          <button
            type="button"
            [ngClass]="commentsTabClass()"
            (click)="toggleTab('comments'); $event.stopPropagation()"
            [attr.aria-label]="commentsExpanded() ? 'Hide comments' : 'Show comments'"
            [attr.aria-expanded]="commentsExpanded()"
          >
            <i class="pi pi-comment"></i>
            @if (commentsExpanded()) {
              <span>Comments</span>
              @if (task().comments.length > 0) {
                <span class="bg-white/20 px-1.5 rounded-full">{{ task().comments.length }}</span>
              }
            } @else if (task().comments.length > 0) {
              <span class="absolute -top-0.5 -right-0.5 min-w-3.5 h-3.5 flex items-center justify-center rounded-full bg-indigo-200 text-[9px] text-indigo-700 font-medium">{{ task().comments.length }}</span>
            }
          </button>

          <!-- Mobile status change buttons (spacer pushes to right) -->
          <div class="flex-1"></div>
          <div class="flex md:hidden items-center gap-1">
            @if (task().status !== 'Todo') {
              <!-- Back arrow - move to previous status -->
              <button
                type="button"
                class="w-7 h-7 rounded-full flex items-center justify-center transition-colors"
                [ngClass]="previousStatusButtonClass()"
                (click)="moveToPreviousStatus(); $event.stopPropagation()"
                [attr.aria-label]="'Move to ' + previousStatus()"
              >
                <i class="pi pi-arrow-left text-xs"></i>
              </button>
            }
            @if (task().status !== 'Done') {
              <!-- Forward arrow - move to next status -->
              <button
                type="button"
                class="w-7 h-7 rounded-full flex items-center justify-center transition-colors"
                [ngClass]="nextStatusButtonClass()"
                (click)="moveToNextStatus(); $event.stopPropagation()"
                [attr.aria-label]="'Move to ' + nextStatus()"
              >
                <i class="pi pi-arrow-right text-xs"></i>
              </button>
            }
          </div>
        </div>

        <!-- Due Date expanded content -->
        @if (dueDateExpanded()) {
          <div class="mt-2 p-2 bg-amber-50/50 rounded-lg border border-amber-200/50 relative">
            <div class="flex items-center gap-1 flex-wrap">
              <button
                type="button"
                (click)="selectQuickDate('today'); $event.stopPropagation()"
                class="px-2 py-1 text-xs rounded transition-colors"
                [class]="isDateSelected('today') ? 'bg-amber-200 text-amber-800 font-medium' : 'bg-white text-gray-600 hover:bg-gray-100'"
              >Today</button>
              <button
                type="button"
                (click)="selectQuickDate('tomorrow'); $event.stopPropagation()"
                class="px-2 py-1 text-xs rounded transition-colors"
                [class]="isDateSelected('tomorrow') ? 'bg-amber-200 text-amber-800 font-medium' : 'bg-white text-gray-600 hover:bg-gray-100'"
              >+1</button>
              <button
                type="button"
                (click)="selectQuickDate('nextWeek'); $event.stopPropagation()"
                class="px-2 py-1 text-xs rounded transition-colors"
                [class]="isDateSelected('nextWeek') ? 'bg-amber-200 text-amber-800 font-medium' : 'bg-white text-gray-600 hover:bg-gray-100'"
              >+7</button>
              <button
                type="button"
                (click)="selectQuickDate('plus35'); $event.stopPropagation()"
                class="px-2 py-1 text-xs rounded transition-colors"
                [class]="isDateSelected('plus35') ? 'bg-amber-200 text-amber-800 font-medium' : 'bg-white text-gray-600 hover:bg-gray-100'"
              >+35</button>
              <button
                type="button"
                (click)="showDatePicker.set(true); $event.stopPropagation()"
                class="px-2 py-1 text-xs rounded bg-white text-gray-600 hover:bg-gray-100 transition-colors"
                aria-label="Open calendar"
              ><i class="pi pi-calendar-plus text-[10px]"></i></button>
              @if (task().dueDate) {
                <button
                  type="button"
                  (click)="clearDueDate(); $event.stopPropagation()"
                  class="ml-auto px-2 py-1 text-xs rounded text-danger hover:bg-danger-bg transition-colors"
                  aria-label="Clear due date"
                ><i class="pi pi-times text-[10px]"></i> Clear</button>
              }
            </div>
            @if (showDatePicker()) {
              <app-date-picker-popover
                [currentDate]="task().dueDate"
                [showQuickOptions]="false"
                (onSelect)="onDateSelect($event)"
                (onClear)="clearDueDate()"
                (onClose)="showDatePicker.set(false)"
              />
            }
          </div>
        }

        <!-- Comments expanded content -->
        @if (commentsExpanded()) {
          <div class="mt-2 p-2 bg-indigo-50/50 rounded-lg border border-indigo-200/50">
            <!-- Comments list -->
            @if (task().comments.length > 0) {
              <div class="space-y-1.5 mb-2">
                @for (comment of task().comments; track comment.id) {
                  @if (editingCommentId() === comment.id) {
                    <!-- Editing comment -->
                    <div class="flex items-center gap-1.5 text-xs">
                      <i class="pi pi-comment text-primary/40"></i>
                      <textarea
                        #commentEditInput
                        appAutoResize
                        [value]="editCommentContent()"
                        (input)="editCommentContent.set(asTextArea($event).value)"
                        (keydown.enter)="onCommentEnterKey(asKeyboardEvent($event))"
                        (keydown.escape)="cancelCommentEdit()"
                        (blur)="saveCommentEdit(comment.id)"
                        rows="1"
                        aria-label="Edit comment"
                        class="flex-1 bg-transparent border-0 outline-none text-foreground-muted resize-none leading-normal"
                      ></textarea>
                    </div>
                  } @else {
                    <!-- Display comment as minimal row -->
                    <div
                      class="group/comment flex items-start gap-1.5 cursor-pointer text-xs"
                      role="button"
                      tabindex="0"
                      (click)="onCommentClick($event, comment)"
                      (keydown.enter)="startCommentEdit(comment); $event.preventDefault(); $event.stopPropagation()"
                      (keydown.space)="startCommentEdit(comment); $event.preventDefault(); $event.stopPropagation()"
                    >
                      <i class="pi pi-comment text-primary/40 shrink-0 mt-0.5"></i>
                      <span class="text-foreground flex-1 min-w-0 break-words" [innerHTML]="comment.content | linkify"></span>
                      @if (confirmingCommentDeleteId() === comment.id) {
                        <app-delete-confirm-button
                          [ariaLabel]="'Confirm delete comment: ' + comment.content"
                          [shrink]="true"
                          (onConfirm)="confirmCommentDelete(comment.id)"
                        />
                      } @else {
                        <!-- Time: mobile shows both time and delete, desktop swaps on hover/focus -->
                        <span class="text-foreground-muted/30 shrink-0 md:group-hover/comment:hidden md:group-focus-within/comment:hidden">{{ formatCommentTime(comment) }}</span>
                        <!-- Mobile: always visible delete button -->
                        <button
                          type="button"
                          class="flex md:hidden text-foreground-muted/30 hover:text-danger shrink-0 text-xs"
                          (click)="startCommentDeleteConfirm(comment.id); $event.stopPropagation()"
                          [attr.aria-label]="getDeleteCommentAriaLabel(comment)"
                        >
                          <i class="pi pi-trash"></i>
                        </button>
                        <!-- Desktop: hover/focus-reveal delete button for keyboard accessibility -->
                        <button
                          type="button"
                          class="hidden md:group-hover/comment:flex md:group-focus-within/comment:flex text-foreground-muted/40 hover:text-danger shrink-0 text-xs"
                          (click)="startCommentDeleteConfirm(comment.id); $event.stopPropagation()"
                          [attr.aria-label]="getDeleteCommentAriaLabel(comment)"
                        >
                          <i class="pi pi-trash"></i>
                        </button>
                      }
                    </div>
                  }
                }
              </div>
            }

            <!-- Add comment input -->
            <div class="flex items-center gap-1.5 text-xs">
              <i class="pi pi-plus text-foreground/40"></i>
              <textarea
                #newCommentInput
                appAutoResize
                [value]="newCommentText()"
                (input)="newCommentText.set(asTextArea($event).value)"
                (keydown.enter)="onNewCommentEnterKey(asKeyboardEvent($event))"
                (keydown.escape)="newCommentText.set('')"
                placeholder="Add comment..."
                aria-label="Add comment"
                rows="1"
                class="flex-1 bg-transparent border-0 outline-none text-foreground placeholder-foreground/50 resize-none leading-normal"
              ></textarea>
            </div>
          </div>
        }
    </div>
  `,
})
export class TaskCardComponent {
  private readonly injector = inject(Injector);
  private readonly destroyRef = inject(DestroyRef);
  private readonly deleteConfirmation = inject(DeleteConfirmationService);
  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);

  // Flag to prevent click-outside from firing during initial render
  private initialized = false;

  readonly task = input.required<Task>();
  readonly searchQuery = input('');

  readonly onEdit = output<string>();
  readonly onDelete = output<void>();
  readonly onAddComment = output<string>();
  readonly onEditComment = output<{ commentId: string; content: string }>();
  readonly onDeleteComment = output<string>();
  readonly onSetDueDate = output<string>();
  readonly onClearDueDate = output<void>();
  readonly onTogglePriority = output<void>();
  readonly onStatusChange = output<'Todo' | 'InProgress' | 'Done'>();

  readonly editing = signal(false);
  readonly editTitle = signal('');
  readonly editInput = viewChild<ElementRef<HTMLTextAreaElement>>('editInput');

  // Comment editing state
  readonly editingCommentId = signal<string | null>(null);
  readonly editCommentContent = signal('');
  readonly newCommentText = signal('');
  readonly commentEditInput = viewChild<ElementRef<HTMLTextAreaElement>>('commentEditInput');
  readonly newCommentInput = viewChild<ElementRef<HTMLTextAreaElement>>('newCommentInput');

  // Delete confirmation state
  readonly confirmingTaskDelete = signal(false);
  readonly confirmingCommentDeleteId = signal<string | null>(null);

  // Tab selection state (Google Home style - one tab at a time)
  readonly selectedTab = signal<'dueDate' | 'comments' | null>(null);
  readonly dueDateExpanded = computed(() => this.selectedTab() === 'dueDate');
  readonly commentsExpanded = computed(() => this.selectedTab() === 'comments');

  // Date picker popover state
  readonly showDatePicker = signal(false);

  // Due date display calculations
  private readonly daysDiff = computed(() => {
    const dueDate = this.task().dueDate;
    if (!dueDate) return null;

    const date = new Date(dueDate + 'T00:00:00');
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return Math.floor((date.getTime() - today.getTime()) / 86400000);
  });

  readonly dueDateDisplayText = computed(() => {
    const diff = this.daysDiff();
    if (diff === null) return null;

    const dueDate = this.task().dueDate!;
    const date = new Date(dueDate + 'T00:00:00');

    if (diff < -1) return `${-diff}d ago`;
    if (diff === -1) return 'Yesterday';
    if (diff === 0) return 'Today';
    if (diff === 1) return 'Tomorrow';
    if (diff <= 6) return date.toLocaleDateString('en-US', { weekday: 'short' });
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
  });

  /** Returns true if the task is overdue (past due date and not done) */
  isOverdue(): boolean {
    const diff = this.daysDiff();
    return diff !== null && diff < 0 && this.task().status !== 'Done';
  }

  /** Returns CSS classes for the due date tab button based on state */
  dueDateTabClass(): string {
    const hasDueDate = this.task().dueDate;
    const isExpanded = this.dueDateExpanded();
    const isDone = this.task().status === 'Done';
    const diff = this.daysDiff();

    // Common base classes for all states
    const common = 'flex items-center justify-center rounded-full transition-all text-xs shrink-0';

    // Expanded state - darker/more prominent to show selection
    if (isExpanded) {
      const pill = `${common} h-7 px-3 gap-1.5`;

      if (isDone) {
        return `${pill} bg-due-done text-due-done-foreground line-through`;
      }
      if (diff !== null && diff < 0) {
        return `${pill} bg-danger text-white font-medium`;
      }
      // Use yellow-500 with light amber text to match collapsed background color
      return `${pill} bg-yellow-500 text-amber-100 font-medium`;
    }

    // Collapsed with date - lighter colors
    if (hasDueDate) {
      const pill = `${common} h-7 px-3 gap-1.5`;

      if (isDone) {
        return `${pill} bg-due-done text-due-done-foreground line-through`;
      }
      if (diff !== null && diff < 0) {
        return `${pill} bg-overdue text-overdue-foreground font-medium`;
      }
      if (diff === 0) {
        return `${pill} bg-due-today text-due-today-foreground`;
      }
      if (diff === 1) {
        return `${pill} bg-due-soon text-due-soon-foreground`;
      }
      return `${pill} bg-amber-100 text-amber-700`;
    }

    // Collapsed circular icon (no date set)
    return `${common} w-7 h-7 bg-foreground-muted/10 text-foreground-muted/40 hover:bg-foreground-muted/20`;
  }

  /** Returns CSS classes for the comments tab button based on state */
  commentsTabClass(): string {
    const isExpanded = this.commentsExpanded();
    const hasComments = this.task().comments.length > 0;

    // Common base classes for all states
    const common = 'relative flex items-center justify-center rounded-full transition-all text-xs shrink-0';

    if (isExpanded) {
      // Expanded pill with indigo styling
      return `${common} h-7 px-3 gap-1.5 bg-indigo-500 text-white font-medium`;
    }

    // Collapsed circular icon
    if (hasComments) {
      return `${common} w-7 h-7 bg-indigo-100 text-indigo-600 hover:bg-indigo-200`;
    }
    return `${common} w-7 h-7 bg-foreground-muted/10 text-foreground-muted/40 hover:bg-foreground-muted/20`;
  }

  // Tick signal for auto-updating relative times (updates every minute)
  private readonly tick = signal(Date.now());

  readonly relativeTime = computed(() => {
    // Include tick in dependency to trigger updates
    this.tick();
    const task = this.task();
    if (task.status === 'InProgress' && task.startedAt) {
      return this.formatTime(task.startedAt, 'elapsed');
    }
    if (task.status === 'Done' && task.completedAt) {
      return this.formatTime(task.completedAt, 'completed');
    }
    return null;
  });

  constructor() {
    // Update tick every minute for auto-updating relative times
    const intervalId = setInterval(() => this.tick.set(Date.now()), 60000);
    this.destroyRef.onDestroy(() => {
      clearInterval(intervalId);
      this.deleteConfirmation.cleanup();
    });

    // Set initialized after first render to prevent click-outside from firing immediately
    afterNextRender(() => {
      this.initialized = true;
    }, { injector: this.injector });
  }

  /** Close expanded tabs when clicking outside the task card */
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    if (!this.initialized || !this.selectedTab()) return;

    const target = event.target;
    // Guard for non-Node targets (e.g., SVG elements in some browsers)
    if (!(target instanceof Node)) return;

    if (!this.elementRef.nativeElement.contains(target)) {
      this.closeExpanded();
    }
  }

  /** Close expanded tabs when pressing Escape */
  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    if (this.selectedTab()) {
      this.closeExpanded();
    }
  }

  /** Close all expanded content */
  private closeExpanded(): void {
    this.selectedTab.set(null);
    this.showDatePicker.set(false);
  }

  /** Type-safe helper for accessing textarea value from events */
  asTextArea(event: Event): HTMLTextAreaElement {
    return event.target as HTMLTextAreaElement;
  }

  /** Type-safe helper for keyboard events */
  asKeyboardEvent(event: Event): KeyboardEvent {
    return event as KeyboardEvent;
  }

  private formatTime(dateStr: string, type: 'elapsed' | 'completed'): string {
    const date = new Date(dateStr);
    if (isNaN(date.getTime())) return '';

    const diffMs = Math.max(0, Date.now() - date.getTime());
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    const suffix = type === 'completed' ? ' ago' : '';
    const justNow = type === 'completed' ? 'just now' : 'just started';

    if (diffMins < 1) return justNow;
    if (diffMins < 60) return `${diffMins}m${suffix}`;
    if (diffHours < 24) return `${diffHours}h${suffix}`;
    if (diffDays < 7) return `${diffDays}d${suffix}`;
    return date.toLocaleDateString();
  }

  startEdit(): void {
    this.editTitle.set(this.task().title);
    this.editing.set(true);
    afterNextRender(() => {
      const textarea = this.editInput()?.nativeElement;
      if (textarea) {
        textarea.focus();
        textarea.select();
      }
    }, { injector: this.injector });
  }

  onEnterKey(event: KeyboardEvent): void {
    if (event.shiftKey) {
      return;
    }
    event.preventDefault();
    this.saveEdit();
  }

  saveEdit(): void {
    const newTitle = this.editTitle().trim();
    if (newTitle && newTitle !== this.task().title) {
      this.onEdit.emit(newTitle);
    }
    this.editing.set(false);
  }

  cancelEdit(): void {
    this.editing.set(false);
  }

  // Tab toggle method (Google Home style - one tab at a time)
  toggleTab(tab: 'dueDate' | 'comments'): void {
    const currentTab = this.selectedTab();
    if (currentTab === tab) {
      // Clicking the same tab collapses it
      this.selectedTab.set(null);
      this.showDatePicker.set(false);
    } else {
      // Switch to the new tab
      this.selectedTab.set(tab);
      this.showDatePicker.set(false);

      // Auto-focus the add comment input when expanding comments
      if (tab === 'comments') {
        afterNextRender(() => {
          this.newCommentInput()?.nativeElement.focus();
        }, { injector: this.injector });
      }
    }
  }

  // Due date quick selection methods
  selectQuickDate(option: 'today' | 'tomorrow' | 'nextWeek' | 'plus35'): void {
    const date = this.getQuickOptionDate(option);
    this.onSetDueDate.emit(this.formatDateString(date));
  }

  private getQuickOptionDate(option: 'today' | 'tomorrow' | 'nextWeek' | 'plus35'): Date {
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    switch (option) {
      case 'today':
        return today;
      case 'tomorrow':
        return new Date(today.getTime() + 86400000);
      case 'nextWeek':
        return new Date(today.getTime() + 7 * 86400000);
      case 'plus35':
        return new Date(today.getTime() + 35 * 86400000);
    }
  }

  isDateSelected(option: 'today' | 'tomorrow' | 'nextWeek' | 'plus35'): boolean {
    const current = this.task().dueDate;
    if (!current) return false;

    const optionDate = this.getQuickOptionDate(option);
    return this.formatDateString(optionDate) === current;
  }

  private formatDateString(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  onDateSelect(date: string): void {
    this.onSetDueDate.emit(date);
    this.showDatePicker.set(false);
  }

  clearDueDate(): void {
    this.onClearDueDate.emit();
    this.showDatePicker.set(false);
  }

  formatCommentTime(comment: Comment): string {
    const dateStr = comment.updatedAt !== comment.createdAt ? comment.updatedAt : comment.createdAt;
    const prefix = comment.updatedAt !== comment.createdAt ? 'edited ' : '';
    return prefix + this.formatTime(dateStr, 'completed');
  }

  /** Generate a concise aria-label for the delete comment button */
  getDeleteCommentAriaLabel(comment: Comment): string {
    const content = comment.content?.trim();
    if (!content) {
      return 'Delete comment';
    }
    const maxLength = 40;
    if (content.length <= maxLength) {
      return `Delete comment: ${content}`;
    }
    return `Delete comment: ${content.slice(0, maxLength).trimEnd()}…`;
  }

  onCommentClick(event: MouseEvent, comment: Comment): void {
    event.stopPropagation();

    // Check if the click was on a link - don't trigger edit mode
    const target = event.target as HTMLElement;
    if (target.tagName === 'A' || target.closest('a')) {
      return;
    }

    this.startCommentEdit(comment);
  }

  startCommentEdit(comment: Comment): void {
    this.editingCommentId.set(comment.id);
    this.editCommentContent.set(comment.content);
    afterNextRender(() => {
      const textarea = this.commentEditInput()?.nativeElement;
      if (textarea) {
        textarea.focus();
        textarea.select();
      }
    }, { injector: this.injector });
  }

  onCommentEnterKey(event: KeyboardEvent): void {
    if (event.shiftKey) {
      return;
    }
    event.preventDefault();
    const commentId = this.editingCommentId();
    if (commentId) {
      this.saveCommentEdit(commentId);
    }
  }

  saveCommentEdit(commentId: string): void {
    const content = this.editCommentContent().trim();
    const originalComment = this.task().comments.find(c => c.id === commentId);
    if (content && originalComment && content !== originalComment.content) {
      this.onEditComment.emit({ commentId, content });
    }
    this.cancelCommentEdit();
  }

  cancelCommentEdit(): void {
    this.editingCommentId.set(null);
    this.editCommentContent.set('');
  }

  onNewCommentEnterKey(event: KeyboardEvent): void {
    if (event.shiftKey) {
      return;
    }
    event.preventDefault();
    event.stopPropagation();
    this.submitNewComment();
  }

  submitNewComment(): void {
    const content = this.newCommentText().trim();
    if (content) {
      this.onAddComment.emit(content);
      this.newCommentText.set('');
      // Reset textarea height after clearing content
      const textarea = this.newCommentInput()?.nativeElement;
      if (textarea) {
        textarea.style.height = 'auto';
      }
    }
  }

  // Task delete confirmation methods
  startTaskDeleteConfirm(): void {
    this.deleteConfirmation.cleanup();
    this.confirmingTaskDelete.set(true);
    this.deleteConfirmation.start(() => {
      this.confirmingTaskDelete.set(false);
    });
  }

  confirmTaskDelete(): void {
    this.deleteConfirmation.cleanup();
    this.confirmingTaskDelete.set(false);
    this.onDelete.emit();
  }

  // Comment delete confirmation methods
  startCommentDeleteConfirm(commentId: string): void {
    this.deleteConfirmation.cleanup();
    this.confirmingCommentDeleteId.set(commentId);
    this.deleteConfirmation.start(() => {
      this.confirmingCommentDeleteId.set(null);
    });
  }

  confirmCommentDelete(commentId: string): void {
    this.deleteConfirmation.cleanup();
    this.confirmingCommentDeleteId.set(null);
    this.onDeleteComment.emit(commentId);
  }

  // Status change methods for mobile
  previousStatus(): 'Todo' | 'InProgress' {
    return this.task().status === 'Done' ? 'InProgress' : 'Todo';
  }

  nextStatus(): 'InProgress' | 'Done' {
    return this.task().status === 'Todo' ? 'InProgress' : 'Done';
  }

  previousStatusButtonClass(): string {
    const prev = this.previousStatus();
    if (prev === 'Todo') {
      return 'bg-todo text-todo-foreground hover:bg-todo-hover';
    }
    return 'bg-inprogress text-inprogress-foreground hover:bg-inprogress-hover';
  }

  nextStatusButtonClass(): string {
    const next = this.nextStatus();
    if (next === 'InProgress') {
      return 'bg-inprogress text-inprogress-foreground hover:bg-inprogress-hover';
    }
    return 'bg-done text-done-foreground hover:bg-done-hover';
  }

  moveToPreviousStatus(): void {
    this.onStatusChange.emit(this.previousStatus());
  }

  moveToNextStatus(): void {
    this.onStatusChange.emit(this.nextStatus());
  }
}
