import { Component, ChangeDetectionStrategy, inject, OnInit, OnDestroy, HostListener, ElementRef, viewChild, computed, effect, untracked } from '@angular/core';
import { Router } from '@angular/router';
import { Tooltip } from 'primeng/tooltip';
import { MeetingService } from './meeting.service';
import { Meeting } from './meeting.model';
import { MeetingRowComponent } from './meeting-row.component';
import { MeetingRowSkeletonComponent } from './meeting-row-skeleton.component';
import { ImportDialogComponent } from './import-dialog.component';
import { CalendarService } from '../shared/services/calendar.service';
import { ToastService } from '../shared/services/toast.service';
import { ContextualHeaderService } from '../shared/services/contextual-header.service';
import { formatTimeAgo, formatShortDate } from '../shared/date-utils';

@Component({
  selector: 'app-meetings-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MeetingRowComponent, MeetingRowSkeletonComponent, ImportDialogComponent, Tooltip],
  template: `
    <div class="max-w-6xl mx-auto px-6 md:px-8 py-8 md:py-10">
      <h1 class="sr-only">Meetings</h1>
      <!-- Search + Actions -->
      <div class="flex items-center gap-3 mb-6">
        <div class="relative flex-1">
          <i class="pi pi-search absolute left-3 top-1/2 -translate-y-1/2 text-xs text-foreground-secondary"></i>
          <input
            #searchInput
            type="text"
            placeholder="Search meetings..."
            [value]="meetingService.searchQuery()"
            (input)="meetingService.setSearchQuery(asInput($event).value)"
            (keydown.escape)="clearSearch()"
            class="w-full h-9 pl-9 pr-16 text-sm text-foreground-secondary placeholder-foreground-secondary bg-surface-muted hover:bg-surface-muted/80 focus:bg-surface-muted/80 rounded-lg focus:outline-none transition-colors duration-150"
            aria-label="Search meetings"
          >
          @if (meetingService.searchQuery()) {
            <button
              type="button"
              class="absolute right-3 top-1/2 -translate-y-1/2 text-foreground-muted hover:text-foreground transition-colors"
              (click)="clearSearch()"
              aria-label="Clear search"
            >
              <i class="pi pi-times text-xs"></i>
            </button>
          } @else {
            <kbd class="absolute right-3 top-1/2 -translate-y-1/2 hidden md:inline px-1.5 py-0.5 text-xs text-foreground-muted bg-surface border border-border rounded font-sans">/</kbd>
          }
        </div>
        @if (isCalendarConnected()) {
          <button
            type="button"
            class="touch-target w-9 h-9 rounded-lg flex items-center justify-center bg-surface-muted hover:bg-surface-muted/80 transition-colors shrink-0"
            [pTooltip]="syncTooltip()"
            tooltipPosition="bottom"
            [disabled]="calendarService.syncing()"
            (click)="syncCalendar()"
            aria-label="Sync Google Calendar"
          >
            @if (calendarService.syncing()) {
              <i class="pi pi-spin pi-spinner text-sm text-foreground-muted"></i>
            } @else {
              <i class="pi pi-sync text-sm text-done-foreground"></i>
            }
          </button>
        }
        <button
          type="button"
          class="flex items-center gap-2 px-3 py-1.5 bg-surface-muted text-foreground-secondary rounded-md text-sm font-medium hover:bg-surface-muted/80 transition-colors shrink-0"
          (click)="importDialog.open()"
          aria-label="Import meetings"
        >
          <i class="pi pi-download text-xs"></i>
          <span class="hidden sm:inline">Import</span>
        </button>
        <button
          type="button"
          class="flex items-center gap-2 px-3 py-1.5 bg-accent-solid text-white rounded-md text-sm font-medium hover:bg-accent-solid/90 transition-colors shrink-0"
          (click)="openNewMeeting()"
          aria-label="New meeting"
        >
          <i class="pi pi-plus text-xs"></i>
          <span class="hidden sm:inline">New Meeting</span>
        </button>
      </div>

      <!-- Loading skeletons -->
      @if (!meetingService.initialLoadComplete()) {
        <div class="space-y-6">
          <!-- Skeleton day group -->
          <div>
            <div class="flex items-center gap-3 mb-3">
              <div class="h-4 w-16 bg-surface-muted rounded animate-pulse"></div>
              <div class="h-3 w-24 bg-surface-muted rounded animate-pulse"></div>
            </div>
            <div class="space-y-2">
              @for (i of skeletonArray; track i) {
                <app-meeting-row-skeleton />
              }
            </div>
          </div>
        </div>
      } @else if (meetingService.groupedMeetings().length === 0) {
        <!-- Empty state -->
        <div class="text-center py-16">
          @if (meetingService.searchQuery()) {
            <i class="pi pi-search text-4xl text-foreground-muted mb-4"></i>
            <p class="text-lg font-semibold text-foreground mb-2">No meetings match your search</p>
            <p class="text-sm text-foreground-muted">Try adjusting your search terms.</p>
          } @else if (!isCalendarConnected()) {
            <i class="pi pi-comments text-4xl text-foreground-muted mb-4"></i>
            <p class="text-lg font-semibold text-foreground mb-2">No meetings yet</p>
            <p class="text-sm text-foreground-muted mb-6">Get started by connecting your calendar or adding a meeting manually.</p>
            <div class="flex items-center justify-center gap-3">
              <button type="button"
                class="flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium border border-border text-foreground-secondary bg-surface-subtle hover:bg-surface-muted transition-colors"
                (click)="connectCalendar()"
              >
                <i class="pi pi-google text-xs"></i>
                Connect Google Calendar
              </button>
              <span class="text-xs text-foreground-muted">or</span>
              <button type="button"
                class="flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium bg-accent-solid text-white hover:bg-accent-solid/90 transition-colors"
                (click)="openNewMeeting()"
              >
                <i class="pi pi-plus text-xs"></i>
                New Meeting
              </button>
            </div>
            <p class="text-xs text-foreground-muted mt-4">
              You can also
              <button type="button" class="text-accent-foreground hover:underline" (click)="importDialog.open()">
                import from a screenshot
              </button>
            </p>
          } @else {
            <i class="pi pi-calendar text-4xl text-foreground-muted mb-4"></i>
            <p class="text-lg font-semibold text-foreground mb-2">No meetings in the next 7 days</p>
            <p class="text-sm text-foreground-muted mb-6">Your Google Calendar is connected but no upcoming meetings were found.</p>
            <div class="flex items-center justify-center gap-3">
              <button type="button"
                class="flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium border border-border text-foreground-secondary bg-surface-subtle hover:bg-surface-muted transition-colors"
                [disabled]="calendarService.syncing()"
                (click)="syncCalendar()"
              >
                <i class="pi pi-sync text-xs"></i>
                Sync Now
              </button>
              <span class="text-xs text-foreground-muted">or</span>
              <button type="button"
                class="flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium bg-accent-solid text-white hover:bg-accent-solid/90 transition-colors"
                (click)="openNewMeeting()"
              >
                <i class="pi pi-plus text-xs"></i>
                New Meeting
              </button>
            </div>
          }
        </div>
      } @else {
        <!-- Daily grouped list -->
        <div class="space-y-6">
          @for (group of meetingService.groupedMeetings(); track group.label) {
            <div>
              <!-- Day header (sticky) -->
              <div class="day-header flex items-center gap-3 mb-3">
                <span class="text-sm font-semibold text-foreground">{{ group.label }}</span>
                <span class="text-xs text-foreground-muted">{{ group.subLabel }}</span>
                <span class="px-2 py-0.5 bg-surface-muted text-foreground-muted text-xs rounded-full">
                  {{ group.meetings.length }} {{ group.meetings.length === 1 ? 'meeting' : 'meetings' }}
                </span>
              </div>

              <!-- Meetings for this day -->
              <div class="space-y-2">
                @for (meeting of group.meetings; track meeting.id) {
                  <app-meeting-row
                    [meeting]="meeting"
                    (onOpen)="openMeeting(meeting)"
                    (onDelete)="deleteMeeting(meeting)"
                  />
                }
              </div>
            </div>
          }
        </div>
      }
    </div>

    <app-import-dialog #importDialog (onImported)="meetingService.loadMeetings()" />
  `,
  styles: [`
    .day-header {
      position: sticky;
      top: 0;
      background: var(--color-bg-base);
      z-index: 10;
      padding: 8px 0;
    }
  `],
})
export class MeetingsPage implements OnInit, OnDestroy {
  readonly meetingService = inject(MeetingService);
  readonly calendarService = inject(CalendarService);
  private readonly toast = inject(ToastService);
  private readonly router = inject(Router);
  private readonly headerService = inject(ContextualHeaderService);

