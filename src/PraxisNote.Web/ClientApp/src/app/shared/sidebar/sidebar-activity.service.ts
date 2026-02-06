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
    this.taskService.todoTasks().slice(0, 2)
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
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return this.taskService.tasks()
      .filter(t => t.dueDate && t.status !== 'Done')
      .sort((a, b) => new Date(a.dueDate!).getTime() - new Date(b.dueDate!).getTime())
      .slice(0, 3)
      .map(t => {
        const due = new Date(t.dueDate!);
        due.setHours(0, 0, 0, 0);
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
    const meetings: (ActivityItem & { _ts: number })[] = this.meetingService.meetings()
      .slice()
      .sort((a, b) => new Date(b.updatedAt).getTime() - new Date(a.updatedAt).getTime())
      .slice(0, 3)
      .map(m => ({
        id: m.id, title: m.title || 'Untitled Meeting', icon: 'pi-comments',
        type: 'meeting' as const, route: ['/meetings', m.id],
        meta: this.timeAgo(m.updatedAt), _ts: new Date(m.updatedAt).getTime(),
      }));
    return [...notes, ...meetings]
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
    const diff = Date.now() - new Date(isoDate).getTime();
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
