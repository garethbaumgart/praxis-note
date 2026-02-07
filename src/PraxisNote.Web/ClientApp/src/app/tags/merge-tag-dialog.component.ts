import { Component, ChangeDetectionStrategy, input, output, signal, computed, inject } from '@angular/core';
import { Subscription } from 'rxjs';
import { Dialog } from 'primeng/dialog';
import { TagService } from './tag.service';
import { Tag, MergePreview } from './tag.model';

@Component({
  selector: 'app-merge-tag-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Dialog],
  template: `
    <p-dialog
      header="Merge tag"
      [visible]="visible()"
      (visibleChange)="onDialogVisibleChange($event)"
      [modal]="true"
      [style]="{ width: '24rem' }"
      [draggable]="false"
      [resizable]="false">

      <!-- Stepper indicator -->
      <div class="flex items-center gap-0 mb-5">
        @if (step() === 1) {
          <div class="flex items-center gap-1.5">
            <div class="w-5 h-5 rounded-full bg-accent-solid text-white text-[10px] font-bold flex items-center justify-center shrink-0">1</div>
            <span class="text-[10px] font-semibold text-accent-foreground">Select target</span>
          </div>
        } @else {
          <div class="flex items-center gap-1.5">
            <div class="w-5 h-5 rounded-full bg-done text-done-foreground text-[10px] font-bold flex items-center justify-center shrink-0">
              <i class="pi pi-check" style="font-size: 8px"></i>
            </div>
            <span class="text-[10px] font-semibold text-done-foreground">Select target</span>
          </div>
        }
        <div class="w-6 h-px bg-border mx-1 shrink-0"></div>
        <div class="flex items-center gap-1.5">
          @if (step() === 2) {
            <div class="w-5 h-5 rounded-full bg-accent-solid text-white text-[10px] font-bold flex items-center justify-center shrink-0">2</div>
            <span class="text-[10px] font-semibold text-accent-foreground">Review & confirm</span>
          } @else {
            <div class="w-5 h-5 rounded-full bg-surface-muted text-foreground-muted text-[10px] font-bold flex items-center justify-center shrink-0">2</div>
            <span class="text-[10px] font-semibold text-foreground-muted">Review & confirm</span>
          }
        </div>
      </div>

      @if (step() === 1) {
        <!-- Step 1: Select target -->
        <div class="mb-4">
          <div class="text-[10px] font-semibold text-foreground-muted uppercase tracking-wider mb-1">Merging</div>
          <div class="flex items-center gap-2">
            <span class="inline-flex items-center gap-1 px-2.5 py-1 rounded-full bg-tag text-tag-foreground text-xs font-medium">
              {{ sourceTag()?.name }}
              <span class="text-[9px] opacity-60">{{ sourceTag()?.usageCount }}</span>
            </span>
            <span class="text-[10px] text-foreground-muted italic">into...</span>
          </div>
        </div>

        <div>
          <div class="text-[10px] font-semibold text-foreground-muted uppercase tracking-wider mb-2">Target tag</div>
          <!-- Search input -->
          <div class="relative mb-2">
            <i class="pi pi-search absolute left-3 top-1/2 -translate-y-1/2 text-foreground-muted" style="font-size: 11px" aria-hidden="true"></i>
            <input
              type="text"
              placeholder="Search tags..."
              class="w-full h-[34px] rounded-lg border border-border bg-surface-muted pl-8 pr-3 text-xs text-foreground placeholder:text-foreground-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent"
              [value]="searchText()"
              (input)="searchText.set($any($event.target).value)"
              aria-label="Search tags"
            />
          </div>
          <!-- Tag list -->
          <div class="rounded-lg border border-border overflow-hidden overflow-y-auto" style="max-height: 200px">
            @for (tag of filteredTags(); track tag.id) {
              <button
                type="button"
                class="flex items-center justify-between w-full px-3 py-2 hover:bg-surface-muted transition text-left"
                [class.bg-accent]="selectedTarget()?.id === tag.id"
                [class.text-white]="selectedTarget()?.id === tag.id"
                (click)="selectTarget(tag)"
                [attr.aria-label]="'Select ' + tag.name"
              >
                <span class="text-xs" [class.font-medium]="selectedTarget()?.id === tag.id">{{ tag.name }}</span>
                <span class="text-[10px]" [class.text-foreground-muted]="selectedTarget()?.id !== tag.id" [class.opacity-70]="selectedTarget()?.id === tag.id">{{ tag.usageCount }} items</span>
              </button>
            } @empty {
              <div class="px-3 py-4 text-center text-xs text-foreground-muted">No matching tags</div>
            }
          </div>
        </div>

        <!-- Footer -->
        <div class="flex justify-end gap-2 mt-4">
          <button
            type="button"
            class="px-4 py-2 text-sm border border-border rounded-lg text-foreground-secondary hover:bg-surface-muted transition"
            (click)="close()">
            Cancel
          </button>
          <button
            type="button"
            class="px-4 py-2 text-sm bg-accent-solid text-white rounded-lg font-medium hover:opacity-90 transition flex items-center gap-1"
            [disabled]="!selectedTarget()"
            (click)="goToStep2()">
            Next
            <i class="pi pi-arrow-right" style="font-size: 10px"></i>
          </button>
        </div>
      } @else {
        <!-- Step 2: Review & confirm -->
        <!-- Merge summary -->
        <div class="flex items-center gap-3 mb-4">
          <div class="text-center">
            <span class="inline-flex items-center gap-1 px-2.5 py-1 rounded-full bg-tag text-tag-foreground text-xs font-medium">
              {{ sourceTag()?.name }}
            </span>
            <div class="text-[9px] text-foreground-muted mt-1">{{ sourceTag()?.usageCount }} items</div>
          </div>
          <i class="pi pi-arrow-right text-accent-foreground" style="font-size: 14px" aria-hidden="true"></i>
          <div class="text-center">
            <span class="inline-flex items-center gap-1 px-2.5 py-1 rounded-full bg-accent text-accent-foreground text-xs font-medium border border-accent-solid">
              {{ selectedTarget()?.name }}
            </span>
            <div class="text-[9px] text-foreground-muted mt-1">{{ selectedTarget()?.usageCount }} items</div>
          </div>
        </div>

        @if (loadingPreview()) {
          <div class="flex items-center justify-center py-6" role="status" aria-label="Loading preview">
            <i class="pi pi-spin pi-spinner text-xl text-foreground-muted" aria-hidden="true"></i>
            <span class="sr-only">Loading preview...</span>
          </div>
        } @else if (previewError()) {
          <div class="text-sm text-danger text-center py-4">{{ previewError() }}</div>
        } @else if (preview()) {
          <!-- Detailed preview table -->
          <div class="rounded-lg border border-border p-3 mb-3 bg-surface">
            <div class="text-[10px] font-semibold text-foreground-muted uppercase tracking-wider mb-2">Result after merge</div>
            <table class="w-full text-xs">
              <thead>
                <tr class="text-foreground-muted">
                  <th class="text-left font-medium pb-1"></th>
                  <th class="text-right font-medium pb-1 text-tag-foreground">{{ preview()!.sourceTagName }}</th>
                  <th class="text-right font-medium pb-1 text-accent-foreground">{{ preview()!.targetTagName }}</th>
                  <th class="text-right font-medium pb-1 text-done-foreground">Result</th>
                </tr>
              </thead>
              <tbody class="text-foreground-secondary">
                <tr>
                  <td class="py-0.5"><i class="pi pi-check-square mr-1" style="font-size: 9px" aria-hidden="true"></i>Tasks</td>
                  <td class="text-right py-0.5">{{ preview()!.sourceTaskCount }}</td>
                  <td class="text-right py-0.5">{{ preview()!.targetTaskCount }}</td>
                  <td class="text-right py-0.5 font-semibold text-done-foreground">{{ preview()!.resultTaskCount }}</td>
                </tr>
                <tr>
                  <td class="py-0.5"><i class="pi pi-file-edit mr-1" style="font-size: 9px" aria-hidden="true"></i>Notes</td>
                  <td class="text-right py-0.5">{{ preview()!.sourceNoteCount }}</td>
                  <td class="text-right py-0.5">{{ preview()!.targetNoteCount }}</td>
                  <td class="text-right py-0.5 font-semibold text-done-foreground">{{ preview()!.resultNoteCount }}</td>
                </tr>
                <tr>
                  <td class="py-0.5"><i class="pi pi-comments mr-1" style="font-size: 9px" aria-hidden="true"></i>Meetings</td>
                  <td class="text-right py-0.5">{{ preview()!.sourceMeetingCount }}</td>
                  <td class="text-right py-0.5">{{ preview()!.targetMeetingCount }}</td>
                  <td class="text-right py-0.5 font-semibold text-done-foreground">{{ preview()!.resultMeetingCount }}</td>
                </tr>
                <tr class="border-t border-border-muted">
                  <td class="py-1 font-semibold text-foreground">Total</td>
                  <td class="text-right py-1">{{ sourceTotalCount() }}</td>
                  <td class="text-right py-1">{{ targetTotalCount() }}</td>
                  <td class="text-right py-1 font-bold text-done-foreground">{{ resultTotalCount() }}</td>
                </tr>
              </tbody>
            </table>
            @if (preview()!.overlapCount > 0) {
              <div class="text-[10px] text-foreground-muted mt-1">
                <i class="pi pi-info-circle mr-1" style="font-size: 9px" aria-hidden="true"></i>{{ preview()!.overlapCount }} items already had both tags (duplicates removed)
              </div>
            }
          </div>

          <!-- Warning box -->
          <div class="flex items-start gap-2 rounded-lg border border-danger-bg bg-danger-bg px-3 py-2">
            <i class="pi pi-exclamation-triangle text-danger mt-0.5" style="font-size: 11px" aria-hidden="true"></i>
            <span class="text-xs text-danger">
              <strong>"{{ sourceTag()?.name }}"</strong> will be permanently deleted. This cannot be undone.
            </span>
          </div>
        }

        <!-- Footer -->
        <div class="flex justify-end gap-2 mt-4">
          <button
            type="button"
            class="px-4 py-2 text-sm border border-border rounded-lg text-foreground-secondary hover:bg-surface-muted transition flex items-center gap-1"
            (click)="goToStep1()">
            <i class="pi pi-arrow-left" style="font-size: 10px"></i>
            Back
          </button>
          <button
            type="button"
            class="px-4 py-2 text-sm bg-accent-solid text-white rounded-lg font-medium hover:opacity-90 transition flex items-center gap-1"
            [disabled]="loadingPreview() || !!previewError()"
            (click)="confirmMerge()">
            <i class="pi pi-clone" style="font-size: 10px"></i>
            Merge tags
          </button>
        </div>
      }
    </p-dialog>
  `,
})
export class MergeTagDialogComponent {
  readonly visible = input.required<boolean>();
  readonly sourceTag = input<Tag | null>(null);
  readonly allTags = input.required<Tag[]>();
  readonly onClose = output<void>();
  readonly onMerge = output<{ sourceId: string; targetId: string }>();