  private readonly searchInputRef = viewChild<ElementRef<HTMLInputElement>>('searchInput');
  readonly importDialog = viewChild.required<ImportDialogComponent>('importDialog');

  readonly skeletonArray = Array.from({ length: 4 }, (_, i) => i);

  readonly isCalendarConnected = computed(() => this.calendarService.status()?.isConnected ?? false);

  readonly syncTooltip = computed(() => {
    const status = this.calendarService.status();
    if (!status?.isConnected) return '';
    const lastSync = formatTimeAgo(status.lastSyncedAt);
    const endDate = new Date();
    endDate.setDate(endDate.getDate() + 7);
    const endDateStr = formatShortDate(endDate);
    return `Google Calendar connected\nLast synced: ${lastSync}\nSync window: Today \u2013 ${endDateStr}`;
  });

  constructor() {
    effect(() => {
      const result = this.calendarService.lastSyncResult();
      if (result) {
        untracked(() => {
          let msg = result.importedCount > 0
            ? `Imported ${result.importedCount} meeting${result.importedCount !== 1 ? 's' : ''} for the next 7 days`
            : 'No new meetings found';
          if (result.skippedCount > 0) {
            msg += `, ${result.skippedCount} already existed`;
          }
          this.toast.success({ summary: 'Calendar synced', detail: msg });
          this.meetingService.loadMeetings();
          this.calendarService.clearLastSyncResult();
        });
      }
    });
  }

