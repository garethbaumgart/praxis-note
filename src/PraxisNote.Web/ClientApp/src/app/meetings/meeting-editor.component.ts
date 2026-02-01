import { Component, ChangeDetectionStrategy, signal, output, inject, computed, effect, HostListener } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgClass } from '@angular/common';
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
import { AudioRecorderService } from './audio-recorder.service';
import { DeepgramTranscriptionService } from './deepgram-transcription.service';
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
  imports: [FormsModule, NgClass, DialogModule, ButtonModule, InputTextModule, TextareaModule, DatePickerModule, SelectModule, MeetingAnalysisComponent],
  styles: [`
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
    @keyframes pulse-recording {
      0%, 100% { opacity: 1; }
      50% { opacity: 0.4; }
    }
    .recording-pulse {
      animation: pulse-recording 1.5s ease-in-out infinite;
    }
    .audio-bar {
      transition: height 0.1s ease-out;
    }
    .live-transcript {
      background: var(--color-bg-muted);
      border-radius: 6px;
      padding: 10px;
      max-height: 150px;
      overflow-y: auto;
    }
  `],
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
      <div class="px-5 py-4 space-y-4" [ngClass]="isEditing() ? ['max-h-[520px]', 'overflow-y-auto'] : []">
        <!-- Title -->
        <div class="flex items-center gap-3">
          <i class="pi pi-file-edit text-foreground-muted w-5 text-center" aria-hidden="true"></i>
          <input
            pInputText
            type="text"
            class="flex-1"
            placeholder="Meeting title (optional)"
            aria-label="Meeting title"
            [value]="title()"
            (input)="title.set(asInput($event).value)"
          />
        </div>

        <!-- Attendees -->
        <div class="flex items-center gap-3">
          <i class="pi pi-users text-foreground-muted w-5 text-center" aria-hidden="true"></i>
          <input
            pInputText
            type="text"
            class="flex-1"
            placeholder="Attendees (comma separated)"
            aria-label="Attendees"
            [value]="attendees()"
            (input)="attendees.set(asInput($event).value)"
          />
        </div>

        <!-- Date -->
        <div class="flex items-center gap-3">
          <i class="pi pi-calendar text-foreground-muted w-5 text-center" aria-hidden="true"></i>
          <div class="flex flex-wrap gap-2 flex-1">
            @for (option of dateOptions; track option.label) {
              <button
                type="button"
                class="px-3 py-1.5 text-sm rounded-full transition-colors"
                [attr.aria-pressed]="selectedDateChip() === option.label"
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
              [attr.aria-expanded]="showDatePicker()"
              aria-label="Pick a date"
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
          <i class="pi pi-clock text-foreground-muted w-5 text-center" aria-hidden="true"></i>
          <div class="flex items-center gap-1">
            <p-select
              [options]="hourOptions"
              [ngModel]="selectedHour()"
              (ngModelChange)="selectedHour.set($event)"
              optionLabel="label"
              optionValue="value"
              [style]="{ width: '80px' }"
              appendTo="body"
              ariaLabel="Meeting hour"
            />
            <span class="text-foreground-muted font-medium">:</span>
            <p-select
              [options]="minuteOptions()"
              [ngModel]="selectedMinute()"
              (ngModelChange)="selectedMinute.set($event)"
              optionLabel="label"
              optionValue="value"
              [style]="{ width: '80px' }"
              appendTo="body"
              ariaLabel="Meeting minute"
            />
            <p-select
              [options]="periodOptions"
              [ngModel]="selectedPeriod()"
              (ngModelChange)="selectedPeriod.set($event)"
              [style]="{ width: '78px', minWidth: '78px' }"
              appendTo="body"
              ariaLabel="AM or PM"
            />
          </div>
        </div>

        <!-- Transcript (only shown when editing) -->
        @if (isEditing()) {
          <!-- Audio Recording Section -->
          @if (recorder.isActive()) {
            <div class="flex gap-3">
              <i class="pi pi-microphone text-danger w-5 text-center mt-2" aria-hidden="true"></i>
              <div class="flex-1 bg-surface-subtle rounded-lg p-4">
                <div class="flex items-center justify-between mb-3">
                  <div class="flex items-center gap-2">
                    <span class="w-3 h-3 bg-danger rounded-full recording-pulse" aria-hidden="true"></span>
                    <span class="text-sm font-medium text-foreground">
                      {{ recorder.isPaused() ? 'Paused' : 'Recording' }}
                    </span>
                  </div>
                  <span class="text-sm text-foreground-muted font-mono" aria-label="Recording duration">{{ recorder.formattedTime() }}</span>
                </div>
                <!-- Audio level bars -->
                <div class="flex items-end gap-0.5 h-8 mb-3" aria-hidden="true">
                  @for (level of recorder.audioLevels(); track $index) {
                    <div
                      class="audio-bar flex-1 rounded-sm"
                      [class.bg-accent-solid]="level > 0.05"
                      [class.bg-surface-muted]="level <= 0.05"
                      [style.height.%]="Math.max(level * 100, 10)"
                    ></div>
                  }
                </div>
                <div class="flex justify-center gap-2">
                  @if (recorder.isRecording()) {
                    <button
                      type="button"
                      class="px-3 py-1.5 text-xs bg-surface-muted text-foreground-secondary rounded-md hover:bg-surface-muted/80 transition-colors"
                      (click)="recorder.pause()"
                      aria-label="Pause recording"
                    >
                      <i class="pi pi-pause mr-1"></i>Pause
                    </button>
                  } @else {
                    <button
                      type="button"
                      class="px-3 py-1.5 text-xs bg-surface-muted text-foreground-secondary rounded-md hover:bg-surface-muted/80 transition-colors"
                      (click)="recorder.resume()"
                      aria-label="Resume recording"
                    >
                      <i class="pi pi-play mr-1"></i>Resume
                    </button>
                  }
                  <button
                    type="button"
                    class="px-3 py-1.5 text-xs bg-danger text-white rounded-md hover:opacity-90 transition-opacity"
                    (click)="stopRecording()"
                    aria-label="Stop recording"
                  >
                    <i class="pi pi-stop-circle mr-1"></i>Stop
                  </button>
                </div>
              </div>
            </div>
          }

          @if (recorder.error()) {
            <div class="flex gap-3">
              <div class="w-5"></div>
              <p class="text-xs text-danger">{{ recorder.error() }}</p>
            </div>
          }
          @if (transcription.error()) {
            <div class="flex gap-3">
              <div class="w-5"></div>
              <p class="text-xs text-danger">{{ transcription.error() }}</p>
            </div>
          }

          <!-- Tab visibility warning -->
          @if (showTabWarning()) {
            <div class="flex gap-3">
              <div class="w-5"></div>
              <div class="flex items-center gap-2 text-xs text-foreground-muted bg-surface-muted rounded px-3 py-1.5">
                <i class="pi pi-info-circle text-xs"></i>
                <span>Keep this tab active for best recording quality.</span>
              </div>
            </div>
          }

          <!-- Live transcript preview while recording -->
          @if (recorder.isActive() && (transcription.transcript() || transcription.interimText())) {
            <div class="flex gap-3">
              <div class="w-5"></div>
              <div class="flex-1 live-transcript">
                <div class="flex items-center gap-1.5 mb-2">
                  <i class="pi pi-volume-up text-xs text-accent-solid"></i>
                  <span class="text-xs font-medium text-foreground-secondary">Live Transcript</span>
                </div>
                <p class="text-sm text-foreground leading-relaxed">
                  {{ transcription.transcript() }}
                  @if (transcription.interimText()) {
                    <span class="text-foreground-muted italic">{{ transcription.interimText() }}</span>
                  }
                </p>
              </div>
            </div>
          }

          <div class="flex gap-3">
            <i class="pi pi-align-left text-foreground-muted w-5 text-center mt-2" aria-hidden="true"></i>
            <div class="flex-1">
              <textarea
                pTextarea
                class="w-full resize-none text-sm"
                [rows]="5"
                placeholder="Paste transcript..."
                aria-label="Meeting transcript"
                [value]="transcript()"
                (input)="transcript.set(asTextarea($event).value)"
              ></textarea>
              <div class="flex justify-between items-center mt-1">
                <div class="flex items-center gap-2">
                  @if (!recorder.isActive()) {
                      <button
                        type="button"
                        class="flex items-center gap-1.5 text-xs text-foreground-muted hover:text-danger transition-colors"
                        (click)="startRecording()"
                        aria-label="Record and transcribe from microphone"
                      >
                        <i class="pi pi-circle-fill text-[8px] text-danger"></i>
                        Record
                      </button>
                  }
                </div>
                <span class="text-xs text-foreground-muted">{{ transcript().length }} characters</span>
              </div>
            </div>
          </div>

          <!-- AI Analysis -->
          @if (currentMeeting()) {
            <div class="flex gap-3">
              <i class="pi pi-sparkles text-foreground-muted w-5 text-center mt-1" aria-hidden="true"></i>
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
  readonly recorder = inject(AudioRecorderService);
  readonly transcription = inject(DeepgramTranscriptionService);

  /** Expose Math for template */
  readonly Math = Math;

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
  readonly showTabWarning = signal(false);

  // Date selection state
  readonly selectedDateChip = signal<string | null>('Tomorrow');
  readonly customDateLabel = signal<string | null>(null);
  readonly showDatePicker = signal(false);

  // Time selection state
  readonly selectedHour = signal(10);
  readonly selectedMinute = signal(0);
  readonly selectedPeriod = signal<'AM' | 'PM'>('AM');

  // Date options
  readonly dateOptions: DateOption[] = [
    { label: 'Today', getValue: () => new Date() },
    { label: 'Tomorrow', getValue: () => this.addDays(new Date(), 1) },
    { label: 'Next Week', getValue: () => this.addDays(new Date(), 7) },
  ];

  // Time options
  readonly hourOptions: TimeOption[] = [
    { label: '12', value: 12 },
    { label: '1', value: 1 },
    { label: '2', value: 2 },
    { label: '3', value: 3 },
    { label: '4', value: 4 },
    { label: '5', value: 5 },
    { label: '6', value: 6 },
    { label: '7', value: 7 },
    { label: '8', value: 8 },
    { label: '9', value: 9 },
    { label: '10', value: 10 },
    { label: '11', value: 11 },
  ];

  readonly defaultMinuteOptions: TimeOption[] = [
    { label: '00', value: 0 },
    { label: '05', value: 5 },
    { label: '10', value: 10 },
    { label: '15', value: 15 },
    { label: '20', value: 20 },
    { label: '25', value: 25 },
    { label: '30', value: 30 },
    { label: '35', value: 35 },
    { label: '40', value: 40 },
    { label: '45', value: 45 },
    { label: '50', value: 50 },
    { label: '55', value: 55 },
  ];

  readonly minuteOptions = signal<TimeOption[]>(this.defaultMinuteOptions);

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
      const minute = this.selectedMinute();
      const period = this.selectedPeriod();
      const currentDate = this.meetingDate();

      if (currentDate) {
        const newDate = new Date(currentDate);
        const hour24 = this.toHour24(hour, period);
        newDate.setHours(hour24, minute, 0, 0);
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

  /** Convert 12-hour format to 24-hour format */
  private toHour24(hour12: number, period: 'AM' | 'PM'): number {
    if (period === 'PM' && hour12 !== 12) {
      return hour12 + 12;
    } else if (period === 'AM' && hour12 === 12) {
      return 0;
    }
    return hour12;
  }

  selectDateOption(option: DateOption): void {
    this.selectedDateChip.set(option.label);
    this.customDateLabel.set(null);
    this.showDatePicker.set(false);

    const newDate = option.getValue();
    // Preserve the current time
    const currentDate = this.meetingDate();
    if (currentDate) {
      newDate.setHours(currentDate.getHours(), currentDate.getMinutes(), currentDate.getSeconds(), currentDate.getMilliseconds());
    } else {
      // Default to selected time
      const hour24 = this.toHour24(this.selectedHour(), this.selectedPeriod());
      newDate.setHours(hour24, this.selectedMinute(), 0, 0);
    }
    this.meetingDate.set(newDate);
  }

  onDatePickerChange(date: Date | null): void {
    if (!date) return;

    // Preserve the current time
    const currentDate = this.meetingDate();
    if (currentDate) {
      date.setHours(currentDate.getHours(), currentDate.getMinutes(), currentDate.getSeconds(), currentDate.getMilliseconds());
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

    // Use the exact minute value; if it's not on a 5-minute interval, add it to the options
    const minutes = date.getMinutes();
    if (minutes % 5 !== 0) {
      const pad = minutes < 10 ? '0' : '';
      const customOption: TimeOption = { label: `${pad}${minutes}`, value: minutes };
      const opts = [...this.defaultMinuteOptions, customOption].sort((a, b) => a.value - b.value);
      this.minuteOptions.set(opts);
    } else {
      this.minuteOptions.set(this.defaultMinuteOptions);
    }
    this.selectedMinute.set(minutes);
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
    this.showTabWarning.set(false);
    this.transcription.reset();
    this.recorder.discard();

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
      this.selectedMinute.set(0);
      this.minuteOptions.set(this.defaultMinuteOptions);
      this.selectedPeriod.set('AM');

      this.attendees.set('');
      this.transcript.set('');
    }
    this.visible.set(true);
  }

  async startRecording(): Promise<void> {
    this.transcription.reset();
    await this.recorder.start();
    if (this.recorder.isActive()) {
      this.transcription.start();
      this.recorder.onAudioChunk.set((blob) => this.transcription.sendAudio(blob));
      this.showTabWarning.set(true);
    }
  }

  async stopRecording(): Promise<void> {
    try {
      this.recorder.onAudioChunk.set(null);
      this.transcription.stop();
      await this.recorder.stop();
      this.showTabWarning.set(false);

      // Set transcript from transcription results
      const recognizedText = this.transcription.transcript();
      if (recognizedText) {
        const current = this.transcript();
        const separator = current ? '\n\n' : '';
        this.transcript.set(current + separator + recognizedText);
      }
    } catch (error) {
      this.recorder.onAudioChunk.set(null);
      this.transcription.stop();
      this.showTabWarning.set(false);
      console.error('Failed to stop audio recording:', error);
      this.toast.error('Failed to stop recording. Please try again.');
    }
  }

  @HostListener('window:beforeunload', ['$event'])
  onBeforeUnload(event: BeforeUnloadEvent): void {
    if (this.recorder.isActive()) {
      event.preventDefault();
      event.returnValue = '';
    }
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

  /** Type-safe helper for input events */
  asInput(event: Event): HTMLInputElement {
    return event.target as HTMLInputElement;
  }

  /** Type-safe helper for textarea events */
  asTextarea(event: Event): HTMLTextAreaElement {
    return event.target as HTMLTextAreaElement;
  }
}
