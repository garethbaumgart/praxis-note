import { Component, computed, ElementRef, input, output, signal, viewChild, inject, Injector, afterNextRender, ChangeDetectionStrategy, DestroyRef } from '@angular/core';
import { Task, Comment } from './task.model';

@Component({
  selector: 'app-task-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
  template: `
    <div
      class="bg-surface rounded-md py-2 px-3 border transition-colors group"
      [class.border-todo-border]="task().status === 'Todo'"
      [class.border-inprogress-border]="task().status === 'InProgress'"
      [class.border-done-border]="task().status === 'Done'"
    >
      @if (editing()) {
        <div>
          <textarea
            #editInput
            [value]="editTitle()"
            (input)="onInput($event)"
            (keydown.enter)="onEnterKey($any($event))"
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
                class="flex items-center gap-1 text-red-500 animate-pulse"
                (click)="confirmTaskDelete(); $event.stopPropagation()"
                aria-label="Confirm delete task"
              >
                <i class="pi pi-trash text-[10px]"></i>
                <span class="text-[10px]">Confirm?</span>
              </button>
            } @else {
              @if (relativeTime(); as time) {
                <span
                  class="text-xs md:group-hover:hidden"
                  [class.text-inprogress-foreground-muted]="task().status === 'InProgress'"
                  [class.text-done-foreground-muted]="task().status === 'Done'"
                >{{ time }}</span>
              }
              <button
                type="button"
                class="hidden md:group-hover:flex text-foreground-muted/40 hover:text-danger"
                (click)="startTaskDeleteConfirm(); $event.stopPropagation()"
                aria-label="Delete task"
              >
                <i class="pi pi-trash text-[10px]"></i>
              </button>
            }
          </div>
        </div>

        <!-- Add comment input -->
        <div class="mt-2 flex items-start gap-1.5">
          <i class="pi pi-plus text-[10px] text-foreground-muted/30 shrink-0 mt-0.5"></i>
          <textarea
            [value]="newCommentText()"
            (input)="onNewCommentInput($event)"
            (keydown.enter)="onNewCommentEnterKey($any($event))"
            (keydown.escape)="newCommentText.set('')"
            placeholder="Add comment..."
            aria-label="Add comment"
            rows="1"
            class="flex-1 text-xs bg-transparent border-0 outline-none text-foreground-muted placeholder-foreground-muted/30 resize-none leading-normal"
          ></textarea>
        </div>

        <!-- Comments (Linear - Minimal Activity) -->
        @if (task().comments.length > 0) {
          <div class="mt-2 space-y-2">
            @for (comment of task().comments; track comment.id) {
              @if (editingCommentId() === comment.id) {
                <!-- Editing comment -->
                <div>
                  <div class="flex items-start gap-1.5">
                    <i class="pi pi-comment text-[10px] text-foreground-muted/40 shrink-0 mt-0.5"></i>
                    <textarea
                      #commentEditInput
                      [value]="editCommentContent()"
                      (input)="onCommentInput($event)"
                      (keydown.enter)="onCommentEnterKey($any($event))"
                      (keydown.escape)="cancelCommentEdit()"
                      (blur)="saveCommentEdit(comment.id)"
                      rows="1"
                      aria-label="Edit comment"
                      class="flex-1 text-xs text-foreground-muted bg-transparent border-0 border-b border-primary/50 outline-none resize-none p-0 leading-normal"
                    ></textarea>
                  </div>
                  <p class="text-xs text-foreground-muted/40 mt-0.5 ml-5">Enter to save · Esc to cancel</p>
                </div>
              } @else {
                <!-- Display comment as minimal row -->
                <div
                  class="group/comment flex items-start gap-1.5 cursor-pointer"
                  role="button"
                  tabindex="0"
                  (click)="startCommentEdit(comment); $event.stopPropagation()"
                  (keydown.enter)="startCommentEdit(comment); $event.preventDefault(); $event.stopPropagation()"
                  (keydown.space)="startCommentEdit(comment); $event.preventDefault(); $event.stopPropagation()"
                >
                  <i class="pi pi-comment text-[10px] text-foreground-muted/40 shrink-0 mt-0.5"></i>
                  <span class="text-xs text-foreground-muted flex-1 min-w-0 whitespace-pre-wrap">{{ comment.content }}</span>
                  @if (confirmingCommentDeleteId() === comment.id) {
                    <!-- Delete confirmation mode -->
                    <button
                      type="button"
                      class="flex items-center gap-1 text-red-500 animate-pulse shrink-0"
                      (click)="confirmCommentDelete(comment.id); $event.stopPropagation()"
                      [attr.aria-label]="'Confirm delete comment: ' + comment.content"
                    >
                      <i class="pi pi-trash text-[10px]"></i>
                      <span class="text-[10px]">Confirm?</span>
                    </button>
                  } @else {
                    <span class="text-xs text-foreground-muted/30 shrink-0 group-hover/comment:hidden">{{ formatCommentTime(comment) }}</span>
                    <button
                      type="button"
                      class="hidden group-hover/comment:flex text-foreground-muted/40 hover:text-danger shrink-0"
                      (click)="startCommentDeleteConfirm(comment.id); $event.stopPropagation()"
                      [attr.aria-label]="'Delete comment: ' + comment.content"
                    >
                      <i class="pi pi-trash text-[10px]"></i>
                    </button>
                  }
                </div>
              }
            }
          </div>
        }
      }
    </div>
  `,
  styles: [`
    textarea {
      font-family: inherit;
    }
  `]
})
export class TaskCardComponent {
  private readonly injector = inject(Injector);
  private readonly destroyRef = inject(DestroyRef);

