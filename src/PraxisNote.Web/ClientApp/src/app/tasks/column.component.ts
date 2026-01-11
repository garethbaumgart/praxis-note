import { Component, input, output, signal, viewChild, ElementRef, ChangeDetectionStrategy, inject, Injector, afterNextRender } from '@angular/core';
import { CdkDragDrop, CdkDrag, CdkDropList, CdkDragPlaceholder } from '@angular/cdk/drag-drop';
import { NgClass } from '@angular/common';
import { TaskCardComponent } from './task-card.component';
import { Task } from './task.model';
import { AutoResizeDirective } from '../shared/directives/auto-resize.directive';
import { StatusColorPipe } from '../shared/pipes/status-color.pipe';

type TaskStatus = 'Todo' | 'InProgress' | 'Done';

@Component({
  selector: 'app-column',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgClass, TaskCardComponent, CdkDropList, CdkDrag, CdkDragPlaceholder, AutoResizeDirective, StatusColorPipe],
  template: `
    <div
      class="flex flex-col rounded-lg p-3 min-h-48 transition-all"
      [ngClass]="{
        'bg-todo': status() === 'Todo',
        'bg-inprogress': status() === 'InProgress',
        'bg-done': status() === 'Done'
      }"
    >
      <div class="flex items-center justify-between mb-3">
        <div class="flex items-center gap-2">
          <i
            class="pi text-sm"
            [ngClass]="{
              'pi-lightbulb text-todo-foreground': status() === 'Todo',
              'pi-clock text-inprogress-foreground': status() === 'InProgress',
              'pi-check-circle text-done-foreground': status() === 'Done'
            }"
          ></i>
          <span
            class="text-xs font-medium uppercase tracking-wide"
            [ngClass]="status() | statusColor:'text'"
          >{{ label() }}</span>
          <span
            class="text-xs"
            [ngClass]="status() | statusColor:'text-muted'"
          >{{ tasks().length }}</span>
        </div>
        @if (showAddButton() && !isCreating()) {
          <div class="flex items-center gap-1">
            <button
              class="flex items-center justify-center w-6 h-6 rounded transition-colors"
              [ngClass]="{
                'text-todo-foreground-muted hover:text-todo-foreground hover:bg-todo-hover': status() === 'Todo',
                'text-inprogress-foreground-muted hover:text-inprogress-foreground hover:bg-inprogress-hover': status() === 'InProgress',
                'text-done-foreground-muted hover:text-done-foreground hover:bg-done-hover': status() === 'Done'
              }"
              (click)="startCreate()"
              [attr.aria-label]="'Add task to ' + label()"
            >
              <i class="pi pi-plus text-xs"></i>
            </button>
            @if (showKbdHint()) {
              <kbd
                class="hidden md:inline px-1.5 py-0.5 text-xs rounded font-sans"
                [ngClass]="{
                  'text-todo-foreground-muted bg-todo-hover': status() === 'Todo',
                  'text-inprogress-foreground-muted bg-inprogress-hover': status() === 'InProgress',
                  'text-done-foreground-muted bg-done-hover': status() === 'Done'
                }"
              >N</kbd>
            }
          </div>
        }
      </div>
      <div
        cdkDropList
        #dropList="cdkDropList"
        [cdkDropListData]="tasks()"
        [cdkDropListConnectedTo]="connectedTo()"
        (cdkDropListDropped)="onDrop.emit($event)"
        class="flex-1 space-y-2 min-h-12"
      >
        @if (isCreating()) {
          <div
            class="bg-surface border rounded-md p-3 shadow-sm"
            [ngClass]="status() | statusColor:'border'"
          >
            <textarea
              #inlineInput
              appAutoResize
              [value]="inlineTitle()"
              (input)="inlineTitle.set(asTextArea($event).value)"
              (keydown.enter)="onEnterKey(asKeyboardEvent($event))"
              (keydown.escape)="cancelCreate()"
              (blur)="onBlur($event)"
              placeholder="Task name..."
              rows="1"
              class="w-full text-sm font-medium text-foreground placeholder-foreground-muted border-0 focus:outline-none focus:ring-0 p-0 bg-transparent resize-none leading-normal"
            ></textarea>
          </div>
        }
        @for (task of tasks(); track task.id) {
          <div cdkDrag [cdkDragData]="task" class="cursor-grab active:cursor-grabbing touch-manipulation">
            <app-task-card
              [task]="task"
              (onEdit)="onEditTask.emit({ id: task.id, title: $event })"
              (onDelete)="onDeleteTask.emit(task.id)"
              (onAddComment)="onAddComment.emit({ taskId: task.id, content: $event })"
              (onEditComment)="onEditComment.emit({ taskId: task.id, commentId: $event.commentId, content: $event.content })"
              (onDeleteComment)="onDeleteComment.emit({ taskId: task.id, commentId: $event })"
              (onSetDueDate)="onSetDueDate.emit({ taskId: task.id, date: $event })"
              (onClearDueDate)="onClearDueDate.emit({ taskId: task.id })"
            />
            <div
              *cdkDragPlaceholder
              class="border-2 border-dashed rounded-md h-16"
              [ngClass]="{
                'bg-todo-hover border-todo-border': status() === 'Todo',
                'bg-inprogress-hover border-inprogress-border': status() === 'InProgress',
                'bg-done-hover border-done-border': status() === 'Done'
              }"
            ></div>
          </div>
        } @empty {
          @if (!isCreating()) {
            <p class="text-sm text-foreground-muted text-center py-8">{{ emptyMessage() }}</p>
          }
        }
      </div>
    </div>
  `,
})
export class ColumnComponent {
  private readonly injector = inject(Injector);

