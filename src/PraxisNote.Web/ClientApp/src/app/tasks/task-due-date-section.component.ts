import { Component, computed, input, output, signal, ChangeDetectionStrategy } from '@angular/core';
import { TaskStatus } from './task.model';
import { DatePickerPopoverComponent } from './date-picker-popover.component';

@Component({
  selector: 'app-task-due-date-section',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePickerPopoverComponent],
  template: `
    <!-- Due Date tab button -->
    <button
      type="button"
      class="flex items-center justify-center rounded-full transition-all text-xs shrink-0 h-7"
      [class.px-3]="isDueDatePill()"
      [class.gap-1.5]="isDueDatePill()"
      [class.w-7]="isDueDateCircle()"
      [class.bg-due-done]="isDueDateExpandedDone() || isDueDateCollapsedDone()"
      [class.text-due-done-foreground]="isDueDateExpandedDone() || isDueDateCollapsedDone()"
      [class.line-through]="isDueDateExpandedDone() || isDueDateCollapsedDone()"
      [class.bg-danger]="isDueDateExpandedOverdue()"
      [class.text-white]="isDueDateExpandedOverdue()"
      [class.font-medium]="isDueDateExpandedOverdue() || isDueDateExpandedNormal() || isDueDateCollapsedOverdue()"
      [class.bg-duedate-expanded]="isDueDateExpandedNormal()"
      [class.text-duedate-expanded-foreground]="isDueDateExpandedNormal()"
      [class.bg-overdue]="isDueDateCollapsedOverdue()"
      [class.text-overdue-foreground]="isDueDateCollapsedOverdue()"
      [class.bg-due-today]="isDueDateCollapsedToday()"
      [class.text-due-today-foreground]="isDueDateCollapsedToday()"
      [class.bg-due-soon]="isDueDateCollapsedTomorrow()"
      [class.text-due-soon-foreground]="isDueDateCollapsedTomorrow()"
      [class.bg-duedate-default]="isDueDateCollapsedDefault()"
      [class.text-duedate-default-foreground]="isDueDateCollapsedDefault()"
      [class.bg-foreground-muted/10]="isDueDateCircle()"
      [class.text-foreground-muted/40]="isDueDateCircle()"
      [class.hover:bg-foreground-muted/20]="isDueDateCircle()"
      (click)="onToggle.emit(); $event.stopPropagation()"
      [attr.aria-label]="dueDate() ? (expanded() ? 'Collapse due date' : 'Expand due date') : 'Set due date'"
      [attr.aria-expanded]="expanded()"
    >
      <i class="pi" [class.pi-exclamation-circle]="isOverdue()" [class.pi-calendar]="!isOverdue()"></i>
      @if (expanded() || dueDate()) {
        <span>{{ dueDateDisplayText() ?? 'Due Date' }}</span>
      }
    </button>

    <!-- Due Date expanded content -->
    @if (expanded()) {
      <div class="mt-2 p-2 bg-duedate-section rounded-lg border border-duedate-section-border relative">
        <div class="flex items-center gap-1 flex-wrap">
          <button
            type="button"
            (click)="selectQuickDate('today'); $event.stopPropagation()"
            class="px-2 py-1 text-xs rounded transition-colors"
            [class]="isDateSelected('today') ? 'bg-duedate-btn-selected text-duedate-btn-selected-foreground font-medium' : 'bg-duedate-btn text-duedate-btn-foreground hover:bg-duedate-btn-hover'"
          >Today</button>
          <button
            type="button"
            (click)="selectQuickDate('tomorrow'); $event.stopPropagation()"
            class="px-2 py-1 text-xs rounded transition-colors"
            [class]="isDateSelected('tomorrow') ? 'bg-duedate-btn-selected text-duedate-btn-selected-foreground font-medium' : 'bg-duedate-btn text-duedate-btn-foreground hover:bg-duedate-btn-hover'"
          >+1</button>
          <button
            type="button"
            (click)="selectQuickDate('friday'); $event.stopPropagation()"
            class="px-2 py-1 text-xs rounded transition-colors"
            [class]="isDateSelected('friday') ? 'bg-duedate-btn-selected text-duedate-btn-selected-foreground font-medium' : 'bg-duedate-btn text-duedate-btn-foreground hover:bg-duedate-btn-hover'"
          >Fri</button>
          <button
            type="button"
            (click)="selectQuickDate('nextWeek'); $event.stopPropagation()"
            class="px-2 py-1 text-xs rounded transition-colors"
            [class]="isDateSelected('nextWeek') ? 'bg-duedate-btn-selected text-duedate-btn-selected-foreground font-medium' : 'bg-duedate-btn text-duedate-btn-foreground hover:bg-duedate-btn-hover'"
          >+7</button>
          <button
            type="button"
            (click)="selectQuickDate('plus35'); $event.stopPropagation()"
            class="px-2 py-1 text-xs rounded transition-colors"
            [class]="isDateSelected('plus35') ? 'bg-duedate-btn-selected text-duedate-btn-selected-foreground font-medium' : 'bg-duedate-btn text-duedate-btn-foreground hover:bg-duedate-btn-hover'"
          >+35</button>
          <button
            type="button"
            (click)="showDatePicker.set(true); $event.stopPropagation()"
            class="px-2 py-1 text-xs rounded bg-duedate-btn text-duedate-btn-foreground hover:bg-duedate-btn-hover transition-colors"
            aria-label="Open calendar"
          ><i class="pi pi-calendar-plus text-[10px]"></i></button>
          @if (dueDate()) {
            <button
              type="button"
              (click)="clearDueDate(); $event.stopPropagation()"
              class="ml-auto px-2 py-1 text-xs rounded text-danger hover:bg-danger-bg transition-colors"
              aria-label="Clear due date"
            ><i class="pi pi-times text-[10px]"></i> Clear</button>
          }
        </div>
        @if (showDatePicker()) {
          <app-date-picker-popover
            [currentDate]="dueDate()"
            [showQuickOptions]="false"
            (onSelect)="onDateSelect($event)"
            (onClear)="clearDueDate()"
            (onClose)="showDatePicker.set(false)"
          />
        }
      </div>
    }
  `,
})
export class TaskDueDateSectionComponent {
  // Inputs
  readonly dueDate = input<string | null>(null);
  readonly taskStatus = input.required<TaskStatus>();
  readonly expanded = input.required<boolean>();

