import { Component, computed, ElementRef, input, output, signal, viewChild } from '@angular/core';
import { Button } from 'primeng/button';
import { Task } from './task.model';

@Component({
  selector: 'app-task-card',
  standalone: true,
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
            (keydown.enter)="$event.preventDefault(); saveEdit()"
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
            <p
              class="text-sm text-foreground"
              [class.line-through]="task().status === 'Done'"
              [class.text-foreground-muted]="task().status === 'Done'"
            >
              {{ task().title }}
            </p>
            @if (relativeTime(); as time) {
              <span
                class="text-xs"
                [class.text-inprogress-foreground-muted]="task().status === 'InProgress'"
                [class.text-done-foreground-muted]="task().status === 'Done'"
              >{{ time }}</span>
            }
          </div>
          <!-- Actions: always visible on mobile, hover to reveal on desktop -->
          <div class="flex items-center gap-1 shrink-0 md:opacity-0 md:group-hover:opacity-100 group-focus-within:opacity-100 transition-opacity">
            <p-button
              icon="pi pi-pencil"
              [rounded]="true"
              [text]="true"
              size="small"
              severity="secondary"
              (onClick)="startEdit(); $event.stopPropagation()"
              aria-label="Edit task"
            />
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
      }
    </div>
  `,
})
export class TaskCardComponent {
  readonly task = input.required<Task>();

  readonly onEdit = output<string>();
  readonly onDelete = output<void>();

  readonly editing = signal(false);
  readonly editTitle = signal('');
  readonly editInput = viewChild<ElementRef<HTMLTextAreaElement>>('editInput');

  readonly relativeTime = computed(() => {
    const task = this.task();
    if (task.status === 'InProgress' && task.startedAt) {
      return this.formatElapsedTime(task.startedAt);
    }
    if (task.status === 'Done' && task.completedAt) {
      return this.formatRelativeTime(task.completedAt);
    }
    return null;
  });

  private formatElapsedTime(dateStr: string): string {
    const date = new Date(dateStr);

    // Handle invalid dates
    if (isNaN(date.getTime())) {
      return '';
    }

    const now = new Date();
    const diffMs = now.getTime() - date.getTime();

    // Handle future dates (e.g., clock skew or timezone issues)
    if (diffMs < 0) {
      return 'just started';
    }

    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'just started';
    if (diffMins < 60) return `${diffMins}m`;
    if (diffHours < 24) return `${diffHours}h`;
    if (diffDays < 7) return `${diffDays}d`;
    return date.toLocaleDateString();
  }

  private formatRelativeTime(dateStr: string): string {
    const date = new Date(dateStr);

    // Handle invalid dates
    if (isNaN(date.getTime())) {
      return '';
    }

    const now = new Date();
    const diffMs = now.getTime() - date.getTime();

    // Handle future dates (e.g., clock skew or timezone issues)
    if (diffMs < 0) {
      return 'just now';
    }

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
    this.editTitle.set(this.task().title);
    this.editing.set(true);
    // Focus and auto-resize after view updates
    setTimeout(() => {
      const textarea = this.editInput()?.nativeElement;
      if (textarea) {
        this.autoResize(textarea);
        textarea.focus();
        textarea.select();
      }
    }, 0);
  }

  onInput(event: Event): void {
    const textarea = event.target as HTMLTextAreaElement;
    this.editTitle.set(textarea.value);
    this.autoResize(textarea);
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
}
