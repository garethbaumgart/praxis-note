import { Component, input, output, signal } from '@angular/core';
import { Card } from 'primeng/card';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Task } from './task.model';

@Component({
  selector: 'app-task-card',
  standalone: true,
  imports: [Card, Button, InputText],
  template: `
    <p-card class="block group">
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
        <div class="flex items-start justify-between gap-3">
          <p class="text-sm text-gray-900 flex-1" [class.line-through]="task().status === 'Done'" [class.text-gray-400]="task().status === 'Done'">
            {{ task().title }}
          </p>
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
    </p-card>
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
