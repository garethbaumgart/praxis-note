import { Component, ChangeDetectionStrategy, inject, signal, computed, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Dialog } from 'primeng/dialog';
import { Checkbox } from 'primeng/checkbox';
import { Skeleton } from 'primeng/skeleton';
import { DriveImportService } from './drive-import.service';
import { ToastService } from '../shared/services/toast.service';
import { ErrorStateComponent } from '../shared/components/error-state.component';
import { formatDateTime } from '../shared/date-utils';

@Component({
  selector: 'app-drive-import-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Dialog, Checkbox, FormsModule, Skeleton, ErrorStateComponent],
  template: `
    <p-dialog
      header="Import from Google Drive"
      [visible]="visible()"
      (visibleChange)="onVisibleChange($event)"
      [modal]="true"
      [style]="{ width: '36rem' }"
      [breakpoints]="{ '640px': '95vw' }"
      [draggable]="false"
      [resizable]="false"
      [dismissableMask]="closable()"
      [closable]="closable()"
    >
      @switch (driveImportService.state()) {
        @case ('loading') {
          <div role="status" aria-label="Loading Drive files">
            <span class="sr-only">Loading Drive files...</span>
            <p-skeleton width="100%" height="60px" styleClass="mb-2" />
            <p-skeleton width="100%" height="60px" styleClass="mb-2" />
            <p-skeleton width="100%" height="60px" />
          </div>
        }

        @case ('preview') {
          <!-- Summary bar -->
          <div class="flex items-center justify-between bg-surface-muted rounded-lg px-4 py-2.5 mb-4">
            <div class="flex items-center gap-3">
              <span class="text-sm font-medium text-foreground">
                {{ driveImportService.selectedCount() }} of {{ driveImportService.totalCount() }} selected
              </span>
              @if (driveImportService.duplicateCount() > 0) {
                <span class="text-xs px-2 py-0.5 bg-surface border border-border rounded text-foreground-muted">
                  {{ driveImportService.duplicateCount() }} duplicates skipped
                </span>
              }
            </div>
            <div class="flex gap-2">
              <button type="button" class="text-xs text-accent-solid hover:underline"
                (click)="driveImportService.toggleAll(true)" aria-label="Select all files">
                Select All
              </button>
              <button type="button" class="text-xs text-foreground-muted hover:underline"
                (click)="driveImportService.toggleAll(false)" aria-label="Deselect all files">
                Deselect All
              </button>
            </div>
          </div>

          <!-- File list -->
          <div class="space-y-2.5 max-h-[400px] overflow-y-auto">
            @for (file of driveImportService.files(); track file.id; let idx = $index) {
              <div class="bg-surface-muted rounded-lg p-3"
                [class.opacity-50]="file.duplicateType === 'definite'"
                [style.border-left]="file.duplicateType === 'possible' ? '3px solid var(--color-warning-foreground)' : 'none'"
              >
                <div class="flex gap-3">
                  <!-- Checkbox -->
                  <p-checkbox
                    [ngModel]="file.selected"
                    (ngModelChange)="driveImportService.toggleFile(idx)"
                    [binary]="true"
                    [disabled]="file.duplicateType === 'definite'"
                    styleClass="mt-0.5"
                  />
                  <div class="flex-1 min-w-0">
                    <!-- Header row -->
                    <div class="flex items-center justify-between">
                      <div class="flex items-center gap-2">
                        <span class="text-sm font-semibold text-foreground truncate">
                          {{ file.title || 'Untitled Meeting' }}
                        </span>
                        @if (file.duplicateType === 'definite') {
                          <span class="text-[10px] px-1.5 py-0.5 bg-surface border border-border rounded text-foreground-muted">
                            Duplicate
                          </span>
                        } @else if (file.duplicateType === 'possible') {
                          <span class="text-[10px] px-1.5 py-0.5 bg-warning/20 text-warning-foreground rounded">
                            Possible duplicate
                          </span>
                        } @else {
                          <span class="text-[10px] px-1.5 py-0.5 bg-done/20 text-done-foreground rounded">
                            Ready
                          </span>
                        }
                      </div>
                      @if (file.duplicateType !== 'definite') {
                        <button type="button"
                          class="text-foreground-muted hover:text-foreground transition-colors"
                          (click)="driveImportService.toggleExpanded(idx); $event.stopPropagation()"
                          [attr.aria-label]="file.expanded ? 'Collapse details' : 'Expand details'"
                        >
                          <i class="pi text-xs"
                            [class.pi-chevron-down]="file.expanded"
                            [class.pi-chevron-right]="!file.expanded"
                          ></i>
                        </button>
                      }
                    </div>

                    <!-- Date + attendees -->
                    <div class="text-xs text-foreground-muted mt-1">
                      @if (file.meetingDate) {
                        <i class="pi pi-calendar text-[10px]"></i> {{ fmtDateTime(file.meetingDate) }}
                      }
                      @if (file.attendees) {
                        &nbsp;<i class="pi pi-users text-[10px]"></i> {{ file.attendees }}
                      }
                    </div>

                    <!-- Possible duplicate warning -->
                    @if (file.duplicateType === 'possible' && file.matchedMeetingTitle) {
                      <div class="flex items-center gap-1.5 mt-2 px-2 py-1.5 bg-warning/10 border border-warning/20 rounded text-[10px] text-warning-foreground">
                        <i class="pi pi-info-circle text-[10px]"></i>
                        Similar to: "{{ file.matchedMeetingTitle }}". Import anyway?
                      </div>
                    }

                    <!-- Definite duplicate info -->
                    @if (file.duplicateType === 'definite' && file.matchedMeetingTitle) {
                      <div class="text-[10px] text-foreground-muted mt-1">
                        <i class="pi pi-link text-[9px]"></i> Already imported as "{{ file.matchedMeetingTitle }}"
                      </div>
                    }

                    <!-- Expanded details -->
                    @if (file.expanded && file.duplicateType !== 'definite') {
                      <div class="mt-2 p-2 bg-surface rounded-md text-xs text-foreground-secondary">
                        @if (file.summary) {
                          <div class="font-medium mb-1">Summary</div>
                          <div class="leading-relaxed mb-2">{{ file.summary }}</div>
                        }
                        @if (file.keyPoints && file.keyPoints.length > 0) {
                          <div class="font-medium mb-1">Key Points</div>
                          <ul class="list-disc pl-4 leading-relaxed mb-2">
                            @for (kp of file.keyPoints; track kp) {
                              <li>{{ kp }}</li>
                            }
                          </ul>
                        }
                        @if (file.actionItems && file.actionItems.length > 0) {
                          <div class="font-medium mb-1">Action Items</div>
                          <ul class="list-disc pl-4 leading-relaxed">
                            @for (ai of file.actionItems; track ai.description) {
                              <li>
                                @if (ai.assignee) { <strong>{{ ai.assignee }}:</strong> }
                                {{ ai.description }}
                              </li>
                            }
                          </ul>
                        }
                      </div>
                    }

                    <!-- Tags (visible for non-definite duplicates) -->
                    @if (file.duplicateType !== 'definite') {
                      <div class="flex flex-wrap items-center gap-1 mt-2" (click)="$event.stopPropagation()">
                        @for (tag of file.editedTags; track tag) {
                          <span class="inline-flex items-center gap-0.5 px-1.5 py-0.5 text-[11px] bg-accent-bg text-accent-foreground rounded">
                            <i class="pi pi-tag text-[8px]"></i>
                            {{ tag }}
                            <button type="button"
                              class="ml-0.5 hover:text-danger transition-colors"
                              (click)="driveImportService.removeTag(idx, tag)"
                              [attr.aria-label]="'Remove tag ' + tag"
                            >
                              <i class="pi pi-times text-[8px]"></i>
                            </button>
                          </span>
                        }
                        <input type="text"
                          class="w-16 text-[11px] px-1.5 py-0.5 bg-transparent border border-dashed border-border rounded placeholder:text-foreground-muted focus:outline-none focus:border-accent-solid"
                          placeholder="+ tag"
                          [attr.aria-label]="'Add tag to ' + (file.title || 'Untitled Meeting')"
                          (keydown.enter)="onAddTag(idx, $event)"
                          (click)="$event.stopPropagation()"
                        />
                      </div>
                    }
                  </div>
                </div>
              </div>
            }
          </div>

          <!-- Empty state when no files found -->
          @if (driveImportService.files().length === 0) {
            <div class="flex flex-col items-center justify-center py-8 text-foreground-muted">
              <i class="pi pi-inbox text-2xl mb-2"></i>
              <p class="text-sm">No files ready for import</p>
              <p class="text-xs mt-1">Discover and parse files in Drive settings first.</p>
            </div>
          }

          <!-- Footer -->
          @if (driveImportService.files().length > 0) {
            <div class="flex justify-end gap-3 px-5 py-4 border-t border-border mt-4 -mx-5 -mb-5">
              <button type="button"
                class="px-4 py-1.5 text-sm text-foreground-secondary hover:text-foreground transition-colors"
                (click)="close()">Cancel</button>
              <button type="button"
                class="px-4 py-1.5 text-sm text-white bg-accent-solid hover:opacity-90 rounded-md transition-opacity disabled:opacity-50"
                [disabled]="driveImportService.selectedCount() === 0"
                (click)="confirmImport()">
                <i class="pi pi-download text-xs mr-1"></i>
                Import {{ driveImportService.selectedCount() }}
                {{ driveImportService.selectedCount() === 1 ? 'Meeting' : 'Meetings' }}
              </button>
            </div>
          }
        }

        @case ('importing') {
          <div class="flex flex-col items-center py-8">
            <div class="w-full max-w-[300px] h-1.5 bg-surface-muted rounded-full overflow-hidden mb-4">
              <div class="h-full bg-accent-solid rounded-full transition-all"
                [style.width.%]="importProgressPct()"></div>
            </div>
            <p class="text-sm font-medium text-foreground">
              Importing {{ driveImportService.importProgress().current }}
              of {{ driveImportService.importProgress().total }}...
            </p>
            <p class="text-xs text-foreground-muted mt-1">This may take a moment</p>
          </div>
        }

        @case ('done') {
          <div class="flex flex-col items-center py-8">
            @if (driveImportService.importResult()?.failures?.length) {
              <!-- Partial failure -->
              <i class="pi pi-exclamation-triangle text-4xl text-warning-foreground mb-3"></i>
              <p class="text-sm font-medium text-foreground mb-1">
                {{ driveImportService.importResult()!.importedCount }} of
                {{ driveImportService.importResult()!.importedCount + driveImportService.importResult()!.failures.length }}
                meetings imported
              </p>
              <p class="text-xs text-foreground-muted mb-3">
                {{ driveImportService.importResult()!.failures.length }} failed
              </p>
              <!-- Failed items list -->
              <div class="w-full max-w-sm space-y-2 mb-4">
                @for (failure of driveImportService.importResult()!.failures; track failure.driveFileImportId) {
                  <div class="px-3 py-2 bg-danger/10 border border-danger/20 rounded-lg text-xs">
                    <div class="font-medium text-foreground">{{ failure.fileName }}</div>
                    <div class="text-danger">{{ failure.error }}</div>
                  </div>
                }
              </div>
              <div class="flex gap-3">
                <button type="button"
                  class="px-4 py-1.5 text-sm text-foreground-secondary hover:text-foreground transition-colors"
                  (click)="close()">Close</button>
                <button type="button"
                  class="px-4 py-1.5 text-sm text-white bg-accent-solid hover:opacity-90 rounded-md transition-opacity"
                  (click)="retryFailed()">
                  <i class="pi pi-refresh text-xs mr-1"></i>
                  Retry Failed ({{ driveImportService.importResult()!.failures.length }})
                </button>
              </div>
            } @else {
              <!-- Full success -->
              <i class="pi pi-check-circle text-4xl text-done-foreground mb-3"></i>
              <p class="text-sm font-medium text-foreground mb-1">
                {{ driveImportService.importResult()!.importedCount }}
                {{ driveImportService.importResult()!.importedCount === 1 ? 'meeting' : 'meetings' }} imported
              </p>
              @if (driveImportService.importResult()!.totalActionItems > 0 || driveImportService.importResult()!.tagsCreated > 0) {
                <p class="text-xs text-foreground-muted">
                  @if (driveImportService.importResult()!.totalActionItems > 0) {
                    {{ driveImportService.importResult()!.totalActionItems }} action items found
                  }
                  @if (driveImportService.importResult()!.totalActionItems > 0 && driveImportService.importResult()!.tagsCreated > 0) {
                    &middot;
                  }
                  @if (driveImportService.importResult()!.tagsCreated > 0) {
                    {{ driveImportService.importResult()!.tagsCreated }} tags created
                  }
                </p>
              }
              @if (driveImportService.importResult()!.skippedCount > 0) {
                <p class="text-xs text-foreground-muted mt-1">
                  {{ driveImportService.importResult()!.skippedCount }} already imported, skipped
                </p>
              }
              <button type="button"
                class="mt-4 px-4 py-2 text-sm font-medium text-white bg-accent-solid rounded-md hover:opacity-90 transition-opacity"
                (click)="close()">Done</button>
            }
          </div>
        }

        @case ('error') {
          <app-error-state
            size="sm"
            title="Something went wrong"
            [message]="driveImportService.error()!"
            (retry)="driveImportService.loadPreview()"
          />
        }
      }
    </p-dialog>
  `,
})
export class DriveImportDialogComponent {
  readonly driveImportService = inject(DriveImportService);
  private readonly toast = inject(ToastService);

