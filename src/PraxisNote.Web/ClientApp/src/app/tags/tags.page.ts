import { Component, ChangeDetectionStrategy, inject, OnInit, computed, signal, effect } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { SelectModule } from 'primeng/select';
import { TagService } from './tag.service';
import { Tag } from './tag.model';

@Component({
  selector: 'app-tags-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, SelectModule],
  template: `
    <div class="max-w-7xl mx-auto px-4 md:px-6 py-6 md:py-8">
      <!-- Header -->
      <div class="flex items-center gap-3 mb-6">
        <i class="pi pi-tags text-lg text-foreground-secondary" aria-hidden="true"></i>
        <h1 class="text-lg font-semibold text-foreground">Tags</h1>
        @if (!tagService.loading() && !tagService.error()) {
          <span class="text-sm text-foreground-muted">{{ tagCount() }} tags</span>
        }
      </div>

      @if (tagService.loading()) {
        <!-- Loading state -->
        <div class="flex items-center justify-center py-16" role="status" aria-label="Loading tags">
          <i class="pi pi-spin pi-spinner text-2xl text-foreground-muted" aria-hidden="true"></i>
          <span class="sr-only">Loading tags...</span>
        </div>
      } @else if (tagService.error()) {
        <!-- Error state -->
        <div class="flex flex-col items-center justify-center py-24">
          <p class="text-danger-base">{{ tagService.error() }}</p>
        </div>
      } @else if (tagService.tags().length === 0) {
        <!-- Empty state -->
        <div class="flex flex-col items-center justify-center py-24">
          <i class="pi pi-tags text-5xl text-foreground-muted mb-4" aria-hidden="true"></i>
          <p class="text-foreground-secondary mb-2">No tags yet</p>
          <p class="text-sm text-foreground-muted text-center max-w-sm">
            Tags you create on notes, tasks, and meetings will appear here.
          </p>
        </div>
      } @else {
        <!-- Selector row -->
        <div class="flex items-center gap-3 mb-6">
          <p-select
            [options]="tagService.tags()"
            optionLabel="name"
            [ngModel]="selectedTag()"
            (ngModelChange)="onTagSelected($event)"
            [filter]="true"
            filterBy="name"
            placeholder="Select a tag..."
            [showClear]="true"
            [style]="{ 'min-width': '280px' }"
            appendTo="body"
            ariaLabel="Select a tag"
          />
        </div>

        @if (!selectedTag()) {
          <!-- Landing state (no selection) -->
          <div class="flex flex-col items-center justify-center py-24">
            <i class="pi pi-tags text-5xl text-foreground-muted/40 mb-4" aria-hidden="true"></i>
            <p class="text-foreground-muted">Select a tag to get started</p>
          </div>
        } @else {
          <!-- Selected state placeholder for future item detail panel (Issue #335) -->
          <div class="rounded-lg border border-border bg-surface-subtle p-8 text-center">
            <p class="text-sm text-foreground-muted">
              Items tagged with <span class="font-medium text-foreground">"{{ selectedTag()?.name }}"</span> will appear here.
            </p>
          </div>
        }
      }
    </div>
  `,
})
export class TagsPage implements OnInit {
  readonly tagService = inject(TagService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly selectedTag = signal<Tag | null>(null);

  readonly tagCount = computed(() => this.tagService.tags().length);

  private readonly pendingSelectedId = signal<string | null>(null);

  constructor() {
    // Use effect() to reactively pre-select tag when tags finish loading
    effect(() => {
      const id = this.pendingSelectedId();
      if (!id) return;
      const tags = this.tagService.tags();
      const match = tags.find(t => t.id === id);
      if (match) {
        this.selectedTag.set(match);
        this.pendingSelectedId.set(null);
      }
    });
  }

  ngOnInit(): void {
    this.tagService.loadTags();

    const selectedId = this.route.snapshot.queryParamMap.get('selected');
    if (selectedId) {
      this.pendingSelectedId.set(selectedId);
    }
  }

  onTagSelected(tag: Tag | null): void {
    this.selectedTag.set(tag);
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { selected: tag?.id ?? undefined },
      queryParamsHandling: 'merge',
    });
  }
}
