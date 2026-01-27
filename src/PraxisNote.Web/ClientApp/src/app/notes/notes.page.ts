import { Component, ChangeDetectionStrategy, inject, OnInit, computed, HostListener, ElementRef, viewChild } from '@angular/core';
import { Router } from '@angular/router';
import { NoteService } from './note.service';
import { Note } from './note.model';
import { NoteCardComponent } from './note-card.component';
import { NoteCardSkeletonComponent } from './note-card-skeleton.component';
import { ToastService } from '../shared/services/toast.service';

@Component({
  selector: 'app-notes-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NoteCardComponent, NoteCardSkeletonComponent],
  template: `
    <div class="max-w-7xl mx-auto px-4 md:px-6 py-6 md:py-8">
      <!-- Header -->
      <div class="flex items-center gap-3 mb-6">
        <h1 class="text-lg font-semibold text-foreground">Notes</h1>
        <span class="text-sm text-foreground-muted">{{ noteCount() }} notes</span>
      </div>

      <!-- Search -->
      <div class="relative mb-6">
        <i class="pi pi-search absolute left-3 top-1/2 -translate-y-1/2 text-xs text-foreground-secondary"></i>
        <input
          #searchInput
          type="text"
          placeholder="Search notes..."
          [value]="noteService.searchQuery()"
          (input)="noteService.setSearchQuery(asInput($event).value)"
          (keydown.escape)="clearSearch()"
          class="w-full h-9 pl-9 pr-16 text-sm text-foreground-secondary placeholder-foreground-secondary bg-surface-muted hover:bg-surface-muted/80 focus:bg-surface-muted/80 rounded-lg focus:outline-none transition-colors duration-150"
          aria-label="Search notes"
        >
        @if (noteService.searchQuery()) {
          <button
            type="button"
            class="absolute right-3 top-1/2 -translate-y-1/2 text-foreground-muted hover:text-foreground transition-colors"
            (click)="clearSearch()"
            aria-label="Clear search"
          >
            <i class="pi pi-times text-xs"></i>
          </button>
        } @else {
          <kbd class="absolute right-3 top-1/2 -translate-y-1/2 hidden md:inline px-1.5 py-0.5 text-xs text-foreground-muted bg-surface border border-border rounded font-sans">/</kbd>
        }
      </div>

      <!-- Quick add -->
      <button
        type="button"
        class="quick-add mb-6 px-4 py-3 cursor-text w-full text-left"
        (click)="openNewNote()"
        aria-label="Create new note"
      >
        <span class="text-sm text-foreground-muted">Take a note...</span>
      </button>

      <!-- Loading skeletons -->
      @if (!noteService.initialLoadComplete()) {
        <div class="masonry-grid">
          @for (i of skeletonArray; track i) {
            <div class="masonry-item">
              <app-note-card-skeleton />
            </div>
          }
        </div>
      } @else if (noteService.filteredNotes().length === 0) {
        <!-- Empty state -->
        <div class="text-center py-16">
          @if (noteService.searchQuery()) {
            <i class="pi pi-search text-4xl text-foreground-muted mb-4"></i>
            <p class="text-foreground-muted">No notes match your search</p>
          } @else {
            <i class="pi pi-file-edit text-4xl text-foreground-muted mb-4"></i>
            <p class="text-foreground-secondary mb-2">No notes yet</p>
            <p class="text-sm text-foreground-muted">Click "New Note" to create your first note</p>
          }
        </div>
      } @else {
        <!-- Notes grid -->
        <div class="masonry-grid">
          @for (note of noteService.filteredNotes(); track note.id) {
            <div class="masonry-item">
              <app-note-card
                [note]="note"
                (onOpen)="openNote(note)"
                (onDelete)="deleteNote(note)"
              />
            </div>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .masonry-grid {
      column-count: 4;
      column-gap: 12px;
    }
    @media (max-width: 1280px) { .masonry-grid { column-count: 3; } }
    @media (max-width: 1024px) { .masonry-grid { column-count: 2; } }
    @media (max-width: 640px) { .masonry-grid { column-count: 1; } }

    .masonry-item {
      break-inside: avoid;
      margin-bottom: 12px;
    }

    .quick-add {
      border: 2px dashed var(--color-border-default);
      border-radius: 0.375rem;
      transition: all 0.2s;
    }
    .quick-add:hover {
      border-color: var(--color-accent-solid, #5e81ac);
      background: var(--color-bg-subtle);
    }
  `],
})
export class NotesPage implements OnInit {
  readonly noteService = inject(NoteService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  private readonly searchInputRef = viewChild<ElementRef<HTMLInputElement>>('searchInput');

  readonly skeletonArray = Array.from({ length: 8 }, (_, i) => i);

  readonly noteCount = computed(() => this.noteService.notes().length);

  ngOnInit(): void {
    this.noteService.loadNotes();
  }

  @HostListener('document:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    const target = event.target as HTMLElement;
    const isInInput = target.tagName === 'INPUT' ||
                      target.tagName === 'TEXTAREA' ||
                      target.isContentEditable;

    // Focus search with /
    if (event.key === '/' && !isInInput) {
      event.preventDefault();
      this.focusSearch();
    }

    // New note with N
    if (event.key === 'n' && !isInInput && !event.ctrlKey && !event.metaKey) {
      event.preventDefault();
      this.openNewNote();
    }
  }

  focusSearch(): void {
    this.searchInputRef()?.nativeElement.focus();
  }

  clearSearch(): void {
    this.noteService.setSearchQuery('');
    this.searchInputRef()?.nativeElement.blur();
  }

  openNewNote(): void {
    this.router.navigate(['/notes', 'new']);
  }

  openNote(note: Note): void {
    this.router.navigate(['/notes', note.id]);
  }

  deleteNote(note: Note): void {
    const deleted = this.noteService.deleteNoteWithUndo(note.id);
    if (deleted) {
      this.toast.success({
        summary: 'Note deleted',
        action: {
          label: 'Undo',
          callback: () => this.noteService.undoDelete(note.id),
        },
      });
    }
  }

  asInput(event: Event): HTMLInputElement {
    return event.target as HTMLInputElement;
  }
}
