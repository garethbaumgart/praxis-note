import { Component, input, output, signal, viewChild, ElementRef, ChangeDetectionStrategy, inject, Injector, afterNextRender, computed } from '@angular/core';
import { CdkDragDrop, CdkDrag, CdkDropList, CdkDragPlaceholder } from '@angular/cdk/drag-drop';
import { NgClass } from '@angular/common';
import { TaskCardComponent } from './task-card.component';
import { TaskCardSkeletonComponent } from './task-card-skeleton.component';
import { Task } from './task.model';
import { AutoResizeDirective } from '../shared/directives/auto-resize.directive';
import { StatusColorPipe } from '../shared/pipes/status-color.pipe';

type TaskStatus = 'Todo' | 'InProgress' | 'Done';
type SortMode = 'manual' | 'dueDate' | 'priority';

@Component({
  selector: 'app-column',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgClass, TaskCardComponent, TaskCardSkeletonComponent, CdkDropList, CdkDrag, CdkDragPlaceholder, AutoResizeDirective, StatusColorPipe],
  host: { class: 'block' },
  template: `
    <div
      class="flex flex-col rounded-lg p-3 min-h-48 md:h-full transition-all"
      [ngClass]="{
        'bg-todo': status() === 'Todo',
        'bg-inprogress': status() === 'InProgress',
        'bg-done': status() === 'Done' && !showArchive(),
        'bg-archive': showArchive()
      }"
    >
      <div class="flex items-center justify-between mb-3">
        <div class="flex items-center gap-2">
          <i
            class="pi text-sm"
            [ngClass]="{
              'pi-lightbulb text-todo-foreground': status() === 'Todo',
              'pi-clock text-inprogress-foreground': status() === 'InProgress',
              'pi-check-circle text-done-foreground': status() === 'Done' && !showArchive(),
              'pi-inbox text-archive-foreground': showArchive()
            }"
          ></i>
          <span
            class="text-xs font-medium uppercase tracking-wide"
            [ngClass]="showArchive() ? 'text-archive-foreground' : (status() | statusColor:'text')"
          >{{ label() }}</span>
          <span
            class="text-xs"
            [ngClass]="showArchive() ? 'text-archive-foreground-muted' : (status() | statusColor:'text-muted')"
          >{{ tasks().length }}</span>
          <!-- Sort dropdown -->
          <div class="relative ml-1">
            <button
              type="button"
              class="w-6 h-6 flex items-center justify-center rounded transition-colors"
              [ngClass]="{
                'bg-interactive text-interactive-foreground': sortMode() !== 'manual',
                'text-foreground-muted hover:text-foreground hover:bg-surface-hover': sortMode() === 'manual'
              }"
              (click)="toggleSortMenu(); $event.stopPropagation()"
              [attr.aria-label]="'Sort options'"
              [attr.aria-expanded]="showSortMenu()"
            >
              <i class="pi pi-sort-alt text-xs"></i>
            </button>
            @if (showSortMenu()) {
              <div class="absolute left-0 top-full mt-1 w-36 bg-surface rounded-lg shadow-lg border border-border py-1 z-50">
                <button
                  type="button"
                  class="w-full flex items-center gap-2 px-3 py-1.5 text-xs text-foreground hover:bg-surface-muted transition-colors"
                  (click)="setSortMode('manual'); $event.stopPropagation()"
                >
                  <i class="pi pi-bars text-foreground-muted"></i>
                  <span>Manual order</span>
                  @if (sortMode() === 'manual') {
                    <i class="pi pi-check text-interactive-foreground ml-auto"></i>
                  }
                </button>
                <button
                  type="button"
                  class="w-full flex items-center gap-2 px-3 py-1.5 text-xs text-foreground hover:bg-surface-muted transition-colors"
                  (click)="setSortMode('dueDate'); $event.stopPropagation()"
                >
                  <i class="pi pi-calendar text-foreground-muted"></i>
                  <span>Due date</span>
                  @if (sortMode() === 'dueDate') {
                    <i class="pi pi-check text-interactive-foreground ml-auto"></i>
                  }
                </button>
                <button
                  type="button"
                  class="w-full flex items-center gap-2 px-3 py-1.5 text-xs text-foreground hover:bg-surface-muted transition-colors"
                  (click)="setSortMode('priority'); $event.stopPropagation()"
                >
                  <i class="pi pi-flag text-foreground-muted"></i>
                  <span>Priority</span>
                  @if (sortMode() === 'priority') {
                    <i class="pi pi-check text-interactive-foreground ml-auto"></i>
                  }
                </button>
              </div>
            }
          </div>
        </div>
        <div class="flex items-center gap-1">
          @if (showAddButton() && !isCreating()) {
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
          }
          @if (status() === 'Done' && (archiveCount() > 0 || showArchive() || doneCount() > 0)) {
            <!-- Segmented toggle: Done / Archive -->
            <div
              class="flex items-center gap-0.5 p-0.5 bg-white/50 rounded-full"
              role="group"
              aria-label="View selector"
            >
              <button
                type="button"
                [attr.aria-pressed]="!showArchive()"
                class="flex items-center gap-1 px-2.5 py-1 rounded-full text-[10px] font-medium text-done-foreground transition-colors disabled:opacity-100 disabled:cursor-default"
                [class.bg-done-hover]="!showArchive()"
                [class.hover:bg-done/50]="showArchive()"
                [disabled]="!showArchive()"
                (click)="onArchiveToggle.emit()"
                [attr.aria-label]="'Show done tasks (' + doneCount() + ')'"
              >
                <i class="pi pi-check-circle text-[10px]"></i>
                <span>Done</span>
                @if (doneCount() > 0) {
                  <span class="opacity-60">{{ doneCount() }}</span>
                }
              </button>
              <button
                type="button"
                [attr.aria-pressed]="showArchive()"
                class="flex items-center gap-1 px-2.5 py-1 rounded-full text-[10px] font-medium text-archive-foreground transition-colors disabled:opacity-100 disabled:cursor-default"
                [class.bg-archive-hover]="showArchive()"
                [class.hover:bg-archive/50]="!showArchive()"
                [disabled]="showArchive()"
                (click)="onArchiveToggle.emit()"
                [attr.aria-label]="'Show archived tasks (' + archiveCount() + ')'"
              >
                <i class="pi pi-inbox text-[10px]"></i>
                <span>Archive</span>
                @if (archiveCount() > 0) {
                  <span class="opacity-60">{{ archiveCount() }}</span>
                }
              </button>
            </div>
          }
        </div>
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
        @if (showSkeleton()) {
          <!-- Skeleton loading state -->
          @for (i of [1, 2, 3]; track i) {
            <app-task-card-skeleton />
          }
        } @else {
          @for (task of sortedTasks(); track task.id) {
            <div cdkDrag [cdkDragData]="task" [cdkDragStartDelay]="{ touch: 150, mouse: 0 }" class="cursor-grab active:cursor-grabbing touch-manipulation">
              <app-task-card
                [task]="task"
                [searchQuery]="searchQuery()"
                (onEdit)="onEditTask.emit({ id: task.id, title: $event })"
                (onDelete)="onDeleteTask.emit(task.id)"
                (onAddComment)="onAddComment.emit({ taskId: task.id, content: $event })"
                (onEditComment)="onEditComment.emit({ taskId: task.id, commentId: $event.commentId, content: $event.content })"
                (onDeleteComment)="onDeleteComment.emit({ taskId: task.id, commentId: $event })"
                (onSetDueDate)="onSetDueDate.emit({ taskId: task.id, date: $event })"
                (onClearDueDate)="onClearDueDate.emit({ taskId: task.id })"
                (onTogglePriority)="onTogglePriority.emit({ taskId: task.id })"
              />
              <div
                *cdkDragPlaceholder
                class="border-2 border-dashed rounded-md h-16"
                [ngClass]="{
                  'bg-todo-hover border-todo-border': status() === 'Todo',
                  'bg-inprogress-hover border-inprogress-border': status() === 'InProgress',
                  'bg-done-hover border-done-border': status() === 'Done' && !showArchive(),
                  'bg-archive-hover border-archive-border': showArchive()
                }"
              ></div>
            </div>
          } @empty {
            @if (!isCreating()) {
              <p class="text-sm text-foreground-muted text-center py-8">{{ emptyMessage() }}</p>
            }
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
  readonly archiveCount = input(0);
  readonly doneCount = input(0);
  readonly showArchive = input(false);
  readonly showSortMenu = input(false);
  readonly showSkeleton = input(false);
  readonly searchQuery = input('');

  readonly onDrop = output<CdkDragDrop<Task[]>>();
  readonly onArchiveToggle = output<void>();
  readonly onEditTask = output<{ id: string; title: string }>();
  readonly onDeleteTask = output<string>();
  readonly onTaskCreated = output<string>();
  readonly onAddComment = output<{ taskId: string; content: string }>();
  readonly onEditComment = output<{ taskId: string; commentId: string; content: string }>();
  readonly onDeleteComment = output<{ taskId: string; commentId: string }>();
  readonly onSetDueDate = output<{ taskId: string; date: string }>();
  readonly onClearDueDate = output<{ taskId: string }>();
  readonly onTogglePriority = output<{ taskId: string }>();
  readonly onSortModeChange = output<SortMode>();
  readonly onSortMenuToggle = output<void>();

  readonly isCreating = signal(false);
  readonly inlineTitle = signal('');
  readonly sortMode = signal<SortMode>('manual');

  readonly sortedTasks = computed(() => {
    const tasks = this.tasks();
    const mode = this.sortMode();
    if (mode === 'manual') {
      return tasks;
    }
    if (mode === 'priority') {
      // Sort by priority (true first), then by due date (earliest first, nulls last), then by position
      return [...tasks].sort((a, b) => {
        // First: sort by priority flag
        if (a.isPriority !== b.isPriority) {
          return a.isPriority ? -1 : 1;
        }
        // Second: sort by due date within same priority
        if (a.dueDate !== b.dueDate) {
          if (!a.dueDate) return 1;  // nulls last
          if (!b.dueDate) return -1;
          return a.dueDate.localeCompare(b.dueDate);
        }
        // Third: position as final tiebreaker
        return a.position - b.position;
      });
    }
    // Sort by due date (nulls last), then by priority (flagged first), then by position
    return [...tasks].sort((a, b) => {
      // First: sort by due date
      if (a.dueDate !== b.dueDate) {
        if (!a.dueDate) return 1;  // nulls last
        if (!b.dueDate) return -1;
        return a.dueDate.localeCompare(b.dueDate);
      }
      // Second: sort by priority within same due date
      if (a.isPriority !== b.isPriority) {
        return a.isPriority ? -1 : 1;
      }
      // Third: position as final tiebreaker
      return a.position - b.position;
    });
  });

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

  toggleSortMenu(): void {
    this.onSortMenuToggle.emit();
  }

  setSortMode(mode: SortMode): void {
    this.sortMode.set(mode);
    this.onSortMenuToggle.emit(); // Close menu
    this.onSortModeChange.emit(mode);
  }
}