  ngOnInit(): void {
    this.headerService.breadcrumb.set([{ label: 'Meetings' }]);
    this.meetingService.loadMeetings();
    this.calendarService.loadConnectionStatus();
  }

  ngOnDestroy(): void {
    this.headerService.clearContext();
  }

  @HostListener('document:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    const target = event.target as HTMLElement;
    const isInInput = target.tagName === 'INPUT' ||
                      target.tagName === 'TEXTAREA' ||
                      target.isContentEditable;

    // Focus search with /
    if (event.key === '/' && !isInInput) {
      event.preventDefault();
      this.focusSearch();
    }

    // New meeting with N
    if (event.key === 'n' && !isInInput && !event.ctrlKey && !event.metaKey) {
      event.preventDefault();
      this.openNewMeeting();
    }
  }

  focusSearch(): void {
    this.searchInputRef()?.nativeElement.focus();
  }

  clearSearch(): void {
    this.meetingService.setSearchQuery('');
    this.searchInputRef()?.nativeElement.blur();
  }

  openNewMeeting(): void {
    this.router.navigate(['/meetings', 'new']);
  }

  openMeeting(meeting: Meeting): void {
    this.router.navigate(['/meetings', meeting.id]);
  }

  syncCalendar(): void {
    this.calendarService.syncEvents();
  }

  connectCalendar(): void {
    this.calendarService.connectGoogleCalendar();
  }

  deleteMeeting(meeting: Meeting): void {
    const deleted = this.meetingService.deleteMeetingWithUndo(meeting.id);
    if (deleted) {
      this.toast.success({
        summary: 'Meeting deleted',
        action: {
          label: 'Undo',
          callback: () => this.meetingService.undoDelete(meeting.id),
        },
      });
    }
  }

  asInput(event: Event): HTMLInputElement {
    return event.target as HTMLInputElement;
  }
}
