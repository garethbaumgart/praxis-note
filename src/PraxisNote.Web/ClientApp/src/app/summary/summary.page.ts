import { Component, inject, OnInit, OnDestroy, ChangeDetectionStrategy, computed } from '@angular/core';
import { Router } from '@angular/router';
import { Skeleton } from 'primeng/skeleton';
import { Tooltip } from 'primeng/tooltip';
import { SummaryService } from './summary.service';
import { MeetingSummaryItem } from './summary.model';
import { ContextualHeaderService } from '../shared/services/contextual-header.service';
import { ErrorStateComponent } from '../shared/components/error-state.component';

@Component({
  selector: 'app-summary-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Skeleton, Tooltip, ErrorStateComponent],
  template: `
    <div class="max-w-6xl mx-auto px-6 md:px-8 py-8 md:py-10">
      <h1 class="sr-only">Daily Summary</h1>
      <!-- Date navigation -->
      <div class="flex items-center justify-between gap-4 mb-6">
        <p class="text-sm text-foreground-muted">{{ formattedDate() }}</p>
        <div class="flex items-center gap-2">
          <button
            class="w-8 h-8 flex items-center justify-center rounded-lg border border-border bg-surface-subtle text-foreground-muted hover:bg-surface-muted hover:text-foreground-secondary transition cursor-pointer"
            (click)="summaryService.navigateDate(-1)"
            aria-label="Previous day">
            <i class="pi pi-chevron-left text-xs"></i>
          </button>

          <button
            class="px-3 py-1.5 rounded-lg text-xs font-medium transition cursor-pointer"
            [class.bg-accent]="summaryService.isToday()"
            [class.text-accent-foreground]="summaryService.isToday()"
            [class.bg-surface-subtle]="!summaryService.isToday()"
            [class.border]="!summaryService.isToday()"
            [class.border-border]="!summaryService.isToday()"
            [class.text-foreground-secondary]="!summaryService.isToday()"
            [class.hover:bg-surface-muted]="!summaryService.isToday()"
            (click)="summaryService.goToToday()">
            Today
          </button>

          <button
            class="w-8 h-8 flex items-center justify-center rounded-lg border border-border bg-surface-subtle text-foreground-muted hover:bg-surface-muted hover:text-foreground-secondary transition cursor-pointer disabled:opacity-30 disabled:pointer-events-none"
            [disabled]="summaryService.isToday()"
            (click)="summaryService.navigateDate(1)"
            aria-label="Next day">
            <i class="pi pi-chevron-right text-xs"></i>
          </button>
        </div>
      </div>

      @if (summaryService.loading()) {
        <!-- Skeleton loading state -->
        <div role="status" aria-label="Loading daily summary">
          <span class="sr-only">Loading daily summary...</span>
          <div class="grid grid-cols-2 sm:grid-cols-4 gap-4 mb-6">
            @for (i of skeletonCards; track i) {
              <div class="bg-surface-subtle border border-border rounded-xl p-4 text-center">
                <p-skeleton width="40%" height="28px" styleClass="mb-2 mx-auto" />
                <p-skeleton width="60%" height="10px" styleClass="mx-auto" />
              </div>
            }
          </div>
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-4">
            @for (i of skeletonSections; track i) {
              <div class="bg-surface-subtle border border-border rounded-xl p-4">
                <p-skeleton width="40%" height="14px" styleClass="mb-3" />
                <p-skeleton width="100%" height="12px" styleClass="mb-2" />
                <p-skeleton width="90%" height="12px" styleClass="mb-2" />
                <p-skeleton width="80%" height="12px" />
              </div>
            }
          </div>
        </div>
      } @else if (summaryService.error()) {
        <app-error-state
          title="Something went wrong"
          [message]="summaryService.error()!"
          (retry)="summaryService.loadSummary()"
        />
      } @else if (isEmptyDay()) {
        <!-- Empty state -->
        <div class="text-center py-16">
          <i class="pi pi-clock text-4xl text-foreground-muted mb-4"></i>
          <h2 class="text-lg font-semibold text-foreground mb-2">No activity on this day</h2>
          <p class="text-foreground-muted text-sm max-w-md mx-auto">
            No meetings, tasks, or notes were recorded for this date.
            Navigate to a different day to see your activity summary.
          </p>
        </div>
      } @else {
        <!-- Stats row -->
        <div class="grid grid-cols-2 sm:grid-cols-4 gap-4 mb-6">
          <div class="bg-surface-subtle border border-border rounded-xl p-4 text-center">
            <div class="text-2xl font-bold text-archive-foreground">{{ summaryService.summary()!.stats.meetingCount }}</div>
            <div class="text-xs text-foreground-muted uppercase tracking-wide mt-1">Meetings</div>
          </div>
          <div class="bg-surface-subtle border border-border rounded-xl p-4 text-center">
            <div class="text-2xl font-bold text-done-foreground">{{ summaryService.summary()!.stats.tasksCompleted }}</div>
            <div class="text-xs text-foreground-muted uppercase tracking-wide mt-1">Completed</div>
          </div>
          <div class="bg-surface-subtle border border-border rounded-xl p-4 text-center">
            <div class="text-2xl font-bold text-inprogress-foreground">{{ summaryService.summary()!.stats.tasksStarted }}</div>
            <div class="text-xs text-foreground-muted uppercase tracking-wide mt-1">In Progress</div>
          </div>
          <div class="bg-surface-subtle border border-border rounded-xl p-4 text-center">
            <div class="text-2xl font-bold text-overdue-foreground">{{ summaryService.summary()!.stats.actionItemsOpen }}</div>
            <div class="text-xs text-foreground-muted uppercase tracking-wide mt-1">Action Items</div>
          </div>
        </div>

        <!-- Two-column grid: Meetings + Outstanding Action Items -->
        <div class="grid grid-cols-1 lg:grid-cols-2 gap-4 mb-4">
          <!-- Meetings section -->
          <div class="bg-surface-subtle border border-border rounded-xl p-4">
            <div class="flex items-center gap-2 mb-3">
              <i class="pi pi-comments text-sm text-archive-foreground"></i>
              <h3 class="text-sm font-semibold text-foreground">Meetings</h3>
            </div>
            @if (summaryService.summary()!.meetings.length === 0) {
              <p class="text-foreground-muted text-xs py-4 text-center">No meetings on this day</p>
            } @else {
              @for (meeting of summaryService.summary()!.meetings; track meeting.id) {
                <div
                  class="flex items-center gap-3 px-2 py-2 rounded-lg hover:bg-surface-muted cursor-pointer transition"
                  (click)="navigateToMeeting(meeting.id)"
                  (keydown.enter)="navigateToMeeting(meeting.id)"
                  (keydown.space)="$event.preventDefault(); navigateToMeeting(meeting.id)"
                  tabindex="0"
                  role="button"
                  [attr.aria-label]="'Open meeting: ' + (meeting.title ?? 'Untitled Meeting')">
                  <span class="text-xs font-semibold text-foreground-muted w-12 shrink-0">
                    {{ formatMeetingTime(meeting.meetingDate) }}
                  </span>
                  <div class="flex-1 min-w-0">
                    <div class="text-sm font-medium text-foreground truncate">{{ meeting.title ?? 'Untitled Meeting' }}</div>
                    <div class="text-xs text-foreground-muted">
                      {{ meeting.actionItemCount }} action{{ meeting.actionItemCount !== 1 ? 's' : '' }}
                      @if (meeting.decisionCount > 0) {
                        <span> · {{ meeting.decisionCount }} decision{{ meeting.decisionCount !== 1 ? 's' : '' }}</span>
                      }
                    </div>
                  </div>
                  <span class="text-xs px-2 py-0.5 rounded shrink-0 font-medium" [class]="getMeetingStatusClasses(meeting)">
                    {{ meeting.status }}
                  </span>
                </div>
              }
            }
          </div>

          <!-- Outstanding Action Items section -->
          <div class="bg-surface-subtle border border-border rounded-xl p-4">
            <div class="flex items-center gap-2 mb-3">
              <i class="pi pi-exclamation-circle text-sm text-overdue-foreground"></i>
              <h3 class="text-sm font-semibold text-foreground">Outstanding Action Items</h3>
            </div>
            @if (summaryService.summary()!.outstandingActionItems.length === 0) {
              <p class="text-foreground-muted text-xs py-4 text-center">No outstanding action items</p>
            } @else {
              @for (item of summaryService.summary()!.outstandingActionItems; track item.actionItemId) {
                <div
                  class="flex items-start gap-2 px-2 py-2 rounded-lg hover:bg-surface-muted cursor-pointer transition"
                  (click)="navigateToMeeting(item.meetingId)"
                  (keydown.enter)="navigateToMeeting(item.meetingId)"
                  (keydown.space)="$event.preventDefault(); navigateToMeeting(item.meetingId)"
                  tabindex="0"
                  role="button"
                  [attr.aria-label]="'Action item: ' + item.description">
                  <div class="w-3.5 h-3.5 border-2 border-border rounded mt-0.5 shrink-0"></div>
                  <div class="flex-1 min-w-0">
                    <div class="text-sm text-foreground">{{ item.description }}</div>
                    <div class="text-xs text-foreground-muted mt-0.5">
                      {{ item.meetingTitle ?? 'Untitled Meeting' }}
                      @if (item.assignee) {
                        <span> · {{ item.assignee }}</span>
                      }
                      @if (item.isLinkedToTask) {
                        <span
                          class="ml-1 text-accent-foreground"
                          pTooltip="Linked to task"
                          tooltipPosition="top">
                          <i class="pi pi-link text-xs"></i>
                          {{ item.linkedTaskStatus }}
                        </span>
                      }
                    </div>
                  </div>
                </div>
              }
            }
          </div>
        </div>

        <!-- Tasks Completed section -->
        @if (summaryService.summary()!.completedTasks.length > 0) {
          <div class="bg-surface-subtle border border-border rounded-xl p-4 mb-4">
            <div class="flex items-center gap-2 mb-3">
              <i class="pi pi-check-circle text-sm text-done-foreground"></i>
              <h3 class="text-sm font-semibold text-foreground">Tasks Completed</h3>
            </div>
            <div class="flex flex-wrap gap-2">
              @for (task of summaryService.summary()!.completedTasks; track task.id) {
                <span
                  class="text-xs px-2.5 py-1 bg-done text-done-foreground rounded cursor-pointer hover:opacity-80 transition inline-flex items-center gap-1"
                  (click)="navigateToTasks()"
                  (keydown.enter)="navigateToTasks()"
                  (keydown.space)="$event.preventDefault(); navigateToTasks()"
                  tabindex="0"
                  role="button"
                  [attr.aria-label]="'Completed task: ' + task.title">
                  @if (task.isPriority) {
                    <i class="pi pi-exclamation-circle text-xs text-danger"></i>
                  }
                  {{ task.title }}
                </span>
              }
            </div>
          </div>
        }

        <!-- In Progress Tasks section -->
        @if (summaryService.summary()!.inProgressTasks.length > 0) {
          <div class="bg-surface-subtle border border-border rounded-xl p-4 mb-4">
            <div class="flex items-center gap-2 mb-3">
              <i class="pi pi-spinner text-sm text-inprogress-foreground"></i>
              <h3 class="text-sm font-semibold text-foreground">Tasks In Progress</h3>
            </div>
            <div class="flex flex-wrap gap-2">
              @for (task of summaryService.summary()!.inProgressTasks; track task.id) {
                <span
                  class="text-xs px-2.5 py-1 bg-inprogress text-inprogress-foreground rounded cursor-pointer hover:opacity-80 transition inline-flex items-center gap-1"
                  (click)="navigateToTasks()"
                  (keydown.enter)="navigateToTasks()"
                  (keydown.space)="$event.preventDefault(); navigateToTasks()"
                  tabindex="0"
                  role="button"
                  [attr.aria-label]="'In progress task: ' + task.title">
                  @if (task.isPriority) {
                    <i class="pi pi-exclamation-circle text-xs text-danger"></i>
                  }
                  {{ task.title }}
                </span>
              }
            </div>
          </div>
        }

        <!-- Notes Updated section -->
        @if (summaryService.summary()!.notesUpdated.length > 0) {
          <div class="bg-surface-subtle border border-border rounded-xl p-4 mb-4">
            <div class="flex items-center gap-2 mb-3">
              <i class="pi pi-file-edit text-sm text-archive-foreground"></i>
              <h3 class="text-sm font-semibold text-foreground">Notes Updated</h3>
            </div>
            @for (note of summaryService.summary()!.notesUpdated; track note.id) {
              <div
                class="flex items-center gap-3 px-2 py-2 rounded-lg hover:bg-surface-muted cursor-pointer transition"
                (click)="navigateToNote(note.id)"
                (keydown.enter)="navigateToNote(note.id)"
                (keydown.space)="$event.preventDefault(); navigateToNote(note.id)"
                tabindex="0"
                role="button"
                [attr.aria-label]="'Open note: ' + note.title">
                <i class="pi pi-file-edit text-sm text-archive-foreground"></i>
                <span class="flex-1 text-sm text-foreground-secondary truncate">{{ note.title }}</span>
                <span class="text-xs text-foreground-muted shrink-0">
                  @if (note.isNew) {
                    Created
                  } @else {
                    Edited
                  }
                </span>
              </div>
            }
          </div>
        }
      }
    </div>
  `,
})
export class SummaryPage implements OnInit, OnDestroy {
  protected readonly summaryService = inject(SummaryService);
  private readonly router = inject(Router);
  private readonly headerService = inject(ContextualHeaderService);

