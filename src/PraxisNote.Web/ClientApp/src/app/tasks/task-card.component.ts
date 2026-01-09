import { Component, computed, ElementRef, input, output, signal, viewChild, inject, Injector, afterNextRender, ChangeDetectionStrategy, DestroyRef } from '@angular/core';
import { Button } from 'primeng/button';
import { Task, Comment } from './task.model';

@Component({
  selector: 'app-task-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button],
  template: `
    <div
      class="bg-surface rounded-md py-2 px-3 border transition-colors group"
      [class.border-todo-border]="task().status === 'Todo'"
      [class.border-inprogress-border]="task().status === 'InProgress'"
      [class.border-done-border]="task().status === 'Done'"
    >
      @if (editing()) {
        <div class="flex items-start gap-2">
          <textarea
            #editInput
            [value]="editTitle()"
            (input)="onInput($event)"
            (keydown.enter)="onEnterKey($any($event))"
            (keydown.escape)="cancelEdit()"
            rows="1"
            class="flex-1 text-sm text-foreground bg-transparent border-0 outline-none resize-none p-0 leading-normal"
          ></textarea>
          <div class="flex items-center gap-1 shrink-0">
            <p-button
              icon="pi pi-check"
              [rounded]="true"
              [text]="true"
              size="small"
              severity="success"
              (onClick)="saveEdit()"
              aria-label="Save"
            />
            <p-button
              icon="pi pi-times"
              [rounded]="true"
              [text]="true"
              size="small"
              severity="secondary"
              (onClick)="cancelEdit()"
              aria-label="Cancel"
            />
          </div>
        </div>
      } @else {
        <!-- Task content with document icon -->
        <div class="flex items-start gap-2">
          <i
            class="pi text-sm mt-0.5"
            [class.pi-lightbulb]="task().status === 'Todo'"
            [class.text-todo-foreground-muted]="task().status === 'Todo'"
            [class.pi-clock]="task().status === 'InProgress'"
            [class.text-inprogress-foreground-muted]="task().status === 'InProgress'"
            [class.pi-check-circle]="task().status === 'Done'"
            [class.text-done-foreground-muted]="task().status === 'Done'"
          ></i>
          <div class="flex-1 min-w-0">
            <!-- Clickable title for inline editing -->
            <p
              class="text-sm text-foreground whitespace-pre-wrap cursor-pointer hover:bg-surface-hover rounded px-1 -mx-1 transition-colors"
              [class.line-through]="task().status === 'Done'"
              [class.text-foreground-muted]="task().status === 'Done'"
              (click)="startEdit(); $event.stopPropagation()"
            >{{ task().title }}</p>
            @if (relativeTime(); as time) {
              <span
                class="text-xs"
                [class.text-inprogress-foreground-muted]="task().status === 'InProgress'"
                [class.text-done-foreground-muted]="task().status === 'Done'"
              >{{ time }}</span>
            }
          </div>
          <!-- Delete button only - edit via clicking title -->
          <div class="flex items-center gap-1 shrink-0 md:opacity-0 md:group-hover:opacity-100 group-focus-within:opacity-100 transition-opacity">
            <p-button
              icon="pi pi-trash"
              [rounded]="true"
              [text]="true"
              size="small"
              severity="danger"
              (onClick)="onDelete.emit(); $event.stopPropagation()"
              aria-label="Delete task"
            />
          </div>
        </div>

        <!-- Comments (Option C: Stacked Notes) -->
        @if (task().comments.length > 0) {
          <div class="mt-3 space-y-1">
            @for (comment of task().comments; track comment.id) {
              @if (editingCommentId() === comment.id) {
                <!-- Editing comment -->
                <div class="bg-surface-hover rounded px-3 py-1.5 flex items-start gap-2">
                  <textarea
                    #commentEditInput
                    [value]="editCommentContent()"
                    (input)="onCommentInput($event)"
                    (keydown.enter)="onCommentEnterKey($any($event))"
                    (keydown.escape)="cancelCommentEdit()"
                    rows="1"
                    aria-label="Edit comment"
                    class="flex-1 text-xs text-foreground-muted bg-transparent border-0 outline-none resize-none p-0 leading-normal"
                  ></textarea>
                  <div class="flex items-center gap-0.5 shrink-0">
                    <p-button
                      icon="pi pi-check"
                      [rounded]="true"
                      [text]="true"
                      size="small"
                      severity="success"
                      (onClick)="saveCommentEdit(comment.id)"
                      aria-label="Save"
                    />
                    <p-button
                      icon="pi pi-times"
                      [rounded]="true"
                      [text]="true"
                      size="small"
                      severity="secondary"
                      (onClick)="cancelCommentEdit()"
                      aria-label="Cancel"
                    />
                  </div>
                </div>
              } @else {
                <!-- Display comment as stacked block -->
                <div
                  class="group/comment bg-surface-hover rounded px-3 py-1.5 flex items-center justify-between cursor-pointer hover:bg-surface-hover/80 transition-colors"
                  role="button"
                  tabindex="0"
                  (click)="startCommentEdit(comment); $event.stopPropagation()"
                  (keydown.enter)="startCommentEdit(comment); $event.preventDefault(); $event.stopPropagation()"
                  (keydown.space)="startCommentEdit(comment); $event.preventDefault(); $event.stopPropagation()"
                >
                  <span class="text-xs text-foreground-muted flex-1 min-w-0 truncate">{{ comment.content }}</span>
                  <span class="text-xs text-foreground-muted/50 ml-2 shrink-0 group-hover/comment:hidden">{{ formatCommentTime(comment) }}</span>
                  <button
                    type="button"
                    class="hidden group-hover/comment:flex text-foreground-muted/50 hover:text-danger ml-2 shrink-0"
                    (click)="onDeleteComment.emit(comment.id); $event.stopPropagation()"
                    [attr.aria-label]="'Delete comment: ' + comment.content"
                  >
                    <i class="pi pi-times text-xs"></i>
                  </button>
                </div>
              }
            }
          </div>
        }

        <!-- Add comment input (appears on hover) -->
        <div class="mt-2 opacity-0 group-hover:opacity-100 group-focus-within:opacity-100 transition-opacity">
          <input
            type="text"
            [value]="newCommentText()"
            (input)="newCommentText.set($any($event.target).value)"
            (keydown.enter)="submitNewComment(); $event.stopPropagation()"
            (keydown.escape)="newCommentText.set('')"
            placeholder="+ Add note"
            aria-label="Add note"
            class="w-full text-xs bg-surface-hover/50 hover:bg-surface-hover focus:bg-surface-hover rounded px-3 py-1.5 border-0 outline-none text-foreground-muted placeholder-foreground-muted/50 transition-colors"
          />
        </div>
      }
    </div>
  `,
})
export class TaskCardComponent {
  private readonly injector = inject(Injector);
  private readonly destroyRef = inject(DestroyRef);