  private readonly tagService = inject(TagService);
  private previewSub?: Subscription;

  readonly step = signal<1 | 2>(1);
  readonly searchText = signal('');
  readonly selectedTarget = signal<Tag | null>(null);
  readonly preview = signal<MergePreview | null>(null);
  readonly loadingPreview = signal(false);
  readonly previewError = signal<string | null>(null);

  readonly filteredTags = computed(() => {
    const source = this.sourceTag();
    if (!source) return [];
    const search = this.searchText().toLowerCase();
    return this.allTags()
      .filter(t => t.id !== source.id)
      .filter(t => !search || t.name.toLowerCase().includes(search));
  });

  readonly sourceTotalCount = computed(() => {
    const p = this.preview();
    return p ? p.sourceTaskCount + p.sourceNoteCount + p.sourceMeetingCount : 0;
  });

  readonly targetTotalCount = computed(() => {
    const p = this.preview();
    return p ? p.targetTaskCount + p.targetNoteCount + p.targetMeetingCount : 0;
  });

  readonly resultTotalCount = computed(() => {
    const p = this.preview();
    return p ? p.resultTaskCount + p.resultNoteCount + p.resultMeetingCount : 0;
  });

  selectTarget(tag: Tag): void {
    this.selectedTarget.set(tag);
  }

