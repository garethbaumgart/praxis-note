import { Component, ChangeDetectionStrategy, inject, OnInit, OnDestroy, computed, signal, effect, ViewChild, ElementRef } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SelectModule } from 'primeng/select';
import { Menu } from 'primeng/menu';
import { MenuItem } from 'primeng/api';
import { Dialog } from 'primeng/dialog';
import { Select } from 'primeng/select';
import { Skeleton } from 'primeng/skeleton';
import { TagService } from './tag.service';
import { TagHubService } from './tag-hub.service';
import { Tag } from './tag.model';
import { TagItemDto } from './tag-hub.model';
import { TagListSkeletonComponent } from './tag-list-skeleton.component';
import { MergeTagDialogComponent } from './merge-tag-dialog.component';
import { formatShortDate } from '../shared/date-utils';
import { ContextualHeaderService } from '../shared/services/contextual-header.service';
import { ErrorStateComponent } from '../shared/components/error-state.component';

interface DateGroup {
  label: string;
  items: TagItemDto[];
}

@Component({
  selector: 'app-tags-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, SelectModule, Menu, Dialog, Skeleton, TagListSkeletonComponent, MergeTagDialogComponent, ErrorStateComponent],
  styles: [`
    @media (hover: none) {
      .group button { opacity: 1 !important; }
    }
  `],
  template: `
    <div class="max-w-6xl mx-auto px-6 md:px-8 py-8 md:py-10">
      <h1 class="sr-only">Tags</h1>
      @if (tagService.loading()) {
        <!-- Loading tags state -->
        <app-tag-list-skeleton />
      } @else if (tagService.error()) {
        <app-error-state
          title="Something went wrong"
          [message]="tagService.error()!"
          (retry)="tagService.loadTags()"
        />
      } @else if (tagService.tags().length === 0) {
        <!-- No tags state -->
        <div class="text-center py-16">
          <i class="pi pi-tags text-4xl text-foreground-muted mb-4" aria-hidden="true"></i>
          <p class="text-lg font-semibold text-foreground mb-2">No tags yet</p>
          <p class="text-sm text-foreground-muted max-w-md mx-auto">
            Tags you create on notes, tasks, and meetings will appear here.
          </p>
        </div>
      } @else {
        <!-- Selector row -->
        <div class="flex flex-wrap items-center gap-3 mb-6">
          @if (renamingTag()) {
            <!-- Inline rename mode -->
            <div class="flex items-center gap-2">
              <input
                #renameInput
                type="text"
                class="px-3 py-2 text-sm border border-border rounded-lg bg-surface text-foreground focus:outline-none focus:ring-2 focus:ring-accent"
                [value]="renameValue()"
                (input)="renameValue.set($any($event.target).value)"
                (keydown.enter)="confirmRename()"
                (keydown.escape)="cancelRename()"
                [style]="{ 'min-width': '200px' }"
              />
              <button
                type="button"
                class="w-8 h-8 flex items-center justify-center rounded-lg bg-accent text-white hover:opacity-90 transition"
                (click)="confirmRename()"
                aria-label="Save rename">
                <i class="pi pi-check text-xs"></i>
              </button>
              <button
                type="button"
                class="w-8 h-8 flex items-center justify-center rounded-lg border border-border text-foreground-muted hover:bg-surface-muted transition"
                (click)="cancelRename()"
                aria-label="Cancel rename">
                <i class="pi pi-times text-xs"></i>
              </button>
            </div>
            <span class="text-xs text-foreground-muted">Enter save · Esc cancel</span>
          } @else {
            <!-- Normal selector -->
            <p-select
              #tagSelect
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
            >
              <ng-template pTemplate="item" let-tag>
                <div class="flex items-center w-full group/tag">
                  <span class="text-sm">{{ tag.name }}</span>
                  <span class="ml-auto text-xs text-foreground-muted mr-2">{{ tag.usageCount }}</span>
                  <button
                    type="button"
                    class="w-6 h-6 flex items-center justify-center rounded text-foreground-muted opacity-30 group-hover/tag:opacity-100 focus-visible:opacity-100 hover:bg-surface-muted transition-opacity"
                    (click)="showTagActions($event, tag)"
                    aria-label="Actions for tag {{ tag.name }}">
                    <i class="pi pi-ellipsis-v text-xs"></i>
                  </button>
                </div>
              </ng-template>
              <ng-template pTemplate="selectedItem" let-tag>
                <span>{{ tag.name }}</span>
              </ng-template>
            </p-select>
            @if (selectedTag() && !hub.loading()) {
              <span class="text-sm text-foreground-muted">
                {{ summaryLine() }}
              </span>
            }
            @if (selectedTag() && hub.loading()) {
              <p-skeleton width="12rem" height="1rem" styleClass="inline-block" />
            }
          }
        </div>

        @if (!selectedTag()) {
          <!-- No tag selected -->
          <div class="flex flex-col items-center justify-center py-16">
            <div class="w-12 h-12 rounded-xl bg-surface-muted flex items-center justify-center mb-3">
              <i class="pi pi-tags text-xl text-foreground-muted" aria-hidden="true"></i>
            </div>
            <p class="text-sm font-medium text-foreground-secondary">Choose a tag</p>
            <p class="text-xs text-foreground-muted mt-1">Select a tag above to see related items</p>
          </div>
        } @else if (hub.loading()) {
          <!-- Skeleton loading -->
          <div class="space-y-4" role="status" aria-label="Loading tag items">
            <span class="sr-only">Loading tag items...</span>
            <div class="flex items-center gap-2 mb-3">
              <p-skeleton width="5rem" height="1.5rem" styleClass="rounded-full" />
              <div class="flex-1 h-px bg-border-muted"></div>
            </div>
            @for (i of skeletonRows; track i) {
              <div class="flex items-center gap-3 px-3 py-2.5">
                <p-skeleton width="1.75rem" height="1.75rem" styleClass="rounded-md" />
                <div class="flex-1 min-w-0 space-y-1.5">
                  <p-skeleton width="55%" height="0.875rem" />
                  <p-skeleton width="30%" height="0.75rem" />
                </div>
                <p-skeleton width="3rem" height="0.75rem" />
              </div>
            }
          </div>
        } @else if (hub.error()) {
          <app-error-state
            title="Something went wrong"
            message="We couldn't load the items for this tag"
            (retry)="retryLoad()"
          />
        } @else if (hub.items().length === 0) {
          <!-- Empty state -->
          <div class="flex flex-col items-center justify-center py-16">
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
                    <div
                      class="flex items-center gap-3 px-3 py-2.5 rounded-lg group hover:bg-surface-muted transition-colors cursor-pointer"
                      [attr.aria-label]="item.type + ': ' + item.title"
                      role="link"
                      tabindex="0"
                      (click)="navigateToItem(item)"
                      (keydown.enter)="navigateToItem(item)"
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

                      <!-- Relative date + open-in-new-tab button -->
                      <div class="flex items-center gap-2 shrink-0">
                        <span class="text-xs text-foreground-muted">{{ relativeDate(item.date) }}</span>
                        <button
                          type="button"
                          class="p-1 rounded hover:bg-surface text-foreground-muted opacity-0 group-hover:opacity-100 transition-opacity"
                          (click)="openInNewTab(item, $event)"
                          aria-label="Open in new tab"
                        >
                          <i class="pi pi-external-link text-xs" aria-hidden="true"></i>
                        </button>
                      </div>
                    </div>
                  }
                </div>
              </div>
            }
          </div>
        }
      }
    </div>

    <!-- Popup action menu (rendered at body level) -->
    <p-menu #tagActionMenu [model]="tagActionMenuItems()" [popup]="true" appendTo="body" />

    <!-- Delete confirmation dialog -->
    <p-dialog
      header="Delete tag?"
      [visible]="!!deletingTag()"
      (visibleChange)="$event || cancelDelete()"
      [modal]="true"
      [style]="{ width: '24rem' }"
      [draggable]="false">
      @if (deletingTag(); as tag) {
        <div class="flex flex-col gap-3">
          <p class="text-sm text-foreground">
            <span class="font-semibold">"{{ tag.name }}"</span> will be removed from:
          </p>
          @if (deleteBreakdown()) {
            <p class="text-sm text-foreground-secondary">{{ deleteBreakdown() }}</p>
          } @else {
            <p class="text-sm text-foreground-muted">This tag is not used by any items.</p>
          }
          <p class="text-xs text-foreground-muted">This cannot be undone.</p>
        </div>
        <div class="flex justify-end gap-2 mt-4">
          <button
            type="button"
            class="px-4 py-2 text-sm border border-border rounded-lg text-foreground-secondary hover:bg-surface-muted transition"
            (click)="cancelDelete()">
            Cancel
          </button>
          <button
            type="button"
            class="px-4 py-2 text-sm bg-danger text-white rounded-lg font-medium hover:opacity-90 transition"
            (click)="confirmDelete()">
            Delete tag
          </button>
        </div>
      }
    </p-dialog>

    <!-- Merge tag dialog -->
    <app-merge-tag-dialog
      [visible]="showMergeDialog()"
      [sourceTag]="mergeSourceTag()"
      [allTags]="tagService.tags()"
      (onClose)="onMergeClose()"
      (onMerge)="onMergeConfirm($event)"
    />
  `,
})
export class TagsPage implements OnInit, OnDestroy {
  readonly tagService = inject(TagService);
  readonly hub = inject(TagHubService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly headerService = inject(ContextualHeaderService);

  @ViewChild('tagActionMenu') tagActionMenu!: Menu;
  @ViewChild('tagSelect') tagSelect!: Select;
  @ViewChild('renameInput') renameInput?: ElementRef<HTMLInputElement>;

  readonly selectedTag = signal<Tag | null>(null);
  readonly tagCount = computed(() => this.tagService.tags().length);

  // Action menu state
  readonly actionTag = signal<Tag | null>(null);

  readonly tagActionMenuItems = computed<MenuItem[]>(() => [
    { label: 'Rename', icon: 'pi pi-pencil', command: () => this.startRename() },
    { label: 'Merge into...', icon: 'pi pi-arrow-right-arrow-left', command: () => this.startMerge() },
    { separator: true },
    { label: 'Delete', icon: 'pi pi-trash', styleClass: 'text-danger', command: () => this.startDelete() },
  ]);

  // Rename state
  readonly renamingTag = signal<Tag | null>(null);
  readonly renameValue = signal('');

  // Delete state
  readonly deletingTag = signal<Tag | null>(null);

  // Merge state
  readonly showMergeDialog = signal(false);
  readonly mergeSourceTag = signal<Tag | null>(null);

  readonly deleteBreakdown = computed(() => {
    const tag = this.deletingTag();
    if (!tag) return '';
    const parts: string[] = [];
    if (tag.meetingCount > 0) parts.push(`${tag.meetingCount} ${tag.meetingCount === 1 ? 'meeting' : 'meetings'}`);
    if (tag.noteCount > 0) parts.push(`${tag.noteCount} ${tag.noteCount === 1 ? 'note' : 'notes'}`);
    if (tag.taskCount > 0) parts.push(`${tag.taskCount} ${tag.taskCount === 1 ? 'task' : 'tasks'}`);
    return parts.join(' \u00b7 ');
  });

  readonly summaryLine = computed(() => {
    const m = this.hub.meetingCount();
    const n = this.hub.noteCount();
    const t = this.hub.taskCount();
    const parts: string[] = [];
    if (m > 0) parts.push(`${m} ${m === 1 ? 'meeting' : 'meetings'}`);
    if (n > 0) parts.push(`${n} ${n === 1 ? 'note' : 'notes'}`);
    if (t > 0) parts.push(`${t} ${t === 1 ? 'task' : 'tasks'}`);
    return parts.join(' \u00b7 ');
  });
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
    this.headerService.breadcrumb.set([{ label: 'Tag Hub' }]);
    this.tagService.loadTags();

    const selectedId = this.route.snapshot.queryParamMap.get('selected');
    if (selectedId) {
      this.pendingSelectedId.set(selectedId);
    }
  }

