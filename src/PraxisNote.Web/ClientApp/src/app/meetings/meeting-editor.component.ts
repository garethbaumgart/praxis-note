import { Component, ChangeDetectionStrategy, signal, output, inject, computed, effect } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { DatePickerModule } from 'primeng/datepicker';
import { SelectModule } from 'primeng/select';
import { Meeting, ActionItemStatus } from './meeting.model';
import { MeetingAnalysisComponent } from './meeting-analysis.component';
import { MeetingService } from './meeting.service';
import { ToastService } from '../shared/services/toast.service';

interface DateOption {
  label: string;
  getValue: () => Date;
}

interface TimeOption {
  label: string;
  value: number;
}

@Component({
  selector: 'app-meeting-editor',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, DialogModule, ButtonModule, InputTextModule, TextareaModule, DatePickerModule, SelectModule, MeetingAnalysisComponent],
  styles: `
    :host ::ng-deep .p-dialog-content {
      padding: 0 !important;
    }
    :host ::ng-deep .p-dialog-header {
      padding: 0.75rem 1.25rem;
      border-bottom: 1px solid var(--color-border);
    }
    :host ::ng-deep .p-dialog-footer {
      padding: 0.75rem 1.25rem;
      border-top: 1px solid var(--color-border);
    }
    :host ::ng-deep .p-datepicker {
      border: none;
      background: transparent;
    }
  `,
  template: `
    <p-dialog
      [visible]="visible()"
      (visibleChange)="visible.set($event)"
      [modal]="true"
      [draggable]="false"
      [resizable]="false"
      [closable]="true"
      [style]="{ width: isEditing() ? '580px' : '520px' }"
      [header]="isEditing() ? 'Edit Meeting' : 'New Meeting'"
    >
      <div class="px-5 py-4 space-y-4" [class.max-h-[520px]]="isEditing()" [class.overflow-y-auto]="isEditing()">
        <!-- Title -->
        <div class="flex items-center gap-3">
          <i class="pi pi-file-edit text-foreground-muted w-5 text-center"></i>
          <input
            pInputText
            type="text"
            class="flex-1"
            placeholder="Meeting title (optional)"
            [value]="title()"
            (input)="title.set($any($event.target).value)"
          />
        </div>

        <!-- Attendees -->
        <div class="flex items-center gap-3">
          <i class="pi pi-users text-foreground-muted w-5 text-center"></i>
          <input
            pInputText
            type="text"
            class="flex-1"
            placeholder="Attendees (comma separated)"
            [value]="attendees()"
            (input)="attendees.set($any($event.target).value)"
          />
        </div>

        <!-- Date -->
        <div class="flex items-center gap-3">
          <i class="pi pi-calendar text-foreground-muted w-5 text-center"></i>
          <div class="flex flex-wrap gap-2 flex-1">
            @for (option of dateOptions; track option.label) {
              <button
                type="button"
                class="px-3 py-1.5 text-sm rounded-full transition-colors"
                [class.bg-accent]="selectedDateChip() === option.label"
                [class.text-accent-foreground]="selectedDateChip() === option.label"
                [class.bg-surface-muted]="selectedDateChip() !== option.label"
                [class.text-foreground-secondary]="selectedDateChip() !== option.label"
                [class.hover:bg-accent]="selectedDateChip() !== option.label"
                [class.hover:text-accent-foreground]="selectedDateChip() !== option.label"
                (click)="selectDateOption(option)"
              >
                {{ option.label }}
              </button>
            }
            <!-- Custom date chip (shown when a specific date is selected in edit mode) -->
            @if (selectedDateChip() === 'custom' && customDateLabel()) {
              <button
                type="button"
                class="px-3 py-1.5 text-sm rounded-full bg-accent text-accent-foreground"
              >
                {{ customDateLabel() }}
              </button>
            }
            <button
              type="button"
              class="px-3 py-1.5 text-sm rounded-full bg-surface-muted text-foreground-secondary hover:bg-accent hover:text-accent-foreground transition-colors flex items-center gap-1"
              (click)="showDatePicker.set(!showDatePicker())"
            >
              <i class="pi pi-calendar text-xs"></i>
              {{ isEditing() ? 'Change' : 'Pick' }}
            </button>
          </div>
        </div>

        <!-- Date Picker (hidden by default, shown when Pick is clicked) -->
        @if (showDatePicker()) {
          <div class="flex items-start gap-3">
            <div class="w-5"></div>
            <div class="flex-1">
              <p-datepicker
                [inline]="true"
                [ngModel]="meetingDate()"
                (ngModelChange)="onDatePickerChange($event)"
                dateFormat="dd M yy"
              />
            </div>
          </div>
        }

        <!-- Time -->
        <div class="flex items-center gap-3">
          <i class="pi pi-clock text-foreground-muted w-5 text-center"></i>
          <div class="flex items-center gap-2">
            <p-select
              [options]="hourOptions"
              [ngModel]="selectedHour()"
              (ngModelChange)="selectedHour.set($event)"
              optionLabel="label"
              optionValue="value"
              [style]="{ width: '90px' }"
              appendTo="body"
            />
            <p-select
              [options]="periodOptions"
              [ngModel]="selectedPeriod()"
              (ngModelChange)="selectedPeriod.set($event)"
              [style]="{ width: '80px' }"
              appendTo="body"
            />
          </div>
        </div>

        <!-- Transcript (only shown when editing) -->
        @if (isEditing()) {
          <div class="flex gap-3">
            <i class="pi pi-align-left text-foreground-muted w-5 text-center mt-2"></i>
            <div class="flex-1">
              <textarea
                pTextarea
                class="w-full resize-none text-sm"
                [rows]="5"
                placeholder="Paste transcript..."
                [value]="transcript()"
                (input)="transcript.set($any($event.target).value)"
              ></textarea>
              <div class="flex justify-end mt-1">
                <span class="text-xs text-foreground-muted">{{ transcript().length }} characters</span>
              </div>
            </div>
          </div>

          <!-- AI Analysis -->
          @if (currentMeeting()) {
            <div class="flex gap-3">
              <i class="pi pi-sparkles text-foreground-muted w-5 text-center mt-1"></i>
              <div class="flex-1">
                <app-meeting-analysis
                  [meeting]="currentMeeting()!"
                  [actionItemStatuses]="actionItemStatuses()"
                  [promotingIds]="promotingIds()"
                  (onAnalyze)="analyze()"
                  (onToggleActionItem)="toggleActionItem($event)"
                  (onPromoteActionItem)="promoteActionItem($event)"
                  (onNavigateToTask)="navigateToTask($event)"
                />
              </div>
            </div>
          }
        }
      </div>

      <ng-template pTemplate="footer">
        <div class="flex justify-end gap-3">
          <button
            type="button"
            class="px-4 py-1.5 text-sm text-foreground-secondary hover:text-foreground transition-colors"
            (click)="visible.set(false)"
          >
            Cancel
          </button>
          <p-button
            [label]="isEditing() ? 'Save' : 'Create'"
            (onClick)="save()"
          />
        </div>
      </ng-template>
    </p-dialog>
  `,
})
export class MeetingEditorComponent {
  private readonly meetingService = inject(MeetingService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);