  goToStep2(): void {
    const target = this.selectedTarget();
    const source = this.sourceTag();
    if (!target || !source) return;

    this.step.set(2);
    this.loadingPreview.set(true);
    this.previewError.set(null);
    this.preview.set(null);

    this.previewSub?.unsubscribe();
    this.previewSub = this.tagService.getMergePreview(source.id, target.id).subscribe({
      next: (result) => {
        this.preview.set(result);
        this.loadingPreview.set(false);
      },
      error: () => {
        this.previewError.set('Failed to load merge preview');
        this.loadingPreview.set(false);
      },
    });
  }

  goToStep1(): void {
    this.step.set(1);
    this.preview.set(null);
    this.previewError.set(null);
  }

  confirmMerge(): void {
    const target = this.selectedTarget();
    const source = this.sourceTag();
    if (!target || !source) return;

    this.onMerge.emit({ sourceId: source.id, targetId: target.id });
  }

  close(): void {
    this.resetState();
    this.onClose.emit();
  }

  onDialogVisibleChange(visible: boolean): void {
    if (!visible) {
      this.resetState();
      this.onClose.emit();
    }
  }

  private resetState(): void {
    this.previewSub?.unsubscribe();
    this.step.set(1);
    this.searchText.set('');
    this.selectedTarget.set(null);
    this.preview.set(null);
    this.loadingPreview.set(false);
    this.previewError.set(null);
  }
}
