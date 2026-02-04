import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ExtractedCalendarEvent, ScreenshotExtractionResult } from './screenshot-import.model';

export type ImportState = 'idle' | 'extracting' | 'preview' | 'importing' | 'done' | 'error';

@Injectable({ providedIn: 'root' })
export class ScreenshotImportService {
  private readonly http = inject(HttpClient);

  readonly state = signal<ImportState>('idle');
  readonly events = signal<ExtractedCalendarEvent[]>([]);
  readonly error = signal<string | null>(null);
  readonly importedCount = signal(0);

  reset(): void {
    this.state.set('idle');
    this.events.set([]);
    this.error.set(null);
    this.importedCount.set(0);
  }

  extractFromImage(base64Image: string, mediaType: string): void {
    this.state.set('extracting');
    this.error.set(null);

    this.http.post<ScreenshotExtractionResult>('/api/meetings/extract-from-screenshot', {
      base64Image,
      mediaType,
    }).subscribe({
      next: result => {
        if (result.events.length === 0) {
          this.error.set('No meetings found in the screenshot. Try a clearer image of your calendar.');
          this.state.set('error');
          return;
        }
        this.events.set(result.events.map(e => ({ ...e, selected: true })));
        this.state.set('preview');
      },
      error: () => {
        this.error.set('Failed to extract meetings from screenshot. Please try again.');
        this.state.set('error');
      },
    });
  }

  toggleEvent(index: number): void {
    this.events.update(events =>
      events.map((e, i) => i === index ? { ...e, selected: !e.selected } : e)
    );
  }

  toggleAll(selected: boolean): void {
    this.events.update(events => events.map(e => ({ ...e, selected })));
  }

  importSelected(onCreated: (id: string) => void): void {
    const selected = this.events().filter(e => e.selected);
    if (selected.length === 0) return;

    this.state.set('importing');
    this.importedCount.set(0);
    let completed = 0;

    for (const event of selected) {
      this.http.post<{ id: string }>('/api/meetings', {
        title: event.title,
        meetingDate: event.startTime,
        attendees: event.attendees,
      }).subscribe({
        next: result => {
          completed++;
          this.importedCount.set(completed);
          onCreated(result.id);
          if (completed === selected.length) {
            this.state.set('done');
          }
        },
        error: () => {
          completed++;
          this.importedCount.set(completed);
          if (completed === selected.length) {
            this.state.set('done');
          }
        },
      });
    }
  }
}
