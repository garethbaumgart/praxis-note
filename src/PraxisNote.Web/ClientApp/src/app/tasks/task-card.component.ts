import { Component, computed, ElementRef, input, output, signal, viewChild, inject, Injector, afterNextRender, ChangeDetectionStrategy, DestroyRef } from '@angular/core';
import { Button } from 'primeng/button';
import { Task } from './task.model';

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
  private readonly injector = inject(Injector);
  private readonly destroyRef = inject(DestroyRef);

  readonly task = input.required<Task>();

  readonly onEdit = output<string>();
  readonly onDelete = output<void>();

  readonly editing = signal(false);
  readonly editTitle = signal('');
  readonly editInput = viewChild<ElementRef<HTMLTextAreaElement>>('editInput');

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
}
