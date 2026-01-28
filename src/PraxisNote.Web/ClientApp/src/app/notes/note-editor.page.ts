import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal,
  computed,
  OnInit,
  OnDestroy,
  HostListener,
  ElementRef,
  viewChild,
  afterNextRender,
  Injector,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, debounceTime, takeUntil } from 'rxjs';
import { Note, CheckboxStatus, NoteTag } from './note.model';
import { NoteService } from './note.service';
import { PdfExportService } from './pdf-export.service';
import { TiptapEditorComponent } from './tiptap-editor.component';
import { ToastService } from '../shared/services/toast.service';
import { TagService } from '../tasks/tag.service';
import { Tag } from '../tasks/tag.model';

@Component({
  selector: 'app-note-editor-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TiptapEditorComponent],
  template: `
    <div class="note-editor-page">
      <!-- Top bar with breadcrumb -->
      <header class="header">
        <div class="breadcrumb">
          <button
            type="button"
            class="back-link"
            (click)="navigateBack()"
            aria-label="Back to notes"
          >
            <i class="pi pi-arrow-left"></i>
            <span>Notes</span>
          </button>
          <span class="separator">/</span>
          <span class="current-note">{{ noteTitle() || 'Untitled' }}</span>
        </div>
        <div class="actions">
          <span class="save-status" [class.saving]="isSaving()">
            @if (isSaving()) {
              <i class="pi pi-spin pi-spinner"></i>
              <span>Saving...</span>
            } @else if (lastSaved()) {
              <i class="pi pi-check"></i>
              <span>Saved</span>
            }
          </span>
          @if (note()) {
            <button
              type="button"
              class="action-btn"
              (click)="exportToPdf()"
              aria-label="Export to PDF"
              title="Export to PDF"
            >
              <i class="pi pi-file-pdf"></i>
            </button>
            <button
              type="button"
              class="action-btn"
              (click)="deleteNote()"
              aria-label="Delete note"
              title="Delete note"
            >
              <i class="pi pi-trash"></i>
            </button>
          }
        </div>
      </header>

      <!-- Editor area -->
      <main class="editor-container">
        @if (loading()) {
          <div class="loading">
            <i class="pi pi-spin pi-spinner text-2xl text-foreground-muted"></i>
          </div>
        } @else if (notFound()) {
          <div class="not-found">
            <i class="pi pi-exclamation-triangle text-4xl text-foreground-muted mb-4"></i>
            <p class="text-foreground-secondary">Note not found</p>
            <button
              type="button"
              class="mt-4 px-4 py-2 text-sm bg-accent-solid text-white rounded-md"
              (click)="navigateBack()"
            >
              Back to Notes
            </button>
          </div>
        } @else {
          <div class="editor-wrapper">
            <app-tiptap-editor
              [initialContent]="initialContent()"
              [isNewNote]="isNewNote()"
              [resetTrigger]="resetCounter()"
              [checkboxStatuses]="checkboxStatuses()"
              (contentChange)="onContentChange($event)"
              (promoteCheckbox)="onPromoteCheckbox($event)"
            />
          </div>
        }
      </main>

      <!-- Footer status bar with tags -->
      <footer class="footer">
        @if (note()) {
          <span class="text-xs text-foreground-muted">
            Last edited {{ formatDate(note()!.updatedAt) }}
          </span>
        }
        <span class="flex-1"></span>
        <!-- Tags section (editable) -->
        @if (note()) {
          <div class="tags-section">
            <!-- Display existing tags -->
            @for (tag of visibleTags(); track tag.id) {
              <span class="tag-badge">
                {{ tag.name }}
                <button
                  type="button"
                  class="tag-badge-remove"
                  (click)="removeTag(tag.id)"
                  [attr.aria-label]="'Remove tag ' + tag.name"
                >
                  <i class="pi pi-times"></i>
                </button>
              </span>
            }
            @if (overflowCount() > 0 && !showTagPicker()) {
              <!-- Overflow button to expand -->
              <button
                type="button"
                class="overflow-btn"
                (click)="inlineTagsExpanded.set(true)"
                [attr.aria-label]="'Show ' + overflowCount() + ' more tags'"
              >
                +{{ overflowCount() }}
              </button>
            }
            <!-- Inline tag input (when adding tag) -->
            @if (showTagPicker()) {
              <div class="tag-input-wrapper">
                <input
                  #tagInput
                  type="text"
                  [placeholder]="noteTags().length > 0 ? 'Add tag...' : 'Add first tag...'"
                  [value]="tagSearch()"
                  (input)="tagSearch.set(asInput($event).value)"
                  (keydown.enter)="onTagEnter(); $event.preventDefault()"
                  (keydown.escape)="showTagPicker.set(false)"
                  class="tag-input"
                  aria-label="Search or create tag"
                >
                <!-- Dropdown suggestions -->
                @if (tagSuggestions().length > 0 || canCreateTag()) {
                  <div class="tag-dropdown">
                    @for (tag of tagSuggestions(); track tag.id) {
                      <button
                        type="button"
                        class="tag-dropdown-item"
                        (click)="addTag({ id: tag.id, name: tag.name })"
                      >
                        <span [innerHTML]="highlightMatch(tag.name)"></span>
                        <span class="text-foreground-muted">{{ tag.usageCount }}</span>
                      </button>
                    }
                    @if (canCreateTag()) {
                      @if (tagSuggestions().length > 0) {
                        <div class="tag-dropdown-divider"></div>
                      }
                      <button
                        type="button"
                        class="tag-dropdown-item create"
                        (click)="createAndAddTag(tagSearch().trim())"
                      >
                        <i class="pi pi-plus text-[10px] mr-1"></i>
                        Create "{{ tagSearch().trim() }}"
                      </button>
                    }
                  </div>
                }
              </div>
            } @else {
              <!-- Add tag button (when not adding) -->
              <button
                type="button"
                class="add-tag-btn"
                (click)="openTagInput()"
                aria-label="Add tag"
              >
                <i class="pi pi-plus text-[9px]"></i>
              </button>
            }
            <!-- Collapse/"Less" button (only when expanded and has overflow) -->
            @if (inlineTagsExpanded() && noteTags().length > 3 && !showTagPicker()) {
              <button
                type="button"
                class="collapse-btn"
                (click)="inlineTagsExpanded.set(false)"
                aria-label="Show fewer tags"
              >
                <i class="pi pi-chevron-up text-[8px]"></i>
                <span>Less</span>
              </button>
            }
          </div>
        }
      </footer>
    </div>
  `,
  styles: [`
    .note-editor-page {
      display: flex;
      flex-direction: column;
      height: 100%;
      background: var(--color-bg-default);
    }

    :host {
      display: block;
      height: 100%;
    }

    .header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0.75rem 1.5rem;
      border-bottom: 1px solid var(--color-border-default);
      background: var(--color-bg-default);
    }

    .breadcrumb {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.875rem;
    }

    .back-link {
      display: flex;
      align-items: center;
      gap: 0.375rem;
      color: var(--color-accent-solid);
      background: none;
      border: none;
      cursor: pointer;
      font-size: 0.875rem;
      padding: 0.25rem 0.5rem;
      margin: -0.25rem -0.5rem;
      border-radius: 0.25rem;
      transition: background 0.15s;
    }

    .back-link:hover {
      background: var(--color-bg-subtle);
    }

    .separator {
      color: var(--color-text-muted);
    }

    .current-note {
      color: var(--color-text-secondary);
      max-width: 300px;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .actions {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .save-status {
      display: flex;
      align-items: center;
      gap: 0.375rem;
      font-size: 0.75rem;
      color: var(--color-text-muted);
      padding-right: 0.75rem;
    }

    .save-status.saving {
      color: var(--color-accent-solid);
    }

    .action-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 2rem;
      height: 2rem;
      border: none;
      background: none;
      color: var(--color-text-secondary);
      border-radius: 0.375rem;
      cursor: pointer;
      transition: all 0.15s;
    }

    .action-btn:hover {
      background: var(--color-bg-subtle);
      color: var(--color-text-default);
    }

    .editor-container {
      flex: 1;
      overflow: auto;
      background: var(--color-bg-subtle);
    }

    .loading,
    .not-found {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      height: 100%;
    }

    .editor-wrapper {
      max-width: 100%;
      padding: 1.5rem 2rem;
      min-height: 100%;
      background: var(--color-bg-default);
    }

    @media (max-width: 768px) {
      .editor-wrapper {
        padding: 1rem;
      }
    }

    .footer {
      display: flex;
      align-items: center;
      padding: 0.5rem 1.5rem;
      border-top: 1px solid var(--color-border-default);
      background: var(--color-bg-base);
    }

    .tags-section {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 0.25rem;
    }

    .tag-badge {
      display: inline-flex;
      align-items: center;
      gap: 0.25rem;
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
      display: inline-flex;
      align-items: center;
      justify-content: center;
      cursor: pointer;
      color: var(--color-tag-text);
      opacity: 0.6;
      transition: opacity 0.15s;
    }

    .tag-badge-remove:hover {
      opacity: 1;
    }

    .overflow-btn {
      padding: 2px 6px;
      border-radius: 9999px;
      font-size: 10px;
      background: var(--color-tags-section-bg);
      color: var(--color-text-muted);
      border: none;
      cursor: pointer;
      transition: all 0.15s;
    }

    .overflow-btn:hover {
      background: var(--color-tags-badge-bg);
      color: var(--color-tag-text);
    }

    .tag-input-wrapper {
      position: relative;
      flex: 1;
      min-width: 100px;
    }

    .tag-input {
      width: 100%;
      height: 24px;
      padding: 0 8px;
      font-size: 12px;
      background: var(--color-bg-muted);
      border-radius: 9999px;
      border: none;
      outline: none;
    }

    .tag-dropdown {
      position: absolute;
      left: 0;
      bottom: calc(100% + 4px);
      width: 192px;
      background: var(--color-bg-base);
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
    }

    .tag-dropdown-item:hover {
      background: var(--color-bg-subtle);
    }

    .tag-dropdown-item.create {
      color: var(--color-accent-solid);
    }

    .tag-dropdown-divider {
      border-top: 1px solid var(--color-border-default);
      margin: 4px 0;
    }

    .add-tag-btn {
      all: unset;
      display: flex;
      align-items: center;
      justify-content: center;
      width: 20px;
      height: 20px;
      border-radius: 9999px;
      color: var(--color-text-muted);
      opacity: 0.3;
      cursor: pointer;
      transition: all 0.15s;
    }

    .add-tag-btn:hover {
      color: var(--color-tag-text);
      background: var(--color-tags-badge-bg);
      opacity: 1;
    }

    .collapse-btn {
      all: unset;
      display: flex;
      align-items: center;
      gap: 2px;
      margin-left: auto;
      padding: 2px 6px;
      border-radius: 9999px;
      font-size: 10px;
      background: var(--color-tags-section-bg);
      color: var(--color-text-muted);
      cursor: pointer;
      transition: all 0.15s;
    }

    .collapse-btn:hover {
      background: var(--color-tags-collapsed-bg);
    }
  `],
})
export class NoteEditorPage implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly noteService = inject(NoteService);
  private readonly tagService = inject(TagService);
  private readonly pdfExportService = inject(PdfExportService);
  private readonly toast = inject(ToastService);
  private readonly injector = inject(Injector);

  private readonly destroy$ = new Subject<void>();
  private readonly contentChange$ = new Subject<string>();

  readonly note = signal<Note | null>(null);
  readonly loading = signal(true);
  readonly notFound = signal(false);
  readonly isSaving = signal(false);
  readonly lastSaved = signal(false);
  readonly initialContent = signal('');
  readonly isNewNote = signal(false);
  readonly resetCounter = signal(0);
  readonly checkboxStatuses = signal<CheckboxStatus[]>([]);

  // Tag-related signals
  readonly showTagPicker = signal(false);
  readonly inlineTagsExpanded = signal(false);
  readonly tagSearch = signal('');
  readonly tagInput = viewChild<ElementRef<HTMLInputElement>>('tagInput');

  private currentContent = '';
  private noteId: string | null = null;
  private pollingTimeoutId: ReturnType<typeof setTimeout> | null = null;
  private isDestroyed = false;

  readonly noteTitle = computed(() => {
    const n = this.note();
    if (!n) return '';
    // Extract title from content (first line or heading)
    try {
      const parsed = JSON.parse(n.content);
      if (parsed?.content?.[0]) {
        const firstNode = parsed.content[0];
        return this.extractText(firstNode).trim().substring(0, 50) || 'Untitled';
      }
    } catch {
      // Plain text fallback
      return n.content.split('\n')[0]?.substring(0, 50) || 'Untitled';
    }
    return 'Untitled';
  });

  // Tag computed properties
  readonly noteTags = computed(() => this.note()?.tags ?? []);
  
  private readonly MAX_VISIBLE_TAGS = 3;

  readonly visibleTags = computed(() => {
    const tags = this.noteTags();
    const expanded = this.inlineTagsExpanded();
    const adding = this.showTagPicker();
    
    if (expanded || adding) return tags;
    return tags.slice(0, this.MAX_VISIBLE_TAGS);
  });

  readonly overflowCount = computed(() => {
    const total = this.noteTags().length;
    const expanded = this.inlineTagsExpanded();
    const adding = this.showTagPicker();
    
    if (expanded || adding) return 0;
    return Math.max(0, total - this.MAX_VISIBLE_TAGS);
  });

  readonly existingTagIds = computed(() => this.noteTags().map(t => t.id));

  readonly tagSuggestions = computed(() => {
    const query = this.tagSearch().toLowerCase().trim();
    const existingIds = this.existingTagIds();
    const allTags = this.tagService.tags();
    
    if (!query) return allTags.filter(t => !existingIds.includes(t.id));
    
    return allTags
      .filter(t => !existingIds.includes(t.id) && t.name.toLowerCase().includes(query))
      .sort((a, b) => b.usageCount - a.usageCount);
  });

  readonly canCreateTag = computed(() => {
    const query = this.tagSearch().trim();
    const suggestions = this.tagSuggestions();
    return query.length >= 2 && !suggestions.some(t => t.name.toLowerCase() === query.toLowerCase());
  });

  ngOnInit(): void {
    // Load tags
    if (this.tagService.tags().length === 0) {
      this.tagService.loadTags();
    }

    // Set up auto-save with debounce
    this.contentChange$
      .pipe(debounceTime(1000), takeUntil(this.destroy$))
      .subscribe((content) => this.autoSave(content));

    // Get note ID from route
    this.route.paramMap.pipe(takeUntil(this.destroy$)).subscribe((params) => {
      const id = params.get('id');
      if (id === 'new') {
        this.initNewNote();
      } else if (id) {
        this.loadNote(id);
      } else {
        this.notFound.set(true);
        this.loading.set(false);
      }
    });
  }

  ngOnDestroy(): void {
    this.isDestroyed = true;
    this.cancelPolling();
    this.destroy$.next();
    this.destroy$.complete();
  }

  private cancelPolling(): void {
    if (this.pollingTimeoutId !== null) {
      clearTimeout(this.pollingTimeoutId);
      this.pollingTimeoutId = null;
    }
  }

  @HostListener('document:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    // Save with Cmd/Ctrl+S
    if ((event.metaKey || event.ctrlKey) && event.key === 's') {
      event.preventDefault();
      this.saveNow();
    }

    // Go back with Escape when not typing in an input/editor
    if (event.key === 'Escape' && !this.isInEditableElement(event)) {
      this.navigateBack();
    }
  }

  private isInEditableElement(event: KeyboardEvent): boolean {
    const target = event.target as HTMLElement | null;
    if (!target) {
      return false;
    }

    const editableElement = target.closest(
      'input, textarea, [contenteditable=""], [contenteditable="true"]'
    ) as HTMLElement | null;

    if (editableElement) {
      return true;
    }

    return target.isContentEditable;
  }

  private initNewNote(): void {
    this.isNewNote.set(true);
    this.loading.set(false);
    this.initialContent.set('');
    this.currentContent = '';
    this.resetCounter.update((c) => c + 1);
  }

  private loadNote(id: string): void {
    // Cancel any previous polling when route changes
    this.cancelPolling();

    this.noteId = id;
    this.loading.set(true);

    // First ensure notes are loaded
    if (!this.noteService.initialLoadComplete()) {
      this.noteService.loadNotes();
    }

    let attempts = 0;
    const maxAttempts = 100; // 10 seconds max (100 * 100ms)

    // Wait for notes to load, then find the note
    const checkForNote = () => {
      // Stop polling if component is destroyed
      if (this.isDestroyed) {
        return;
      }

      attempts++;
      const notes = this.noteService.notes();
      const n = notes.find((note) => note.id === id);

      if (n) {
        this.note.set(n);
        this.initialContent.set(n.content);
        this.currentContent = n.content;
        this.isNewNote.set(false);
        this.resetCounter.update((c) => c + 1);
        this.loadCheckboxStatuses(n.id);
        this.loading.set(false);
      } else if (this.noteService.initialLoadComplete()) {
        this.notFound.set(true);
        this.loading.set(false);
      } else if (attempts < maxAttempts) {
        // Notes not loaded yet, try again
        this.pollingTimeoutId = setTimeout(checkForNote, 100);
      } else {
        // Max attempts reached, show not found
        this.notFound.set(true);
        this.loading.set(false);
      }
    };

    checkForNote();
  }

  private loadCheckboxStatuses(noteId: string): void {
    this.noteService.getCheckboxStatus(noteId).subscribe({
      next: (statuses) => this.checkboxStatuses.set(statuses),
      error: () => this.checkboxStatuses.set([]),
    });
  }

  onContentChange(content: string): void {
    this.currentContent = content;
    this.lastSaved.set(false);
    this.contentChange$.next(content);
  }

  private autoSave(content: string): void {
    if (this.isNewNote()) {
      // Create new note - use callback to get the real server ID
      this.isNewNote.set(false);
      this.noteService.createNote(content, (realId) => {
        if (this.isDestroyed) return;
        this.noteId = realId;
        // Find the note in the service's list to update local state
        const n = this.noteService.notes().find(note => note.id === realId);
        if (n) {
          this.note.set(n);
        }
        // Update URL without navigation
        this.router.navigate(['/notes', realId], { replaceUrl: true });
        this.lastSaved.set(true);
      });
    } else if (this.noteId) {
      // Update existing note
      this.isSaving.set(true);
      this.noteService.updateNote(this.noteId, content);
      // Small delay to show saving indicator
      setTimeout(() => {
        this.isSaving.set(false);
        this.lastSaved.set(true);
      }, 300);
    }
  }

  private saveNow(): void {
    if (this.currentContent !== null && this.currentContent !== undefined) {
      this.autoSave(this.currentContent);
    }
  }

  navigateBack(): void {
    // Save before leaving if there are unsaved changes
    if (this.currentContent && !this.lastSaved()) {
      this.saveNow();
    }
    this.router.navigate(['/notes']);
  }

  exportToPdf(): void {
    const n = this.note();
    if (n) {
      const noteToExport: Note = {
        ...n,
        content: this.currentContent || n.content,
      };
      this.pdfExportService.exportNoteToPdf(noteToExport);
    }
  }

  deleteNote(): void {
    // Use noteId (always the real server ID) rather than note().id which may be stale
    const id = this.noteId;
    if (!id) return;

    const deleted = this.noteService.deleteNoteWithUndo(id);
    if (deleted) {
      this.toast.success({
        summary: 'Note deleted',
        action: {
          label: 'Undo',
          callback: () => this.noteService.undoDelete(id),
        },
      });
      this.router.navigate(['/notes']);
    }
  }

  onPromoteCheckbox(event: { checkboxIndex: number }): void {
    const n = this.note();
    if (!n) {
      this.toast.error('Cannot promote checkbox in unsaved note');
      return;
    }

    const checkboxId = `cb-${event.checkboxIndex + 1}`;

    this.noteService.promoteCheckbox(n.id, checkboxId).subscribe({
      next: (result) => {
        this.toast.success({ summary: 'Task created', detail: result.title });
        this.loadCheckboxStatuses(n.id);
      },
      error: () => {
        this.toast.error('Failed to promote checkbox to task');
      },
    });
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'just now';
    if (diffMins < 60) return `${diffMins} min ago`;
    if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`;
    if (diffDays < 7) return `${diffDays} day${diffDays > 1 ? 's' : ''} ago`;

    return date.toLocaleDateString();
  }

  private extractText(node: { type: string; text?: string; content?: unknown[] }): string {
    if (node.type === 'text' && node.text) {
      return node.text;
    }
    if (!node.content) {
      return '';
    }
    return (node.content as { type: string; text?: string; content?: unknown[] }[])
      .map((child) => this.extractText(child))
      .join('');
  }

  // Tag methods
  openTagInput(): void {
    this.showTagPicker.set(true);
    this.tagSearch.set('');

    // Focus the input after render
    afterNextRender(() => {
      this.tagInput()?.nativeElement.focus();
    }, { injector: this.injector });
  }

  onTagEnter(): void {
    const query = this.tagSearch().trim();
    const suggestions = this.tagSuggestions();

    // If there's an exact match, select it
    const exactMatch = suggestions.find(t =>
      t.name.toLowerCase() === query.toLowerCase()
    );
    if (exactMatch) {
      this.addTag({ id: exactMatch.id, name: exactMatch.name });
      return;
    }

    // If can create, create it
    if (this.canCreateTag()) {
      this.createAndAddTag(query);
      return;
    }

    // If there's a single suggestion, select it
    if (suggestions.length === 1) {
      this.addTag({ id: suggestions[0].id, name: suggestions[0].name });
    }
  }

  addTag(tag: NoteTag): void {
    const n = this.note();
    if (!n) return;

    // Guard against duplicates
    if (this.noteTags().some(t => t.id === tag.id)) {
      this.showTagPicker.set(false);
      this.tagSearch.set('');
      return;
    }

    this.noteService.addTagToNote(n.id, tag, () => {
      this.tagService.incrementUsageCount(tag.id);
    });
    this.showTagPicker.set(false);
    this.tagSearch.set('');
  }

  removeTag(tagId: string): void {
    const n = this.note();
    if (!n) return;

    this.noteService.removeTagFromNote(n.id, tagId, () => {
      this.tagService.decrementUsageCount(tagId);
    });
  }

  createAndAddTag(name: string): void {
    this.tagService.createTag(name, (createdTag: Tag) => {
      this.addTag({ id: createdTag.id, name: createdTag.name });
    });
    this.showTagPicker.set(false);
    this.tagSearch.set('');
  }

  /** Highlight matching portion of tag name in dropdown */
  highlightMatch(tagName: string): string {
    const query = this.tagSearch().toLowerCase().trim();
    if (!query) return this.escapeHtml(tagName);

    const lowerName = tagName.toLowerCase();
    const index = lowerName.indexOf(query);
    if (index === -1) return this.escapeHtml(tagName);

    const before = tagName.slice(0, index);
    const match = tagName.slice(index, index + query.length);
    const after = tagName.slice(index + query.length);

    return `${this.escapeHtml(before)}<mark class="search-highlight">${this.escapeHtml(match)}</mark>${this.escapeHtml(after)}`;
  }

  private escapeHtml(text: string): string {
    const map: { [key: string]: string } = {
      '&': '&amp;',
      '<': '&lt;',
      '>': '&gt;',
      '"': '&quot;',
      "'": '&#39;',
      '/': '&#x2F;',
    };
    return text.replace(/[&<>"'/]/g, (char) => map[char]);
  }

  /** Type-safe helper for accessing input value from events */
  asInput(event: Event): HTMLInputElement {
    return event.target as HTMLInputElement;
  }
}
