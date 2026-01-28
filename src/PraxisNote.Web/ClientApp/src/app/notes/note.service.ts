import { Injectable, inject, signal, computed, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { HttpClient } from '@angular/common/http';
import { Subject, debounceTime } from 'rxjs';
import { Note, CheckboxStatus, PromoteCheckboxResult } from './note.model';
import { ToastService } from '../shared/services/toast.service';

interface PendingDeletion {
  note: Note;
  timeoutId: ReturnType<typeof setTimeout>;
  index: number;
}

@Injectable({ providedIn: 'root' })
export class NoteService {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);
  private readonly toast = inject(ToastService);

  private readonly pendingDeletions = new Map<string, PendingDeletion>();
  private readonly updateSubject = new Subject<{ id: string; content: string }>();

  private readonly _notes = signal<Note[]>([]);
  private readonly _loading = signal(false);
  private readonly _initialLoadComplete = signal(false);
  private readonly _searchQuery = signal('');

  constructor() {
    // Debounce content updates to avoid excessive API calls while typing
    this.updateSubject
      .pipe(debounceTime(500), takeUntilDestroyed(this.destroyRef))
      .subscribe(({ id, content }) => {
        this.http.put(`/api/notes/${id}`, { content }).subscribe({
          error: () => {
            this.toast.error('Failed to save note');
            this.loadNotes();
          },
        });
      });
  }

  readonly notes = this._notes.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly initialLoadComplete = this._initialLoadComplete.asReadonly();
  readonly searchQuery = this._searchQuery.asReadonly();

  readonly filteredNotes = computed(() => {
    const query = this._searchQuery().toLowerCase().trim();
    const notes = this._notes();

    if (!query) return notes;

    return notes.filter(
      n =>
        n.content.toLowerCase().includes(query) ||
        n.tags.some(t => t.name.toLowerCase().includes(query)) ||
        n.checkboxes.some(c => c.text.toLowerCase().includes(query))
    );
  });

  setSearchQuery(query: string): void {
    this._searchQuery.set(query);
  }

  loadNotes(): void {
    this._loading.set(true);
    this.http.get<Note[]>('/api/notes').subscribe({
      next: notes => {
        // Filter out notes with pending deletions to avoid restoring them
        const pendingIds = new Set(this.pendingDeletions.keys());
        const filtered = pendingIds.size > 0
          ? notes.filter(n => !pendingIds.has(n.id))
          : notes;
        this._notes.set(filtered);
        this._loading.set(false);
        this._initialLoadComplete.set(true);
      },
      error: () => {
        this._loading.set(false);
        this._initialLoadComplete.set(true);
      },
    });
  }

  createNote(content?: string, onCreated?: (id: string) => void): void {
    const tempId = crypto.randomUUID();
    const now = new Date().toISOString();
    const newNote: Note = {
      id: tempId,
      content: content ?? '',
      checkboxes: [],
      tags: [],
      createdAt: now,
      updatedAt: now,
    };

    // Optimistic update - add note immediately at the beginning
    this._notes.update(notes => [newNote, ...notes]);

    this.http.post<{ id: string }>('/api/notes', { content }).subscribe({
      next: result => {
        // Update with real ID from server
        this._notes.update(notes =>
          notes.map(n => (n.id === tempId ? { ...n, id: result.id } : n))
        );
        onCreated?.(result.id);
      },
      error: () => {
        this.toast.error('Failed to create note');
        // Remove the optimistically added note
        this._notes.update(notes => notes.filter(n => n.id !== tempId));
      },
    });
  }

  updateNote(id: string, content: string): void {
    // Optimistic update
    this._notes.update(notes =>
      notes.map(n =>
        n.id === id
          ? { ...n, content, updatedAt: new Date().toISOString() }
          : n
      )
    );

    // Debounced API call
    this.updateSubject.next({ id, content });
  }

  deleteNote(id: string): void {
    // Optimistic update
    this._notes.update(notes => notes.filter(n => n.id !== id));

    this.http.delete(`/api/notes/${id}`).subscribe({
      error: () => {
        this.toast.error('Failed to delete note');
        this.loadNotes();
      },
    });
  }

  /**
   * Delete a note with undo capability.
   * Returns the deleted note for display purposes, or null if not found.
   */
  deleteNoteWithUndo(id: string, undoTimeoutMs = 5000): Note | null {
    const notes = this._notes();
    const index = notes.findIndex(n => n.id === id);
    if (index === -1) return null;

    const note = notes[index];

    // Cancel any existing pending deletion
    this.cancelPendingDeletion(id);

    // Remove from UI immediately
    this._notes.update(notes => notes.filter(n => n.id !== id));

    // Schedule actual deletion
    const timeoutId = setTimeout(() => {
      this.commitDeletion(id);
    }, undoTimeoutMs);

    this.pendingDeletions.set(id, { note, timeoutId, index });

    return note;
  }

  /**
   * Undo a pending deletion, restoring the note at its original position.
   */
  undoDelete(id: string): boolean {
    const pending = this.pendingDeletions.get(id);
    if (!pending) return false;

    clearTimeout(pending.timeoutId);
    this.pendingDeletions.delete(id);

    // Restore at original position
    this._notes.update(notes => {
      const clampedIndex = Math.min(pending.index, notes.length);
      return [
        ...notes.slice(0, clampedIndex),
        pending.note,
        ...notes.slice(clampedIndex),
      ];
    });

    return true;
  }

  private commitDeletion(id: string): void {
    const pending = this.pendingDeletions.get(id);
    if (!pending) return;

    this.pendingDeletions.delete(id);

    this.http.delete(`/api/notes/${id}`).subscribe({
      error: () => {
        this.toast.error('Failed to delete note');
        // Restore on error
        this._notes.update(notes => {
          const clampedIndex = Math.min(pending.index, notes.length);
          return [
            ...notes.slice(0, clampedIndex),
            pending.note,
            ...notes.slice(clampedIndex),
          ];
        });
      },
    });
  }

  private cancelPendingDeletion(id: string): void {
    const pending = this.pendingDeletions.get(id);
    if (pending) {
      clearTimeout(pending.timeoutId);
      this.pendingDeletions.delete(id);
    }
  }

  /**
   * Promotes a checkbox to a task on the kanban board.
   * Returns an observable with the created task info.
   */
  promoteCheckbox(noteId: string, checkboxId: string) {
    return this.http.post<PromoteCheckboxResult>(
      `/api/notes/${noteId}/checkboxes/${checkboxId}/promote`,
      {}
    );
  }

  /**
   * Gets the link status of all checkboxes in a note.
   * Shows which checkboxes are linked to tasks and their current status.
   */
  getCheckboxStatus(noteId: string) {
    return this.http.get<CheckboxStatus[]>(`/api/notes/${noteId}/checkbox-status`);
  }

  addTagToNote(noteId: string, tag: { id: string; name: string }, onSuccess?: () => void, onError?: () => void): void {
    // Check if tag already exists on note
    const note = this._notes().find(n => n.id === noteId);
    const existingTags = note?.tags ?? [];
    if (existingTags.some(t => t.id === tag.id)) {
      return; // Tag already on note
    }

    // Optimistic update
    this._notes.update(notes =>
      notes.map(n =>
        n.id === noteId
          ? { ...n, tags: [...(n.tags ?? []), tag] }
          : n
      )
    );

    this.http.post(`/api/notes/${noteId}/tags/${tag.id}`, {}).subscribe({
      next: () => {
        onSuccess?.();
      },
      error: () => {
        // Rollback optimistic update
        this._notes.update(notes =>
          notes.map(n =>
            n.id === noteId
              ? { ...n, tags: (n.tags ?? []).filter(tg => tg.id !== tag.id) }
              : n
          )
        );
        this.toast.error('Failed to add tag');
        onError?.();
      },
    });
  }

  removeTagFromNote(noteId: string, tagId: string, onSuccess?: () => void, onError?: () => void): void {
    const note = this._notes().find(n => n.id === noteId);
    const removedTag = (note?.tags ?? []).find(t => t.id === tagId);

    // Guard: if tag isn't on the note, short-circuit
    if (!removedTag) {
      return;
    }

    // Optimistic update
    this._notes.update(notes =>
      notes.map(n =>
        n.id === noteId
          ? { ...n, tags: (n.tags ?? []).filter(tg => tg.id !== tagId) }
          : n
      )
    );

    this.http.delete(`/api/notes/${noteId}/tags/${tagId}`).subscribe({
      next: () => {
        onSuccess?.();
      },
      error: () => {
        // Rollback optimistic update
        this._notes.update(notes =>
          notes.map(n =>
            n.id === noteId
              ? { ...n, tags: [...(n.tags ?? []), removedTag] }
              : n
          )
        );
        this.toast.error('Failed to remove tag');
        onError?.();
      },
    });
  }
}