  readonly task = input.required<Task>();

  readonly onEdit = output<string>();
  readonly onDelete = output<void>();
  readonly onAddComment = output<string>();
  readonly onEditComment = output<{ commentId: string; content: string }>();
  readonly onDeleteComment = output<string>();

  readonly editing = signal(false);
  readonly editTitle = signal('');
  readonly editInput = viewChild<ElementRef<HTMLTextAreaElement>>('editInput');

  // Comment editing state
  readonly editingCommentId = signal<string | null>(null);
  readonly editCommentContent = signal('');
  readonly newCommentText = signal('');
  readonly commentEditInput = viewChild<ElementRef<HTMLTextAreaElement>>('commentEditInput');

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
    this.destroyRef.onDestroy(() => clearInterval(intervalId));
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
    // Focus and auto-resize after view updates
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
      // Allow Shift+Enter for multi-line input
      return;
    }
    event.preventDefault();
    this.saveEdit();
  }

  private autoResize(textarea: HTMLTextAreaElement): void {
    textarea.style.height = 'auto';
    textarea.style.height = textarea.scrollHeight + 'px';
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
  formatCommentTime(comment: Comment): string {
    const dateStr = comment.updatedAt !== comment.createdAt ? comment.updatedAt : comment.createdAt;
    const prefix = comment.updatedAt !== comment.createdAt ? 'edited ' : '';
    return prefix + this.formatTime(dateStr, 'completed');
  }

  startCommentEdit(comment: Comment): void {
    this.editingCommentId.set(comment.id);
    this.editCommentContent.set(comment.content);
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

  submitNewComment(): void {
    const content = this.newCommentText().trim();
    if (content) {
      this.onAddComment.emit(content);
      this.newCommentText.set('');
    }
  }
}
