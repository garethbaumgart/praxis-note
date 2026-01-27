import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal,
  computed,
  OnInit,
  OnDestroy,
  HostListener,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, debounceTime, takeUntil } from 'rxjs';
import { Note, CheckboxStatus } from './note.model';
import { NoteService } from './note.service';
import { PdfExportService } from './pdf-export.service';
import { TiptapEditorComponent } from './tiptap-editor.component';
import { ToastService } from '../shared/services/toast.service';

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
          }
          <button
            type="button"
            class="action-btn"
            (click)="deleteNote()"
            aria-label="Delete note"
            title="Delete note"
          >
            <i class="pi pi-trash"></i>
          </button>
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

      <!-- Footer status bar -->
      <footer class="footer">
        @if (note()) {
          <span class="text-xs text-foreground-muted">
            Last edited {{ formatDate(note()!.updatedAt) }}
          </span>
        }
        <span class="flex-1"></span>
        @if (note()?.tags?.length) {
          <div class="tags">
            @for (tag of note()!.tags; track tag.id) {
              <span class="tag">{{ tag.name }}</span>
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
      background: var(--color-bg-default);
    }

    .tags {
      display: flex;
      gap: 0.375rem;
    }

    .tag {
      display: inline-flex;
      align-items: center;
      background: var(--color-tag-bg);
      color: var(--color-tag-text);
      font-size: 10px;
      font-weight: 500;
      padding: 2px 8px;
      border-radius: 9999px;
      height: 18px;
    }
  `],
})
export class NoteEditorPage implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly noteService = inject(NoteService);
  private readonly pdfExportService = inject(PdfExportService);
  private readonly toast = inject(ToastService);

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

  private currentContent = '';
  private noteId: string | null = null;

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

  ngOnInit(): void {
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
    this.destroy$.next();
    this.destroy$.complete();
  }

  @HostListener('document:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    // Save with Cmd/Ctrl+S
    if ((event.metaKey || event.ctrlKey) && event.key === 's') {
      event.preventDefault();
      this.saveNow();
    }

    // Go back with Escape
    if (event.key === 'Escape') {
      this.navigateBack();
    }
  }

  private initNewNote(): void {
    this.isNewNote.set(true);
    this.loading.set(false);
    this.initialContent.set('');
    this.currentContent = '';
    this.resetCounter.update((c) => c + 1);
  }

  private loadNote(id: string): void {
    this.noteId = id;
    this.loading.set(true);

    // First ensure notes are loaded
    if (!this.noteService.initialLoadComplete()) {
      this.noteService.loadNotes();
    }

    // Wait for notes to load, then find the note
    const checkForNote = () => {
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
      } else {
        // Notes not loaded yet, try again
        setTimeout(checkForNote, 100);
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
      // Create new note - service handles optimistic updates
      this.noteService.createNote(content);
      this.isNewNote.set(false);
      // Watch for the new note to appear in the service's notes
      this.watchForNewNote(content);
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

  private watchForNewNote(content: string): void {
    // Poll for the new note (created optimistically by the service)
    const checkForNote = () => {
      const notes = this.noteService.notes();
      // Find note with matching content that was just created
      const newNote = notes.find((n) => n.content === content);
      if (newNote) {
        this.note.set(newNote);
        this.noteId = newNote.id;
        // Update URL without navigation
        this.router.navigate(['/notes', newNote.id], { replaceUrl: true });
        this.lastSaved.set(true);
      } else {
        // Try again shortly
        setTimeout(checkForNote, 50);
      }
    };
    checkForNote();
  }

  private saveNow(): void {
    if (this.currentContent) {
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
    const n = this.note();
    if (!n) return;

    if (confirm('Are you sure you want to delete this note?')) {
      // Use deleteNoteWithUndo for undo capability
      const deleted = this.noteService.deleteNoteWithUndo(n.id);
      if (deleted) {
        this.toast.success({
          summary: 'Note deleted',
          action: {
            label: 'Undo',
            callback: () => this.noteService.undoDelete(n.id),
          },
        });
        this.router.navigate(['/notes']);
      }
    }
  }

  onPromoteCheckbox(event: { checkboxIndex: number }): void {
    const n = this.note();
    if (!n) {
      this.toast.error('Save the note first to promote checkboxes');
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
}