  // Outputs
  readonly onToggle = output<void>();
  readonly onSetDate = output<string>();
  readonly onClearDate = output<void>();

  // Internal state
  readonly showDatePicker = signal(false);

  // Due date display calculations
  private readonly daysDiff = computed(() => {
    const dueDate = this.dueDate();
    if (!dueDate) return null;

    const date = new Date(dueDate + 'T00:00:00');
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return Math.floor((date.getTime() - today.getTime()) / 86400000);
  });

  // Due date tab state computeds for template bindings
  readonly isDueDatePill = computed(() => this.expanded() || !!this.dueDate());
  readonly isDueDateExpandedDone = computed(() => this.expanded() && this.taskStatus() === 'Done');
  readonly isDueDateExpandedOverdue = computed(() => {
    const diff = this.daysDiff();
    return this.expanded() && this.taskStatus() !== 'Done' && diff !== null && diff < 0;
  });
  readonly isDueDateExpandedNormal = computed(() => {
    const diff = this.daysDiff();
    return this.expanded() && this.taskStatus() !== 'Done' && (diff === null || diff >= 0);
  });
  readonly isDueDateCollapsedDone = computed(() => !this.expanded() && !!this.dueDate() && this.taskStatus() === 'Done');
  // Common condition for collapsed date states (not done)
  private readonly isCollapsedWithDateNotDone = computed(
    () => !this.expanded() && !!this.dueDate() && this.taskStatus() !== 'Done'
  );
  readonly isDueDateCollapsedOverdue = computed(
    () => this.isCollapsedWithDateNotDone() && this.daysDiff() !== null && this.daysDiff()! < 0
  );
  readonly isDueDateCollapsedToday = computed(
    () => this.isCollapsedWithDateNotDone() && this.daysDiff() === 0
  );
  readonly isDueDateCollapsedTomorrow = computed(
    () => this.isCollapsedWithDateNotDone() && this.daysDiff() === 1
  );
  readonly isDueDateCollapsedDefault = computed(
    () => this.isCollapsedWithDateNotDone() && this.daysDiff() !== null && this.daysDiff()! > 1
  );
  readonly isDueDateCircle = computed(() => !this.expanded() && !this.dueDate());

  readonly dueDateDisplayText = computed(() => {
    const diff = this.daysDiff();
    if (diff === null) return null;

    const dueDate = this.dueDate()!;
    const date = new Date(dueDate + 'T00:00:00');

    if (diff < -1) return `${-diff}d ago`;
    if (diff === -1) return 'Yesterday';
    if (diff === 0) return 'Today';
    if (diff === 1) return 'Tomorrow';
    if (diff <= 6) return date.toLocaleDateString('en-US', { weekday: 'short' });
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
  });

  /** Returns true if the task is overdue (past due date and not done) */
  isOverdue(): boolean {
    const diff = this.daysDiff();
    return diff !== null && diff < 0 && this.taskStatus() !== 'Done';
  }

  // Due date quick selection methods
  selectQuickDate(option: 'today' | 'tomorrow' | 'friday' | 'nextWeek' | 'plus35'): void {
    const date = this.getQuickOptionDate(option);
    this.onSetDate.emit(this.formatDateString(date));
  }

  private getQuickOptionDate(option: 'today' | 'tomorrow' | 'friday' | 'nextWeek' | 'plus35'): Date {
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    switch (option) {
      case 'today':
        return today;
      case 'tomorrow':
        return new Date(today.getTime() + 86400000);
      case 'friday':
        return this.getNextFriday(today);
      case 'nextWeek':
        return new Date(today.getTime() + 7 * 86400000);
      case 'plus35':
        return new Date(today.getTime() + 35 * 86400000);
    }
  }

  private getNextFriday(from: Date): Date {
    const dayOfWeek = from.getDay(); // 0 = Sunday, 5 = Friday
    const daysUntilFriday = (5 - dayOfWeek + 7) % 7 || 7; // If today is Friday, get next Friday
    return new Date(from.getTime() + daysUntilFriday * 86400000);
  }

  isDateSelected(option: 'today' | 'tomorrow' | 'friday' | 'nextWeek' | 'plus35'): boolean {
    const current = this.dueDate();
    if (!current) return false;

    const optionDate = this.getQuickOptionDate(option);
    return this.formatDateString(optionDate) === current;
  }

  private formatDateString(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  onDateSelect(date: string): void {
    this.onSetDate.emit(date);
    this.showDatePicker.set(false);
  }

  clearDueDate(): void {
    this.onClearDate.emit();
    this.showDatePicker.set(false);
  }
}