  readonly visible = signal(false);
  readonly onImported = output<void>();

  readonly closable = computed(() => {
    const state = this.driveImportService.state();
    return state !== 'loading' && state !== 'importing';
  });

  readonly importProgressPct = computed(() => {
    const p = this.driveImportService.importProgress();
    return p.total > 0 ? (p.current / p.total) * 100 : 0;
  });

  open(): void {
    this.driveImportService.reset();
    this.visible.set(true);
    this.driveImportService.loadPreview();
  }

  close(): void {
    this.visible.set(false);
    const result = this.driveImportService.importResult();
    if (result && result.importedCount > 0) {
      this.toast.success({
        summary: `${result.importedCount} meeting${result.importedCount !== 1 ? 's' : ''} imported from Drive`,
      });
      this.onImported.emit();
    }
    this.driveImportService.reset();
  }

  onVisibleChange(newVisible: boolean): void {
    if (!newVisible) {
      this.close();
    }
  }

  confirmImport(): void {
    this.driveImportService.confirmImport();
  }

  retryFailed(): void {
    this.driveImportService.retryFailed();
  }

  fmtDateTime(iso: string): string {
    return formatDateTime(iso);
  }

  onAddTag(fileIndex: number, event: Event): void {
    const input = event.target as HTMLInputElement;
    const value = input.value.trim();
    if (value) {
      this.driveImportService.addTag(fileIndex, value);
      input.value = '';
    }
  }
}
