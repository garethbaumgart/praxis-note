import { Component, ChangeDetectionStrategy, input, output, signal, effect, inject } from '@angular/core';
import { Dialog } from 'primeng/dialog';
import { Note } from './note.model';
import { NoteService } from './note.service';

@Component({
  selector: 'app-note-editor',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Dialog],
  template: `
    <p-dialog
      [visible]="visible()"
      (visibleChange)="onClose.emit()"
      [modal]="true"
      [dismissableMask]="true"
      [closable]="true"
      [draggable]="false"
      [resizable]="false"
      [header]="note() ? 'Edit Note' : 'New Note'"
      [style]="{ width: '90vw', maxWidth: '600px' }"
      styleClass="note-editor-dialog"
    >
      <div class="p-4">
        <textarea
          #contentInput
          [value]="content()"
          (input)="onContentChange($any($event.target).value)"
          (keydown.escape)="onClose.emit()"
          placeholder="Take a note..."
          rows="10"
          class="w-full p-3 text-sm text-foreground bg-surface-subtle border border-border rounded-md resize-none focus:outline-none focus:border-accent-solid placeholder:text-foreground-muted"
          aria-label="Note content"
        ></textarea>

        <!-- Tags display -->
        @if (note()?.tags?.length) {
          <div class="mt-3 flex flex-wrap gap-1">
            @for (tag of note()!.tags; track tag.id) {
              <span class="tag-badge">{{ tag.name }}</span>
            }
          </div>
        }

        <!-- Action buttons -->
        <div class="flex justify-end gap-2 mt-4 pt-4 border-t border-border">
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

  readonly visible = input.required<boolean>();
  readonly note = input<Note | null>(null);
  readonly onClose = output<void>();

  readonly content = signal('');

  constructor() {
    // Sync content when note changes
    effect(() => {
      const n = this.note();
      this.content.set(n?.content ?? '');
    });
  }

  onContentChange(value: string): void {
    this.content.set(value);
  }

  save(): void {
    const n = this.note();
    const newContent = this.content();

    if (n) {
      // Update existing note
      this.noteService.updateNote(n.id, newContent);
    } else {
      // Create new note
      this.noteService.createNote(newContent);
    }

    this.onClose.emit();
  }
}
