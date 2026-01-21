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

  createTag(name: string, color: string): void {
    const tempId = crypto.randomUUID();
    const newTag: Tag = { id: tempId, name, color, usageCount: 0 };

    // Optimistic update
    this._tags.update(tags => [...tags, newTag].sort((a, b) => a.name.localeCompare(b.name)));

    this.http.post<{ id: string }>('/api/tags', { name, color }).subscribe({
      next: (result) => {
        // Update temp ID with real ID
        this._tags.update(tags =>
          tags.map(t => (t.id === tempId ? { ...t, id: result.id } : t))
        );
      },
      error: (err) => {
        // Remove optimistic tag
        this._tags.update(tags => tags.filter(t => t.id !== tempId));
        const message = err?.error?.error || 'Failed to create tag';
        this.toast.error(message);
      },
    });
  }

  updateTag(id: string, name?: string, color?: string): void {
    const originalTag = this._tags().find(t => t.id === id);
    if (!originalTag) return;

    // Optimistic update
    this._tags.update(tags =>
      tags
        .map(t =>
          t.id === id
            ? { ...t, name: name ?? t.name, color: color ?? t.color }
            : t
        )
        .sort((a, b) => a.name.localeCompare(b.name))
    );

    this.http.put(`/api/tags/${id}`, { name, color }).subscribe({
      error: (err) => {
        // Revert on error
        this._tags.update(tags =>
          tags
            .map(t => (t.id === id ? originalTag : t))
            .sort((a, b) => a.name.localeCompare(b.name))
        );
        const message = err?.error?.error || 'Failed to update tag';
        this.toast.error(message);
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
        // Restore on error
        this._tags.update(tags =>
          [...tags, deletedTag].sort((a, b) => a.name.localeCompare(b.name))
        );
        this.toast.error('Failed to delete tag');
      },
    });
  }

  /** Update local tag usage count when a tag is added/removed from a task */
  incrementUsageCount(tagId: string): void {
    this._tags.update(tags =>
      tags.map(t => (t.id === tagId ? { ...t, usageCount: t.usageCount + 1 } : t))
    );
  }

  decrementUsageCount(tagId: string): void {
    this._tags.update(tags =>
      tags.map(t =>
        t.id === tagId ? { ...t, usageCount: Math.max(0, t.usageCount - 1) } : t
      )
    );
  }
}