  readonly visible = signal(false);
  readonly isEditing = signal(false);
  private readonly meetingId = signal<string | null>(null);
  readonly onSave = output<{ title?: string; meetingDate?: string; attendees?: string; transcript?: string }>();

  readonly actionItemStatuses = signal<ActionItemStatus[]>([]);
  readonly promotingIds = signal<Set<string>>(new Set());

  readonly currentMeeting = computed(() => {
    const id = this.meetingId();
    if (!id) return null;
    return this.meetingService.meetings().find(m => m.id === id) ?? null;
  });

  // Track action items count to avoid effect running on every meeting change
  private readonly actionItemsCount = computed(() =>
    this.currentMeeting()?.actionItems.length ?? 0
  );

  // Form state using signals
  readonly title = signal('');
  readonly meetingDate = signal<Date | null>(null);
  readonly attendees = signal('');
  readonly transcript = signal('');

  // Date selection state
  readonly selectedDateChip = signal<string | null>('Tomorrow');
  readonly customDateLabel = signal<string | null>(null);
  readonly showDatePicker = signal(false);

  // Time selection state
  readonly selectedHour = signal(10);
  readonly selectedPeriod = signal<'AM' | 'PM'>('AM');

  // Date options
  readonly dateOptions: DateOption[] = [
    { label: 'Today', getValue: () => new Date() },
    { label: 'Tomorrow', getValue: () => this.addDays(new Date(), 1) },
    { label: 'Next Week', getValue: () => this.addDays(new Date(), 7) },
  ];

