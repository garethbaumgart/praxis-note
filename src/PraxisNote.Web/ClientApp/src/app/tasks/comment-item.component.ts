import { Component, input, output, signal, viewChild, ElementRef, inject, Injector, afterNextRender, ChangeDetectionStrategy, DestroyRef, OnInit, OnChanges } from '@angular/core';
import { Button } from 'primeng/button';
import { Comment } from './task.model';

@Component({
  selector: 'app-comment-item',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button],
  template: `
    @if (editing()) {
      <div class="flex items-start gap-2">
        <textarea
          #editInput
          [value]="editContent()"
          (input)="onInput($event)"
          (keydown.enter)="onEnterKey($any($event))"
          (keydown.escape)="cancelEdit()"
          rows="1"
          class="flex-1 text-sm text-foreground bg-transparent border border-border rounded px-2 py-1 outline-none resize-none leading-normal focus:border-primary"
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
      <div class="group flex items-start gap-2">
        <div
          class="flex-1 min-w-0 cursor-pointer"
          (click)="startEdit()"
        >
          <p class="text-sm text-foreground whitespace-pre-wrap">{{ comment().content }}</p>
          <span class="text-xs text-foreground-muted">{{ relativeTime() }}</span>
        </div>
        <div class="flex items-center gap-1 shrink-0 opacity-0 group-hover:opacity-100 transition-opacity">
          <p-button
            icon="pi pi-trash"
            [rounded]="true"
            [text]="true"
            size="small"
            severity="danger"
            (onClick)="onDelete.emit(); $event.stopPropagation()"
            aria-label="Delete comment"
          />
        </div>
      </div>
    }
  `,
})
export class CommentItemComponent implements OnInit, OnChanges {
  private readonly injector = inject(Injector);
  private readonly destroyRef = inject(DestroyRef);

  readonly comment = input.required<Comment>();

  readonly onEdit = output<string>();
  readonly onDelete = output<void>();

  readonly editing = signal(false);
  readonly editContent = signal('');
  readonly editInput = viewChild<ElementRef<HTMLTextAreaElement>>('editInput');

  // Tick signal for auto-updating relative times (updates every minute)
  private readonly tick = signal(Date.now());

  readonly relativeTime = signal('');

  constructor() {
    // Update tick every minute for auto-updating relative times
    const intervalId = setInterval(() => {
      this.tick.set(Date.now());
      this.updateRelativeTime();
    }, 60000);
    this.destroyRef.onDestroy(() => clearInterval(intervalId));
  }

  ngOnInit(): void {
    this.updateRelativeTime();
  }

  ngOnChanges(): void {
    this.updateRelativeTime();
  }

  private updateRelativeTime(): void {
    const comment = this.comment();
    const dateStr = comment.updatedAt !== comment.createdAt ? comment.updatedAt : comment.createdAt;
    const prefix = comment.updatedAt !== comment.createdAt ? 'edited ' : '';
    this.relativeTime.set(prefix + this.formatTime(dateStr));
  }

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

  startEdit(): void {
    this.editContent.set(this.comment().content);
    this.editing.set(true);
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
    this.editContent.set(textarea.value);
    this.autoResize(textarea);
  }

  onEnterKey(event: KeyboardEvent): void {
    if (event.shiftKey) {
      return; // Allow Shift+Enter for multi-line
    }
    event.preventDefault();
    this.saveEdit();
  }

  private autoResize(textarea: HTMLTextAreaElement): void {
    textarea.style.height = 'auto';
    textarea.style.height = textarea.scrollHeight + 'px';
  }

  saveEdit(): void {
    const newContent = this.editContent().trim();
    if (newContent && newContent !== this.comment().content) {
      this.onEdit.emit(newContent);
    }
    this.editing.set(false);
  }

  cancelEdit(): void {
    this.editing.set(false);
  }
}
