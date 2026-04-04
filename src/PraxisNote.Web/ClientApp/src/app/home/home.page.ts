import { Component, inject, computed, signal, OnInit, OnDestroy } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../auth';
import { NoteService } from '../notes/note.service';
import { HomeDashboardService } from './home-dashboard.service';
import { GreetingService } from './greeting.service';
import { OutstandingActionItemsComponent } from './outstanding-action-items.component';
import { PageContentComponent } from '../shared/components/page-content.component';
import { ContextualHeaderService } from '../shared/services/contextual-header.service';
import { ProfileService } from '../profiles/profile.service';

@Component({
  selector: 'app-home-page',
  standalone: true,
  imports: [RouterLink, OutstandingActionItemsComponent, PageContentComponent],
  template: `
    <app-page-content>
      <h1 class="sr-only">Home</h1>

      <!-- 1. Greeting -->
      <section class="mb-6 animate-fade-in">
        <p class="text-lg font-semibold text-foreground">
          {{ greeting() }}
        </p>
        <p class="text-foreground-muted text-sm mt-1">{{ todayDate() }}</p>
      </section>

      <!-- Fresh start empty state for new profiles -->
      @if (dashboard.isProfileEmpty() && profileService.hasMultipleProfiles()) {
        <div class="text-center py-16 animate-fade-in-delay-1">
          <i class="pi pi-inbox text-4xl text-foreground-muted mb-4" aria-hidden="true"></i>
          <p class="text-lg font-semibold text-foreground mb-2">Fresh start!</p>
          <p class="text-sm text-foreground-muted">This profile is empty. Start by creating a note or task.</p>
        </div>
      }

      <!-- 2. Priority / Overdue Banner -->
      @if (dashboard.hasPriorityBanner()) {
        <section
          class="mb-5 animate-fade-in-delay-1 priority-banner"
          role="alert"
          aria-live="polite">
          <div class="flex items-center gap-3">
            <div class="flex-shrink-0 w-8 h-8 rounded-lg bg-overdue flex items-center justify-center">
              <i class="pi pi-exclamation-triangle text-overdue-foreground text-sm" aria-hidden="true"></i>
            </div>
            <div class="flex-1 min-w-0">
              <p class="text-sm font-semibold text-foreground">{{ dashboard.prioritySummary() }}</p>
              <p class="text-xs text-foreground-secondary truncate">{{ dashboard.priorityDetail() }}</p>
            </div>
            <a
              routerLink="/tasks"
              class="text-sm font-medium text-overdue-foreground hover:underline flex-shrink-0"
              aria-label="View overdue tasks">
              View <i class="pi pi-arrow-right text-xs ml-0.5" aria-hidden="true"></i>
            </a>
          </div>
        </section>
      }

      <!-- 3. Quick Action Buttons -->
      <section class="grid grid-cols-3 gap-3 mb-5 animate-fade-in-delay-2">
        <button
          type="button"
          class="quick-action group"
          aria-label="Create new note"
          (click)="newNote()">
          <div class="w-9 h-9 rounded-lg bg-archive flex items-center justify-center mb-2 group-hover:scale-105 transition-transform">
            <i class="pi pi-file-edit text-archive-foreground text-sm" aria-hidden="true"></i>
          </div>
          <span class="text-sm font-medium text-foreground">New Note</span>
        </button>

        <button
          type="button"
          class="quick-action group"
          aria-label="Go to tasks board"
          (click)="newTask()">
          <div class="w-9 h-9 rounded-lg bg-todo flex items-center justify-center mb-2 group-hover:scale-105 transition-transform">
            <i class="pi pi-check-square text-todo-foreground text-sm" aria-hidden="true"></i>
          </div>
          <span class="text-sm font-medium text-foreground">New Task</span>
        </button>

        <button
          type="button"
          class="quick-action group"
          aria-label="Start new meeting"
          (click)="startRecording()">
          <div class="w-9 h-9 rounded-lg bg-accent flex items-center justify-center mb-2 group-hover:scale-105 transition-transform">
            <i class="pi pi-microphone text-accent-foreground text-sm" aria-hidden="true"></i>
          </div>
          <span class="text-sm font-medium text-foreground">Record</span>
        </button>
      </section>

      <!-- 4. Upcoming Meetings Banner -->
      @if (dashboard.hasUpcomingMeetings()) {
        <section class="mb-5 animate-fade-in-delay-3 meetings-banner">
          <div class="flex items-center gap-2 mb-3">
            <i class="pi pi-calendar text-archive-foreground text-sm" aria-hidden="true"></i>
            <h2 class="text-sm font-semibold text-foreground">Upcoming Meetings</h2>
          </div>
          <div class="flex flex-col sm:flex-row gap-2">
            @for (meeting of dashboard.upcomingMeetings(); track meeting.id) {
              <button
                type="button"
                class="meeting-chip"
                [attr.aria-label]="'View meeting: ' + meeting.title"
                (click)="goToMeeting(meeting.id)">
                <span class="meeting-chip-time">{{ meeting.time }}</span>
                <span class="meeting-chip-divider" aria-hidden="true"></span>
                <span class="meeting-chip-info">
                  <span class="meeting-chip-title">{{ meeting.title }}</span>
                  <span class="meeting-chip-day">{{ meeting.dayLabel }}</span>
                </span>
              </button>
            }
          </div>
        </section>
      }

      <!-- 5. Two-Column Widgets -->
      <section class="grid grid-cols-1 lg:grid-cols-2 gap-4 mb-5 animate-fade-in-delay-4">

        <!-- Left: My Tasks -->
        <div class="widget-card">
          <div class="flex items-center justify-between mb-3">
            <h2 class="text-sm font-semibold text-foreground">My Tasks</h2>
            <a
              routerLink="/tasks"
              class="text-xs font-medium text-accent-foreground hover:underline"
              aria-label="View all tasks">
              View all <i class="pi pi-arrow-right text-[10px] ml-0.5" aria-hidden="true"></i>
            </a>
          </div>

          @if (dashboard.inProgressTasks().length === 0 && dashboard.upNextTasks().length === 0) {
            <div class="py-8 text-center">
              <i class="pi pi-check-square text-2xl text-foreground-muted mb-2" aria-hidden="true"></i>
              <p class="text-sm text-foreground-muted">No tasks yet</p>
              <button
                type="button"
                class="text-xs text-accent-foreground font-medium mt-2 hover:underline"
                aria-label="Go to tasks board"
                (click)="newTask()">
                Create a task <i class="pi pi-arrow-right text-[10px] ml-0.5" aria-hidden="true"></i>
              </button>
            </div>
          } @else {
            @if (dashboard.inProgressTasks().length > 0) {
              <div class="mb-3">
                <p class="text-xs font-medium text-inprogress-foreground uppercase tracking-wide mb-1.5">In Progress</p>
                @for (task of dashboard.inProgressTasks(); track task.id) {
                  <button
                    type="button"
                    class="task-row"
                    [attr.aria-label]="'Go to task: ' + task.title + (task.isPriority ? ' (priority)' : '')"
                    (click)="navigateToTask(task.id)">
                    <span class="task-status-dot bg-inprogress-foreground" aria-hidden="true"></span>
                    <span class="flex-1 text-sm text-foreground truncate">{{ task.title }}</span>
                    @if (task.isPriority) {
                      <i class="pi pi-bolt text-inprogress-foreground text-xs flex-shrink-0" aria-hidden="true"></i>
                    }
                  </button>
                }
              </div>
            }

            @if (dashboard.upNextTasks().length > 0) {
              <div>
                <p class="text-xs font-medium text-todo-foreground uppercase tracking-wide mb-1.5">Up Next</p>
                @for (task of dashboard.upNextTasks(); track task.id) {
                  <button
                    type="button"
                    class="task-row"
                    [attr.aria-label]="'Go to task: ' + task.title + (task.isPriority ? ' (priority)' : '')"
                    (click)="navigateToTask(task.id)">
                    <span class="task-status-dot bg-todo-foreground" aria-hidden="true"></span>
                    <span class="flex-1 text-sm text-foreground truncate">{{ task.title }}</span>
                    @if (task.isPriority) {
                      <i class="pi pi-bolt text-todo-foreground text-xs flex-shrink-0" aria-hidden="true"></i>
                    }
                  </button>
                }
              </div>
            }
          }
        </div>

        <!-- Right: Pick Up Where You Left Off -->
        <div class="widget-card">
          <div class="flex items-center justify-between mb-3">
            <h2 class="text-sm font-semibold text-foreground">Pick Up Where You Left Off</h2>
          </div>

          @if (dashboard.recentItems().length === 0) {
            <div class="py-8 text-center">
              <i class="pi pi-history text-2xl text-foreground-muted mb-2" aria-hidden="true"></i>
              <p class="text-sm text-foreground-muted">No recent activity</p>
            </div>
          } @else {
            @for (item of dashboard.recentItems(); track (item.type + ':' + item.id)) {
              <button
                type="button"
                class="recent-row"
                [attr.aria-label]="'Open ' + item.type + ': ' + item.title"
                (click)="goToRecentItem(item)">
                <div class="flex-shrink-0 w-7 h-7 rounded-md flex items-center justify-center"
                     [class.bg-archive]="item.type === 'note'"
                     [class.bg-accent]="item.type === 'meeting'">
                  <i [class]="item.icon"
                     [class.text-archive-foreground]="item.type === 'note'"
                     [class.text-accent-foreground]="item.type === 'meeting'"
                     class="text-xs" aria-hidden="true"></i>
                </div>
                <div class="flex-1 min-w-0">
                  <p class="text-sm text-foreground truncate">{{ item.title }}</p>
                  @if (item.subtitle) {
                    <p class="text-xs text-foreground-muted truncate">{{ item.subtitle }}</p>
                  }
                </div>
                <span class="text-xs text-foreground-muted flex-shrink-0">{{ item.timeAgo }}</span>
              </button>
            }
          }
        </div>
      </section>

      <!-- 6. Outstanding Action Items -->
      @if (dashboard.hasActionItems() || dashboard.actionItemsLoading() || dashboard.actionItemsError()) {
        <section class="mb-5 animate-fade-in-delay-5">
          <app-outstanding-action-items />
        </section>
      }

    </app-page-content>
  `,
  styles: [`
    .priority-banner {
      padding: 0.875rem 1rem;
      background: var(--color-overdue-bg);
      border: 1px solid var(--color-danger-base);
      border-radius: 0.75rem;
    }

    .quick-action {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 1rem 0.5rem;
      background: var(--color-bg-subtle);
      border: 1px solid var(--color-border-default);
      border-radius: 0.75rem;
      cursor: pointer;
      transition: border-color 0.15s, box-shadow 0.15s;
    }
    .quick-action:hover {
      border-color: var(--color-primary-text);
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
    }

    .meetings-banner {
      padding: 0.875rem 1rem;
      background: var(--color-archive-bg);
      border: 1px solid var(--color-archive-border);
      border-radius: 0.75rem;
    }

    .meeting-chip {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.5rem 0.75rem;
      background: var(--color-bg-subtle);
      border: 1px solid var(--color-archive-border);
      border-radius: 0.5rem;
      cursor: pointer;
      transition: border-color 0.15s, box-shadow 0.15s;
      flex: 1;
      min-width: 0;
    }
    .meeting-chip:hover {
      border-color: var(--color-archive-text);
      box-shadow: 0 1px 4px rgba(0, 0, 0, 0.06);
    }
    .meeting-chip-time {
      font-size: 0.8125rem;
      font-weight: 600;
      color: var(--color-archive-text);
      white-space: nowrap;
    }
    .meeting-chip-divider {
      width: 1px;
      height: 1.25rem;
      background: var(--color-border-default);
      flex-shrink: 0;
    }
    .meeting-chip-info {
      display: flex;
      flex-direction: column;
      min-width: 0;
    }
    .meeting-chip-title {
      font-size: 0.8125rem;
      font-weight: 500;
      color: var(--color-text-primary);
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }
    .meeting-chip-day {
      font-size: 0.6875rem;
      color: var(--color-text-muted);
    }

    .widget-card {
      padding: 1rem;
      background: var(--color-bg-subtle);
      border: 1px solid var(--color-border-default);
      border-radius: 0.75rem;
    }

    .task-row {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      padding: 0.375rem 0.25rem;
      border-radius: 0.375rem;
      cursor: pointer;
      width: 100%;
      text-align: left;
      border: none;
      background: none;
      font: inherit;
      transition: background 0.1s;
    }
    .task-row:hover {
      background: var(--color-bg-muted);
    }

    .task-status-dot {
      width: 6px;
      height: 6px;
      border-radius: 50%;
      flex-shrink: 0;
    }

    .recent-row {
      display: flex;
      align-items: center;
      gap: 0.625rem;
      padding: 0.375rem 0.25rem;
      border-radius: 0.375rem;
      cursor: pointer;
      width: 100%;
      text-align: left;
      border: none;
      background: none;
      font: inherit;
      transition: background 0.1s;
    }
    .recent-row:hover {
      background: var(--color-bg-muted);
    }
  `],
})
export class HomePage implements OnInit, OnDestroy {
  protected readonly dashboard = inject(HomeDashboardService);
  protected readonly profileService = inject(ProfileService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly noteService = inject(NoteService);
  private readonly headerService = inject(ContextualHeaderService);
  private readonly greetingService = inject(GreetingService);

  readonly firstName = computed(() => {
    const name = this.auth.user()?.name;
    return name?.split(' ')[0] ?? '';
  });

  readonly greeting = signal('');

  readonly todayDate = computed(() => {
    const now = new Date();
    return now.toLocaleDateString(undefined, {
      weekday: 'long',
      month: 'long',
      day: 'numeric',
      year: 'numeric',
    });
  });

  ngOnInit(): void {
    this.headerService.breadcrumb.set([{ label: 'Home' }]);
    this.dashboard.loadAllData();

    const name = this.firstName();
    if (name) {
      this.greeting.set(this.greetingService.generateGreeting(name));
    }
  }

  ngOnDestroy(): void {
    this.headerService.clearContext();
  }

  private readonly homeBreadcrumbSource = { breadcrumbSource: { label: 'Home', route: '/home' } };

  newNote(): void {
    this.noteService.createNote(undefined, (id) => {
      this.router.navigate(['/notes', id], { state: this.homeBreadcrumbSource });
    });
  }

  newTask(): void {
    this.router.navigate(['/tasks']);
  }

  startRecording(): void {
    this.router.navigate(['/meetings', 'new'], { state: this.homeBreadcrumbSource });
  }

  goToMeeting(id: string): void {
    this.router.navigate(['/meetings', id], { state: this.homeBreadcrumbSource });
  }

  navigateToTask(taskId: string): void {
    this.router.navigate(['/tasks'], { queryParams: { highlight: taskId } });
  }

  goToRecentItem(item: { id: string; type: 'note' | 'meeting' }): void {
    if (item.type === 'note') {
      this.router.navigate(['/notes', item.id], { state: this.homeBreadcrumbSource });
    } else {
      this.router.navigate(['/meetings', item.id], { state: this.homeBreadcrumbSource });
    }
  }
}
