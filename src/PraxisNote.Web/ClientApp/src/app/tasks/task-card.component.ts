import { Component, computed, ElementRef, input, output, signal, viewChild, inject, Injector, afterNextRender, ChangeDetectionStrategy, DestroyRef } from '@angular/core';
import { NgClass } from '@angular/common';
import { Task, Comment } from './task.model';
import { AutoResizeDirective } from '../shared/directives/auto-resize.directive';
import { StatusColorPipe } from '../shared/pipes/status-color.pipe';
import { LinkifyPipe } from '../shared/pipes/linkify.pipe';
import { DeleteConfirmationService } from '../shared/services/delete-confirmation.service';
import { DueDateBadgeComponent } from './due-date-badge.component';

@Component({
  selector: 'app-task-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgClass, AutoResizeDirective, StatusColorPipe, LinkifyPipe, DueDateBadgeComponent],
  template: `
    <div
      class="bg-surface rounded-md py-2 px-3 border transition-colors group"
      [ngClass]="task().status | statusColor:'border'"
    >
      @if (editing()) {
        <div>
          <textarea
            #editInput
            appAutoResize
            [value]="editTitle()"
            (input)="editTitle.set(asTextArea($event).value)"
            (keydown.enter)="onEnterKey(asKeyboardEvent($event))"
            (keydown.escape)="cancelEdit()"
            (blur)="saveEdit()"
            rows="1"
            class="w-full text-sm text-foreground bg-transparent border-0 border-b border-primary/50 outline-none resize-none p-0 leading-normal"
          ></textarea>
          <p class="text-xs text-foreground-muted/50 mt-1">Enter to save · Esc to cancel</p>
        </div>
      } @else {
        <!-- Task content -->
        <div class="flex items-start gap-2">
          <div class="flex-1 min-w-0">
            <!-- Clickable title for inline editing -->
            <p
              class="text-sm text-foreground whitespace-pre-wrap cursor-pointer hover:bg-surface-hover rounded px-1 -mx-1 transition-colors"
              [class.line-through]="task().status === 'Done'"
              [class.text-foreground-muted]="task().status === 'Done'"
              (click)="startEdit(); $event.stopPropagation()"
            >{{ task().title }}</p>
          </div>
          <!-- Time (visible) / Delete button (on hover) -->
          <div class="flex items-center shrink-0">
            @if (confirmingTaskDelete()) {
              <!-- Delete confirmation mode -->
              <button
                type="button"
                class="flex items-center gap-1 text-red-500 animate-pulse text-xs"
                (click)="confirmTaskDelete(); $event.stopPropagation()"
                aria-label="Confirm delete task"
              >
                <i class="pi pi-trash"></i>
                <span>Confirm?</span>
              </button>
            } @else {
              @if (relativeTime(); as time) {
                <!-- Relative container prevents layout shift on hover -->
                <div class="relative leading-none">
                  <span
                    class="text-xs transition-opacity md:group-hover:opacity-0"
                    [class.text-inprogress-foreground-muted]="task().status === 'InProgress'"
                    [class.text-done-foreground-muted]="task().status === 'Done'"
                  >{{ time }}</span>
                  <button
                    type="button"
                    class="absolute inset-0 flex items-center justify-end text-foreground-muted/40 hover:text-danger text-xs invisible opacity-0 md:group-hover:visible md:group-hover:opacity-100 transition-opacity"
                    (click)="startTaskDeleteConfirm(); $event.stopPropagation()"
                    aria-label="Delete task"
                  >
                    <i class="pi pi-trash"></i>
                  </button>
                </div>
              } @else {
                <!-- No time displayed (Todo) - simple hover button -->
                <button
                  type="button"
                  class="hidden md:group-hover:flex text-foreground-muted/40 hover:text-danger text-xs transition-opacity"
                  (click)="startTaskDeleteConfirm(); $event.stopPropagation()"
                  aria-label="Delete task"
                >
                  <i class="pi pi-trash"></i>
                </button>
              }
            }
          </div>
        </div>

        <!-- Icon row for due date and comments toggle -->
        <div class="mt-1.5 flex items-center gap-2 text-xs">
          <app-due-date-badge
            [dueDate]="task().dueDate"
            [taskStatus]="task().status"
            (onSetDueDate)="onSetDueDate.emit($event)"
            (onClearDueDate)="onClearDueDate.emit()"
          />

          <!-- Comment toggle button -->
          <button
            type="button"
            class="relative flex items-center justify-center w-6 h-6 rounded transition-colors"
            [class.text-foreground-muted/40]="!commentsExpanded()"
            [class.hover:text-foreground-muted]="!commentsExpanded()"
            [class.text-primary]="commentsExpanded()"
            [class.bg-primary/10]="commentsExpanded()"
            (click)="toggleComments(); $event.stopPropagation()"
            [attr.aria-label]="commentsExpanded() ? 'Hide comments' : 'Show comments'"
            [attr.aria-expanded]="commentsExpanded()"
          >
            <i class="pi pi-comment"></i>
            @if (task().comments.length > 0) {
              <span
                class="absolute -top-1 -right-1 min-w-4 h-4 px-1 flex items-center justify-center rounded-full text-[10px] font-medium"
                [class.bg-primary]="commentsExpanded()"
                [class.text-white]="commentsExpanded()"
                [class.bg-foreground-muted/20]="!commentsExpanded()"
                [class.text-foreground-muted]="!commentsExpanded()"
              >{{ task().comments.length }}</span>
            }
          </button>
        </div>

        <!-- Comments section (expandable) -->
        @if (commentsExpanded()) {
          <!-- Add comment input -->
          <div class="mt-2 flex items-start gap-1.5 text-xs">
            <i class="pi pi-plus text-foreground-muted/30 shrink-0 mt-0.5"></i>
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
              class="flex-1 bg-transparent border-0 outline-none text-foreground-muted placeholder-foreground-muted/30 resize-none leading-normal"
            ></textarea>
          </div>

          <!-- Comments list -->
          @if (task().comments.length > 0) {
          <div class="mt-2 space-y-2">
            @for (comment of task().comments; track comment.id) {
              @if (editingCommentId() === comment.id) {
                <!-- Editing comment -->
                <div class="text-xs">
                  <div class="flex items-start gap-1.5">
                    <i class="pi pi-comment text-foreground-muted/40 shrink-0 mt-0.5"></i>
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
                      class="flex-1 text-foreground-muted bg-transparent border-0 border-b border-primary/50 outline-none resize-none p-0 leading-normal"
                    ></textarea>
                  </div>
                  <p class="text-foreground-muted/40 mt-0.5 ml-5">Enter to save · Esc to cancel</p>
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
                  <i class="pi pi-comment text-foreground-muted/40 shrink-0 mt-0.5"></i>
                  <span class="text-foreground-muted flex-1 min-w-0 break-words" [innerHTML]="comment.content | linkify"></span>
                  @if (confirmingCommentDeleteId() === comment.id) {
                    <!-- Delete confirmation mode -->
                    <button
                      type="button"
                      class="flex items-center gap-1 text-red-500 animate-pulse shrink-0 text-xs"
                      (click)="confirmCommentDelete(comment.id); $event.stopPropagation()"
                      [attr.aria-label]="'Confirm delete comment: ' + comment.content"
                    >
                      <i class="pi pi-trash"></i>
                      <span>Confirm?</span>
                    </button>
                  } @else {
                    <span class="text-foreground-muted/30 shrink-0 group-hover/comment:hidden">{{ formatCommentTime(comment) }}</span>
                    <button
                      type="button"
                      class="hidden group-hover/comment:flex text-foreground-muted/40 hover:text-danger shrink-0 text-xs"
                      (click)="startCommentDeleteConfirm(comment.id); $event.stopPropagation()"
                      [attr.aria-label]="'Delete comment: ' + comment.content"
                    >
                      <i class="pi pi-trash"></i>
                    </button>
                  }
                </div>
              }
            }
          </div>
          }
        }
      }
    </div>
  `,
})
export class TaskCardComponent {
  private readonly injector = inject(Injector);
  private readonly destroyRef = inject(DestroyRef);
  private readonly deleteConfirmation = inject(DeleteConfirmationService);

  readonly task = input.required<Task>();

  readonly onEdit = output<string>();
  readonly onDelete = output<void>();
  readonly onAddComment = output<string>();
  readonly onEditComment = output<{ commentId: string; content: string }>();
  readonly onDeleteComment = output<string>();
  readonly onSetDueDate = output<string>();
  readonly onClearDueDate = output<void>();

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

  // Comments expand/collapse state
  readonly commentsExpanded = signal(false);

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

  // Comment methods
  toggleComments(): void {
    this.commentsExpanded.update(v => !v);
  }

  formatCommentTime(comment: Comment): string {
    const dateStr = comment.updatedAt !== comment.createdAt ? comment.updatedAt : comment.createdAt;
    const prefix = comment.updatedAt !== comment.createdAt ? 'edited ' : '';
    return prefix + this.formatTime(dateStr, 'completed');
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
}
