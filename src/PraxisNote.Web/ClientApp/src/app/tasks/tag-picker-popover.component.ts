import { Component, input, output, signal, computed, inject, ElementRef, viewChild, afterNextRender, Injector, HostListener, ChangeDetectionStrategy } from '@angular/core';
import { Tag, TaskTag } from './tag.model';

/** Default colors for new tags */
const TAG_COLORS = [
  '#bf616a', // Aurora red
  '#d08770', // Aurora orange
  '#ebcb8b', // Aurora yellow
  '#a3be8c', // Aurora green
  '#88c0d0', // Frost cyan
  '#5e81ac', // Frost blue
  '#b48ead', // Aurora purple
];

@Component({
  selector: 'app-tag-picker-popover',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="absolute z-50 left-0 top-full mt-1 w-56 bg-surface rounded-lg shadow-lg border border-border p-2">
      <!-- Search/create input -->
      <div class="relative mb-2">
        <input
          #searchInput
          type="text"
          [value]="searchQuery()"
          (input)="searchQuery.set(asInput($event).value)"
          (keydown.enter)="handleEnter(); $event.stopPropagation()"
          (keydown.escape)="onClose.emit(); $event.stopPropagation()"
          placeholder="Search or create..."
          class="w-full h-8 px-2 text-sm text-foreground placeholder-foreground-muted bg-surface-muted rounded focus:outline-none focus:ring-1 focus:ring-accent-solid"
          aria-label="Search or create tag"
        >
      </div>

      <!-- Tag list -->
      <div class="max-h-40 overflow-y-auto space-y-1">
        @for (tag of filteredTags(); track tag.id) {
          @if (!isTagSelected(tag.id)) {
            <button
              type="button"
              class="w-full flex items-center gap-2 px-2 py-1.5 text-sm text-foreground rounded hover:bg-surface-muted transition-colors"
              (click)="selectTag(tag); $event.stopPropagation()"
              [attr.aria-label]="'Add tag: ' + tag.name"
            >
              <span
                class="w-3 h-3 rounded-full shrink-0"
                [style.background-color]="tag.color"
              ></span>
              <span class="flex-1 text-left truncate">{{ tag.name }}</span>
              <span class="text-xs text-foreground-muted">{{ tag.usageCount }}</span>
            </button>
          }
        } @empty {
          @if (!canCreateTag()) {
            <p class="text-sm text-foreground-muted text-center py-2">No tags found</p>
          }
        }
      </div>

      <!-- Create new tag option -->
      @if (canCreateTag()) {
        <div class="mt-2 pt-2 border-t border-border">
          <div class="flex items-center gap-2 mb-2">
            <span class="text-xs text-foreground-muted">Create:</span>
            <span class="text-sm font-medium text-foreground">{{ searchQuery() }}</span>
          </div>
          <div class="flex gap-1">
            @for (color of tagColors; track color) {
              <button
                type="button"
                class="w-6 h-6 rounded-full transition-transform hover:scale-110"
                [style.background-color]="color"
                [class.ring-2]="selectedColor() === color"
                [class.ring-foreground]="selectedColor() === color"
                (click)="selectColor(color); $event.stopPropagation()"
                [attr.aria-label]="'Select color ' + color"
              ></button>
            }
          </div>
          <button
            type="button"
            class="mt-2 w-full px-2 py-1.5 text-sm text-accent-solid hover:bg-accent rounded transition-colors"
            (click)="createTag(); $event.stopPropagation()"
            [disabled]="!selectedColor()"
          >
            <i class="pi pi-plus text-xs mr-1"></i>
            Create "{{ searchQuery() }}"
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
  readonly selectedTags = input.required<TaskTag[]>();

  readonly onSelect = output<Tag>();
  readonly onCreate = output<{ name: string; color: string }>();
  readonly onClose = output<void>();

  readonly searchQuery = signal('');
  readonly selectedColor = signal<string | null>(null);
  readonly searchInput = viewChild<ElementRef<HTMLInputElement>>('searchInput');

  readonly tagColors = TAG_COLORS;

  readonly filteredTags = computed(() => {
    const query = this.searchQuery().toLowerCase().trim();
    const tags = this.allTags();
    if (!query) return tags;
    return tags.filter(t => t.name.toLowerCase().includes(query));
  });

  readonly canCreateTag = computed(() => {
    const query = this.searchQuery().trim();
    if (!query) return false;
    // Check if exact name exists
    return !this.allTags().some(t => t.name.toLowerCase() === query.toLowerCase());
  });

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

  isTagSelected(tagId: string): boolean {
    return this.selectedTags().some(t => t.id === tagId);
  }

  selectTag(tag: Tag): void {
    this.onSelect.emit(tag);
    this.searchQuery.set('');
  }

  selectColor(color: string): void {
    this.selectedColor.set(color);
  }

  createTag(): void {
    const name = this.searchQuery().trim();
    const color = this.selectedColor();
    if (name && color) {
      this.onCreate.emit({ name, color });
      this.searchQuery.set('');
      this.selectedColor.set(null);
    }
  }

  handleEnter(): void {
    const filtered = this.filteredTags().filter(t => !this.isTagSelected(t.id));
    if (filtered.length === 1) {
      this.selectTag(filtered[0]);
    } else if (this.canCreateTag() && this.selectedColor()) {
      this.createTag();
    }
  }

  asInput(event: Event): HTMLInputElement {
    return event.target as HTMLInputElement;
  }
}
