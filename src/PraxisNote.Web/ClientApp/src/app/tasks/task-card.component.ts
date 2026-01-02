import { Component, input, output, signal } from '@angular/core';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Task } from './task.model';

@Component({
  selector: 'app-task-card',
  standalone: true,
  imports: [Button, InputText],
  template: `
    <div
      class="bg-white rounded-md py-2 px-3 border transition-colors group"
      [class.border-gray-200]="task().status === 'Todo'"
      [class.border-blue-200]="task().status === 'InProgress'"
      [class.border-green-200]="task().status === 'Done'"
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
          <i class="pi pi-file text-gray-400 text-sm mt-0.5"></i>
          <p
            class="text-sm text-gray-800 flex-1"
            [class.line-through]="task().status === 'Done'"
            [class.text-gray-400]="task().status === 'Done'"
          >
            {{ task().title }}
          </p>
          <!-- Hover actions -->
          <div class="flex items-center gap-1 shrink-0 opacity-0 group-hover:opacity-100 transition-opacity">
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

  readonly onStatusChange = output<'Todo' | 'InProgress' | 'Done'>();
  readonly onEdit = output<string>();
  readonly onDelete = output<void>();

  readonly editing = signal(false);
  readonly editTitle = signal('');

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
