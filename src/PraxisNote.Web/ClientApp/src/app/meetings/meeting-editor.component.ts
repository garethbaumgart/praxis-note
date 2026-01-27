import { Component, ChangeDetectionStrategy, signal, output, inject, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { DatePickerModule } from 'primeng/datepicker';
import { Meeting } from './meeting.model';
import { MeetingAnalysisComponent } from './meeting-analysis.component';
import { MeetingService } from './meeting.service';

@Component({
  selector: 'app-meeting-editor',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, DialogModule, ButtonModule, InputTextModule, TextareaModule, DatePickerModule, MeetingAnalysisComponent],
  template: `
    <p-dialog
      [visible]="visible()"
      (visibleChange)="visible.set($event)"
      [modal]="true"
      [draggable]="false"
      [resizable]="false"
      [closable]="true"
      [style]="{ width: '600px' }"
      [header]="isEditing() ? 'Edit Meeting' : 'New Meeting'"
    >
      <div class="space-y-4">
        <!-- Title -->
        <div>
          <label for="title" class="block text-sm font-medium text-foreground mb-1">Title</label>
          <input
            pInputText
            id="title"
            type="text"
            class="w-full"
            placeholder="Meeting title (optional)"
            [(ngModel)]="title"
          />
        </div>

        <!-- Date & Time -->
        <div>
          <label for="meetingDate" class="block text-sm font-medium text-foreground mb-1">Date & Time</label>
          <p-datepicker
            id="meetingDate"
            [(ngModel)]="meetingDate"
            [showTime]="true"
            [hourFormat]="'12'"
            dateFormat="dd M yy"
            [style]="{ width: '100%' }"
            appendTo="body"
          />
        </div>

        <!-- Attendees -->
        <div>
          <label for="attendees" class="block text-sm font-medium text-foreground mb-1">Attendees</label>
          <input
            pInputText
            id="attendees"
            type="text"
            class="w-full"
            placeholder="John, Sarah, Mike..."
            [(ngModel)]="attendees"
          />
          <p class="text-xs text-foreground-muted mt-1">Separate names with commas</p>
        </div>

        <!-- Transcript (only shown when editing) -->
        @if (isEditing()) {
          <div>
            <div class="flex items-center justify-between mb-1">
              <label for="transcript" class="block text-sm font-medium text-foreground">Transcript</label>
              <span class="text-xs text-foreground-muted">{{ transcript.length }} characters</span>
            </div>
            <textarea
              pTextarea
              id="transcript"
              class="w-full"
              [rows]="8"
              placeholder="Paste your meeting transcript here..."
              [(ngModel)]="transcript"
            ></textarea>
            <p class="text-xs text-foreground-muted mt-1">Paste a transcript from your meeting recording or notes</p>
          </div>

          <!-- AI Analysis -->
          @if (currentMeeting()) {
            <app-meeting-analysis
              [meeting]="currentMeeting()!"
              (onAnalyze)="analyze()"
            />
          }
        }
      </div>

      <ng-template pTemplate="footer">
        <div class="flex justify-end gap-2">
          <p-button
            label="Cancel"
            severity="secondary"
            [text]="true"
            (onClick)="visible.set(false)"
          />
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

  readonly visible = signal(false);
  readonly isEditing = signal(false);
  private readonly meetingId = signal<string | null>(null);
  readonly onSave = output<{ title?: string; meetingDate?: string; attendees?: string; transcript?: string }>();

  readonly currentMeeting = computed(() => {
    const id = this.meetingId();
    if (!id) return null;
    return this.meetingService.meetings().find(m => m.id === id) ?? null;
  });

  title = '';
  meetingDate: Date | null = null;
  attendees = '';
  transcript = '';

  open(meeting?: Meeting): void {
    if (meeting) {
      this.isEditing.set(true);
      this.meetingId.set(meeting.id);
      this.title = meeting.title ?? '';
      this.meetingDate = meeting.meetingDate ? new Date(meeting.meetingDate) : new Date();
      this.attendees = meeting.attendees ?? '';
      this.transcript = meeting.transcriptContent ?? '';
    } else {
      this.isEditing.set(false);
      this.meetingId.set(null);
      this.title = '';
      this.meetingDate = new Date();
      this.attendees = '';
      this.transcript = '';
    }
    this.visible.set(true);
  }

  analyze(): void {
    const id = this.meetingId();
    if (id) {
      this.meetingService.analyzeMeeting(id);
    }
  }

  save(): void {
    this.onSave.emit({
      title: this.title || undefined,
      meetingDate: this.meetingDate?.toISOString(),
      attendees: this.attendees || undefined,
      transcript: this.transcript,
    });
    this.visible.set(false);
  }
}
