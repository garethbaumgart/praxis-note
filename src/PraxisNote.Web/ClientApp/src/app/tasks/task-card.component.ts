import { Component, computed, input, output, signal } from '@angular/core';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Task } from './task.model';

@Component({
  selector: 'app-task-card',
  standalone: true,
  imports: [Button, InputText],
  template: `
    <div
      class="bg-surface rounded-md py-2 px-3 border transition-colors group"
      [class.border-todo-border]="task().status === 'Todo'"
      [class.border-inprogress-border]="task().status === 'InProgress'"
      [class.border-done-border]="task().status === 'Done'"
    >
      @if (editing()) {
        <div class="flex gap-2">
          <input
            pInputText
            [value]="editTitle()"
            (input)="editTitle.set($any($event.target).value)"
            class="flex-1"
            (keydown.enter)="saveEdit()"
            (keydown.escape)="cancelEdit()"
          />
          <p-button
            icon="pi pi-check"
            [rounded]="true"
            [text]="true"
            severity="success"
            (onClick)="saveEdit()"
            aria-label="Save"
          />
          <p-button
            icon="pi pi-times"
            [rounded]="true"
            [text]="true"
            severity="secondary"
            (onClick)="cancelEdit()"
            aria-label="Cancel"
          />
        </div>
      } @else {
        <!-- Task content with document icon -->
        <div class="flex items-start gap-2">
          <i
            class="pi text-sm mt-0.5"
            [class.pi-circle]="task().status === 'Todo'"
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
            @if (relativeTime()) {
              <span class="text-xs text-done-foreground-muted">{{ relativeTime() }}</span>
            }
          </div>
          <!-- Hover actions -->
          <div class="flex items-center gap-1 shrink-0 opacity-0 group-hover:opacity-100 group-focus-within:opacity-100 transition-opacity">
            <p-button
              icon="pi pi-pencil"
              [rounded]="true"
              [text]="true"
              size="small"
              severity="secondary"
              (onClick)="startEdit()"
              aria-label="Edit task"
            />
            <p-button
              icon="pi pi-trash"
              [rounded]="true"
              [text]="true"
              size="small"
              severity="danger"
              (onClick)="onDelete.emit()"
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

  readonly relativeTime = computed(() => {
    const task = this.task();
    if (task.status !== 'Done' || !task.completedAt) return null;
    return this.formatRelativeTime(task.completedAt);
  });

  private formatRelativeTime(dateStr: string): string {
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
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
