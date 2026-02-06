import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Tag, MergePreview, MergeResult } from './tag.model';
import { ToastService } from '../shared/services/toast.service';

@Injectable({ providedIn: 'root' })
export class TagService {
  private readonly http = inject(HttpClient);
  private readonly toast = inject(ToastService);

  private readonly _tags = signal<Tag[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  readonly tags = this._tags.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  loadTags(): void {
    this._loading.set(true);
    this._error.set(null);
    this.http.get<Tag[]>('/api/tags').subscribe({
      next: (tags) => {
        this._tags.set([...tags].sort((a, b) => a.name.localeCompare(b.name)));
        this._loading.set(false);
      },
      error: () => {
        this._loading.set(false);
        this._error.set('Failed to load tags');
      },
    });
  }

  createTag(name: string, onCreated?: (tag: Tag) => void): void {
    const tempId = crypto.randomUUID();
    const newTag: Tag = {
      id: tempId,
      name,
      usageCount: 0,
      taskCount: 0,
      noteCount: 0,
      meetingCount: 0,
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

  /** Increment usage count for a tag. */
  incrementUsageCount(tagId: string, entityType: 'task' | 'note' | 'meeting'): void {
    this._tags.update(tags =>
      tags.map(t => {
        if (t.id !== tagId) return t;
        const countKey = `${entityType}Count` as const;
        return {
          ...t,
          usageCount: t.usageCount + 1,
          [countKey]: (t[countKey] as number) + 1,
        };
      })
    );
  }

  /** Decrement usage count for a tag. */
  decrementUsageCount(tagId: string, entityType: 'task' | 'note' | 'meeting'): void {
    this._tags.update(tags =>
      tags.map(t => {
        if (t.id !== tagId) return t;
        const countKey = `${entityType}Count` as const;
        return {
          ...t,
          usageCount: Math.max(0, t.usageCount - 1),
          [countKey]: Math.max(0, (t[countKey] as number) - 1),
        };
      })
    );
  }

  /** Fetch merge preview (non-destructive). Returns observable for dialog to subscribe to. */
  getMergePreview(sourceId: string, targetId: string): Observable<MergePreview> {
    return this.http.get<MergePreview>(`/api/tags/${sourceId}/merge-preview/${targetId}`);
  }

  /** Execute merge. Removes source tag from local state on success. */
  mergeTags(sourceId: string, targetId: string): void {
    this.http.post<MergeResult>(`/api/tags/${sourceId}/merge-into/${targetId}`, {}).subscribe({
      next: () => {
        // Remove source tag from local list
        this._tags.update(tags => tags.filter(t => t.id !== sourceId));
        // Reload tags to get accurate usage counts for target
        this.loadTags();
        this.toast.success({ summary: 'Tags merged successfully' });
      },
      error: (err) => {
        if (err.status === 404) {
          this.toast.error('One or both tags not found');
        } else if (err.status === 400) {
          this.toast.error('Cannot merge a tag into itself');
        } else {
          this.toast.error('Failed to merge tags');
        }
      },
    });
  }
}
