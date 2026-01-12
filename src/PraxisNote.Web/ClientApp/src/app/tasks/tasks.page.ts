import { Component, HostListener, inject, OnInit, viewChild, ChangeDetectionStrategy, computed, signal } from '@angular/core';
import { CdkDragDrop, CdkDropList } from '@angular/cdk/drag-drop';
import { TaskService } from './task.service';
import { ColumnComponent } from './column.component';
import { Task } from './task.model';

type TaskStatus = 'Todo' | 'InProgress' | 'Done';

@Component({
  selector: 'app-tasks-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ColumnComponent],
  template: `
    <div class="max-w-7xl mx-auto px-4 md:px-6 py-6 md:py-8">
      <!-- Header -->
      <div class="flex items-center justify-between mb-6">
        <h1 class="text-xl font-semibold text-foreground">Tasks</h1>
      </div>

      <!-- Loading state -->
      @if (taskService.loading()) {
        <div class="flex items-center justify-center py-20">
          <i class="pi pi-spin pi-spinner text-3xl text-primary"></i>
        </div>
      } @else {
        <!-- Kanban Board -->
        <div class="grid grid-cols-1 md:grid-cols-3 gap-4 md:gap-6">
          <app-column
            #todoColumn
            status="Todo"
            label="Todo"
            [tasks]="taskService.todoTasks()"
            [connectedTo]="todoConnectedTo()"
            [showAddButton]="true"
            [showKbdHint]="true"
            emptyMessage="Press N to add your first task"
            (onDrop)="drop($event, 'Todo')"
            (onEditTask)="updateTask($event.id, $event.title)"
            (onDeleteTask)="deleteTask($event)"
            (onTaskCreated)="createTask($event, 'Todo')"
            (onAddComment)="addComment($event.taskId, $event.content)"
            (onEditComment)="editComment($event.taskId, $event.commentId, $event.content)"
            (onDeleteComment)="deleteComment($event.taskId, $event.commentId)"
            (onSetDueDate)="setDueDate($event.taskId, $event.date)"
            (onClearDueDate)="clearDueDate($event.taskId)"
          />

          <app-column
            #inProgressColumn
            status="InProgress"
            label="In Progress"
            [tasks]="taskService.inProgressTasks()"
            [connectedTo]="inProgressConnectedTo()"
            [showAddButton]="true"
            emptyMessage="Nothing in progress"
            (onDrop)="drop($event, 'InProgress')"
            (onEditTask)="updateTask($event.id, $event.title)"
            (onDeleteTask)="deleteTask($event)"
            (onTaskCreated)="createTask($event, 'InProgress')"
            (onAddComment)="addComment($event.taskId, $event.content)"
            (onEditComment)="editComment($event.taskId, $event.commentId, $event.content)"
            (onDeleteComment)="deleteComment($event.taskId, $event.commentId)"
            (onSetDueDate)="setDueDate($event.taskId, $event.date)"
            (onClearDueDate)="clearDueDate($event.taskId)"
          />

          <app-column
            #doneColumn
            status="Done"
            [label]="showArchive() ? 'Archive' : 'Done'"
            [tasks]="doneColumnTasks()"
            [connectedTo]="doneConnectedTo()"
            [showAddButton]="false"
            [emptyMessage]="showArchive() ? 'No archived tasks' : 'Complete some tasks!'"
            [archiveCount]="taskService.archivedCount()"
            [doneCount]="taskService.doneTasks().length"
            [showArchive]="showArchive()"
            (onArchiveToggle)="toggleArchive()"
            (onDrop)="drop($event, 'Done')"
            (onEditTask)="updateTask($event.id, $event.title)"
            (onDeleteTask)="deleteTask($event)"
            (onTaskCreated)="createTask($event, 'Done')"
            (onAddComment)="addComment($event.taskId, $event.content)"
            (onEditComment)="editComment($event.taskId, $event.commentId, $event.content)"
            (onDeleteComment)="deleteComment($event.taskId, $event.commentId)"
            (onSetDueDate)="setDueDate($event.taskId, $event.date)"
            (onClearDueDate)="clearDueDate($event.taskId)"
          />
        </div>
      }
    </div>
  `,
})
export class TasksPage implements OnInit {
  readonly taskService = inject(TaskService);

