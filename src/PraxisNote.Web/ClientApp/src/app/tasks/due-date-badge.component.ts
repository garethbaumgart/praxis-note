import {
  Component,
  computed,
  input,
  output,
  signal,
  ChangeDetectionStrategy,
} from '@angular/core';
import { DatePickerPopoverComponent } from './date-picker-popover.component';

@Component({
  selector: 'app-due-date-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePickerPopoverComponent],
  template: `
    <div class="relative">
      @if (dueDate()) {
        <!-- Has due date - show badge with text -->
        <button
          type="button"
          (click)="togglePicker(); $event.stopPropagation()"
          [class]="badgeClass()"
          class="flex items-center gap-1 px-1.5 py-0.5 rounded hover:bg-surface-hover transition-colors"
        >
          <i [class]="iconClass()"></i>
          <span>{{ displayText() }}</span>
        </button>
      } @else {
        <!-- No due date - just calendar icon -->
        <button
          type="button"
          (click)="togglePicker(); $event.stopPropagation()"
          class="flex items-center justify-center w-6 h-6 rounded hover:bg-surface-hover text-foreground-muted/40 hover:text-foreground-muted transition-colors"
          aria-label="Set due date"
        >
          <i class="pi pi-calendar"></i>
        </button>
      }

      @if (showPicker()) {
        <app-date-picker-popover
          [currentDate]="dueDate()"
          (onSelect)="onDateSelect($event)"
          (onClear)="onDateClear()"
          (onClose)="showPicker.set(false)"
        />
      }
    </div>
  `,
})
export class DueDateBadgeComponent {
  readonly dueDate = input<string | null>(null);
  readonly taskStatus = input<'Todo' | 'InProgress' | 'Done'>('Todo');
  readonly onSetDueDate = output<string>();
  readonly onClearDueDate = output<void>();

  readonly showPicker = signal(false);

  /** Days until due date (negative = overdue, 0 = today, positive = future) */
  private readonly daysDiff = computed(() => {
    const dueDate = this.dueDate();
    if (!dueDate) return null;

    const date = new Date(dueDate + 'T00:00:00');
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return Math.floor((date.getTime() - today.getTime()) / 86400000);
  });

  readonly displayText = computed(() => {
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

  readonly badgeClass = computed(() => {
    const diff = this.daysDiff();
    if (diff === null) return '';

    // Option A: Warm urgency scale - red (overdue), amber (today/tomorrow), slate (future)
    if (this.taskStatus() === 'Done') return 'bg-slate-100 text-slate-400 line-through';
    if (diff < 0) return 'bg-rose-100 text-rose-600 font-medium';
    if (diff === 0) return 'bg-amber-100 text-amber-700';
    if (diff === 1) return 'bg-amber-50 text-amber-600';
    if (diff <= 6) return 'bg-slate-100 text-slate-600';
    return 'bg-slate-100 text-slate-500';
  });

  readonly iconClass = computed(() => {
    const diff = this.daysDiff();
    if (diff === null) return 'pi pi-calendar';
    if (diff < 0) return 'pi pi-exclamation-circle';
    return 'pi pi-calendar';
  });

  togglePicker(): void {
    this.showPicker.update(v => !v);
  }

  onDateSelect(date: string): void {
    this.onSetDueDate.emit(date);
    this.showPicker.set(false);
  }

  onDateClear(): void {
    this.onClearDueDate.emit();
    this.showPicker.set(false);
  }
}
