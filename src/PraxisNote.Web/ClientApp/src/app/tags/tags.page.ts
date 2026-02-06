import { Component, ChangeDetectionStrategy, inject, OnInit, computed, signal, effect } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SelectModule } from 'primeng/select';
import { TagService } from './tag.service';
import { TagHubService } from './tag-hub.service';
import { Tag } from './tag.model';
import { TagItemDto } from './tag-hub.model';
import { formatShortDate } from '../shared/date-utils';

interface DateGroup {
  label: string;
  items: TagItemDto[];
}

@Component({
  selector: 'app-tags-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, SelectModule],
  styles: [`
    @keyframes shimmer {
      0% { background-position: -800px 0; }
      100% { background-position: 800px 0; }
    }
    .skeleton {
      background: linear-gradient(
        90deg,
        var(--color-bg-muted) 25%,
        var(--color-bg-subtle) 50%,
        var(--color-bg-muted) 75%
      );
      background-size: 800px 100%;
      animation: shimmer 1.5s infinite;
      border-radius: 4px;
    }
  `],
  template: `
    <div class="max-w-7xl mx-auto px-4 md:px-6 py-6 md:py-8">
      <!-- Header -->
      <div class="flex items-center gap-3 mb-6">
        <i class="pi pi-tags text-lg text-foreground-secondary" aria-hidden="true"></i>
        <h1 class="text-lg font-semibold text-foreground">Tags</h1>
        @if (!tagService.loading() && !tagService.error()) {
          <span class="text-sm text-foreground-muted">{{ tagCount() }} tags</span>
        }
      </div>

      @if (tagService.loading()) {
        <!-- Loading tags state -->
        <div class="flex items-center justify-center py-16" role="status" aria-label="Loading tags">
          <i class="pi pi-spin pi-spinner text-2xl text-foreground-muted" aria-hidden="true"></i>
          <span class="sr-only">Loading tags...</span>
        </div>
      } @else if (tagService.error()) {
        <!-- Tags error state -->
        <div class="flex flex-col items-center justify-center py-24">
          <p class="text-danger">{{ tagService.error() }}</p>
        </div>
      } @else if (tagService.tags().length === 0) {
        <!-- No tags state -->
        <div class="flex flex-col items-center justify-center py-24">
          <i class="pi pi-tags text-5xl text-foreground-muted mb-4" aria-hidden="true"></i>
          <p class="text-foreground-secondary mb-2">No tags yet</p>
          <p class="text-sm text-foreground-muted text-center max-w-sm">
            Tags you create on notes, tasks, and meetings will appear here.
          </p>
        </div>
      } @else {
        <!-- Selector row -->
        <div class="flex flex-wrap items-center gap-3 mb-6">
          <p-select
            [options]="tagService.tags()"
            optionLabel="name"
            [ngModel]="selectedTag()"
            (ngModelChange)="onTagSelected($event)"
            [filter]="true"
            filterBy="name"
            placeholder="Select a tag..."
            [showClear]="true"
            [style]="{ 'min-width': '280px' }"
            appendTo="body"
            ariaLabel="Select a tag"
          />
          @if (selectedTag() && !hub.loading()) {
            <span class="text-sm text-foreground-muted">
              {{ hub.meetingCount() }} {{ hub.meetingCount() === 1 ? 'meeting' : 'meetings' }} · {{ hub.noteCount() }} {{ hub.noteCount() === 1 ? 'note' : 'notes' }} · {{ hub.taskCount() }} {{ hub.taskCount() === 1 ? 'task' : 'tasks' }}
            </span>
          }
          @if (selectedTag() && hub.loading()) {
            <span class="skeleton inline-block w-48 h-4"></span>
          }
        </div>

        @if (!selectedTag()) {
          <!-- No tag selected -->
          <div class="flex flex-col items-center justify-center py-24">
            <div class="w-12 h-12 rounded-xl bg-surface-muted flex items-center justify-center mb-3">
              <i class="pi pi-tags text-xl text-foreground-muted" aria-hidden="true"></i>
            </div>
            <p class="text-sm font-medium text-foreground-secondary">Choose a tag</p>
            <p class="text-xs text-foreground-muted mt-1">Select a tag above to see related items</p>
          </div>
        } @else if (hub.loading()) {
          <!-- Skeleton loading -->
          <div class="space-y-4" role="status" aria-label="Loading items">
            <div class="flex items-center gap-2 mb-3">
              <span class="skeleton w-20 h-6 rounded-full"></span>
              <div class="flex-1 h-px bg-border-muted"></div>
            </div>
            @for (i of skeletonRows; track i) {
              <div class="flex items-center gap-3 px-3 py-2.5">
                <div class="skeleton w-7 h-7 rounded-md shrink-0"></div>
                <div class="flex-1 min-w-0 space-y-1.5">
                  <div class="skeleton h-3.5 w-[55%]"></div>
                  <div class="skeleton h-3 w-[30%]"></div>
                </div>
                <div class="skeleton h-3 w-12 shrink-0"></div>
              </div>
            }
            <span class="sr-only">Loading items...</span>
          </div>
        } @else if (hub.error()) {
          <!-- Error state -->
          <div class="flex flex-col items-center justify-center py-24">
            <div class="w-12 h-12 rounded-xl bg-danger-bg flex items-center justify-center mb-3">
              <i class="pi pi-exclamation-triangle text-xl text-danger" aria-hidden="true"></i>
            </div>
            <p class="text-sm font-medium text-foreground-secondary">Something went wrong</p>
            <p class="text-xs text-foreground-muted mt-1">We couldn't load the items for this tag</p>
            <button
              (click)="retryLoad()"
              class="mt-3 px-3 py-1.5 border border-border text-foreground-secondary rounded-lg text-xs hover:bg-surface-muted transition-colors"
            >
              Try again
            </button>
          </div>
        } @else if (hub.items().length === 0) {
          <!-- Empty state -->
          <div class="flex flex-col items-center justify-center py-24">
            <div class="w-12 h-12 rounded-xl bg-surface-muted flex items-center justify-center mb-3">
              <i class="pi pi-inbox text-xl text-foreground-muted" aria-hidden="true"></i>
            </div>
            <p class="text-sm font-medium text-foreground-secondary">No items yet</p>
            <p class="text-xs text-foreground-muted mt-1">Nothing is tagged with "{{ selectedTag()?.name }}"</p>
          </div>
        } @else {
          <!-- Item list with date groups -->
          <div class="space-y-5">
            @for (group of dateGroups(); track group.label) {
              <!-- Date chip header -->
              <div>
                <div class="flex items-center gap-2 mb-3">
                  <span class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-surface-muted text-foreground-muted text-xs font-medium">
                    <i class="pi pi-calendar text-[10px]" aria-hidden="true"></i>
                    {{ group.label }}
                  </span>
                  <div class="flex-1 h-px bg-border-muted"></div>
                </div>

                <!-- Items in this group -->
                <div class="space-y-0.5">
                  @for (item of group.items; track item.id) {
                    <a
                      [href]="itemUrl(item)"
                      target="_blank"
                      rel="noopener noreferrer"
                      class="flex items-center gap-3 px-3 py-2.5 rounded-lg group hover:bg-surface-muted transition-colors"
                      [attr.aria-label]="item.type + ': ' + item.title"
                    >
                      <!-- Type icon -->
                      @switch (item.type) {
                        @case ('Meeting') {
                          <div class="w-7 h-7 rounded-md bg-accent flex items-center justify-center shrink-0" aria-hidden="true">
                            <i class="pi pi-comments text-xs text-accent-foreground"></i>
                          </div>
                        }
                        @case ('Note') {
                          <div class="w-7 h-7 rounded-md bg-done flex items-center justify-center shrink-0" aria-hidden="true">
                            <i class="pi pi-file-edit text-xs text-done-foreground"></i>
                          </div>
                        }
                        @case ('Task') {
                          <div class="w-7 h-7 rounded-md bg-inprogress flex items-center justify-center shrink-0" aria-hidden="true">
                            <i class="pi pi-check-square text-xs text-inprogress-foreground"></i>
                          </div>
                        }
                      }

                      <!-- Title + metadata -->
                      <div class="flex-1 min-w-0">
                        <p class="text-sm text-foreground truncate">{{ item.title }}</p>
                        <p class="text-xs text-foreground-muted truncate">
                          @switch (item.type) {
                            @case ('Meeting') {
                              {{ formatMeetingMeta(item) }}
                            }
                            @case ('Note') {
                              Updated {{ relativeDate(item.updatedAt ?? item.date) }}
                            }
                            @case ('Task') {
                              <span
                                class="inline-flex items-center px-1.5 py-0.5 rounded text-[10px] font-medium"
                                [class.bg-todo]="item.status === 'Todo'"
                                [class.text-todo-foreground]="item.status === 'Todo'"
                                [class.bg-inprogress]="item.status === 'InProgress'"
                                [class.text-inprogress-foreground]="item.status === 'InProgress'"
                                [class.bg-done]="item.status === 'Done'"
                                [class.text-done-foreground]="item.status === 'Done'"
                              >{{ formatStatus(item.status) }}</span>
                              @if (item.isPriority) {
                                <span class="inline-block w-1.5 h-1.5 rounded-full bg-danger ml-1.5" aria-label="Priority"></span>
                              }
                              @if (item.dueDate) {
                                <span class="ml-1.5">{{ formatDueDate(item.dueDate) }}</span>
                              }
                            }
                          }
                        </p>
                      </div>

                      <!-- Relative date + external link icon -->
                      <div class="flex items-center gap-2 shrink-0">
                        <span class="text-xs text-foreground-muted">{{ relativeDate(item.date) }}</span>
                        <i class="pi pi-external-link text-xs text-foreground-muted opacity-0 group-hover:opacity-100 transition-opacity" aria-hidden="true"></i>
                      </div>
                    </a>
                  }
                </div>
              </div>
            }
          </div>
        }
      }
    </div>
  `,
})
export class TagsPage implements OnInit {
  readonly tagService = inject(TagService);
  readonly hub = inject(TagHubService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly selectedTag = signal<Tag | null>(null);
  readonly tagCount = computed(() => this.tagService.tags().length);
  readonly skeletonRows = [0, 1, 2, 3];

  private readonly pendingSelectedId = signal<string | null>(null);

  readonly dateGroups = computed<DateGroup[]>(() => {
    const items = this.hub.items();
    if (items.length === 0) return [];

    const now = new Date();
    const mondayThisWeek = getMonday(now);
    const mondayLastWeek = new Date(mondayThisWeek);
    mondayLastWeek.setDate(mondayLastWeek.getDate() - 7);
    const firstOfMonth = new Date(now.getFullYear(), now.getMonth(), 1);

    const groups: Record<string, TagItemDto[]> = {
      'This Week': [],
      'Last Week': [],
      'This Month': [],
      'Earlier': [],
    };

    for (const item of items) {
      const d = new Date(item.date);
      if (d >= mondayThisWeek) {
        groups['This Week'].push(item);
      } else if (d >= mondayLastWeek) {
        groups['Last Week'].push(item);
      } else if (d >= firstOfMonth) {
        groups['This Month'].push(item);
      } else {
        groups['Earlier'].push(item);
      }
    }

    return Object.entries(groups)
      .filter(([, items]) => items.length > 0)
      .map(([label, items]) => ({ label, items }));
  });

  constructor() {
    // Pre-select tag from URL when tags finish loading
    effect(() => {
      const id = this.pendingSelectedId();
      if (!id) return;
      const tags = this.tagService.tags();
      const match = tags.find(t => t.id === id);
      if (match) {
        this.selectedTag.set(match);
        this.pendingSelectedId.set(null);
      }
    });

    // Load items when selected tag changes
    effect(() => {
      const tag = this.selectedTag();
      if (tag) {
        this.hub.loadItems(tag.id);
      } else {
        this.hub.clear();
      }
    });
  }

  ngOnInit(): void {
    this.tagService.loadTags();

    const selectedId = this.route.snapshot.queryParamMap.get('selected');
    if (selectedId) {
      this.pendingSelectedId.set(selectedId);
    }
  }

  onTagSelected(tag: Tag | null): void {
    this.selectedTag.set(tag);
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { selected: tag?.id ?? undefined },
      queryParamsHandling: 'merge',
    });
  }

  retryLoad(): void {
    const tag = this.selectedTag();
    if (tag) {
      this.hub.loadItems(tag.id);
    }
  }

  itemUrl(item: TagItemDto): string {
    switch (item.type) {
      case 'Meeting': return `/meetings/${item.id}`;
      case 'Note': return `/notes/${item.id}`;
      case 'Task': return '/tasks';
    }
  }

  formatMeetingMeta(item: TagItemDto): string {
    const parts: string[] = [];
    if (item.meetingDate) {
      parts.push(formatShortDate(new Date(item.meetingDate)));
    }
    if (item.attendeeCount != null && item.attendeeCount > 0) {
      parts.push(`${item.attendeeCount} attendee${item.attendeeCount !== 1 ? 's' : ''}`);
    }
    return parts.join(' · ') || 'Meeting';
  }

  formatStatus(status?: string): string {
    switch (status) {
      case 'InProgress': return 'In Progress';
      case 'Todo': return 'Todo';
      case 'Done': return 'Done';
      default: return status ?? '';
    }
  }

  formatDueDate(dueDate: string): string {
    const [year, month, day] = dueDate.split('-').map(Number);
    const d = new Date(year, month - 1, day);
    return formatShortDate(d);
  }

  relativeDate(isoDate: string): string {
    const d = new Date(isoDate);
    if (isNaN(d.getTime())) return '';
    const now = new Date();
    const diffMs = now.getTime() - d.getTime();
    const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

    if (diffDays < 0) {
      return formatShortDate(d);
    }
    if (diffDays === 0) {
      if (diffHours <= 0) return 'Just now';
      return `${diffHours}h ago`;
    }
    if (diffDays <= 7) {
      return `${diffDays}d ago`;
    }
    return formatShortDate(d);
  }
}

function getMonday(d: Date): Date {
  const date = new Date(d.getFullYear(), d.getMonth(), d.getDate());
  const day = date.getDay();
  const diff = day === 0 ? 6 : day - 1; // Sunday wraps to 6, others offset by 1
  date.setDate(date.getDate() - diff);
  return date;
}
