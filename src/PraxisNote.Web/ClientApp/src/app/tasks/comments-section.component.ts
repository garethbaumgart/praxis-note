import { Component, input, output, signal, viewChild, ElementRef, inject, Injector, afterNextRender, ChangeDetectionStrategy } from '@angular/core';
import { Comment } from './task.model';
import { CommentItemComponent } from './comment-item.component';

@Component({
  selector: 'app-comments-section',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommentItemComponent],
  template: `
    <div class="border-t border-dashed border-border mt-2 pt-2">
      <!-- Expand/Collapse toggle -->
      <button
        class="flex items-center gap-2 w-full text-left text-foreground-muted hover:text-foreground transition-colors"
        (click)="toggleExpanded()"
      >
        <i class="pi pi-comments text-xs"></i>
        <span class="text-xs">
          {{ comments().length }} comment{{ comments().length !== 1 ? 's' : '' }}
        </span>
        <i
          class="pi text-xs ml-auto"
          [class.pi-chevron-down]="!expanded()"
          [class.pi-chevron-up]="expanded()"
        ></i>
      </button>

      @if (expanded()) {
        <div class="mt-3 space-y-3">
          <!-- Add comment input -->
          <div class="flex items-start gap-2">
            <textarea
              #addInput
              [value]="newComment()"
              (input)="onInput($event)"
              (keydown.enter)="onEnterKey($any($event))"
              (keydown.escape)="clearInput()"
              placeholder="Add a comment..."
              rows="1"
              class="flex-1 text-sm text-foreground placeholder-foreground-muted bg-surface-hover border border-border rounded px-2 py-1.5 outline-none resize-none leading-normal focus:border-primary"
            ></textarea>
            @if (newComment().trim()) {
              <button
                class="px-3 py-1.5 text-sm bg-primary text-primary-foreground rounded hover:bg-primary-hover transition-colors"
                (click)="submitComment()"
              >
                Add
              </button>
            }
          </div>

          <!-- Comments list -->
          @for (comment of comments(); track comment.id) {
            <app-comment-item
              [comment]="comment"
              (onEdit)="onEditComment.emit({ commentId: comment.id, content: $event })"
              (onDelete)="onDeleteComment.emit(comment.id)"
            />
          } @empty {
            <p class="text-xs text-foreground-muted text-center py-2">No comments yet</p>
          }
        </div>
      }
    </div>
  `,
})
export class CommentsSectionComponent {
  private readonly injector = inject(Injector);

  readonly comments = input.required<Comment[]>();

  readonly onAddComment = output<string>();
  readonly onEditComment = output<{ commentId: string; content: string }>();
  readonly onDeleteComment = output<string>();

  readonly expanded = signal(false);
  readonly newComment = signal('');

  readonly addInput = viewChild<ElementRef<HTMLTextAreaElement>>('addInput');

  toggleExpanded(): void {
    const newState = !this.expanded();
    this.expanded.set(newState);

    if (newState) {
      afterNextRender(() => {
        this.addInput()?.nativeElement.focus();
      }, { injector: this.injector });
    }
  }

  onInput(event: Event): void {
    const textarea = event.target as HTMLTextAreaElement;
    this.newComment.set(textarea.value);
    this.autoResize(textarea);
  }

  onEnterKey(event: KeyboardEvent): void {
    if (event.shiftKey) {
      return; // Allow Shift+Enter for multi-line
    }
    event.preventDefault();
    this.submitComment();
  }

  private autoResize(textarea: HTMLTextAreaElement): void {
    textarea.style.height = 'auto';
    textarea.style.height = textarea.scrollHeight + 'px';
  }

  submitComment(): void {
    const content = this.newComment().trim();
    if (content) {
      this.onAddComment.emit(content);
      this.newComment.set('');
      // Reset textarea height
      const textarea = this.addInput()?.nativeElement;
      if (textarea) {
        textarea.style.height = 'auto';
      }
    }
  }

  clearInput(): void {
    this.newComment.set('');
    // Reset textarea height
    const textarea = this.addInput()?.nativeElement;
    if (textarea) {
      textarea.style.height = 'auto';
    }
  }
}
