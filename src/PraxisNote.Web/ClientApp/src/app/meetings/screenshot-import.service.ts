import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { firstValueFrom } from 'rxjs';
import { ExtractedCalendarEvent, ScreenshotExtractionResult } from './screenshot-import.model';
import { ToastService } from '../shared/services/toast.service';

export type ImportState = 'idle' | 'extracting' | 'preview' | 'importing' | 'done' | 'error';

export interface ScreenshotAiError {
  error: string;
  message: string;
  settingsUrl?: string;
}

@Injectable({ providedIn: 'root' })
export class ScreenshotImportService {
  private readonly http = inject(HttpClient);
  private readonly toast = inject(ToastService);

  readonly state = signal<ImportState>('idle');
  readonly events = signal<ExtractedCalendarEvent[]>([]);
  readonly error = signal<string | null>(null);
  readonly aiError = signal<ScreenshotAiError | null>(null);
  readonly importedCount = signal(0);

  reset(): void {
    this.state.set('idle');
    this.events.set([]);
    this.error.set(null);
    this.aiError.set(null);
    this.importedCount.set(0);
  }

  extractFromImage(base64Image: string, mediaType: string): void {
    this.state.set('extracting');
    this.error.set(null);
    this.aiError.set(null);

    let timeZone: string | undefined;
    try {
      timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone;
    } catch {
      timeZone = undefined;
    }
    this.http.post<ScreenshotExtractionResult>('/api/meetings/extract-from-screenshot', {
      base64Image,
      mediaType,
      timeZone,
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
      error: (err: { error?: { error?: string; message?: string; settingsUrl?: string } }) => {
        const aiErrorCode = err.error?.error;
        if (aiErrorCode === 'no_ai_key' || aiErrorCode === 'ai_key_invalid') {
          this.aiError.set(err.error as ScreenshotAiError);
          this.error.set(err.error?.message ?? 'Failed to extract meetings from screenshot. Please try again.');
          this.state.set('error');
        } else if (aiErrorCode === 'ai_rate_limited' || aiErrorCode === 'ai_provider_error') {
          this.toast.error(err.error?.message ?? 'Failed to extract meetings from screenshot. Please try again.');
          this.state.set('idle');
        } else {
          this.error.set(err.error?.message ?? 'Failed to extract meetings from screenshot. Please try again.');
          this.state.set('error');
        }
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

  async importSelected(onCreated: (id: string) => void): Promise<void> {
    const selected = this.events().filter(e => e.selected);
    if (selected.length === 0) return;

    this.state.set('importing');
    this.importedCount.set(0);
    let failures = 0;

    for (let i = 0; i < selected.length; i++) {
      const event = selected[i];
      try {
        const result = await firstValueFrom(this.http.post<{ id: string }>('/api/meetings', {
          title: event.title,
          meetingDate: event.startTime,
          attendees: event.attendees,
        }));
        onCreated(result.id);
      } catch {
        failures++;
      }
      this.importedCount.set(i + 1);
    }

    if (failures > 0 && failures < selected.length) {
      this.error.set(`${failures} meeting(s) failed to import.`);
    } else if (failures === selected.length) {
      this.error.set('All meetings failed to import. Please try again.');
      this.state.set('error');
      return;
    }
    this.state.set('done');
  }
}
