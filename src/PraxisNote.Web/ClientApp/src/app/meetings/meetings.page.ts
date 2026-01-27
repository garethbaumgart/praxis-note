import { Component, ChangeDetectionStrategy, inject, OnInit, computed, HostListener, ElementRef, viewChild } from '@angular/core';
import { MeetingService } from './meeting.service';
import { Meeting } from './meeting.model';
import { MeetingRowComponent } from './meeting-row.component';
import { MeetingRowSkeletonComponent } from './meeting-row-skeleton.component';
import { MeetingEditorComponent } from './meeting-editor.component';
import { ToastService } from '../shared/services/toast.service';

@Component({
  selector: 'app-meetings-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MeetingRowComponent, MeetingRowSkeletonComponent, MeetingEditorComponent],
  template: `
    <div class="max-w-4xl mx-auto px-4 md:px-6 py-6 md:py-8">
      <!-- Header -->
      <div class="flex items-center justify-between mb-6">
        <div class="flex items-center gap-3">
          <h1 class="text-lg font-semibold text-foreground">Meetings</h1>
          <span class="text-sm text-foreground-muted">{{ meetingService.meetingCount() }} meetings</span>
        </div>
        <button
          type="button"
          class="flex items-center gap-2 px-3 py-1.5 bg-accent-solid text-white rounded-md text-sm font-medium hover:bg-accent-solid/90 transition-colors"
          (click)="openNewMeeting()"
        >
          <i class="pi pi-plus text-xs"></i>
          New Meeting
        </button>
      </div>

      <!-- Search -->
      <div class="relative mb-6">
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
            <p class="text-foreground-muted">No meetings match your search</p>
          } @else {
            <i class="pi pi-comments text-4xl text-foreground-muted mb-4"></i>
            <p class="text-foreground-secondary mb-2">No meetings yet</p>
            <p class="text-sm text-foreground-muted">Click "New Meeting" to create your first meeting</p>
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

    <!-- Meeting Editor Dialog -->
    <app-meeting-editor
      #editor
      (onSave)="handleSave($event)"
    />
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
export class MeetingsPage implements OnInit {
  readonly meetingService = inject(MeetingService);
  private readonly toast = inject(ToastService);

  private readonly searchInputRef = viewChild<ElementRef<HTMLInputElement>>('searchInput');
  private readonly editorRef = viewChild<MeetingEditorComponent>('editor');

  readonly skeletonArray = Array.from({ length: 4 }, (_, i) => i);

  private editingMeeting: Meeting | null = null;

  ngOnInit(): void {
    this.meetingService.loadMeetings();
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
    this.editingMeeting = null;
    this.editorRef()?.open();
  }

  openMeeting(meeting: Meeting): void {
    this.editingMeeting = meeting;
    this.editorRef()?.open(meeting);
  }

  handleSave(data: { title?: string; meetingDate?: string; attendees?: string }): void {
    if (this.editingMeeting) {
      this.meetingService.updateMeeting(
        this.editingMeeting.id,
        data.title,
        data.meetingDate,
        data.attendees
      );
    } else {
      this.meetingService.createMeeting(data.title, data.meetingDate, data.attendees);
    }
    this.editingMeeting = null;
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
