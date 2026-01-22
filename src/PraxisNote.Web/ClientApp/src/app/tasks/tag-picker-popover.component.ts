import { Component, computed, ElementRef, input, output, signal, viewChild, inject, Injector, afterNextRender, ChangeDetectionStrategy, HostListener } from '@angular/core';
import { Tag, TaskTag } from './tag.model';

@Component({
  selector: 'app-tag-picker-popover',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="absolute left-0 top-full mt-1 z-50 w-56 bg-surface rounded-lg shadow-lg border border-border p-2">
      <!-- Search/Create input -->
      <div class="relative mb-2">
        <i class="pi pi-search absolute left-2 top-1/2 -translate-y-1/2 text-xs text-foreground-muted/50"></i>
        <input
          #searchInput
          type="text"
          placeholder="Search or create..."
          [value]="searchText()"
          (input)="searchText.set(asInput($event).value)"
          (keydown.enter)="onEnter(); $event.preventDefault()"
          (keydown.escape)="onClose.emit(); $event.stopPropagation()"
          (keydown.arrowDown)="selectNext(); $event.preventDefault()"
          (keydown.arrowUp)="selectPrevious(); $event.preventDefault()"
          class="w-full h-8 pl-7 pr-2 text-sm text-foreground bg-surface-muted rounded border-0 focus:outline-none focus:ring-1 focus:ring-primary"
          aria-label="Search or create tags"
        >
      </div>

      <!-- Tag list -->
      <div class="max-h-48 overflow-y-auto">
        @if (filteredAvailableTags().length === 0 && !canCreate()) {
          <p class="text-xs text-foreground-muted py-2 text-center">No tags available</p>
        } @else {
          @for (tag of filteredAvailableTags(); track tag.id; let i = $index) {
            <button
              type="button"
              class="w-full flex items-center gap-2 px-2 py-1.5 rounded text-sm text-left transition-colors"
              [class.bg-surface-hover]="selectedIndex() === i"
              [class.hover:bg-surface-hover]="selectedIndex() !== i"
              (click)="selectTag(tag); $event.stopPropagation()"
              (mouseenter)="selectedIndex.set(i)"
            >
              <span class="w-2 h-2 rounded-full bg-tag shrink-0"></span>
              <span class="flex-1 truncate">{{ tag.name }}</span>
              <span class="text-xs text-foreground-muted">{{ tag.usageCount }}</span>
            </button>
          }

          <!-- Create option -->
          @if (canCreate()) {
            <button
              type="button"
              class="w-full flex items-center gap-2 px-2 py-1.5 rounded text-sm text-left transition-colors border-t border-border mt-1 pt-2"
              [class.bg-surface-hover]="selectedIndex() === filteredAvailableTags().length"
              [class.hover:bg-surface-hover]="selectedIndex() !== filteredAvailableTags().length"
              (click)="createTag(); $event.stopPropagation()"
              (mouseenter)="selectedIndex.set(filteredAvailableTags().length)"
            >
              <i class="pi pi-plus text-xs text-primary"></i>
              <span class="text-primary">Create "{{ searchText().trim() }}"</span>
            </button>
          }
        }
      </div>
    </div>
  `,
})
export class TagPickerPopoverComponent {
  private readonly injector = inject(Injector);
  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);
  private initialized = false;

  readonly allTags = input.required<Tag[]>();
  readonly existingTagIds = input<string[]>([]);

  readonly onSelect = output<TaskTag>();
  readonly onCreate = output<string>();
  readonly onClose = output<void>();

  readonly searchText = signal('');
  readonly selectedIndex = signal(0);
  readonly searchInput = viewChild<ElementRef<HTMLInputElement>>('searchInput');

  /** Tags that aren't already on the task */
  readonly availableTags = computed(() => {
    const existing = new Set(this.existingTagIds());
    return this.allTags().filter(tag => !existing.has(tag.id));
  });

  /** Filtered by search text */
  readonly filteredAvailableTags = computed(() => {
    const query = this.searchText().toLowerCase().trim();
    if (!query) return this.availableTags();
    return this.availableTags().filter(tag =>
      tag.name.toLowerCase().includes(query)
    );
  });

  /** Can create a new tag with this name */
  readonly canCreate = computed(() => {
    const query = this.searchText().trim();
    if (!query) return false;
    // Check if exact match already exists (case-insensitive)
    return !this.allTags().some(tag =>
      tag.name.toLowerCase() === query.toLowerCase()
    );
  });

  /** Total selectable items (tags + create option) */
  readonly totalItems = computed(() =>
    this.filteredAvailableTags().length + (this.canCreate() ? 1 : 0)
  );

  constructor() {
    afterNextRender(() => {
      this.searchInput()?.nativeElement.focus();
      this.initialized = true;
    }, { injector: this.injector });
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    if (!this.initialized) return;
    const target = event.target;
    if (!(target instanceof Node)) return;
    if (!this.elementRef.nativeElement.contains(target)) {
      this.onClose.emit();
    }
  }

  asInput(event: Event): HTMLInputElement {
    return event.target as HTMLInputElement;
  }

  selectNext(): void {
    const total = this.totalItems();
    if (total === 0) return;
    this.selectedIndex.update(i => (i + 1) % total);
  }

  selectPrevious(): void {
    const total = this.totalItems();
    if (total === 0) return;
    this.selectedIndex.update(i => (i - 1 + total) % total);
  }

  onEnter(): void {
    const index = this.selectedIndex();
    const tags = this.filteredAvailableTags();

    if (index < tags.length) {
      this.selectTag(tags[index]);
    } else if (this.canCreate()) {
      this.createTag();
    }
  }

  selectTag(tag: Tag): void {
    this.onSelect.emit({ id: tag.id, name: tag.name });
  }

  createTag(): void {
    const name = this.searchText().trim();
    if (name) {
      this.onCreate.emit(name);
      this.searchText.set('');
      this.selectedIndex.set(0);
    }
  }
}