  readonly status = input.required<TaskStatus>();
  readonly label = input.required<string>();
  readonly tasks = input.required<Task[]>();
  readonly connectedTo = input.required<CdkDropList[]>();
  readonly showAddButton = input(true);
  readonly showKbdHint = input(false);
  readonly emptyMessage = input('No tasks');

  readonly onDrop = output<CdkDragDrop<Task[]>>();
  readonly onEditTask = output<{ id: string; title: string }>();
  readonly onDeleteTask = output<string>();
  readonly onTaskCreated = output<string>();
  readonly onAddComment = output<{ taskId: string; content: string }>();
  readonly onEditComment = output<{ taskId: string; commentId: string; content: string }>();
  readonly onDeleteComment = output<{ taskId: string; commentId: string }>();
  readonly onSetDueDate = output<{ taskId: string; date: string }>();
  readonly onClearDueDate = output<{ taskId: string }>();

  readonly isCreating = signal(false);
  readonly inlineTitle = signal('');

  readonly inlineInput = viewChild<ElementRef<HTMLTextAreaElement>>('inlineInput');
  readonly dropList = viewChild.required<CdkDropList>('dropList');

  /** Type-safe helper for accessing textarea value from events */
  asTextArea(event: Event): HTMLTextAreaElement {
    return event.target as HTMLTextAreaElement;
  }

  /** Type-safe helper for keyboard events */
  asKeyboardEvent(event: Event): KeyboardEvent {
    return event as KeyboardEvent;
  }

  startCreate(): void {
    this.isCreating.set(true);
    this.inlineTitle.set('');
    afterNextRender(() => {
      this.inlineInput()?.nativeElement.focus();
    }, { injector: this.injector });
  }

  cancelCreate(): void {
    this.isCreating.set(false);
    this.inlineTitle.set('');
  }

  onEnterKey(event: KeyboardEvent): void {
    if (event.shiftKey) {
      return; // Allow Shift+Enter for new line
    }
    event.preventDefault();
    this.submitCreate();
  }

  submitCreate(): void {
    const title = this.inlineTitle().trim();
    if (title) {
      this.onTaskCreated.emit(title);
    }
    this.cancelCreate();
  }

  onBlur(event: FocusEvent): void {
    const relatedTarget = event.relatedTarget as HTMLElement | null;
    // If focus is moving to an interactive element (button, input), don't auto-submit/cancel
    // This prevents issues when clicking buttons or other inputs
    if (relatedTarget?.matches('button, input, textarea, [tabindex]')) {
      return;
    }

    if (this.inlineTitle().trim()) {
      this.submitCreate();
    } else {
      this.cancelCreate();
    }
  }
}