  ngOnDestroy(): void {
    this.headerService.clearContext();
  }

  onTagSelected(tag: Tag | null): void {
    this.selectedTag.set(tag);
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { selected: tag?.id ?? undefined },
      queryParamsHandling: 'merge',
    });
  }

  // --- Action menu ---

  showTagActions(event: Event, tag: Tag): void {
    event.stopPropagation();
    this.actionTag.set(tag);
    this.tagSelect.hide();
    // Anchor popup to the persistent p-select element instead of the now-removed dropdown overlay.
    // PrimeNG Menu.toggle() reads event.currentTarget for positioning.
    const anchor = new MouseEvent('click');
    Object.defineProperty(anchor, 'currentTarget', { value: this.tagSelect.el.nativeElement });
    this.tagActionMenu.toggle(anchor);
  }

  // --- Rename ---

  startRename(): void {
    const tag = this.actionTag();
    if (!tag) return;
    this.renamingTag.set(tag);
    this.renameValue.set(tag.name);
    // Focus input after render
    setTimeout(() => {
      this.renameInput?.nativeElement.focus();
      this.renameInput?.nativeElement.select();
    });
  }

  confirmRename(): void {
    const tag = this.renamingTag();
    const newName = this.renameValue().trim().toLowerCase();
    if (!tag || !newName || newName === tag.name) {
      this.cancelRename();
      return;
    }
    this.tagService.updateTag(tag.id, newName);
    // Update selectedTag reference if it was the renamed tag
    if (this.selectedTag()?.id === tag.id) {
      this.selectedTag.update(t => t ? { ...t, name: newName } : null);
    }
    this.renamingTag.set(null);
  }

  cancelRename(): void {
    this.renamingTag.set(null);
  }

  // --- Delete ---

  startDelete(): void {
    this.deletingTag.set(this.actionTag());
  }

  confirmDelete(): void {
    const tag = this.deletingTag();
    if (!tag) return;
    this.tagService.deleteTag(tag.id);
    // Clear selection if the deleted tag was selected
    if (this.selectedTag()?.id === tag.id) {
      this.onTagSelected(null);
    }
    this.deletingTag.set(null);
  }

  cancelDelete(): void {
    this.deletingTag.set(null);
  }

  // --- Merge ---

  startMerge(): void {
    this.mergeSourceTag.set(this.actionTag());
    this.showMergeDialog.set(true);
  }

  onMergeClose(): void {
    this.showMergeDialog.set(false);
    this.mergeSourceTag.set(null);
  }

  onMergeConfirm(event: { sourceId: string; targetId: string }): void {
    this.tagService.mergeTags(event.sourceId, event.targetId);
    this.showMergeDialog.set(false);
    this.mergeSourceTag.set(null);
    // If the merged source tag was selected, clear selection
    if (this.selectedTag()?.id === event.sourceId) {
      this.onTagSelected(null);
    }
  }

  // --- Existing methods ---

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

  navigateToItem(item: TagItemDto): void {
    this.router.navigate([this.itemUrl(item)]);
  }

  openInNewTab(item: TagItemDto, event: Event): void {
    event.stopPropagation();
    window.open(this.itemUrl(item), '_blank', 'noopener,noreferrer');
  }

  formatMeetingMeta(item: TagItemDto): string {
    const parts: string[] = [];
    if (item.meetingDate) {
      parts.push(formatShortDate(new Date(item.meetingDate)));
    }
    if (item.attendeeCount != null && item.attendeeCount > 0) {
      parts.push(`${item.attendeeCount} attendee${item.attendeeCount !== 1 ? 's' : ''}`);
    }
    return parts.join(' \u00b7 ') || 'Meeting';
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
