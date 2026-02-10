import { Component, ChangeDetectionStrategy, input, output, computed, signal, inject, DestroyRef, afterNextRender, Injector, viewChild, ElementRef } from '@angular/core';
import { generateHTML } from '@tiptap/core';
import { Note, NoteTag } from './note.model';
import { tiptapExtensions } from './tiptap-extensions';
import { Tag } from '../tags/tag.model';
import { DeleteConfirmationService } from '../shared/services/delete-confirmation.service';
import { DeleteConfirmButtonComponent } from '../shared/components/delete-confirm-button.component';

@Component({
  selector: 'app-note-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [DeleteConfirmationService],
  imports: [DeleteConfirmButtonComponent],
  template: `
    <div
      class="note-card bg-surface-subtle rounded-md border border-border hover:shadow-lg transition-all cursor-pointer group"
      role="button"
      tabindex="0"
      (click)="onOpen.emit()"
      (keydown.enter)="handleCardKeydown(asKeyboardEvent($event))"
      (keydown.space)="handleCardKeydown(asKeyboardEvent($event))"
    >
      <div class="p-3">
        <!-- Title (extracted from first heading) -->
        @if (cardTitle()) {
          <h3 class="card-title">{{ cardTitle() }}</h3>
        }

        <!-- Rich content preview -->
        @if (contentHtml()) {
          <div class="note-preview-wrapper">
            <div class="note-preview" [innerHTML]="contentHtml()"></div>
            <div class="note-preview-fade"></div>
          </div>
        } @else if (!cardTitle()) {
          <p class="text-sm text-foreground-muted italic">Empty note</p>
        }

        <!-- Checkboxes preview -->
        @if (note().checkboxes.length > 0) {
          <div class="mt-3 space-y-1.5">
            @for (checkbox of visibleCheckboxes(); track checkbox.id) {
              <div class="flex items-start gap-2 text-sm">
                <div
                  class="note-checkbox mt-0.5"
                  [class.checked]="checkbox.isChecked"
                ></div>
                <span
                  class="flex-1"
                  [class.line-through]="checkbox.isChecked"
                  [class.text-foreground-muted]="checkbox.isChecked"
                  [class.text-foreground]="!checkbox.isChecked"
                >
                  {{ checkbox.text }}
                </span>
              </div>
            }
            @if (hiddenCheckboxCount() > 0) {
              <p class="text-xs text-foreground-muted">
                +{{ hiddenCheckboxCount() }} more items
              </p>
            }
          </div>
        }

        <!-- Tags (interactive) -->
        @if (note().tags.length > 0 || showTagPicker()) {
          <div class="mt-3 flex flex-wrap items-center gap-1">
            @for (tag of visibleTags(); track tag.id) {
              <span class="tag-badge">
                {{ tag.name }}
                <button
                  type="button"
                  class="tag-badge-remove"
                  (click)="removeTag(tag.id); $event.stopPropagation()"
                  [attr.aria-label]="'Remove tag ' + tag.name"
                >
                  <i class="pi pi-times"></i>
                </button>
              </span>
            }
            @if (hiddenTagCount() > 0 && !showTagPicker()) {
              <button
                type="button"
                class="overflow-btn"
                (click)="inlineTagsExpanded.set(true); $event.stopPropagation()"
                [attr.aria-label]="'Show ' + hiddenTagCount() + ' more tags'"
              >
                +{{ hiddenTagCount() }}
              </button>
            }
            @if (showTagPicker()) {
              <div class="flex-1 min-w-[100px] relative">
                <input
                  #inlineTagInput
                  type="text"
                  [placeholder]="note().tags.length > 0 ? 'Add tag...' : 'Add first tag...'"
                  [value]="tagSearch()"
                  (input)="tagSearch.set(asInput($event).value)"
                  (keydown.enter)="onTagEnter(); $event.preventDefault()"
                  (keydown.escape)="showTagPicker.set(false); $event.stopPropagation()"
                  (click)="$event.stopPropagation()"
                  class="tag-search-input"
                  aria-label="Search or create tag"
                >
                @if (tooltipSuggestions().length > 0 || canCreateTag()) {
                  <div class="tag-dropdown">
                    @for (tag of tooltipSuggestions(); track tag.id) {
                      @let parts = getTagHighlightParts(tag.name);
                      <button
                        type="button"
                        class="tag-dropdown-item"
                        (click)="addTag({ id: tag.id, name: tag.name }); $event.stopPropagation()"
                      >
                        <span>{{ parts.before }}@if (parts.match) {<mark class="search-highlight">{{ parts.match }}</mark>}{{ parts.after }}</span>
                        <span class="text-foreground-muted">{{ tag.usageCount }}</span>
                      </button>
                    }
                    @if (canCreateTag()) {
                      @if (tooltipSuggestions().length > 0) {
                        <div class="tag-dropdown-divider"></div>
                      }
                      <button
                        type="button"
                        class="tag-dropdown-item create"
                        (click)="createAndAddTag(tagSearch().trim()); $event.stopPropagation()"
                      >
                        <i class="pi pi-plus text-[10px] mr-1"></i>
                        Create "{{ tagSearch().trim() }}"
                      </button>
                    }
                  </div>
                }
              </div>
            } @else {
              <!-- Add tag + button, visible on hover -->
              <button
                type="button"
                class="add-tag-card-btn"
                (click)="openInlineTagInput(); $event.stopPropagation()"
                aria-label="Add tag"
              >
                <i class="pi pi-plus" style="font-size:9px"></i>
              </button>
            }
          </div>
        }
        <!-- Hover-only add tag button when no tags exist yet -->
        @if (note().tags.length === 0 && !showTagPicker()) {
          <div class="mt-2 add-tag-card-row">
            <button
              type="button"
              class="add-tag-label-btn"
              (click)="openInlineTagInput(); $event.stopPropagation()"
              aria-label="Add tag"
            >
              <i class="pi pi-tag" style="font-size:9px"></i>
              <span>Tag</span>
            </button>
          </div>
        }
      </div>

      <!-- Footer with timestamp and actions -->
      <div class="px-3 pb-2 flex items-center justify-between">
        <span class="text-xs text-foreground-muted">
          {{ formatRelativeTime(note().updatedAt) }}
        </span>

        <!-- Delete actions: mobile (always visible) -->
        <div class="flex md:hidden items-center gap-1">
          @if (confirmingDelete()) {
            <app-delete-confirm-button
              ariaLabel="Confirm delete note"
              (onConfirm)="confirmDelete()"
              (click)="$event.stopPropagation()"
            />
          } @else {
            <button
              type="button"
              class="touch-target p-1.5 text-foreground-muted hover:text-danger rounded transition-colors"
              (click)="startDeleteConfirm(); $event.stopPropagation()"
              aria-label="Delete note"
            >
              <i class="pi pi-trash text-xs"></i>
            </button>
          }
        </div>
        <!-- Delete actions: desktop (hover/focus-reveal without layout shift) -->
        <div class="hidden md:flex md:opacity-0 md:pointer-events-none md:group-hover:opacity-100 md:group-hover:pointer-events-auto md:group-focus-within:opacity-100 md:group-focus-within:pointer-events-auto items-center gap-1 transition-opacity">
          @if (confirmingDelete()) {
            <app-delete-confirm-button
              ariaLabel="Confirm delete note"
              (onConfirm)="confirmDelete()"
              (click)="$event.stopPropagation()"
            />
          } @else {
            <button
              type="button"
              class="touch-target p-1.5 text-foreground-muted hover:text-danger rounded transition-colors"
              (click)="startDeleteConfirm(); $event.stopPropagation()"
              aria-label="Delete note"
            >
              <i class="pi pi-trash text-xs"></i>
            </button>
          }
        </div>
      </div>
    </div>
  `,
  styles: [`
    /* Title */
    .card-title {
      font-size: 0.875rem;
      font-weight: 700;
      color: var(--color-foreground);
      margin: 0 0 0.5rem 0;
      line-height: 1.3;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }

    /* Rich preview container */
    .note-preview-wrapper {
      position: relative;
      max-height: 140px;
      overflow: hidden;
    }

    .note-preview-fade {
      position: absolute;
      bottom: 0;
      left: 0;
      right: 0;
      height: 24px;
      background: linear-gradient(to top, var(--color-surface-subtle), transparent);
      pointer-events: none;
    }

    /* Rich preview typography (scaled down) — ::ng-deep needed for innerHTML content */
    .note-preview {
      font-size: 0.75rem;
      line-height: 1.5;
      color: var(--color-foreground);
    }

    :host ::ng-deep .note-preview > :first-child {
      margin-top: 0;
    }

    :host ::ng-deep .note-preview h1 {
      font-size: 0.85rem;
      font-weight: 700;
      margin: 0.3em 0;
    }

    :host ::ng-deep .note-preview h2 {
      font-size: 0.8rem;
      font-weight: 600;
      margin: 0.3em 0;
    }

    :host ::ng-deep .note-preview h3 {
      font-size: 0.75rem;
      font-weight: 600;
      margin: 0.2em 0;
    }

    :host ::ng-deep .note-preview p {
      margin: 0.25em 0;
    }

    :host ::ng-deep .note-preview ul {
      padding-left: 1.2em;
      margin: 0.25em 0;
      list-style: disc;
    }

    :host ::ng-deep .note-preview ol {
      padding-left: 1.2em;
      margin: 0.25em 0;
      list-style: decimal;
    }

    :host ::ng-deep .note-preview li {
      margin: 0.1em 0;
    }

    :host ::ng-deep .note-preview ul[data-type="taskList"] {
      list-style: none;
      padding-left: 0;
    }

    :host ::ng-deep .note-preview ul[data-type="taskList"] li {
      display: flex;
      align-items: flex-start;
      gap: 0.35em;
    }

    :host ::ng-deep .note-preview ul[data-type="taskList"] li label {
      flex-shrink: 0;
      margin-top: 0.15em;
    }

    :host ::ng-deep .note-preview ul[data-type="taskList"] li label input[type="checkbox"] {
      width: 12px;
      height: 12px;
      accent-color: var(--color-accent-solid);
      pointer-events: none;
    }

    :host ::ng-deep .note-preview ul[data-type="taskList"] li[data-checked="true"] > div {
      text-decoration: line-through;
      color: var(--color-foreground-muted);
    }

    :host ::ng-deep .note-preview ul[data-type="taskList"] li > div {
      flex: 1;
    }

    :host ::ng-deep .note-preview blockquote {
      border-left: 2px solid var(--color-border);
      padding-left: 0.6em;
      margin: 0.3em 0;
      color: var(--color-foreground-secondary);
    }

    :host ::ng-deep .note-preview code {
      background: var(--color-surface-default);
      padding: 0.1em 0.3em;
      border-radius: 3px;
      font-family: monospace;
      font-size: 0.85em;
    }

    :host ::ng-deep .note-preview pre {
      background: var(--color-surface-muted);
      padding: 0.4em 0.6em;
      border-radius: 4px;
      font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
      font-size: 0.8em;
      overflow: hidden;
      margin: 0.3em 0;
      line-height: 1.4;
    }

    :host ::ng-deep .note-preview pre code {
      background: none;
      padding: 0;
    }

    :host ::ng-deep .note-preview mark {
      background: var(--color-editor-mark);
      padding: 0 0.15em;
      border-radius: 2px;
    }

    :host ::ng-deep .note-preview hr {
      border: none;
      border-top: 1px solid var(--color-border);
      margin: 0.4em 0;
    }

    :host ::ng-deep .note-preview img {
      max-width: 100%;
      height: auto;
      border-radius: 3px;
      margin: 0.25em 0;
    }

    :host ::ng-deep .note-preview table {
      border-collapse: collapse;
      width: 100%;
      margin: 0.3em 0;
    }

    :host ::ng-deep .note-preview th,
    :host ::ng-deep .note-preview td {
      border: 1px solid var(--color-border);
      padding: 0.2em 0.4em;
      text-align: left;
      font-size: 0.85em;
    }

    :host ::ng-deep .note-preview th {
      background: var(--color-surface-hover);
      font-weight: 600;
    }

    :host ::ng-deep .note-preview a {
      color: var(--color-accent-solid);
      text-decoration: underline;
    }

    /* Checkboxes */
    .note-checkbox {
      width: 14px;
      height: 14px;
      border: 2px solid var(--color-border-default);
      border-radius: 3px;
      flex-shrink: 0;
    }
    .note-checkbox.checked {
      background: var(--color-accent-solid, #5e81ac);
      border-color: var(--color-accent-solid, #5e81ac);
    }
    .note-checkbox.checked::after {
      content: '\\2713';
      color: white;
      font-size: 9px;
      font-weight: bold;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    /* Tags */
    .tag-badge {
      display: inline-flex;
      align-items: center;
      gap: 4px;
      background: var(--color-tag-bg);
      color: var(--color-tag-text);
      font-size: 10px;
      font-weight: 500;
      padding: 2px 8px;
      border-radius: 9999px;
      height: 18px;
    }

    .tag-badge-remove {
      all: unset;
      position: relative;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      cursor: pointer;
      color: var(--color-tag-text);
      opacity: 0;
      transition: opacity 0.15s;
      font-size: 7px;
    }

    .tag-badge-remove::before {
      content: '';
      position: absolute;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%);
      width: 44px;
      height: 44px;
    }

    .tag-badge:hover .tag-badge-remove,
    .tag-badge:focus-within .tag-badge-remove {
      opacity: 0.6;
    }

    .tag-badge-remove:hover {
      opacity: 1 !important;
    }

    .tag-badge-remove:focus-visible {
      opacity: 1 !important;
      outline: 2px solid var(--color-tag-text);
      outline-offset: 1px;
    }

    .overflow-btn {
      padding: 2px 6px;
      border-radius: 9999px;
      font-size: 10px;
      background: var(--color-tag-bg);
      color: var(--color-foreground-muted);
      border: none;
      cursor: pointer;
      transition: all 0.15s;
    }

    .overflow-btn:hover {
      color: var(--color-tag-text);
    }

    /* Add tag button on card (hidden until card hover) */
    .add-tag-card-btn {
      position: relative;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 18px;
      height: 18px;
      border-radius: 9999px;
      color: var(--color-foreground-muted);
      opacity: 0;
      cursor: pointer;
      transition: all 0.15s;
      background: none;
      border: none;
    }

    .add-tag-card-btn::before {
      content: '';
      position: absolute;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%);
      width: 44px;
      height: 44px;
    }

    .note-card:hover .add-tag-card-btn,
    .note-card:focus-within .add-tag-card-btn {
      opacity: 0.4;
    }

    .add-tag-card-btn:hover {
      opacity: 1 !important;
      color: var(--color-tag-text);
      background: var(--color-tag-bg);
    }

    .add-tag-card-btn:focus-visible {
      opacity: 1 !important;
      color: var(--color-tag-text);
      background: var(--color-tag-bg);
      outline: 2px solid var(--color-tag-text);
      outline-offset: 1px;
    }

    /* Hover-only "Tag" label button for untagged notes */
    .add-tag-card-row {
      opacity: 0;
      transition: opacity 0.15s;
    }

    .note-card:hover .add-tag-card-row,
    .note-card:focus-within .add-tag-card-row {
      opacity: 1;
    }

    .add-tag-label-btn {
      all: unset;
      display: inline-flex;
      align-items: center;
      gap: 4px;
      padding: 2px 10px;
      height: 18px;
      border-radius: 9999px;
      border: 1px dashed var(--color-border-default);
      color: var(--color-foreground-muted);
      font-size: 10px;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.15s;
    }

    .add-tag-label-btn:hover {
      border-color: var(--color-tag-text);
      color: var(--color-tag-text);
      background: var(--color-tag-bg);
    }

    .add-tag-label-btn:focus-visible {
      outline: 2px solid var(--color-tag-text);
      outline-offset: 2px;
    }

    /* Tag search input */
    .tag-search-input {
      width: 100%;
      height: 24px;
      padding: 0 8px;
      font-size: 12px;
      background: var(--color-surface-muted);
      border-radius: 9999px;
      border: none;
      outline: none;
      color: var(--color-foreground);
    }

    /* Tag dropdown */
    .tag-dropdown {
      position: absolute;
      left: 0;
      top: calc(100% + 4px);
      width: 192px;
      max-height: 240px;
      overflow-y: auto;
      background: var(--color-surface);
      border-radius: 8px;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
      border: 1px solid var(--color-border-default);
      padding: 4px 0;
      z-index: 50;
    }

    .tag-dropdown-item {
      all: unset;
      display: flex;
      align-items: center;
      justify-content: space-between;
      width: 100%;
      padding: 6px 12px;
      font-size: 12px;
      cursor: pointer;
      transition: background 0.15s;
      box-sizing: border-box;
      color: var(--color-foreground);
    }

    .tag-dropdown-item:hover,
    .tag-dropdown-item:focus-visible {
      background: var(--color-surface-subtle);
    }

    .tag-dropdown-item:focus-visible {
      outline: 2px solid var(--color-border-default);
      outline-offset: -2px;
    }

    .tag-dropdown-item.create {
      color: var(--color-accent-solid);
    }

    .tag-dropdown-divider {
      border-top: 1px solid var(--color-border-default);
      margin: 4px 0;
    }
  `],
})
export class NoteCardComponent {
  private readonly deleteConfirmation = inject(DeleteConfirmationService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly injector = inject(Injector);

  readonly note = input.required<Note>();
  readonly allTags = input<Tag[]>([]);
  readonly onOpen = output<void>();
  readonly onDelete = output<void>();
  readonly onAddTag = output<NoteTag>();
  readonly onRemoveTag = output<string>();
  readonly onCreateTag = output<string>();

  readonly confirmingDelete = signal(false);
  readonly showTagPicker = signal(false);
  readonly inlineTagsExpanded = signal(false);
  readonly tagSearch = signal('');
  readonly inlineTagInput = viewChild<ElementRef<HTMLInputElement>>('inlineTagInput');

  private readonly maxVisibleCheckboxes = 4;
  private readonly MAX_VISIBLE_TAGS = 3;

  /** Parsed TipTap JSON doc (null if not valid JSON) */
  private readonly parsedContent = computed(() => {
    const content = this.note().content;
    if (!content) return null;
    try {
      const parsed = JSON.parse(content);
      if (parsed.type === 'doc' && parsed.content) return parsed;
    } catch { /* not JSON */ }
    return null;
  });

  /** Extract the first heading as the card title */
  readonly cardTitle = computed(() => {
    const doc = this.parsedContent();
    if (!doc?.content?.length) return '';

    const firstNode = doc.content[0];
    if (firstNode.type === 'heading') {
      return this.extractText(firstNode).trim();
    }
    return '';
  });

  /** Generate rich HTML preview from the remaining content (after title) */
  readonly contentHtml = computed(() => {
    const doc = this.parsedContent();
    if (!doc?.content?.length) {
      // Plain text fallback
      const content = this.note().content;
      if (content && !this.parsedContent()) return `<p style="white-space:pre-wrap">${this.escapeHtml(content)}</p>`;
      return '';
    }

    const hasTitle = this.cardTitle();
    const nodes = hasTitle ? doc.content.slice(1) : doc.content;

    if (nodes.length === 0) return '';

    const previewDoc = { type: 'doc', content: nodes };
    try {
      return generateHTML(previewDoc, tiptapExtensions);
    } catch {
      return '';
    }
  });

  readonly visibleCheckboxes = computed(() =>
    this.note().checkboxes.slice(0, this.maxVisibleCheckboxes)
  );

  readonly hiddenCheckboxCount = computed(() =>
    Math.max(0, this.note().checkboxes.length - this.maxVisibleCheckboxes)
  );

  readonly visibleTags = computed(() => {
    const tags = this.note().tags;
    if (this.inlineTagsExpanded() || this.showTagPicker() || tags.length <= this.MAX_VISIBLE_TAGS) {
      return tags;
    }
    return tags.slice(0, this.MAX_VISIBLE_TAGS);
  });

  readonly hiddenTagCount = computed(() => {
    const tags = this.note().tags;
    if (this.inlineTagsExpanded() || this.showTagPicker() || tags.length <= this.MAX_VISIBLE_TAGS) {
      return 0;
    }
    return tags.length - this.MAX_VISIBLE_TAGS;
  });

  readonly existingTagIds = computed(() => new Set(this.note().tags.map(t => t.id)));

  readonly tooltipSuggestions = computed(() => {
    const query = this.tagSearch().toLowerCase().trim();
    const existingIds = this.existingTagIds();
    const available = this.allTags().filter(tag => !existingIds.has(tag.id));
    if (!query) return available.slice(0, 4);
    return available
      .filter(tag => tag.name.toLowerCase().includes(query))
      .slice(0, 4);
  });

  readonly canCreateTag = computed(() => {
    const query = this.tagSearch().trim();
    if (!query || query.length < 2) return false;
    return !this.allTags().some(tag =>
      tag.name.toLowerCase() === query.toLowerCase()
    );
  });

  constructor() {
    this.destroyRef.onDestroy(() => {
      this.deleteConfirmation.cleanup();
    });
  }

  // Tag methods
  openInlineTagInput(): void {
    this.showTagPicker.set(true);
    this.tagSearch.set('');
    afterNextRender(() => {
      this.inlineTagInput()?.nativeElement.focus();
    }, { injector: this.injector });
  }

  onTagEnter(): void {
    const query = this.tagSearch().trim();
    const suggestions = this.tooltipSuggestions();

    const exactMatch = suggestions.find(t =>
      t.name.toLowerCase() === query.toLowerCase()
    );
    if (exactMatch) {
      this.addTag({ id: exactMatch.id, name: exactMatch.name });
      return;
    }

    if (this.canCreateTag()) {
      this.createAndAddTag(query);
      return;
    }

    if (suggestions.length === 1) {
      this.addTag({ id: suggestions[0].id, name: suggestions[0].name });
    }
  }

  addTag(tag: NoteTag): void {
    if (this.note().tags.some(t => t.id === tag.id)) {
      this.showTagPicker.set(false);
      this.tagSearch.set('');
      return;
    }
    this.onAddTag.emit(tag);
    this.showTagPicker.set(false);
    this.tagSearch.set('');
  }

  removeTag(tagId: string): void {
    this.onRemoveTag.emit(tagId);
  }

  createAndAddTag(name: string): void {
    this.onCreateTag.emit(name);
    this.showTagPicker.set(false);
    this.tagSearch.set('');
  }

  getTagHighlightParts(tagName: string): { before: string; match: string; after: string } {
    const query = this.tagSearch().toLowerCase().trim();
    if (!query) return { before: tagName, match: '', after: '' };
    const lowerName = tagName.toLowerCase();
    const index = lowerName.indexOf(query);
    if (index === -1) return { before: tagName, match: '', after: '' };
    return {
      before: tagName.slice(0, index),
      match: tagName.slice(index, index + query.length),
      after: tagName.slice(index + query.length),
    };
  }

  startDeleteConfirm(): void {
    this.deleteConfirmation.cleanup();
    this.confirmingDelete.set(true);
    this.deleteConfirmation.start(() => {
      this.confirmingDelete.set(false);
    });
  }

  confirmDelete(): void {
    this.deleteConfirmation.cleanup();
    this.confirmingDelete.set(false);
    this.onDelete.emit();
  }

  /** Type-safe helper for keyboard events */
  asKeyboardEvent(event: Event): KeyboardEvent {
    return event as KeyboardEvent;
  }

  /** Type-safe helper for input events */
  asInput(event: Event): HTMLInputElement {
    return event.target as HTMLInputElement;
  }

  /** Handle keydown on card, preventing bubbled events from child controls */
  handleCardKeydown(event: KeyboardEvent): void {
    if (event.target === event.currentTarget) {
      if (event.key === ' ') {
        event.preventDefault();
      }
      this.onOpen.emit();
    }
  }

  formatRelativeTime(dateString: string): string {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMins < 1) return 'just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;

    return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
  }

  /** Recursively extract plain text from a TipTap node */
  private extractText(node: { type: string; text?: string; content?: unknown[] }): string {
    if (node.type === 'text' && node.text) return node.text;
    if (!node.content) return '';
    return (node.content as { type: string; text?: string; content?: unknown[] }[])
      .map(child => this.extractText(child))
      .join('');
  }

  private escapeHtml(text: string): string {
    return text
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;');
  }
}
