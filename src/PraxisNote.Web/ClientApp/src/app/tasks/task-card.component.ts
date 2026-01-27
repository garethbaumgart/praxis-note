import { Component, computed, ElementRef, input, output, signal, viewChild, inject, Injector, afterNextRender, ChangeDetectionStrategy, DestroyRef, HostListener } from '@angular/core';
import { Task, TaskStatus, Comment } from './task.model';
import { Tag, TaskTag } from './tag.model';
import { AutoResizeDirective } from '../shared/directives/auto-resize.directive';
import { LinkifyPipe } from '../shared/pipes/linkify.pipe';
import { HighlightPipe } from '../shared/pipes/highlight.pipe';
import { DeleteConfirmationService } from '../shared/services/delete-confirmation.service';
import { DeleteConfirmButtonComponent } from '../shared/components/delete-confirm-button.component';
import { DatePickerPopoverComponent } from './date-picker-popover.component';

@Component({
  selector: 'app-task-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AutoResizeDirective, LinkifyPipe, HighlightPipe, DeleteConfirmButtonComponent, DatePickerPopoverComponent],
  template: `
    <div
      class="bg-surface-subtle rounded-md py-2 px-3 border transition-colors group"
      [class.border-todo-border]="task().status === 'Todo'"
      [class.border-inprogress-border]="task().status === 'InProgress'"
      [class.border-done-border]="task().status === 'Done' && !isArchive()"
      [class.border-archive-border]="task().status === 'Done' && isArchive()"
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
                    [class.text-done-foreground-muted]="task().status === 'Done' && !isArchive()"
                    [class.text-archive-foreground-muted]="task().status === 'Done' && isArchive()"
                  >{{ time }}</span>
                  <!-- Mobile: always visible delete button -->
                  <button
                    type="button"
                    class="flex md:hidden text-foreground-muted/50 hover:text-danger text-xs transition-colors"
                    (click)="startTaskDeleteConfirm(); $event.stopPropagation()"
                    aria-label="Delete task"
                  >
                    <i class="pi pi-trash"></i>
                  </button>
                  <!-- Desktop: hover-reveal delete button (overlays time) -->
                  <button
                    type="button"
                    class="hidden md:group-hover:flex absolute right-0 text-foreground-muted/50 hover:text-danger text-xs"
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
                  class="flex text-foreground-muted/50 hover:text-danger text-xs transition-colors md:opacity-0 md:pointer-events-none md:group-hover:opacity-100 md:group-hover:pointer-events-auto"
                  (click)="startTaskDeleteConfirm(); $event.stopPropagation()"
                  aria-label="Delete task"
                >
                  <i class="pi pi-trash"></i>
                </button>
              }
            }
          </div>
        </div>

        <!-- Inline tags row (when tags exist OR adding first tag) -->
        @if (hasInlineTags() || showTagPicker()) {
          <div class="mt-1.5 flex flex-wrap items-center gap-1">
              @for (tag of visibleTags(); track tag.id) {
                <span class="tag-badge">
                  {{ tag.name }}
                  <button
                    type="button"
                    class="tag-badge-remove"
                    (click)="removeTag(tag.id); $event.stopPropagation()"
                    [attr.aria-label]="'Remove tag ' + tag.name"
                  >
                    <i class="pi pi-times"></i>
                  </button>
                </span>
              }
              @if (overflowCount() > 0 && !showTagPicker()) {
                <!-- Overflow button to expand -->
                <button
                  type="button"
                  class="px-1.5 py-0.5 rounded-full text-[10px] bg-foreground-muted/10 text-foreground-muted hover:bg-tag/10 hover:text-tag transition-colors"
                  (click)="inlineTagsExpanded.set(true); $event.stopPropagation()"
                  [attr.aria-label]="'Show ' + overflowCount() + ' more tags'"
                >
                  +{{ overflowCount() }}
                </button>
              }
              <!-- Inline search input (when adding tag) -->
              @if (showTagPicker()) {
                <div class="flex-1 min-w-[100px] relative">
                  <input
                    #inlineTagInput
                    type="text"
                    [placeholder]="hasInlineTags() ? 'Add tag...' : 'Add first tag...'"
                    [value]="firstTagSearch()"
                    (input)="firstTagSearch.set(asInput($event).value)"
                    (keydown.enter)="onFirstTagEnter(); $event.preventDefault()"
                    (keydown.escape)="showTagPicker.set(false); $event.stopPropagation()"
                    class="w-full h-6 px-2 text-xs bg-surface-muted rounded-full border-0 outline-none"
                    aria-label="Search or create tag"
                  >
                  <!-- Dropdown suggestions -->
                  @if (tooltipSuggestions().length > 0 || canCreateFirstTag()) {
                    <div class="absolute left-0 top-full mt-1 w-48 bg-surface rounded-lg shadow-lg border border-border py-1 z-50">
                      @for (tag of tooltipSuggestions(); track tag.id) {
                        <button
                          type="button"
                          class="w-full px-3 py-1.5 text-left text-xs hover:bg-surface-hover transition-colors flex items-center justify-between"
                          (click)="addTag({ id: tag.id, name: tag.name }); $event.stopPropagation()"
                        >
                          <span [innerHTML]="highlightMatch(tag.name)"></span>
                          <span class="text-foreground-muted">{{ tag.usageCount }}</span>
                        </button>
                      }
                      @if (canCreateFirstTag()) {
                        @if (tooltipSuggestions().length > 0) {
                          <div class="border-t border-border my-1"></div>
                        }
                        <button
                          type="button"
                          class="w-full px-3 py-1.5 text-left text-xs text-primary hover:bg-surface-hover transition-colors"
                          (click)="createAndAddTag(firstTagSearch().trim()); $event.stopPropagation()"
                        >
                          <i class="pi pi-plus text-[10px] mr-1"></i>
                          Create "{{ firstTagSearch().trim() }}"
                        </button>
                      }
                    </div>
                  }
                </div>
              } @else {
                <!-- Add tag button (when not adding) -->
                <button
                  type="button"
                  class="w-5 h-5 rounded-full flex items-center justify-center text-foreground-muted/30 hover:text-tag hover:bg-tag/10 transition-colors"
                  (click)="openInlineTagInput(); $event.stopPropagation()"
                  aria-label="Add tag"
                >
                  <i class="pi pi-plus text-[9px]"></i>
                </button>
              }
              <!-- Collapse/"Less" button (only when expanded and has overflow) -->
              @if (inlineTagsExpanded() && taskTags().length > 3 && !showTagPicker()) {
                <button
                  type="button"
                  class="ml-auto px-1.5 py-0.5 rounded-full text-[10px] bg-foreground-muted/10 text-foreground-muted hover:bg-foreground-muted/20 transition-colors flex items-center gap-0.5"
                  (click)="inlineTagsExpanded.set(false); $event.stopPropagation()"
                  aria-label="Show fewer tags"
                >
                  <i class="pi pi-chevron-up text-[8px]"></i>
                  <span>Less</span>
                </button>
              }
          </div>
        }

        <!-- Tab bar - Google Home style -->
        <div class="mt-2 flex items-center gap-1.5 relative">
          <!-- Due Date tab -->
          <button
            type="button"
            class="flex items-center justify-center rounded-full transition-all text-xs shrink-0 h-7"
            [class.px-3]="isDueDatePill()"
            [class.gap-1.5]="isDueDatePill()"
            [class.w-7]="isDueDateCircle()"
            [class.bg-due-done]="isDueDateExpandedDone() || isDueDateCollapsedDone()"
            [class.text-due-done-foreground]="isDueDateExpandedDone() || isDueDateCollapsedDone()"
            [class.line-through]="isDueDateExpandedDone() || isDueDateCollapsedDone()"
            [class.bg-danger]="isDueDateExpandedOverdue()"
            [class.text-white]="isDueDateExpandedOverdue()"
            [class.font-medium]="isDueDateExpandedOverdue() || isDueDateExpandedNormal() || isDueDateCollapsedOverdue()"
            [class.bg-duedate-expanded]="isDueDateExpandedNormal()"
            [class.text-duedate-expanded-foreground]="isDueDateExpandedNormal()"
            [class.bg-overdue]="isDueDateCollapsedOverdue()"
            [class.text-overdue-foreground]="isDueDateCollapsedOverdue()"
            [class.bg-due-today]="isDueDateCollapsedToday()"
            [class.text-due-today-foreground]="isDueDateCollapsedToday()"
            [class.bg-due-soon]="isDueDateCollapsedTomorrow()"
            [class.text-due-soon-foreground]="isDueDateCollapsedTomorrow()"
            [class.bg-duedate-default]="isDueDateCollapsedDefault()"
            [class.text-duedate-default-foreground]="isDueDateCollapsedDefault()"
            [class.bg-foreground-muted/10]="isDueDateCircle()"
            [class.text-foreground-muted/40]="isDueDateCircle()"
            [class.hover:bg-foreground-muted/20]="isDueDateCircle()"
            (click)="toggleTab('dueDate'); $event.stopPropagation()"
            [attr.aria-label]="task().dueDate ? (dueDateExpanded() ? 'Collapse due date' : 'Expand due date') : 'Set due date'"
            [attr.aria-expanded]="dueDateExpanded()"
          >
            <i class="pi" [class.pi-exclamation-circle]="isOverdue()" [class.pi-calendar]="!isOverdue()"></i>
            @if (dueDateExpanded() || task().dueDate) {
              <span>{{ dueDateDisplayText() ?? 'Due Date' }}</span>
            }
          </button>

          <!-- Comments tab -->
          <button
            type="button"
            class="relative flex items-center justify-center rounded-full transition-all text-xs shrink-0 h-7"
            [class.px-3]="isCommentsPill()"
            [class.gap-1.5]="isCommentsPill()"
            [class.bg-comments-expanded]="isCommentsPill()"
            [class.text-comments-expanded-foreground]="isCommentsPill()"
            [class.font-medium]="isCommentsPill()"
            [class.w-7]="!isCommentsPill()"
            [class.bg-comments-collapsed]="isCommentsCircleWithComments()"
            [class.text-comments-collapsed-foreground]="isCommentsCircleWithComments()"
            [class.hover:bg-comments-collapsed-hover]="isCommentsCircleWithComments()"
            [class.bg-foreground-muted/10]="isCommentsCircleEmpty()"
            [class.text-foreground-muted/40]="isCommentsCircleEmpty()"
            [class.hover:bg-foreground-muted/20]="isCommentsCircleEmpty()"
            (click)="toggleTab('comments'); $event.stopPropagation()"
            [attr.aria-label]="commentsExpanded() ? 'Hide comments' : 'Show comments'"
            [attr.aria-expanded]="commentsExpanded()"
          >
            <i class="pi pi-comment"></i>
            @if (commentsExpanded()) {
              <span>Comments</span>
              @if (task().comments.length > 0) {
                <span class="bg-comments-expanded-badge px-1.5 rounded-full">{{ task().comments.length }}</span>
              }
            } @else if (task().comments.length > 0) {
              <span class="absolute -top-0.5 -right-0.5 min-w-3.5 h-3.5 flex items-center justify-center rounded-full bg-comments-badge text-[9px] text-comments-badge-foreground font-medium">{{ task().comments.length }}</span>
            }
          </button>

          <!-- Tags tab (only show when no inline tags and not adding - clicking shows inline row) -->
          @if (!hasInlineTags() && !showTagPicker()) {
            <button
              type="button"
              class="relative flex items-center justify-center rounded-full transition-all text-xs shrink-0 h-7 w-7 bg-foreground-muted/10 text-foreground-muted/40 hover:bg-foreground-muted/20"
              (click)="openInlineTagInput(); $event.stopPropagation()"
              aria-label="Add tag"
            >
              <i class="pi pi-tag"></i>
            </button>
          }

          <!-- Mobile status change buttons (spacer pushes to right) -->
          <div class="flex-1"></div>
          <div class="flex md:hidden items-center gap-1">
            @if (task().status !== 'Todo') {
              <!-- Back arrow - move to previous status -->
              <button
                type="button"
                class="w-7 h-7 rounded-full flex items-center justify-center transition-colors"
                [class.bg-todo]="isPrevStatusTodo()"
                [class.text-todo-foreground]="isPrevStatusTodo()"
                [class.hover:bg-todo-hover]="isPrevStatusTodo()"
                [class.bg-inprogress]="isPrevStatusInProgress()"
                [class.text-inprogress-foreground]="isPrevStatusInProgress()"
                [class.hover:bg-inprogress-hover]="isPrevStatusInProgress()"
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
                [class.bg-inprogress]="isNextStatusInProgress()"
                [class.text-inprogress-foreground]="isNextStatusInProgress()"
                [class.hover:bg-inprogress-hover]="isNextStatusInProgress()"
                [class.bg-done]="isNextStatusDone()"
                [class.text-done-foreground]="isNextStatusDone()"
                [class.hover:bg-done-hover]="isNextStatusDone()"
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
          <div class="mt-2 p-2 bg-duedate-section rounded-lg border border-duedate-section-border relative">
            <div class="flex items-center gap-1 flex-wrap">
              <button
                type="button"
                (click)="selectQuickDate('today'); $event.stopPropagation()"
                class="px-2 py-1 text-xs rounded transition-colors"
                [class]="isDateSelected('today') ? 'bg-duedate-btn-selected text-duedate-btn-selected-foreground font-medium' : 'bg-duedate-btn text-duedate-btn-foreground hover:bg-duedate-btn-hover'"
              >Today</button>
              <button
                type="button"
                (click)="selectQuickDate('tomorrow'); $event.stopPropagation()"
                class="px-2 py-1 text-xs rounded transition-colors"
                [class]="isDateSelected('tomorrow') ? 'bg-duedate-btn-selected text-duedate-btn-selected-foreground font-medium' : 'bg-duedate-btn text-duedate-btn-foreground hover:bg-duedate-btn-hover'"
              >+1</button>
              <button
                type="button"
                (click)="selectQuickDate('friday'); $event.stopPropagation()"
                class="px-2 py-1 text-xs rounded transition-colors"
                [class]="isDateSelected('friday') ? 'bg-duedate-btn-selected text-duedate-btn-selected-foreground font-medium' : 'bg-duedate-btn text-duedate-btn-foreground hover:bg-duedate-btn-hover'"
              >Fri</button>
              <button
                type="button"
                (click)="selectQuickDate('nextWeek'); $event.stopPropagation()"
                class="px-2 py-1 text-xs rounded transition-colors"
                [class]="isDateSelected('nextWeek') ? 'bg-duedate-btn-selected text-duedate-btn-selected-foreground font-medium' : 'bg-duedate-btn text-duedate-btn-foreground hover:bg-duedate-btn-hover'"
              >+7</button>
              <button
                type="button"
                (click)="selectQuickDate('plus35'); $event.stopPropagation()"
                class="px-2 py-1 text-xs rounded transition-colors"
                [class]="isDateSelected('plus35') ? 'bg-duedate-btn-selected text-duedate-btn-selected-foreground font-medium' : 'bg-duedate-btn text-duedate-btn-foreground hover:bg-duedate-btn-hover'"
              >+35</button>
              <button
                type="button"
                (click)="toggleDatePicker(); $event.stopPropagation()"
                class="px-2 py-1 text-xs rounded bg-duedate-btn text-duedate-btn-foreground hover:bg-duedate-btn-hover transition-colors"
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
          <div class="mt-2 p-2 bg-comments-section rounded-lg border border-comments-section-border">
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
                        <span class="text-foreground-muted shrink-0 md:group-hover/comment:hidden md:group-focus-within/comment:hidden">{{ formatCommentTime(comment) }}</span>
                        <!-- Mobile: always visible delete button -->
                        <button
                          type="button"
                          class="flex md:hidden text-foreground-muted hover:text-danger shrink-0 text-xs"
                          (click)="startCommentDeleteConfirm(comment.id); $event.stopPropagation()"
                          [attr.aria-label]="getDeleteCommentAriaLabel(comment)"
                        >
                          <i class="pi pi-trash"></i>
                        </button>
                        <!-- Desktop: hover/focus-reveal delete button for keyboard accessibility -->
                        <button
                          type="button"
                          class="hidden md:group-hover/comment:flex md:group-focus-within/comment:flex text-foreground-muted hover:text-danger shrink-0 text-xs"
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
  readonly isArchive = input(false);
  readonly allTags = input<Tag[]>([]);

  readonly onEdit = output<string>();
  readonly onDelete = output<void>();
  readonly onAddComment = output<string>();
  readonly onEditComment = output<{ commentId: string; content: string }>();
  readonly onDeleteComment = output<string>();
  readonly onSetDueDate = output<string>();
  readonly onClearDueDate = output<void>();
  readonly onTogglePriority = output<void>();
  readonly onStatusChange = output<TaskStatus>();
  readonly onAddTag = output<TaskTag>();
  readonly onRemoveTag = output<string>();
  readonly onCreateTag = output<string>();

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
  readonly selectedTab = signal<'dueDate' | 'comments' | 'tags' | null>(null);
  readonly dueDateExpanded = computed(() => this.selectedTab() === 'dueDate');
  readonly commentsExpanded = computed(() => this.selectedTab() === 'comments');
  readonly tagsExpanded = computed(() => this.selectedTab() === 'tags');

  // Date picker popover state
  readonly showDatePicker = signal(false);

  protected toggleDatePicker(): void {
    this.showDatePicker.update(v => !v);
  }

  // Tag picker popover state
  readonly showTagPicker = signal(false);

  // Inline tags expansion state (for overflow handling)
  readonly inlineTagsExpanded = signal(false);

  // Safe accessor for tags that defaults to empty array
  readonly taskTags = computed(() => this.task().tags ?? []);

  // Maximum visible tags before showing "+N" overflow
  private readonly MAX_VISIBLE_TAGS = 3;

  // Visible tags (first 3 when collapsed, all when expanded)
  readonly visibleTags = computed(() => {
    const tags = this.taskTags();
    if (this.inlineTagsExpanded() || tags.length <= this.MAX_VISIBLE_TAGS) {
      return tags;
    }
    return tags.slice(0, this.MAX_VISIBLE_TAGS);
  });

  // Overflow count (remaining tags not shown)
  readonly overflowCount = computed(() => {
    const tags = this.taskTags();
    if (this.inlineTagsExpanded() || tags.length <= this.MAX_VISIBLE_TAGS) {
      return 0;
    }
    return tags.length - this.MAX_VISIBLE_TAGS;
  });

  // Whether to show the inline tags row (when task has tags)
  readonly hasInlineTags = computed(() => this.taskTags().length > 0);

  // Existing tag IDs for filtering in picker
  readonly existingTagIds = computed(() => this.taskTags().map(t => t.id));

  // First tag tooltip state
  readonly firstTagSearch = signal('');
  readonly inlineTagInput = viewChild<ElementRef<HTMLInputElement>>('inlineTagInput');

  // Filtered suggestions for first tag tooltip (max 4)
  readonly tooltipSuggestions = computed(() => {
    const query = this.firstTagSearch().toLowerCase().trim();
    const existingIds = new Set(this.taskTags().map(t => t.id));
    const available = this.allTags().filter(tag => !existingIds.has(tag.id));
    if (!query) return available.slice(0, 4);
    return available
      .filter(tag => tag.name.toLowerCase().includes(query))
      .slice(0, 4);
  });

  // Can create new tag with current search text
  readonly canCreateFirstTag = computed(() => {
    const query = this.firstTagSearch().trim();
    if (!query) return false;
    return !this.allTags().some(tag =>
      tag.name.toLowerCase() === query.toLowerCase()
    );
  });

  // Due date display calculations
  private readonly daysDiff = computed(() => {
    const dueDate = this.task().dueDate;
    if (!dueDate) return null;

    const date = new Date(dueDate + 'T00:00:00');
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return Math.floor((date.getTime() - today.getTime()) / 86400000);
  });

  // Due date tab state computeds for template bindings
  readonly isDueDatePill = computed(() => this.dueDateExpanded() || !!this.task().dueDate);
  readonly isDueDateExpandedDone = computed(() => this.dueDateExpanded() && this.task().status === 'Done');
  readonly isDueDateExpandedOverdue = computed(() => {
    const diff = this.daysDiff();
    return this.dueDateExpanded() && this.task().status !== 'Done' && diff !== null && diff < 0;
  });
  readonly isDueDateExpandedNormal = computed(() => {
    const diff = this.daysDiff();
    return this.dueDateExpanded() && this.task().status !== 'Done' && (diff === null || diff >= 0);
  });
  readonly isDueDateCollapsedDone = computed(() => !this.dueDateExpanded() && !!this.task().dueDate && this.task().status === 'Done');
  // Common condition for collapsed date states (not done)
  private readonly isCollapsedWithDateNotDone = computed(
    () => !this.dueDateExpanded() && !!this.task().dueDate && this.task().status !== 'Done'
  );
  readonly isDueDateCollapsedOverdue = computed(
    () => this.isCollapsedWithDateNotDone() && this.daysDiff() !== null && this.daysDiff()! < 0
  );
  readonly isDueDateCollapsedToday = computed(
    () => this.isCollapsedWithDateNotDone() && this.daysDiff() === 0
  );
  readonly isDueDateCollapsedTomorrow = computed(
    () => this.isCollapsedWithDateNotDone() && this.daysDiff() === 1
  );
  readonly isDueDateCollapsedDefault = computed(
    () => this.isCollapsedWithDateNotDone() && this.daysDiff() !== null && this.daysDiff()! > 1
  );
  readonly isDueDateCircle = computed(() => !this.dueDateExpanded() && !this.task().dueDate);

  // Comments tab state computeds
  readonly isCommentsPill = computed(() => this.commentsExpanded());
  readonly isCommentsCircleWithComments = computed(() => !this.commentsExpanded() && this.task().comments.length > 0);
  readonly isCommentsCircleEmpty = computed(() => !this.commentsExpanded() && this.task().comments.length === 0);

  // Tags tab state computeds
  readonly isTagsPill = computed(() => this.tagsExpanded());
  readonly isTagsCircleWithTags = computed(() => !this.tagsExpanded() && this.taskTags().length > 0);
  readonly isTagsCircleEmpty = computed(() => !this.tagsExpanded() && this.taskTags().length === 0);

  // Status button computeds - derive from previousStatus()/nextStatus() to avoid duplication
  readonly isPrevStatusTodo = computed(() => this.previousStatus() === 'Todo');
  readonly isPrevStatusInProgress = computed(() => this.previousStatus() === 'InProgress');
  readonly isNextStatusInProgress = computed(() => this.nextStatus() === 'InProgress');
  readonly isNextStatusDone = computed(() => this.nextStatus() === 'Done');

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

  /** Close expanded tabs when clicking outside the task card, or close tag picker when clicking outside it */
  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    if (!this.initialized) return;

    const target = event.target;
    // Guard for non-Node targets (e.g., SVG elements in some browsers)
    if (!(target instanceof Node)) return;

    // Close tag picker if clicking outside the tag picker area
    if (this.showTagPicker()) {
      const tagPickerContainer = this.inlineTagInput()?.nativeElement.closest('.relative');
      if (tagPickerContainer && !tagPickerContainer.contains(target)) {
        this.showTagPicker.set(false);
        this.firstTagSearch.set('');
      }
    }

    // Check if anything else is expanded (tab or inline tags)
    if (!this.selectedTab() && !this.inlineTagsExpanded()) return;

    if (!this.elementRef.nativeElement.contains(target)) {
      this.closeExpanded();
    }
  }

  /** Close expanded tabs when pressing Escape */
  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    if (this.selectedTab() || this.inlineTagsExpanded() || this.showTagPicker()) {
      this.closeExpanded();
    }
  }

  /** Close all expanded content */
  private closeExpanded(): void {
    this.selectedTab.set(null);
    this.showDatePicker.set(false);
    this.showTagPicker.set(false);
    this.inlineTagsExpanded.set(false);
  }

  /** Type-safe helper for accessing textarea value from events */
  asTextArea(event: Event): HTMLTextAreaElement {
    return event.target as HTMLTextAreaElement;
  }

  /** Type-safe helper for keyboard events */
  asKeyboardEvent(event: Event): KeyboardEvent {
    return event as KeyboardEvent;
  }

  /** Type-safe helper for accessing input value from events */
  asInput(event: Event): HTMLInputElement {
    return event.target as HTMLInputElement;
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
  toggleTab(tab: 'dueDate' | 'comments' | 'tags'): void {
    const currentTab = this.selectedTab();
    if (currentTab === tab) {
      // Clicking the same tab collapses it
      this.selectedTab.set(null);
      this.showDatePicker.set(false);
      this.showTagPicker.set(false);
    } else {
      // Switch to the new tab
      this.selectedTab.set(tab);
      this.showDatePicker.set(false);
      this.showTagPicker.set(false);

      // Auto-focus the add comment input when expanding comments
      if (tab === 'comments') {
        afterNextRender(() => {
          this.newCommentInput()?.nativeElement.focus();
        }, { injector: this.injector });
      }
    }
  }

  // Due date quick selection methods
  selectQuickDate(option: 'today' | 'tomorrow' | 'friday' | 'nextWeek' | 'plus35'): void {
    const date = this.getQuickOptionDate(option);
    this.onSetDueDate.emit(this.formatDateString(date));
  }

  private getQuickOptionDate(option: 'today' | 'tomorrow' | 'friday' | 'nextWeek' | 'plus35'): Date {
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    switch (option) {
      case 'today':
        return today;
      case 'tomorrow':
        return new Date(today.getTime() + 86400000);
      case 'friday':
        return this.getNextFriday(today);
      case 'nextWeek':
        return new Date(today.getTime() + 7 * 86400000);
      case 'plus35':
        return new Date(today.getTime() + 35 * 86400000);
    }
  }

  private getNextFriday(from: Date): Date {
    const dayOfWeek = from.getDay(); // 0 = Sunday, 5 = Friday
    const daysUntilFriday = (5 - dayOfWeek + 7) % 7 || 7; // If today is Friday, get next Friday
    return new Date(from.getTime() + daysUntilFriday * 86400000);
  }

  isDateSelected(option: 'today' | 'tomorrow' | 'friday' | 'nextWeek' | 'plus35'): boolean {
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

  moveToPreviousStatus(): void {
    this.onStatusChange.emit(this.previousStatus());
  }

  moveToNextStatus(): void {
    this.onStatusChange.emit(this.nextStatus());
  }

  // Tag methods
  openInlineTagInput(): void {
    // Open inline tag input and close any expanded tabs
    this.showTagPicker.set(true);
    this.selectedTab.set(null);
    this.firstTagSearch.set('');

    // Focus the input after render
    afterNextRender(() => {
      this.inlineTagInput()?.nativeElement.focus();
    }, { injector: this.injector });
  }

  onFirstTagEnter(): void {
    const query = this.firstTagSearch().trim();
    const suggestions = this.tooltipSuggestions();

    // If there's an exact match, select it
    const exactMatch = suggestions.find(t =>
      t.name.toLowerCase() === query.toLowerCase()
    );
    if (exactMatch) {
      this.addTag({ id: exactMatch.id, name: exactMatch.name });
      return;
    }

    // If can create, create it
    if (this.canCreateFirstTag()) {
      this.createAndAddTag(query);
      return;
    }

    // If there's a single suggestion, select it
    if (suggestions.length === 1) {
      this.addTag({ id: suggestions[0].id, name: suggestions[0].name });
    }
  }

  addTag(tag: TaskTag): void {
    // Guard against duplicates
    if (this.taskTags().some(t => t.id === tag.id)) {
      this.showTagPicker.set(false);
      this.firstTagSearch.set('');
      return;
    }
    this.onAddTag.emit(tag);
    this.showTagPicker.set(false);
    this.firstTagSearch.set('');
  }

  removeTag(tagId: string): void {
    this.onRemoveTag.emit(tagId);
  }

  createAndAddTag(name: string): void {
    this.onCreateTag.emit(name);
    this.showTagPicker.set(false);
    this.firstTagSearch.set('');
  }

  /** Highlight matching portion of tag name in dropdown */
  highlightMatch(tagName: string): string {
    const query = this.firstTagSearch().toLowerCase().trim();
    if (!query) return this.escapeHtml(tagName);

    const lowerName = tagName.toLowerCase();
    const index = lowerName.indexOf(query);
    if (index === -1) return this.escapeHtml(tagName);

    const before = tagName.slice(0, index);
    const match = tagName.slice(index, index + query.length);
    const after = tagName.slice(index + query.length);

    return `${this.escapeHtml(before)}<mark class="search-highlight">${this.escapeHtml(match)}</mark>${this.escapeHtml(after)}`;
  }

  private escapeHtml(text: string): string {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
  }
}
