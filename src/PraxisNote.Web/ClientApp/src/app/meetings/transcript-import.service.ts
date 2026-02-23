import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { MeetingService } from './meeting.service';

export type TranscriptImportState = 'idle' | 'parsing' | 'preview' | 'importing' | 'done' | 'error';

export interface ParsedMeetingActionItem {
  description: string;
  assignee: string | null;
}

export interface ParsedMeeting {
  title: string | null;
  meetingDate: string | null;
  attendees: string | null;
  summary: string | null;
  keyPoints: string[] | null;
  decisions: string[] | null;
  actionItems: ParsedMeetingActionItem[] | null;
  suggestedTags: string[];
  transcript: string;
  isComplete: boolean;
  warning: string | null;
  selected: boolean;
  isDuplicate: boolean;
}

interface ParseResponse {
  title: string | null;
  meetingDate: string | null;
  attendees: string | null;
  summary: string | null;
  keyPoints: string[] | null;
  decisions: string[] | null;
  actionItems: ParsedMeetingActionItem[] | null;
  suggestedTags: string[];
  transcript: string;
  isComplete: boolean;
  warning: string | null;
}

interface ConfirmResponse {
  importedCount: number;
  totalActionItems: number;
}

@Injectable({ providedIn: 'root' })
export class TranscriptImportService {
  private readonly http = inject(HttpClient);
  private readonly meetingService = inject(MeetingService);

  readonly state = signal<TranscriptImportState>('idle');
  readonly parsedMeetings = signal<ParsedMeeting[]>([]);
  readonly error = signal<string | null>(null);
  readonly importedCount = signal(0);
  readonly totalActionItems = signal(0);
  readonly parseProgress = signal<{ current: number; total: number }>({ current: 0, total: 0 });

  readonly needsReviewCount = computed(() =>
    this.parsedMeetings().filter(m => !m.isComplete && !m.isDuplicate).length
  );

  parseText(text: string): void {
    this.state.set('parsing');
    this.error.set(null);
    this.parseProgress.set({ current: 1, total: 1 });

    const formData = new FormData();
    formData.append('text', text);

    this.http.post<ParseResponse>('/api/meetings/import/parse', formData).subscribe({
      next: result => {
        const meeting: ParsedMeeting = {
          ...result,
          selected: true,
          isDuplicate: false,
        };
        meeting.isDuplicate = this.checkDuplicate(meeting);
        if (meeting.isDuplicate) meeting.selected = false;
        this.parsedMeetings.set([meeting]);
        this.state.set('preview');
      },
      error: () => {
        this.error.set('Failed to parse transcript. Please try again.');
        this.state.set('error');
      },
    });
  }

  async parseFiles(files: FileList): Promise<void> {
    this.state.set('parsing');
    this.error.set(null);
    const total = files.length;
    this.parseProgress.set({ current: 0, total });

    const results: ParsedMeeting[] = [];
    let failures = 0;

    for (let i = 0; i < total; i++) {
      this.parseProgress.set({ current: i + 1, total });
      const file = files[i];

      try {
        const formData = new FormData();
        formData.append('file', file);

        const result = await firstValueFrom(
          this.http.post<ParseResponse>('/api/meetings/import/parse', formData)
        );

        const meeting: ParsedMeeting = {
          ...result,
          selected: true,
          isDuplicate: false,
        };
        meeting.isDuplicate = this.checkDuplicate(meeting);
        if (meeting.isDuplicate) meeting.selected = false;
        results.push(meeting);
      } catch {
        failures++;
      }
    }

    if (results.length === 0) {
      this.error.set('All files failed to parse. Please check the file format and try again.');
      this.state.set('error');
      return;
    }

    this.parsedMeetings.set(results);
    this.state.set('preview');
  }

  toggleMeeting(index: number): void {
    this.parsedMeetings.update(meetings =>
      meetings.map((m, i) => i === index ? { ...m, selected: !m.selected } : m)
    );
  }

  toggleAll(selected: boolean): void {
    this.parsedMeetings.update(meetings =>
      meetings.map(m => m.isDuplicate ? m : { ...m, selected })
    );
  }

  async confirmImport(): Promise<void> {
    const selected = this.parsedMeetings().filter(m => m.selected && !m.isDuplicate);
    if (selected.length === 0) return;

    this.state.set('importing');
    this.importedCount.set(0);

    try {
      const meetings = selected.map(m => ({
        title: m.title,
        meetingDate: m.meetingDate,
        attendees: m.attendees,
        transcript: m.transcript,
        summary: m.summary,
        keyPoints: m.keyPoints ? JSON.stringify(m.keyPoints) : null,
        decisions: m.decisions ? JSON.stringify(m.decisions) : null,
        actionItems: m.actionItems ?? [],
        suggestedTags: m.suggestedTags,
      }));

      const result = await firstValueFrom(
        this.http.post<ConfirmResponse>('/api/meetings/import/confirm', { meetings })
      );

      this.importedCount.set(result.importedCount);
      this.totalActionItems.set(result.totalActionItems);
      this.state.set('done');
    } catch {
      this.error.set('Failed to import meetings. Please try again.');
      this.state.set('error');
    }
  }

  reset(): void {
    this.state.set('idle');
    this.parsedMeetings.set([]);
    this.error.set(null);
    this.importedCount.set(0);
    this.totalActionItems.set(0);
    this.parseProgress.set({ current: 0, total: 0 });
  }

  private checkDuplicate(parsed: ParsedMeeting): boolean {
    if (!parsed.title) return false;

    const existingMeetings = this.meetingService.meetings();
    const parsedTitle = parsed.title.toLowerCase().trim();

    return existingMeetings.some(existing => {
      if (!existing.title) return false;
      if (existing.title.toLowerCase().trim() !== parsedTitle) return false;

      // If both have dates, check within 1 hour
      if (parsed.meetingDate && existing.meetingDate) {
        const parsedDate = new Date(parsed.meetingDate);
        const existingDate = new Date(existing.meetingDate);
        if (isNaN(parsedDate.getTime()) || isNaN(existingDate.getTime())) return false;
        const diffMs = Math.abs(parsedDate.getTime() - existingDate.getTime());
        return diffMs <= 60 * 60 * 1000; // 1 hour
      }

      // If neither has a date, title match alone is enough
      if (!parsed.meetingDate && !existing.meetingDate) return true;

      return false;
    });
  }
}