  protected readonly skeletonCards = [0, 1, 2, 3];
  protected readonly skeletonSections = [0, 1];

  protected readonly formattedDate = computed(() => {
    const dateStr = this.summaryService.selectedDate();
    const date = new Date(dateStr + 'T00:00:00');
    return date.toLocaleDateString(undefined, {
      weekday: 'long',
      day: 'numeric',
      month: 'long',
      year: 'numeric',
    });
  });

  protected readonly isEmptyDay = computed(() => {
    const summary = this.summaryService.summary();
    if (!summary) return true;
    const s = summary.stats;
    return s.meetingCount === 0
      && s.tasksCompleted === 0
      && s.tasksStarted === 0
      && s.actionItemsOpen === 0
      && s.notesUpdated === 0;
  });

  ngOnInit(): void {
    this.headerService.breadcrumb.set([{ label: 'Daily Summary' }]);
    this.summaryService.loadSummary();
  }

  ngOnDestroy(): void {
    this.headerService.clearContext();
  }

  protected formatMeetingTime(dateStr: string | null): string {
    if (!dateStr) return '--';
    const date = new Date(dateStr);
    return date.toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' });
  }

  protected getMeetingStatusClasses(meeting: MeetingSummaryItem): string {
    switch (meeting.status) {
      case 'Reviewed':
        return 'bg-done text-done-foreground';
      case 'Ready':
        return 'bg-accent text-accent-foreground';
      case 'Processing':
        return 'bg-inprogress text-inprogress-foreground';
      case 'Failed':
        return 'bg-overdue text-overdue-foreground';
      default:
        return 'bg-surface-muted text-foreground-muted';
    }
  }

  protected navigateToMeeting(id: string): void {
    this.router.navigate(['/meetings', id]);
  }

  protected navigateToTasks(): void {
    this.router.navigate(['/tasks']);
  }

  protected navigateToNote(id: string): void {
    this.router.navigate(['/notes', id]);
  }
}