  readonly task = input.required<Task>();

  // Events
  readonly onStatusChange = output<'Todo' | 'InProgress' | 'Done'>();
  readonly onTitleChange = output<string>();
  readonly onDelete = output<void>();
  readonly onAddComment = output<string>();
  readonly onUpdateComment = output<{ id: string; content: string }>();
  readonly onDeleteComment = output<string>();

  // Editing state
  readonly editing = signal(false);
  readonly editTitle = signal('');
  readonly editInput = viewChild<ElementRef<HTMLTextAreaElement>>('editInput');

  // Comment editing state
  readonly editingCommentId = signal<string | null>(null);
  readonly editCommentContent = signal('');
  readonly newCommentText = signal('');
  readonly commentEditInput = viewChild<ElementRef<HTMLTextAreaElement>>('commentEditInput');

  // Delete confirmation state
  readonly confirmingTaskDelete = signal(false);
  readonly confirmingCommentDeleteId = signal<string | null>(null);
  private deleteConfirmTimeoutId: ReturnType<typeof setTimeout> | null = null;
  private deleteConfirmClickHandler: (() => void) | null = null;

  // Tick signal for auto-updating relative times (updates every minute)
  private readonly tick = signal(Date.now());

  readonly relativeTime = computed(() => {
    this.tick(); // Depend on tick for auto-updates
    const task = this.task();
    if (task.status === 'InProgress') {
      return this.formatTime(task.statusUpdatedAt, 'elapsed');
    } else if (task.status === 'Done') {
      return this.formatTime(task.statusUpdatedAt, 'completed');
    }
    return null;
  });

  constructor() {
    // Update tick every minute for auto-updating relative times
    const intervalId = setInterval(() => this.tick.set(Date.now()), 60000);
    this.destroyRef.onDestroy(() => {
      clearInterval(intervalId);
      // Clean up delete confirmation timeouts
      this.clearDeleteConfirmation();
    });
  }

  private clearDeleteConfirmation(): void {
    if (this.deleteConfirmTimeoutId) {
      clearTimeout(this.deleteConfirmTimeoutId);
      this.deleteConfirmTimeoutId = null;
    }
    if (this.deleteConfirmClickHandler) {
      document.removeEventListener('click', this.deleteConfirmClickHandler);
      this.deleteConfirmClickHandler = null;
    }
  }

  private formatTime(dateStr: string, type: 'elapsed' | 'completed'): string {
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (type === 'elapsed') {
      if (diffMins < 60) {
        return `${diffMins}m`;
      } else if (diffHours < 24) {
        return `${diffHours}h`;
      } else {
        return `${diffDays}d`;
      }
    } else {
      // completed
      if (diffMins < 1) {
        return 'just now';
      } else if (diffMins < 60) {
        return `${diffMins}m ago`;
      } else if (diffHours < 24) {
        return `${diffHours}h ago`;
      } else {
        return `${diffDays}d ago`;
      }
    }
  }

