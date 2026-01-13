import {
  Component,
  input,
  output,
  signal,
  ChangeDetectionStrategy,
  ElementRef,
  inject,
  afterNextRender,
  Injector,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePicker } from 'primeng/datepicker';

@Component({
  selector: 'app-date-picker-popover',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePicker, FormsModule],
  host: {
    '(document:click)': 'onDocumentClick($event)',
    '(document:keydown.escape)': 'onClose.emit()',
  },
  template: `
    <div
      class="absolute left-0 top-full mt-1 z-50 bg-surface-subtle border border-border rounded-lg shadow-lg"
    >
      <!-- Quick options bar -->
      <div class="flex items-center gap-1.5 p-2 border-b border-border">
        <button
          type="button"
          (click)="selectQuickOption('today'); $event.stopPropagation()"
          class="px-2 py-0.5 text-xs font-medium rounded-full transition-colors"
          [class]="isSelected('today') ? 'bg-amber-200 text-amber-800' : 'bg-amber-100 text-amber-700 hover:bg-amber-200'"
        >
          Today
        </button>
        <button
          type="button"
          (click)="selectQuickOption('tomorrow'); $event.stopPropagation()"
          class="px-2 py-0.5 text-xs font-medium rounded-full transition-colors"
          [class]="isSelected('tomorrow') ? 'bg-amber-100 text-amber-700' : 'bg-amber-50 text-amber-600 hover:bg-amber-100'"
        >
          +1
        </button>
        <button
          type="button"
          (click)="selectQuickOption('nextWeek'); $event.stopPropagation()"
          class="px-2 py-0.5 text-xs font-medium rounded-full transition-colors"
          [class]="isSelected('nextWeek') ? 'bg-slate-200 text-slate-700' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'"
        >
          +7
        </button>
        <button
          type="button"
          (click)="selectQuickOption('plus35'); $event.stopPropagation()"
          class="px-2 py-0.5 text-xs font-medium rounded-full transition-colors"
          [class]="isSelected('plus35') ? 'bg-slate-200 text-slate-700' : 'bg-slate-100 text-slate-600 hover:bg-slate-200'"
        >
          +35
        </button>
        @if (currentDate()) {
          <button
            type="button"
            (click)="clear(); $event.stopPropagation()"
            class="ml-auto text-xs text-red-500 hover:text-red-600 hover:underline"
            aria-label="Clear due date"
          >
            Clear
          </button>
        }
      </div>

      <!-- PrimeNG DatePicker -->
      <div (click)="$event.stopPropagation()">
        <p-datepicker
          [ngModel]="selectedDate()"
          (ngModelChange)="selectedDate.set($event)"
          [inline]="true"
          [minDate]="minDate"
          [showButtonBar]="false"
          (onSelect)="onDateSelected()"
          styleClass="compact-datepicker"
        />
      </div>
    </div>
  `,
})
export class DatePickerPopoverComponent {
  private readonly elementRef = inject(ElementRef);
  private readonly injector = inject(Injector);

  readonly currentDate = input<string | null>(null);
  readonly onSelect = output<string>();
  readonly onClear = output<void>();
  readonly onClose = output<void>();

  readonly minDate = new Date(2020, 0, 1);
  readonly selectedDate = signal<Date | null>(null);

  private initialized = false;

  constructor() {
    afterNextRender(() => {
      this.initialized = true;
      // Initialize selected date from currentDate input
      const current = this.currentDate();
      if (current) {
        this.selectedDate.set(new Date(current + 'T00:00:00'));
      }
    }, { injector: this.injector });
  }

  onDocumentClick(event: Event): void {
    if (!this.initialized) return;

    const target = event.target as HTMLElement;
    if (!this.elementRef.nativeElement.contains(target)) {
      this.onClose.emit();
    }
  }

  selectQuickOption(option: 'today' | 'tomorrow' | 'nextWeek' | 'plus35'): void {
    const date = this.getQuickOptionDate(option);
    this.selectedDate.set(date);
    this.onSelect.emit(this.formatDate(date));
  }

  private getQuickOptionDate(option: 'today' | 'tomorrow' | 'nextWeek' | 'plus35'): Date {
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    switch (option) {
      case 'today':
        return today;
      case 'tomorrow':
        return new Date(today.getTime() + 86400000);
      case 'nextWeek':
        return new Date(today.getTime() + 7 * 86400000);
      case 'plus35':
        return new Date(today.getTime() + 35 * 86400000);
    }
  }

  isSelected(option: 'today' | 'tomorrow' | 'nextWeek' | 'plus35'): boolean {
    const current = this.currentDate();
    if (!current) return false;

    const optionDate = this.getQuickOptionDate(option);
    return this.formatDate(optionDate) === current;
  }

  onDateSelected(): void {
    const date = this.selectedDate();
    if (date) {
      this.onSelect.emit(this.formatDate(date));
    }
  }

  clear(): void {
    this.selectedDate.set(null);
    this.onClear.emit();
  }

  private formatDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
