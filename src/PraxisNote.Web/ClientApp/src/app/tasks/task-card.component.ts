import { Component, computed, input, output, signal } from '@angular/core';
import { NgClass } from '@angular/common';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Task } from './task.model';

@Component({
  selector: 'app-task-card',
  standalone: true,
  imports: [NgClass, Button, InputText],
  template: `
    <div class="bg-white rounded-lg border border-gray-200 p-3 shadow-sm hover:shadow-md transition-shadow group">
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
        <!-- Status badge -->
        <div class="mb-2">
          <span
            class="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium"
            [ngClass]="statusBadgeClass()"
          >
            {{ statusLabel() }}
          </span>
        </div>

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
        </div>

        <!-- Hover actions -->
        <div class="flex justify-end gap-1 mt-2 opacity-0 group-hover:opacity-100 transition-opacity">
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

  readonly statusLabel = computed(() => {
    switch (this.task().status) {
      case 'Todo': return 'Todo';
      case 'InProgress': return 'In Progress';
      case 'Done': return 'Done';
    }
  });

  readonly statusBadgeClass = computed(() => {
    switch (this.task().status) {
      case 'Todo': return 'bg-gray-100 text-gray-600';
      case 'InProgress': return 'bg-blue-50 text-blue-600';
      case 'Done': return 'bg-green-50 text-green-600';
    }
  });

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