  formatCommentTime(comment: Comment): string {
    const date = new Date(comment.updatedAt);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMins < 1) {
      return 'now';
    } else if (diffMins < 60) {
      return `${diffMins}m`;
    } else if (diffHours < 24) {
      return `${diffHours}h`;
    } else {
      return `${diffDays}d`;
    }
  }

  startEdit(): void {
    this.editTitle.set(this.task().title);
    this.editing.set(true);

    // Use AfterNextRender to focus and auto-resize after Angular renders
    afterNextRender(() => {
      const textarea = this.editInput()?.nativeElement;
      if (textarea) {
        this.autoResize(textarea);
        textarea.focus();
        textarea.select();
      }
    }, { injector: this.injector });
  }

  onInput(event: Event): void {
    const textarea = event.target as HTMLTextAreaElement;
    this.editTitle.set(textarea.value);
    this.autoResize(textarea);
  }

  onEnterKey(event: KeyboardEvent): void {
    if (event.shiftKey) {
      return; // Allow Shift+Enter for new line
    }
    event.preventDefault();
    event.stopPropagation();
    this.saveEdit();
  }

  saveEdit(): void {
    const newTitle = this.editTitle().trim();
    if (newTitle && newTitle !== this.task().title) {
      this.onTitleChange.emit(newTitle);
    }
    this.editing.set(false);
    this.editTitle.set('');
  }

  cancelEdit(): void {
    this.editing.set(false);
    this.editTitle.set('');
  }

  private autoResize(textarea: HTMLTextAreaElement): void {
    textarea.style.height = 'auto';
    textarea.style.height = `${textarea.scrollHeight}px`;
  }

  // Comment editing methods
  startCommentEdit(comment: Comment): void {
    this.editCommentContent.set(comment.content);
    this.editingCommentId.set(comment.id);

    // Use AfterNextRender to focus and auto-resize after Angular renders
    afterNextRender(() => {
      const textarea = this.commentEditInput()?.nativeElement;
      if (textarea) {
        this.autoResize(textarea);
        textarea.focus();
        textarea.select();
      }
    }, { injector: this.injector });
  }

  onCommentInput(event: Event): void {
    const textarea = event.target as HTMLTextAreaElement;
    this.editCommentContent.set(textarea.value);
    this.autoResize(textarea);
  }

  onCommentEnterKey(event: KeyboardEvent): void {
    if (event.shiftKey) {
      return; // Allow Shift+Enter for new line
    }
    event.preventDefault();
    event.stopPropagation();
    const commentId = this.editingCommentId();
    if (commentId) {
      this.saveCommentEdit(commentId);
    }
  }

  saveCommentEdit(commentId: string): void {
    const newContent = this.editCommentContent().trim();
    if (newContent) {
      const currentComment = this.task().comments.find(c => c.id === commentId);
      if (currentComment && newContent !== currentComment.content) {
        this.onUpdateComment.emit({ id: commentId, content: newContent });
      }
    }
    this.editingCommentId.set(null);
    this.editCommentContent.set('');
  }

  cancelCommentEdit(): void {
    this.editingCommentId.set(null);
    this.editCommentContent.set('');
  }

  onNewCommentInput(event: Event): void {
    const textarea = event.target as HTMLTextAreaElement;
    this.newCommentText.set(textarea.value);
    this.autoResize(textarea);
  }

  onNewCommentEnterKey(event: KeyboardEvent): void {
    if (event.shiftKey) {
      return; // Allow Shift+Enter for new line
    }
    event.preventDefault();
    event.stopPropagation();
    this.submitNewComment(event.target as HTMLTextAreaElement);
  }

  submitNewComment(textarea?: HTMLTextAreaElement): void {
    const content = this.newCommentText().trim();
    if (content) {
      this.onAddComment.emit(content);
      this.newCommentText.set('');
      // Reset textarea height
      if (textarea) {
        textarea.style.height = 'auto';
      }
    }
  }

  // Task delete confirmation methods
  startTaskDeleteConfirm(): void {
    this.clearDeleteConfirmation();
    this.confirmingTaskDelete.set(true);

    // Auto-cancel after 3 seconds
    this.deleteConfirmTimeoutId = setTimeout(() => {
      this.confirmingTaskDelete.set(false);
      this.clearDeleteConfirmation();
    }, 3000);

    // Cancel on any click outside (after current event completes)
    setTimeout(() => {
      this.deleteConfirmClickHandler = () => {
        if (this.confirmingTaskDelete()) {
          this.confirmingTaskDelete.set(false);
        }
        this.clearDeleteConfirmation();
      };
      document.addEventListener('click', this.deleteConfirmClickHandler, { once: true });
    }, 0);
  }

  confirmTaskDelete(): void {
    this.clearDeleteConfirmation();
    this.confirmingTaskDelete.set(false);
    this.onDelete.emit();
  }

  // Comment delete confirmation methods
  startCommentDeleteConfirm(commentId: string): void {
    this.clearDeleteConfirmation();
    this.confirmingCommentDeleteId.set(commentId);

    // Auto-cancel after 3 seconds
    this.deleteConfirmTimeoutId = setTimeout(() => {
      if (this.confirmingCommentDeleteId() === commentId) {
        this.confirmingCommentDeleteId.set(null);
      }
      this.clearDeleteConfirmation();
    }, 3000);

    // Cancel on any click outside (after current event completes)
    setTimeout(() => {
      this.deleteConfirmClickHandler = () => {
        if (this.confirmingCommentDeleteId() === commentId) {
          this.confirmingCommentDeleteId.set(null);
        }
        this.clearDeleteConfirmation();
      };
      document.addEventListener('click', this.deleteConfirmClickHandler, { once: true });
    }, 0);
  }

  confirmCommentDelete(commentId: string): void {
    this.clearDeleteConfirmation();
    this.confirmingCommentDeleteId.set(null);
    this.onDeleteComment.emit(commentId);
  }
}
