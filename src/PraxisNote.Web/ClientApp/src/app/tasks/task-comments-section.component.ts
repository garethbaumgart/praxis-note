import { Component, ElementRef, input, output, signal, viewChild, inject, Injector, afterNextRender, ChangeDetectionStrategy, DestroyRef } from '@angular/core';
import { Comment } from './task.model';
import { AutoResizeDirective } from '../shared/directives/auto-resize.directive';
import { LinkifyPipe } from '../shared/pipes/linkify.pipe';
import { DeleteConfirmationService } from '../shared/services/delete-confirmation.service';
import { DeleteConfirmButtonComponent } from '../shared/components/delete-confirm-button.component';

@Component({
  selector: 'app-task-comments-section',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AutoResizeDirective, LinkifyPipe, DeleteConfirmButtonComponent],
  template: `
    <div class="mt-2 p-2 bg-comments-section rounded-lg border border-comments-section-border">
      <!-- Comments list -->
      @if (comments().length > 0) {
        <div class="space-y-1.5 mb-2">
          @for (comment of comments(); track comment.id) {
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
                    class="touch-target flex md:hidden text-foreground-muted hover:text-danger shrink-0 text-xs"
                    (click)="startCommentDeleteConfirm(comment.id); $event.stopPropagation()"
                    [attr.aria-label]="getDeleteCommentAriaLabel(comment)"
                  >
                    <i class="pi pi-trash"></i>
                  </button>
                  <!-- Desktop: hover/focus-reveal delete button for keyboard accessibility -->
                  <button
                    type="button"
                    class="touch-target hidden md:group-hover/comment:flex md:group-focus-within/comment:flex text-foreground-muted hover:text-danger shrink-0 text-xs"
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
  `,
})
export class TaskCommentsSectionComponent {
  private readonly injector = inject(Injector);
  private readonly destroyRef = inject(DestroyRef);
  private readonly deleteConfirmation = inject(DeleteConfirmationService);

  // Inputs
  readonly comments = input.required<Comment[]>();

  // Outputs
  readonly onAdd = output<string>();
  readonly onEdit = output<{ commentId: string; content: string }>();
  readonly onDelete = output<string>();

  // Internal state
  readonly editingCommentId = signal<string | null>(null);
  readonly editCommentContent = signal('');
  readonly newCommentText = signal('');
  readonly confirmingCommentDeleteId = signal<string | null>(null);

  // ViewChild refs
  readonly commentEditInput = viewChild<ElementRef<HTMLTextAreaElement>>('commentEditInput');
  readonly newCommentInput = viewChild<ElementRef<HTMLTextAreaElement>>('newCommentInput');

  constructor() {
    this.destroyRef.onDestroy(() => {
      this.deleteConfirmation.cleanup();
    });
  }

  /** Focus the new comment input - called by parent after expanding */
  focusNewCommentInput(): void {
    this.newCommentInput()?.nativeElement.focus();
  }

  /** Type-safe helper for accessing textarea value from events */
  asTextArea(event: Event): HTMLTextAreaElement {
    return event.target as HTMLTextAreaElement;
  }

  /** Type-safe helper for keyboard events */
  asKeyboardEvent(event: Event): KeyboardEvent {
    return event as KeyboardEvent;
  }

  formatCommentTime(comment: Comment): string {
    const dateStr = comment.updatedAt !== comment.createdAt ? comment.updatedAt : comment.createdAt;
    const prefix = comment.updatedAt !== comment.createdAt ? 'edited ' : '';
    return prefix + this.formatTime(dateStr);
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
    return `Delete comment: ${content.slice(0, maxLength).trimEnd()}...`;
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
    const originalComment = this.comments().find(c => c.id === commentId);
    if (content && originalComment && content !== originalComment.content) {
      this.onEdit.emit({ commentId, content });
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
      this.onAdd.emit(content);
      this.newCommentText.set('');
      // Reset textarea height after clearing content
      const textarea = this.newCommentInput()?.nativeElement;
      if (textarea) {
        textarea.style.height = 'auto';
      }
    }
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
    this.onDelete.emit(commentId);
  }

  /** Format a timestamp as relative time (e.g., "5m ago", "2h ago") */
  private formatTime(dateStr: string): string {
    const date = new Date(dateStr);
    if (isNaN(date.getTime())) return '';

    const diffMs = Math.max(0, Date.now() - date.getTime());
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;
    return date.toLocaleDateString();
  }
}
