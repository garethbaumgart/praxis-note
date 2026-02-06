import { Injectable, inject, computed } from '@angular/core';
import { TaskService } from '../tasks/task.service';
import { NoteService } from '../notes/note.service';
import { MeetingService } from '../meetings/meeting.service';
import { Task } from '../tasks/task.model';
import { Note } from '../notes/note.model';
import { formatShortDate } from '../shared/date-utils';

export interface MeetingChip {
  id: string;
  title: string;
  time: string;
  dayLabel: string;
  status: string;
}

export interface RecentItem {
  id: string;
  type: 'note' | 'meeting';
  title: string;
  subtitle: string;
  icon: string;
  updatedAt: string;
  timeAgo: string;
}

@Injectable({ providedIn: 'root' })
export class HomeDashboardService {
  private readonly taskService = inject(TaskService);
  private readonly noteService = inject(NoteService);
  private readonly meetingService = inject(MeetingService);

  // --- Priority banner ---

  readonly overdueTasks = computed<Task[]>(() => {
    const now = new Date();
    now.setHours(0, 0, 0, 0);
    return this.taskService.tasks()
      .filter(t => {
        if (!t.dueDate || t.status === 'Done') return false;
        const due = new Date(t.dueDate + 'T00:00:00');
        return due < now;
      })
      .sort((a, b) => new Date(a.dueDate! + 'T00:00:00').getTime() - new Date(b.dueDate! + 'T00:00:00').getTime());
  });

  readonly dueSoonTasks = computed<Task[]>(() => {
    const now = new Date();
    now.setHours(0, 0, 0, 0);
    const twoDaysFromNow = new Date(now);
    twoDaysFromNow.setDate(twoDaysFromNow.getDate() + 2);
    return this.taskService.tasks()
      .filter(t => {
        if (!t.dueDate || t.status === 'Done') return false;
        const due = new Date(t.dueDate + 'T00:00:00');
        return due >= now && due <= twoDaysFromNow;
      })
      .sort((a, b) => new Date(a.dueDate! + 'T00:00:00').getTime() - new Date(b.dueDate! + 'T00:00:00').getTime());
  });

  readonly hasPriorityBanner = computed(() =>
    this.overdueTasks().length > 0 || this.dueSoonTasks().length > 0
  );

  readonly prioritySummary = computed(() => {
    const overdue = this.overdueTasks().length;
    const dueSoon = this.dueSoonTasks().length;
    if (overdue > 0) {
      return `${overdue} overdue task${overdue > 1 ? 's' : ''}`;
    }
    return `${dueSoon} task${dueSoon > 1 ? 's' : ''} due soon`;
  });

  readonly priorityDetail = computed(() => {
    const overdue = this.overdueTasks();
    const dueSoon = this.dueSoonTasks();
    if (overdue.length > 0) {
      return overdue[0].title;
    }
    if (dueSoon.length > 0) {
      return dueSoon[0].title;
    }
    return '';
  });

  // --- My Tasks widget ---

  readonly inProgressTasks = computed(() =>
    this.taskService.inProgressTasks().slice(0, 4)
  );

  readonly upNextTasks = computed(() =>
    this.taskService.todoTasks().slice(0, 4)
  );

  // --- Upcoming meetings ---

  readonly upcomingMeetings = computed<MeetingChip[]>(() => {
    const now = new Date();
    return this.meetingService.meetings()
      .filter(m => {
        if (!m.meetingDate) return false;
        return new Date(m.meetingDate) > now;
      })
      .sort((a, b) => new Date(a.meetingDate!).getTime() - new Date(b.meetingDate!).getTime())
      .slice(0, 3)
      .map(m => ({
        id: m.id,
        title: m.title ?? 'Untitled Meeting',
        time: this.formatMeetingTime(m.meetingDate!),
        dayLabel: this.getDayLabel(m.meetingDate!),
        status: m.status,
      }));
  });

  readonly hasUpcomingMeetings = computed(() =>
    this.upcomingMeetings().length > 0
  );

  // --- Recent items ("Pick Up Where You Left Off") ---

  readonly recentItems = computed<RecentItem[]>(() => {
    const notes: RecentItem[] = this.noteService.notes().map(n => ({
      id: n.id,
      type: 'note' as const,
      title: this.extractNoteTitle(n),
      subtitle: this.extractNoteSubtitle(n),
      icon: 'pi pi-file-edit',
      updatedAt: n.updatedAt,
      timeAgo: this.timeAgo(n.updatedAt),
    }));

    const meetings: RecentItem[] = this.meetingService.meetings().map(m => ({
      id: m.id,
      type: 'meeting' as const,
      title: m.title ?? 'Untitled Meeting',
      subtitle: m.attendees ?? '',
      icon: 'pi pi-microphone',
      updatedAt: m.updatedAt,
      timeAgo: this.timeAgo(m.updatedAt),
    }));

    return [...notes, ...meetings]
      .sort((a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime())
      .slice(0, 5);
  });

  // --- Data loading ---

  loadAllData(): void {
    this.taskService.loadTasks();
    this.noteService.loadNotes();
    this.meetingService.loadMeetings();
  }

  // --- Helpers ---

  private getDayLabel(dateStr: string): string {
    const date = new Date(dateStr);
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const tomorrow = new Date(today);
    tomorrow.setDate(tomorrow.getDate() + 1);

    const meetingDay = new Date(date);
    meetingDay.setHours(0, 0, 0, 0);

    if (meetingDay.getTime() === today.getTime()) return 'Today';
    if (meetingDay.getTime() === tomorrow.getTime()) return 'Tomorrow';
    return formatShortDate(date);
  }

  private formatMeetingTime(dateStr: string): string {
    const date = new Date(dateStr);
    return date.toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' });
  }

  private extractNoteTitle(note: Note): string {
    if (!note.content) return 'Untitled Note';
    const firstLine = note.content.split('\n')[0].trim();
    if (!firstLine) return 'Untitled Note';
    // Strip markdown heading markers
    const stripped = firstLine.replace(/^#+\s*/, '');
    return stripped.length > 60 ? stripped.substring(0, 60) + '...' : stripped;
  }

  private extractNoteSubtitle(note: Note): string {
    if (!note.content) return '';
    const lines = note.content.split('\n').filter(l => l.trim());
    if (lines.length < 2) return '';
    const secondLine = lines[1].trim().replace(/^#+\s*/, '');
    return secondLine.length > 80 ? secondLine.substring(0, 80) + '...' : secondLine;
  }

  private timeAgo(dateStr: string): string {
    const now = new Date();
    const date = new Date(dateStr);
    const diffMs = now.getTime() - date.getTime();
    const diffMin = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMin / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMin < 1) return 'Just now';
    if (diffMin < 60) return `${diffMin}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays === 1) return 'Yesterday';
    if (diffDays < 7) return `${diffDays}d ago`;
    return formatShortDate(date);
  }
}
