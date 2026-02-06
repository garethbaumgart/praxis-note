import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { TagItemDto, TagItemsResponse } from './tag-hub.model';

@Injectable({ providedIn: 'root' })
export class TagHubService {
  private readonly http = inject(HttpClient);

  private readonly _items = signal<TagItemDto[]>([]);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _meetingCount = signal(0);
  private readonly _noteCount = signal(0);
  private readonly _taskCount = signal(0);
  private currentTagId: string | null = null;

  readonly items = this._items.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly meetingCount = this._meetingCount.asReadonly();
  readonly noteCount = this._noteCount.asReadonly();
  readonly taskCount = this._taskCount.asReadonly();

  readonly totalCount = computed(() => this._meetingCount() + this._noteCount() + this._taskCount());

  loadItems(tagId: string): void {
    this.currentTagId = tagId;
    this._loading.set(true);
    this._error.set(null);
    this.http.get<TagItemsResponse>(`/api/tags/${tagId}/items`).subscribe({
      next: (response) => {
        if (this.currentTagId !== tagId) return;
        this._items.set(response.items);
        this._meetingCount.set(response.meetingCount);
        this._noteCount.set(response.noteCount);
        this._taskCount.set(response.taskCount);
        this._loading.set(false);
      },
      error: () => {
        if (this.currentTagId !== tagId) return;
        this._loading.set(false);
        this._error.set('Failed to load items');
        this._items.set([]);
        this._meetingCount.set(0);
        this._noteCount.set(0);
        this._taskCount.set(0);
      },
    });
  }

  clear(): void {
    this.currentTagId = null;
    this._items.set([]);
    this._loading.set(false);
    this._error.set(null);
    this._meetingCount.set(0);
    this._noteCount.set(0);
    this._taskCount.set(0);
  }
}
