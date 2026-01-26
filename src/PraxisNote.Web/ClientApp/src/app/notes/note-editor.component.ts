import { Component, ChangeDetectionStrategy, input, output, signal, effect, inject, viewChild } from '@angular/core';
import { Dialog } from 'primeng/dialog';
import { Note } from './note.model';
import { NoteService } from './note.service';
import { PdfExportService } from './pdf-export.service';
import { TiptapEditorComponent } from './tiptap-editor.component';

@Component({
  selector: 'app-note-editor',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Dialog, TiptapEditorComponent],
  template: `
    <p-dialog
      [visible]="visible()"
      (visibleChange)="onClose.emit()"
      (onShow)="onDialogShow()"
      [modal]="true"
      [dismissableMask]="true"
      [closable]="true"
      [draggable]="false"
      [resizable]="false"
      [focusOnShow]="false"
      [header]="note() ? 'Edit Note' : 'New Note'"
      [style]="{ width: '90vw', maxWidth: '700px' }"
      styleClass="note-editor-dialog"
    >
      <div class="p-4">
        <!-- TipTap Editor -->
        <app-tiptap-editor
          [initialContent]="initialContent()"
          [isNewNote]="isNewNote()"
          [resetTrigger]="resetCounter()"
          (contentChange)="onContentChange($event)"
        />

        <!-- Tags display -->
        @if (note()?.tags?.length) {
          <div class="mt-3 flex flex-wrap gap-1">
            @for (tag of note()!.tags; track tag.id) {
              <span class="tag-badge">{{ tag.name }}</span>
            }
          </div>
        }

        <!-- Action buttons -->
        <div class="flex justify-between items-center mt-4 pt-4 border-t border-border">
          <!-- Export button (only for existing notes) -->
          <div>
            @if (note()) {
              <button
                type="button"
                class="flex items-center gap-1.5 px-3 py-2 text-sm text-foreground-secondary hover:text-foreground rounded-md transition-colors"
                (click)="exportToPdf()"
                aria-label="Export to PDF"
              >
                <i class="pi pi-file-pdf text-sm"></i>
                <span>Export PDF</span>
              </button>
            }
          </div>

          <div class="flex gap-2">
            <button
              type="button"
              class="px-4 py-2 text-sm text-foreground-secondary hover:text-foreground rounded-md transition-colors"
              (click)="onClose.emit()"
            >
              Cancel
            </button>
            <button
              type="button"
              class="px-4 py-2 text-sm text-white bg-accent-solid hover:opacity-90 rounded-md transition-opacity"
              (click)="save()"
            >
              Save
            </button>
          </div>
        </div>
      </div>
    </p-dialog>
  `,
  styles: [`
    :host ::ng-deep .note-editor-dialog .p-dialog-header {
      padding: 1rem 1.5rem;
      border-bottom: 1px solid var(--color-border-default);
    }
    .tag-badge {
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
export class NoteEditorComponent {
  private readonly noteService = inject(NoteService);
  private readonly pdfExportService = inject(PdfExportService);

  readonly visible = input.required<boolean>();
  readonly note = input<Note | null>(null);
  readonly onClose = output<void>();

  /** Reference to the tiptap editor component */
  private readonly tiptapEditor = viewChild(TiptapEditorComponent);

  /** Initial content to pass to editor (only set when dialog opens) */
  readonly initialContent = signal('');

  /** Tracks whether current dialog is for a new note */
  readonly isNewNote = signal(false);

  /** Reset counter to force editor re-initialization */
  readonly resetCounter = signal(0);

  /** Current content for saving (updated on every editor change) */
  private currentContent = '';

  constructor() {
    // Sync content when note changes or dialog opens
    effect(() => {
      const isVisible = this.visible();
      const n = this.note();
      // Reset content when dialog opens (whether editing or creating new)
      if (isVisible) {
        const content = n?.content ?? '';
        this.initialContent.set(content);
        this.currentContent = content;
        this.isNewNote.set(!n);
        // Increment to force editor reset
        this.resetCounter.update((c) => c + 1);
      }
    });
  }

  onContentChange(value: string): void {
    // Only update the save value, don't feed back to editor
    this.currentContent = value;
  }

  save(): void {
    const n = this.note();
    const newContent = this.currentContent;

    if (n) {
      // Update existing note
      this.noteService.updateNote(n.id, newContent);
    } else {
      // Create new note
      this.noteService.createNote(newContent);
    }

    this.onClose.emit();
  }

  exportToPdf(): void {
    const n = this.note();
    if (n) {
      // Use current content (may have unsaved changes)
      const noteToExport: Note = {
        ...n,
        content: this.currentContent || n.content,
      };
      this.pdfExportService.exportNoteToPdf(noteToExport);
    }
  }

  onDialogShow(): void {
    // Focus the editor when dialog opens (only for new notes)
    if (!this.note()) {
      // Small delay to ensure editor is ready after dialog animation
      setTimeout(() => {
        this.tiptapEditor()?.focus();
      }, 50);
    }
  }
}
