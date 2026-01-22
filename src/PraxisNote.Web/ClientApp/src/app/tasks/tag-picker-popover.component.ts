import { Component, computed, ElementRef, input, output, signal, viewChild, inject, Injector, afterNextRender, ChangeDetectionStrategy, HostListener } from '@angular/core';
import { Tag, TaskTag } from './tag.model';

@Component({
  selector: 'app-tag-picker-popover',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="w-56 bg-surface rounded-lg shadow-lg border border-border overflow-hidden"
      [class.absolute]="position() === 'below'"
      [class.left-0]="position() === 'below'"
      [class.top-full]="position() === 'below'"
      [class.mt-1]="position() === 'below'"
      [class.z-50]="position() === 'below'"
    >
      <!-- Search/Create input -->
      <div class="p-2 border-b border-border">
        <div class="relative">
          <i class="pi pi-search absolute left-2 top-1/2 -translate-y-1/2 text-xs text-foreground-muted/50"></i>
          <input
            #searchInput
            type="text"
            placeholder="Search or create tag..."
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
      </div>

      <!-- Tag list -->
      <div class="max-h-48 overflow-y-auto py-1">
        @if (filteredAvailableTags().length === 0 && !canCreate()) {
          <p class="text-xs text-foreground-muted py-3 text-center">No tags available</p>
        } @else if (filteredAvailableTags().length === 0 && canCreate()) {
          <p class="text-xs text-foreground-muted py-2 text-center">No matching tags</p>
        } @else {
          @for (tag of filteredAvailableTags(); track tag.id; let i = $index) {
            <button
              type="button"
              class="w-full flex items-center gap-2 px-3 py-1.5 text-sm text-left transition-colors"
              [class.bg-surface-hover]="selectedIndex() === i"
              [class.hover:bg-surface-hover]="selectedIndex() !== i"
              (click)="selectTag(tag); $event.stopPropagation()"
              (mouseenter)="selectedIndex.set(i)"
            >
              <span class="flex-1 truncate" [innerHTML]="highlightMatch(tag.name)"></span>
              <span class="text-xs text-foreground-muted">{{ tag.usageCount }}</span>
            </button>
          }
        }
      </div>

      <!-- Create option (sticky at bottom) -->
      @if (canCreate()) {
        <div class="border-t border-border px-3 py-2" [class.bg-primary/5]="filteredAvailableTags().length === 0">
          <button
            type="button"
            class="w-full flex items-center justify-center gap-2 py-1 rounded text-sm transition-colors text-primary hover:text-primary-hover"
            [class.font-medium]="filteredAvailableTags().length === 0"
            (click)="createTag(); $event.stopPropagation()"
            (mouseenter)="selectedIndex.set(filteredAvailableTags().length)"
          >
            <i class="pi pi-plus text-xs"></i>
            <span>Create "<strong>{{ searchText().trim() }}</strong>"</span>
            <kbd class="ml-1 text-[10px] px-1 py-0.5 bg-surface-muted rounded text-foreground-muted">↵</kbd>
          </button>
        </div>
      }
    </div>
  `,
})
export class TagPickerPopoverComponent {
  private readonly injector = inject(Injector);
  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);
  private initialized = false;

  readonly allTags = input.required<Tag[]>();
  readonly existingTagIds = input<string[]>([]);
  /** Position mode: 'below' positions absolutely below parent, 'static' lets parent control positioning */
  readonly position = input<'below' | 'static'>('below');

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

  /** Highlight matching portion of tag name */
  highlightMatch(tagName: string): string {
    const query = this.searchText().toLowerCase().trim();
    if (!query) return this.escapeHtml(tagName);

    const lowerName = tagName.toLowerCase();
    const index = lowerName.indexOf(query);
    if (index === -1) return this.escapeHtml(tagName);

    const before = tagName.slice(0, index);
    const match = tagName.slice(index, index + query.length);
    const after = tagName.slice(index + query.length);

    return `${this.escapeHtml(before)}<mark class="bg-warning/30 text-foreground px-0.5 rounded">${this.escapeHtml(match)}</mark>${this.escapeHtml(after)}`;
  }

  private escapeHtml(text: string): string {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
  }
}