  // Time options
  readonly hourOptions: TimeOption[] = [
    { label: '12:00', value: 12 },
    { label: '1:00', value: 1 },
    { label: '2:00', value: 2 },
    { label: '3:00', value: 3 },
    { label: '4:00', value: 4 },
    { label: '5:00', value: 5 },
    { label: '6:00', value: 6 },
    { label: '7:00', value: 7 },
    { label: '8:00', value: 8 },
    { label: '9:00', value: 9 },
    { label: '10:00', value: 10 },
    { label: '11:00', value: 11 },
  ];

  readonly periodOptions = ['AM', 'PM'];

  constructor() {
    // Reload action item statuses only when action items count changes (e.g., after analysis completes)
    effect(() => {
      const count = this.actionItemsCount();
      const id = this.meetingId();
      if (id && count > 0) {
        this.loadActionItemStatuses();
      }
    });

    // Update meetingDate when time selection changes
    effect(() => {
      const hour = this.selectedHour();
      const period = this.selectedPeriod();
      const currentDate = this.meetingDate();

      if (currentDate) {
        const newDate = new Date(currentDate);
        let hour24 = hour;
        if (period === 'PM' && hour !== 12) {
          hour24 = hour + 12;
        } else if (period === 'AM' && hour === 12) {
          hour24 = 0;
        }
        newDate.setHours(hour24, 0, 0, 0);
        // Only update if time actually changed to avoid infinite loop
        if (newDate.getTime() !== currentDate.getTime()) {
          this.meetingDate.set(newDate);
        }
      }
    });
  }

  private addDays(date: Date, days: number): Date {
    const result = new Date(date);
    result.setDate(result.getDate() + days);
    return result;
  }

  selectDateOption(option: DateOption): void {
    this.selectedDateChip.set(option.label);
    this.customDateLabel.set(null);
    this.showDatePicker.set(false);

    const newDate = option.getValue();
    // Preserve the current time
    const currentDate = this.meetingDate();
    if (currentDate) {
      newDate.setHours(currentDate.getHours(), currentDate.getMinutes(), 0, 0);
    } else {
      // Default to selected hour
      let hour24 = this.selectedHour();
      if (this.selectedPeriod() === 'PM' && hour24 !== 12) {
        hour24 = hour24 + 12;
      } else if (this.selectedPeriod() === 'AM' && hour24 === 12) {
        hour24 = 0;
      }
      newDate.setHours(hour24, 0, 0, 0);
    }
    this.meetingDate.set(newDate);
  }

  onDatePickerChange(date: Date): void {
    if (!date) return;

    // Preserve the current time
    const currentDate = this.meetingDate();
    if (currentDate) {
      date.setHours(currentDate.getHours(), currentDate.getMinutes(), 0, 0);
    }

    this.meetingDate.set(date);
    this.selectedDateChip.set('custom');
    this.customDateLabel.set(this.formatDateLabel(date));
    this.showDatePicker.set(false);
  }

  private formatDateLabel(date: Date): string {
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
  }

  private extractTimeFromDate(date: Date): void {
    let hours = date.getHours();
    const period: 'AM' | 'PM' = hours >= 12 ? 'PM' : 'AM';

    if (hours === 0) {
      hours = 12;
    } else if (hours > 12) {
      hours = hours - 12;
    }

    this.selectedHour.set(hours);
    this.selectedPeriod.set(period);
  }