  readonly todoColumn = viewChild<ColumnComponent>('todoColumn');
  readonly inProgressColumn = viewChild<ColumnComponent>('inProgressColumn');
  readonly doneColumn = viewChild<ColumnComponent>('doneColumn');

  readonly showArchive = signal(false);

  readonly doneColumnTasks = computed(() =>
    this.showArchive()
      ? this.taskService.archivedTasks()
      : this.taskService.doneTasks()
  );

  readonly todoConnectedTo = computed(() => {
    const inProgress = this.inProgressColumn()?.dropList();
    const done = this.doneColumn()?.dropList();
    return [inProgress, done].filter((list): list is CdkDropList => !!list);
  });

  readonly inProgressConnectedTo = computed(() => {
    const todo = this.todoColumn()?.dropList();
    const done = this.doneColumn()?.dropList();
    return [todo, done].filter((list): list is CdkDropList => !!list);
  });

  readonly doneConnectedTo = computed(() => {
    const todo = this.todoColumn()?.dropList();
    const inProgress = this.inProgressColumn()?.dropList();
    return [todo, inProgress].filter((list): list is CdkDropList => !!list);
  });

  ngOnInit(): void {
    this.taskService.loadTasks();
    this.taskService.loadArchivedCount();
  }

  toggleArchive(): void {
    const newValue = !this.showArchive();
    this.showArchive.set(newValue);

    if (newValue) {
      this.taskService.loadArchivedTasks();
    } else {
      this.taskService.clearArchivedTasks();
    }
  }

  @HostListener('document:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    const target = event.target as HTMLElement;
    if (target.tagName === 'INPUT' || target.tagName === 'TEXTAREA' || target.isContentEditable) {
      return;
    }

    // N to start inline task creation in Todo column
    if (event.key.toLowerCase() === 'n' && !event.metaKey && !event.ctrlKey) {
      const todoCol = this.todoColumn();
      if (todoCol && !todoCol.isCreating()) {
        event.preventDefault();
        todoCol.startCreate();
      }
    }
  }

  createTask(title: string, status: TaskStatus): void {
    this.taskService.createTaskInColumn(title, status);
  }

  updateTask(id: string, title: string): void {
    this.taskService.updateTask(id, title);
  }

  deleteTask(id: string): void {
    this.taskService.deleteTask(id);
  }

  addComment(taskId: string, content: string): void {
    this.taskService.addComment(taskId, content);
  }

  editComment(taskId: string, commentId: string, content: string): void {
    this.taskService.updateComment(taskId, commentId, content);
  }

  deleteComment(taskId: string, commentId: string): void {
    this.taskService.deleteComment(taskId, commentId);
  }

  setDueDate(taskId: string, date: string): void {
    this.taskService.setDueDate(taskId, date);
  }

  clearDueDate(taskId: string): void {
    this.taskService.clearDueDate(taskId);
  }

  drop(event: CdkDragDrop<Task[]>, targetStatus: TaskStatus): void {
    const task = event.item.data as Task;

    if (event.previousContainer === event.container) {
      if (event.previousIndex !== event.currentIndex) {
        const tasks = event.container.data;
        const taskIds = tasks.map(t => t.id);
        taskIds.splice(event.previousIndex, 1);
        taskIds.splice(event.currentIndex, 0, task.id);
        this.taskService.reorderTasks(targetStatus, taskIds);
      }
    } else {
      this.taskService.changeStatus(task.id, targetStatus, event.currentIndex);
    }
  }
}
