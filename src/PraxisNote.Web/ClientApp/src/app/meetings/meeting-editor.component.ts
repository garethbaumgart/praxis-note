import { Component, ChangeDetectionStrategy, inject, signal, OnInit, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { DatePickerModule } from 'primeng/datepicker';
import { Meeting } from './meeting.model';

@Component({
  selector: 'app-meeting-editor',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, DialogModule, ButtonModule, InputTextModule, DatePickerModule],
  template: `
    <p-dialog
      [visible]="visible()"
      (visibleChange)="visible.set($event)"
      [modal]="true"
      [draggable]="false"
      [resizable]="false"
      [closable]="true"
      [style]="{ width: '450px' }"
      [header]="meeting() ? 'Edit Meeting' : 'New Meeting'"
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
            [label]="meeting() ? 'Save' : 'Create'"
            (onClick)="save()"
          />
        </div>
      </ng-template>
    </p-dialog>
  `,
})
export class MeetingEditorComponent implements OnInit {
  readonly visible = signal(false);
  readonly meeting = input<Meeting | null>(null);
  readonly onSave = output<{ title?: string; meetingDate?: string; attendees?: string }>();

  title = '';
  meetingDate: Date | null = null;
  attendees = '';

  ngOnInit(): void {
    const m = this.meeting();
    if (m) {
      this.title = m.title ?? '';
      this.meetingDate = m.meetingDate ? new Date(m.meetingDate) : null;
      this.attendees = m.attendees ?? '';
    } else {
      this.meetingDate = new Date();
    }
  }

  open(meeting?: Meeting): void {
    if (meeting) {
      this.title = meeting.title ?? '';
      this.meetingDate = meeting.meetingDate ? new Date(meeting.meetingDate) : new Date();
      this.attendees = meeting.attendees ?? '';
    } else {
      this.title = '';
      this.meetingDate = new Date();
      this.attendees = '';
    }
    this.visible.set(true);
  }

  save(): void {
    this.onSave.emit({
      title: this.title || undefined,
      meetingDate: this.meetingDate?.toISOString(),
      attendees: this.attendees || undefined,
    });
    this.visible.set(false);
  }
}
