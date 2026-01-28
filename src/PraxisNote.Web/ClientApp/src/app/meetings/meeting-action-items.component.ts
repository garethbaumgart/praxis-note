import { Component, ChangeDetectionStrategy, input, output, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CheckboxModule } from 'primeng/checkbox';
import { ButtonModule } from 'primeng/button';
import { ActionItem, ActionItemStatus } from './meeting.model';

@Component({
  selector: 'app-meeting-action-items',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, CheckboxModule, ButtonModule],
  template: `
    <div>
      <h4 class="text-xs font-semibold text-foreground-muted uppercase tracking-wide mb-2">Action Items</h4>
      @if (actionItems().length === 0) {
        <p class="text-sm text-foreground-muted italic">No action items were identified in this meeting.</p>
      } @else {
        <ul class="space-y-2">
          @for (item of actionItems(); track item.id) {
            @let status = getStatus(item.id);
            <li class="flex items-start gap-3">
              <p-checkbox
                [binary]="true"
                [ngModel]="item.isCompleted"
                (onChange)="onToggle.emit(item.id)"
                [inputId]="'action-' + item.id"
              />
              <div class="flex-1 min-w-0">
                <div class="flex items-center gap-2 flex-wrap">
                  <label
                    [for]="'action-' + item.id"
                    class="text-sm cursor-pointer"
                    [class.line-through]="item.isCompleted"
                    [class.text-foreground-muted]="item.isCompleted"
                    [class.text-foreground]="!item.isCompleted"
                  >
                    {{ item.description }}
                  </label>
                  @if (item.assignee) {
                    <span class="inline-flex items-center px-2 py-0.5 rounded-full text-xs bg-accent text-accent-foreground">
                      {{ item.assignee }}
                    </span>
                  }
                  @if (status?.isLinked) {
                    <button
                      type="button"
                      class="task-status-badge"
                      [class.status-todo]="status?.taskStatus === 'Todo'"
                      [class.status-inprogress]="status?.taskStatus === 'InProgress'"
                      [class.status-done]="status?.taskStatus === 'Done'"
                      (click)="onNavigateToTask.emit(status?.taskId ?? '')"
                      [attr.aria-label]="'View task - ' + status?.taskStatus"
                    >
                      <i class="pi pi-check-square text-xs"></i>
                      {{ status?.taskStatus }}
                    </button>
                  } @else if (!item.isCompleted) {
                    <button
                      type="button"
                      class="promote-button"
                      [class.promoting]="promotingIds().has(item.id)"
                      [disabled]="promotingIds().has(item.id)"
                      (click)="handlePromote(item.id)"
                      aria-label="Promote to task"
                    >
                      @if (promotingIds().has(item.id)) {
                        <i class="pi pi-spin pi-spinner text-xs"></i>
                      } @else {
                        <i class="pi pi-arrow-right text-xs"></i>
                      }
                      Task
                    </button>
                  }
                </div>
              </div>
            </li>
          }
        </ul>
      }
    </div>
  `,
  styles: [`
    .promote-button {
      display: inline-flex;
      align-items: center;
      gap: 4px;
      padding: 2px 8px;
      font-size: 11px;
      font-weight: 500;
      border-radius: 9999px;
      background: var(--color-surface-subtle);
      color: var(--color-foreground-muted);
      border: 1px solid var(--color-border);
      cursor: pointer;
      transition: all 0.15s ease;
    }

    .promote-button:hover:not(:disabled) {
      background: var(--color-primary-bg);
      color: var(--color-primary-text);
      border-color: var(--color-primary-border);
    }

    .promote-button:disabled {
      opacity: 0.6;
      cursor: not-allowed;
    }

    .promote-button.promoting {
      background: var(--color-inprogress-bg);
      color: var(--color-inprogress-text);
      border-color: var(--color-inprogress-border);
    }

    .task-status-badge {
      display: inline-flex;
      align-items: center;
      gap: 4px;
      padding: 2px 8px;
      font-size: 11px;
      font-weight: 500;
      border-radius: 9999px;
      border: none;
      cursor: pointer;
      transition: all 0.15s ease;
    }

    .task-status-badge:hover {
      filter: brightness(0.95);
    }

    .status-todo {
      background: var(--color-todo-bg);
      color: var(--color-todo-text);
    }

    .status-inprogress {
      background: var(--color-inprogress-bg);
      color: var(--color-inprogress-text);
    }

    .status-done {
      background: var(--color-done-bg);
      color: var(--color-done-text);
    }
  `],
})
export class MeetingActionItemsComponent {
  readonly actionItems = input.required<ActionItem[]>();
  readonly actionItemStatuses = input<ActionItemStatus[]>([]);
  readonly promotingIds = input<Set<string>>(new Set());

  readonly onToggle = output<string>();
  readonly onPromote = output<string>();
  readonly onNavigateToTask = output<string>();

  private readonly statusMap = computed(() => {
    const map = new Map<string, ActionItemStatus>();
    for (const status of this.actionItemStatuses()) {
      map.set(status.actionItemId, status);
    }
    return map;
  });

  getStatus(actionItemId: string): ActionItemStatus | undefined {
    return this.statusMap().get(actionItemId);
  }

  handlePromote(actionItemId: string): void {
    this.onPromote.emit(actionItemId);
  }
}
