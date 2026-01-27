import { Injectable, inject, signal, computed, DestroyRef } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Meeting, MeetingGroup } from './meeting.model';
import { ToastService } from '../shared/services/toast.service';

interface PendingDeletion {
  meeting: Meeting;
  timeoutId: ReturnType<typeof setTimeout>;
  index: number;
}

@Injectable({ providedIn: 'root' })
export class MeetingService {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  private readonly toast = inject(ToastService);

  private readonly pendingDeletions = new Map<string, PendingDeletion>();

  private readonly _meetings = signal<Meeting[]>([]);
  private readonly _loading = signal(false);
  private readonly _initialLoadComplete = signal(false);
  private readonly _searchQuery = signal('');

  readonly meetings = this._meetings.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly initialLoadComplete = this._initialLoadComplete.asReadonly();
  readonly searchQuery = this._searchQuery.asReadonly();

  readonly filteredMeetings = computed(() => {
    const query = this._searchQuery().toLowerCase().trim();
    const meetings = this._meetings();

    if (!query) return meetings;

    return meetings.filter(
      m =>
        (m.title?.toLowerCase().includes(query) ?? false) ||
        (m.attendees?.toLowerCase().includes(query) ?? false)
    );
  });

  readonly groupedMeetings = computed(() => {
    const meetings = this.filteredMeetings();
    return this.groupMeetingsByDate(meetings);
  });

  readonly meetingCount = computed(() => this._meetings().length);

  setSearchQuery(query: string): void {
    this._searchQuery.set(query);
  }

  loadMeetings(): void {
    this._loading.set(true);
    this.http.get<Meeting[]>('/api/meetings').subscribe({
      next: meetings => {
        this._meetings.set(meetings);
        this._loading.set(false);
        this._initialLoadComplete.set(true);
      },
      error: () => {
        this._loading.set(false);
        this._initialLoadComplete.set(true);
      },
    });
  }

  createMeeting(title?: string, meetingDate?: string, attendees?: string): void {
    const tempId = crypto.randomUUID();
    const now = new Date().toISOString();
    const newMeeting: Meeting = {
      id: tempId,
      title: title ?? null,
      meetingDate: meetingDate ?? now,
      attendees: attendees ?? null,
      status: 'Draft',
      createdAt: now,
      updatedAt: now,
    };

    // Optimistic update - add meeting immediately at the beginning
    this._meetings.update(meetings => [newMeeting, ...meetings]);

    this.http.post<{ id: string }>('/api/meetings', { title, meetingDate, attendees }).subscribe({
      next: result => {
        // Update with real ID from server
        this._meetings.update(meetings =>
          meetings.map(m => (m.id === tempId ? { ...m, id: result.id } : m))
        );
      },
      error: () => {
        this.toast.error('Failed to create meeting');
        // Remove the optimistically added meeting
        this._meetings.update(meetings => meetings.filter(m => m.id !== tempId));
      },
    });
  }

  updateMeeting(id: string, title?: string, meetingDate?: string, attendees?: string): void {
    // Optimistic update
    this._meetings.update(meetings =>
      meetings.map(m =>
        m.id === id
          ? { ...m, title: title ?? m.title, meetingDate: meetingDate ?? m.meetingDate, attendees: attendees ?? m.attendees, updatedAt: new Date().toISOString() }
          : m
      )
    );

    this.http.put(`/api/meetings/${id}`, { title, meetingDate, attendees }).subscribe({
      error: () => {
        this.toast.error('Failed to update meeting');
        this.loadMeetings();
      },
    });
  }

  deleteMeeting(id: string): void {
    this._meetings.update(meetings => meetings.filter(m => m.id !== id));

    this.http.delete(`/api/meetings/${id}`).subscribe({
      error: () => {
        this.toast.error('Failed to delete meeting');
        this.loadMeetings();
      },
    });
  }

  deleteMeetingWithUndo(id: string, undoTimeoutMs = 5000): Meeting | null {
    const meetings = this._meetings();
    const index = meetings.findIndex(m => m.id === id);
    if (index === -1) return null;

    const meeting = meetings[index];

    this.cancelPendingDeletion(id);

    this._meetings.update(meetings => meetings.filter(m => m.id !== id));

    const timeoutId = setTimeout(() => {
      this.commitDeletion(id);
    }, undoTimeoutMs);

    this.pendingDeletions.set(id, { meeting, timeoutId, index });

    return meeting;
  }

  undoDelete(id: string): boolean {
    const pending = this.pendingDeletions.get(id);
    if (!pending) return false;

    clearTimeout(pending.timeoutId);
    this.pendingDeletions.delete(id);

    this._meetings.update(meetings => {
      const clampedIndex = Math.min(pending.index, meetings.length);
      return [
        ...meetings.slice(0, clampedIndex),
        pending.meeting,
        ...meetings.slice(clampedIndex),
      ];
    });

    return true;
  }

  private commitDeletion(id: string): void {
    const pending = this.pendingDeletions.get(id);
    if (!pending) return;

    this.pendingDeletions.delete(id);

    this.http.delete(`/api/meetings/${id}`).subscribe({
      error: () => {
        this.toast.error('Failed to delete meeting');
        this._meetings.update(meetings => {
          const clampedIndex = Math.min(pending.index, meetings.length);
          return [
            ...meetings.slice(0, clampedIndex),
            pending.meeting,
            ...meetings.slice(clampedIndex),
          ];
        });
      },
    });
  }

  private cancelPendingDeletion(id: string): void {
    const pending = this.pendingDeletions.get(id);
    if (pending) {
      clearTimeout(pending.timeoutId);
      this.pendingDeletions.delete(id);
    }
  }

  private groupMeetingsByDate(meetings: Meeting[]): MeetingGroup[] {
    const groups = new Map<string, Meeting[]>();
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const yesterday = new Date(today);
    yesterday.setDate(yesterday.getDate() - 1);

    for (const meeting of meetings) {
      const meetingDate = meeting.meetingDate ? new Date(meeting.meetingDate) : new Date(meeting.createdAt);
      meetingDate.setHours(0, 0, 0, 0);

      const dateKey = meetingDate.toISOString().split('T')[0];

      if (!groups.has(dateKey)) {
        groups.set(dateKey, []);
      }
      groups.get(dateKey)!.push(meeting);
    }

    // Sort groups by date descending
    const sortedEntries = Array.from(groups.entries()).sort(
      (a, b) => new Date(b[0]).getTime() - new Date(a[0]).getTime()
    );

    return sortedEntries.map(([dateKey, meetings]) => {
      const date = new Date(dateKey);
      const { label, subLabel } = this.formatDateLabel(date, today, yesterday);
      return { label, subLabel, meetings };
    });
  }

  private formatDateLabel(date: Date, today: Date, yesterday: Date): { label: string; subLabel: string } {
    const dayNames = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
    const monthNames = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

    const subLabel = `${dayNames[date.getDay()]}, ${monthNames[date.getMonth()]} ${date.getDate()}`;

    if (date.getTime() === today.getTime()) {
      return { label: 'Today', subLabel };
    }
    if (date.getTime() === yesterday.getTime()) {
      return { label: 'Yesterday', subLabel };
    }

    // Check if same week
    const weekStart = new Date(today);
    weekStart.setDate(today.getDate() - today.getDay());
    if (date >= weekStart) {
      return { label: dayNames[date.getDay()], subLabel: `${monthNames[date.getMonth()]} ${date.getDate()}` };
    }

    // Older dates
    return { label: `${monthNames[date.getMonth()]} ${date.getDate()}`, subLabel: `${date.getFullYear()}` };
  }
}
