import { Component, ChangeDetectionStrategy, input, output, signal, computed } from '@angular/core';
import { Note } from './note.model';

@Component({
  selector: 'app-note-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="note-card bg-surface-subtle rounded-md border border-border hover:shadow-lg transition-all cursor-pointer group"
      (click)="onOpen.emit()"
    >
      <div class="p-3">
        <!-- Content preview -->
        @if (note().content) {
          <p class="text-sm text-foreground line-clamp-6 whitespace-pre-wrap break-words">
            {{ note().content }}
          </p>
        } @else {
          <p class="text-sm text-foreground-muted italic">Empty note</p>
        }

        <!-- Checkboxes preview -->
        @if (note().checkboxes.length > 0) {
          <div class="mt-3 space-y-1.5">
            @for (checkbox of visibleCheckboxes(); track checkbox.id) {
              <div class="flex items-start gap-2 text-sm">
                <div
                  class="note-checkbox mt-0.5"
                  [class.checked]="checkbox.isChecked"
                ></div>
                <span
                  class="flex-1"
                  [class.line-through]="checkbox.isChecked"
                  [class.text-foreground-muted]="checkbox.isChecked"
                  [class.text-foreground]="!checkbox.isChecked"
                >
                  {{ checkbox.text }}
                </span>
              </div>
            }
            @if (hiddenCheckboxCount() > 0) {
              <p class="text-xs text-foreground-muted">
                +{{ hiddenCheckboxCount() }} more items
              </p>
            }
          </div>
        }

        <!-- Tags -->
        @if (note().tags.length > 0) {
          <div class="mt-3 flex flex-wrap gap-1">
            @for (tag of visibleTags(); track tag.id) {
              <span class="tag-badge">{{ tag.name }}</span>
            }
            @if (hiddenTagCount() > 0) {
              <span class="tag-badge">+{{ hiddenTagCount() }}</span>
            }
          </div>
        }
      </div>

      <!-- Footer with timestamp and actions -->
      <div class="px-3 pb-2 flex items-center justify-between">
        <span class="text-xs text-foreground-muted">
          {{ formatRelativeTime(note().updatedAt) }}
        </span>

        <!-- Hover actions -->
        <div class="card-actions flex items-center gap-1">
          <button
            type="button"
            class="p-1.5 text-foreground-muted hover:text-danger rounded transition-colors"
            (click)="onDelete.emit(); $event.stopPropagation()"
            aria-label="Delete note"
          >
            <i class="pi pi-trash text-xs"></i>
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .note-card:hover .card-actions {
      opacity: 1;
    }
    .card-actions {
      opacity: 0;
      transition: opacity 0.15s;
    }
    .note-checkbox {
      width: 14px;
      height: 14px;
      border: 2px solid var(--color-border-default);
      border-radius: 3px;
      flex-shrink: 0;
    }
    .note-checkbox.checked {
      background: var(--color-accent-solid, #5e81ac);
      border-color: var(--color-accent-solid, #5e81ac);
    }
    .note-checkbox.checked::after {
      content: '✓';
      color: white;
      font-size: 9px;
      font-weight: bold;
      display: flex;
      align-items: center;
      justify-content: center;
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
    .line-clamp-6 {
      display: -webkit-box;
      -webkit-line-clamp: 6;
      -webkit-box-orient: vertical;
      overflow: hidden;
    }
  `],
})
export class NoteCardComponent {
  readonly note = input.required<Note>();
  readonly onOpen = output<void>();
  readonly onDelete = output<void>();

  private readonly maxVisibleCheckboxes = 4;
  private readonly maxVisibleTags = 3;

  readonly visibleCheckboxes = computed(() =>
    this.note().checkboxes.slice(0, this.maxVisibleCheckboxes)
  );

  readonly hiddenCheckboxCount = computed(() =>
    Math.max(0, this.note().checkboxes.length - this.maxVisibleCheckboxes)
  );

  readonly visibleTags = computed(() =>
    this.note().tags.slice(0, this.maxVisibleTags)
  );

  readonly hiddenTagCount = computed(() =>
    Math.max(0, this.note().tags.length - this.maxVisibleTags)
  );

  formatRelativeTime(dateString: string): string {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMins / 60);
    const diffDays = Math.floor(diffHours / 24);

    if (diffMins < 1) return 'just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays < 7) return `${diffDays}d ago`;

    return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
  }
}
