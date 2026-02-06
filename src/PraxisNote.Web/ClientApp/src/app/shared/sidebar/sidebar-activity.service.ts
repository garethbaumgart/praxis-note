import { Injectable, inject, computed, Signal } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map, startWith } from 'rxjs';
import { TaskService } from '../../tasks/task.service';
import { NoteService } from '../../notes/note.service';
import { MeetingService } from '../../meetings/meeting.service';
import { AudioRecorderService } from '../../meetings/audio-recorder.service';

export type ContextSection = 'recent-notes' | 'due-soon' | 'recent-meetings' | 'recent-activity';

export interface ActivityItem {
  id: string;
  title: string;
  icon: string;
  type: 'note' | 'task' | 'meeting';
  route: string[];
  meta: string;
  metaUrgent?: boolean;
}

@Injectable({ providedIn: 'root' })
export class SidebarActivityService {
  private readonly router = inject(Router);
  private readonly taskService = inject(TaskService);
  private readonly noteService = inject(NoteService);
  private readonly meetingService = inject(MeetingService);
  readonly recorder = inject(AudioRecorderService);

  private readonly currentPath: Signal<string> = toSignal(
    this.router.events.pipe(
      filter((e): e is NavigationEnd => e instanceof NavigationEnd),
      map(() => this.router.url.split('?')[0].split('#')[0]),
      startWith(this.router.url.split('?')[0].split('#')[0])
    ),
    { initialValue: this.router.url.split('?')[0].split('#')[0] }
  );

  readonly contextSection = computed<ContextSection>(() => {
    const path = this.currentPath();
    if (path.startsWith('/notes')) return 'recent-notes';
    if (path.startsWith('/tasks')) return 'due-soon';
    if (path.startsWith('/meetings')) return 'recent-meetings';
    return 'recent-activity';
  });

  private static readonly CONTEXT_CONFIG: Record<ContextSection, { label: string; icon: string }> = {
    'recent-notes': { label: 'Recent Notes', icon: 'pi-file-edit' },
    'due-soon': { label: 'Due Soon', icon: 'pi-clock' },
    'recent-meetings': { label: 'Recent Meetings', icon: 'pi-comments' },
    'recent-activity': { label: 'Recent Activity', icon: 'pi-history' },
  };

  readonly contextLabel = computed(() =>
    SidebarActivityService.CONTEXT_CONFIG[this.contextSection()].label
  );

  readonly contextIcon = computed(() =>
    SidebarActivityService.CONTEXT_CONFIG[this.contextSection()].icon
  );

  readonly inProgressTasks = computed(() =>
    this.taskService.inProgressTasks().slice(0, 2)
  );

  readonly upNextTasks = computed(() =>
    this.taskService.todoTasks()
      .slice()
      .sort((a, b) => {
        // Priority tasks first
        if (a.isPriority !== b.isPriority) return a.isPriority ? -1 : 1;
        // Then by due date (earliest first, no-date last)
        if (a.dueDate !== b.dueDate) {
          if (!a.dueDate) return 1;
          if (!b.dueDate) return -1;
          return a.dueDate.localeCompare(b.dueDate);
        }
        // Then by position
        return a.position - b.position;
      })
      .slice(0, 2)
  );

