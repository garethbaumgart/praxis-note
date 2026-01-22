import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Tag } from './tag.model';
import { ToastService } from '../shared/services/toast.service';

@Injectable({ providedIn: 'root' })
export class TagService {
  private readonly http = inject(HttpClient);
  private readonly toast = inject(ToastService);

  private readonly _tags = signal<Tag[]>([]);
  private readonly _loading = signal(false);

  readonly tags = this._tags.asReadonly();
  readonly loading = this._loading.asReadonly();

  loadTags(): void {
    this._loading.set(true);
    this.http.get<Tag[]>('/api/tags').subscribe({
      next: (tags) => {
        this._tags.set(tags);
        this._loading.set(false);
      },
      error: () => {
        this._loading.set(false);
      },
    });
  }

  createTag(name: string, onCreated?: (tag: Tag) => void): void {
    const tempId = crypto.randomUUID();
    const newTag: Tag = {
      id: tempId,
      name,
      usageCount: 0,
    };

    // Optimistic update
    this._tags.update(tags => [...tags, newTag].sort((a, b) => a.name.localeCompare(b.name)));

    this.http.post<{ id: string }>('/api/tags', { name }).subscribe({
      next: (result) => {
        // Replace temp ID with real ID
        this._tags.update(tags =>
          tags.map(t => (t.id === tempId ? { ...t, id: result.id } : t))
        );
        // Call onCreated callback with the real tag
        if (onCreated) {
          const createdTag = this._tags().find(t => t.id === result.id);
          if (createdTag) {
            onCreated(createdTag);
          }
        }
      },
      error: (err) => {
        // Remove optimistic tag
        this._tags.update(tags => tags.filter(t => t.id !== tempId));
        if (err.status === 409) {
          this.toast.error('A tag with this name already exists');
        } else {
          this.toast.error('Failed to create tag');
        }
      },
    });
  }

  updateTag(id: string, name: string): void {
    const oldTag = this._tags().find(t => t.id === id);
    if (!oldTag) return;

    // Optimistic update
    this._tags.update(tags =>
      tags.map(t => (t.id === id ? { ...t, name } : t)).sort((a, b) => a.name.localeCompare(b.name))
    );

    this.http.put(`/api/tags/${id}`, { name }).subscribe({
      error: (err) => {
        // Revert optimistic update
        this._tags.update(tags =>
          tags.map(t => (t.id === id ? oldTag : t)).sort((a, b) => a.name.localeCompare(b.name))
        );
        if (err.status === 409) {
          this.toast.error('A tag with this name already exists');
        } else {
          this.toast.error('Failed to update tag');
        }
      },
    });
  }

  deleteTag(id: string): void {
    const deletedTag = this._tags().find(t => t.id === id);
    if (!deletedTag) return;

    // Optimistic update
    this._tags.update(tags => tags.filter(t => t.id !== id));

    this.http.delete(`/api/tags/${id}`).subscribe({
      error: () => {
        // Restore tag on error
        this._tags.update(tags => [...tags, deletedTag].sort((a, b) => a.name.localeCompare(b.name)));
        this.toast.error('Failed to delete tag');
      },
    });
  }

  /** Increment usage count for a tag (called when adding to a task). */
  incrementUsageCount(tagId: string): void {
    this._tags.update(tags =>
      tags.map(t => (t.id === tagId ? { ...t, usageCount: t.usageCount + 1 } : t))
    );
  }

  /** Decrement usage count for a tag (called when removing from a task). */
  decrementUsageCount(tagId: string): void {
    this._tags.update(tags =>
      tags.map(t => (t.id === tagId ? { ...t, usageCount: Math.max(0, t.usageCount - 1) } : t))
    );
  }
}