  private determineInitialDateChip(date: Date): void {
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const tomorrow = this.addDays(today, 1);
    const nextWeek = this.addDays(today, 7);

    const dateOnly = new Date(date);
    dateOnly.setHours(0, 0, 0, 0);

    if (dateOnly.getTime() === today.getTime()) {
      this.selectedDateChip.set('Today');
      this.customDateLabel.set(null);
    } else if (dateOnly.getTime() === tomorrow.getTime()) {
      this.selectedDateChip.set('Tomorrow');
      this.customDateLabel.set(null);
    } else if (dateOnly.getTime() === nextWeek.getTime()) {
      this.selectedDateChip.set('Next Week');
      this.customDateLabel.set(null);
    } else {
      this.selectedDateChip.set('custom');
      this.customDateLabel.set(this.formatDateLabel(date));
    }
  }

  open(meeting?: Meeting): void {
    // Reset statuses immediately to avoid stale data
    this.actionItemStatuses.set([]);
    this.promotingIds.set(new Set());
    this.showDatePicker.set(false);

    if (meeting) {
      this.isEditing.set(true);
      this.meetingId.set(meeting.id);
      this.title.set(meeting.title ?? '');
      const meetingDate = meeting.meetingDate ? new Date(meeting.meetingDate) : new Date();
      this.meetingDate.set(meetingDate);
      this.attendees.set(meeting.attendees ?? '');
      this.transcript.set(meeting.transcriptContent ?? '');

      // Extract time and determine date chip
      this.extractTimeFromDate(meetingDate);
      this.determineInitialDateChip(meetingDate);
      // Action item statuses will be loaded via the effect when currentMeeting changes
    } else {
      this.isEditing.set(false);
      this.meetingId.set(null);
      this.title.set('');

      // Default to tomorrow at 10 AM
      const tomorrow = this.addDays(new Date(), 1);
      tomorrow.setHours(10, 0, 0, 0);
      this.meetingDate.set(tomorrow);
      this.selectedDateChip.set('Tomorrow');
      this.customDateLabel.set(null);
      this.selectedHour.set(10);
      this.selectedPeriod.set('AM');

      this.attendees.set('');
      this.transcript.set('');
    }
    this.visible.set(true);
  }

  analyze(): void {
    const id = this.meetingId();
    if (id) {
      this.meetingService.analyzeMeeting(id);
    }
  }

  toggleActionItem(actionItemId: string): void {
    const id = this.meetingId();
    if (id) {
      this.meetingService.toggleActionItem(id, actionItemId);
    }
  }

  promoteActionItem(actionItemId: string): void {
    const id = this.meetingId();
    if (!id) return;

    // Add to promoting set
    this.promotingIds.update(ids => new Set([...ids, actionItemId]));

    this.meetingService.promoteActionItem(id, actionItemId).subscribe({
      next: result => {
        this.toast.success({ summary: 'Task created', detail: result.title });
        this.loadActionItemStatuses();
        this.promotingIds.update(ids => {
          const newSet = new Set(ids);
          newSet.delete(actionItemId);
          return newSet;
        });
      },
      error: () => {
        this.toast.error('Failed to promote action item');
        this.promotingIds.update(ids => {
          const newSet = new Set(ids);
          newSet.delete(actionItemId);
          return newSet;
        });
      },
    });
  }

  navigateToTask(taskId: string): void {
    this.visible.set(false);
    this.router.navigate(['/tasks'], { queryParams: { highlight: taskId } });
  }

  private loadActionItemStatuses(): void {
    const id = this.meetingId();
    if (!id) return;

    this.meetingService.getActionItemStatus(id).subscribe({
      next: statuses => {
        // Guard against stale responses when meeting changes
        if (this.meetingId() !== id) return;
        this.actionItemStatuses.set(statuses);
      },
      error: () => {
        if (this.meetingId() !== id) return;
        this.actionItemStatuses.set([]);
      },
    });
  }

  save(): void {
    this.onSave.emit({
      title: this.title() || undefined,
      meetingDate: this.meetingDate()?.toISOString(),
      attendees: this.attendees() || undefined,
      transcript: this.transcript(),
    });
    this.visible.set(false);
  }
}