  readonly recentNotes = computed<ActivityItem[]>(() => {
    return this.noteService.notes()
      .slice()
      .sort((a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime())
      .slice(0, 3)
      .map(n => ({
        id: n.id,
        title: this.extractNoteTitle(n.content),
        icon: 'pi-file-edit',
        type: 'note' as const,
        route: ['/notes', n.id],
        meta: this.timeAgo(n.updatedAt),
      }));
  });

  readonly dueSoonTasks = computed<ActivityItem[]>(() => {
    const now = new Date();
    const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
    return this.taskService.tasks()
      .filter(t => t.dueDate && t.status !== 'Done')
      .sort((a, b) => new Date(a.dueDate! + 'T00:00:00').getTime() - new Date(b.dueDate! + 'T00:00:00').getTime())
      .slice(0, 3)
      .map(t => {
        const due = new Date(t.dueDate! + 'T00:00:00');
        const diffDays = Math.round((due.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
        let meta = '';
        let metaUrgent = false;
        if (diffDays < 0) { meta = 'Overdue'; metaUrgent = true; }
        else if (diffDays === 0) { meta = 'Today'; metaUrgent = true; }
        else if (diffDays === 1) { meta = 'Tomorrow'; }
        else { meta = `${diffDays}d`; }
        return {
          id: t.id, title: t.title, icon: 'pi-check-square',
          type: 'task' as const, route: ['/tasks'], meta, metaUrgent,
        };
      });
  });

  readonly recentMeetings = computed<ActivityItem[]>(() => {
    return this.meetingService.meetings()
      .slice()
      .sort((a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime())
      .slice(0, 3)
      .map(m => ({
        id: m.id,
        title: m.title || 'Untitled Meeting',
        icon: 'pi-comments',
        type: 'meeting' as const,
        route: ['/meetings', m.id],
        meta: this.timeAgo(m.updatedAt),
      }));
  });

  readonly recentActivity = computed<ActivityItem[]>(() => {
    const notes: (ActivityItem & { _ts: number })[] = this.noteService.notes()
      .slice()
      .sort((a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime())
      .slice(0, 3)
      .map(n => ({
        id: n.id, title: this.extractNoteTitle(n.content), icon: 'pi-file-edit',
        type: 'note' as const, route: ['/notes', n.id],
        meta: this.timeAgo(n.updatedAt), _ts: new Date(n.updatedAt).getTime(),
      }));
    const tasks: (ActivityItem & { _ts: number })[] = this.taskService.tasks()
      .filter(t => t.status !== 'Done')
      .slice()
      .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
      .slice(0, 3)
      .map(t => ({
        id: t.id, title: t.title, icon: 'pi-check-square',
        type: 'task' as const, route: ['/tasks'],
        meta: this.timeAgo(t.createdAt), _ts: new Date(t.createdAt).getTime(),
      }));
    const meetings: (ActivityItem & { _ts: number })[] = this.meetingService.meetings()
      .slice()
      .sort((a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime())
      .slice(0, 3)
      .map(m => ({
        id: m.id, title: m.title || 'Untitled Meeting', icon: 'pi-comments',
        type: 'meeting' as const, route: ['/meetings', m.id],
        meta: this.timeAgo(m.updatedAt), _ts: new Date(m.updatedAt).getTime(),
      }));
    return [...notes, ...tasks, ...meetings]
      .sort((a, b) => b._ts - a._ts)
      .slice(0, 3)
      .map(({ _ts, ...item }) => item);
  });

  readonly contextItems = computed<ActivityItem[]>(() => {
    switch (this.contextSection()) {
      case 'recent-notes': return this.recentNotes();
      case 'due-soon': return this.dueSoonTasks();
      case 'recent-meetings': return this.recentMeetings();
      case 'recent-activity': return this.recentActivity();
    }
  });

  private extractNoteTitle(content: string): string {
    try {
      const parsed = JSON.parse(content);
      if (parsed?.content?.[0]) {
        return this.extractText(parsed.content[0]).trim().substring(0, 50) || 'Untitled';
      }
    } catch {
      return content.split('\n')[0]?.substring(0, 50) || 'Untitled';
    }
    return 'Untitled';
  }

  private extractText(node: { type: string; text?: string; content?: unknown[] }): string {
    if (node.type === 'text' && node.text) return node.text;
    if (!node.content) return '';
    return (node.content as { type: string; text?: string; content?: unknown[] }[])
      .map(child => this.extractText(child)).join('');
  }

  private timeAgo(isoDate: string): string {
    const ts = new Date(isoDate).getTime();
    if (isNaN(ts)) return '';
    const diff = Date.now() - ts;
    const mins = Math.floor(diff / 60000);
    if (mins < 1) return 'now';
    if (mins < 60) return `${mins}m`;
    const hours = Math.floor(mins / 60);
    if (hours < 24) return `${hours}h`;
    const days = Math.floor(hours / 24);
    if (days < 7) return `${days}d`;
    return `${Math.floor(days / 7)}w`;
  }
}
