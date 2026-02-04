import { Component, ChangeDetectionStrategy, inject, signal, computed, output } from '@angular/core';
import { Dialog } from 'primeng/dialog';
import { Checkbox } from 'primeng/checkbox';
import { ProgressSpinner } from 'primeng/progressspinner';
import { FormsModule } from '@angular/forms';
import { ScreenshotImportService } from './screenshot-import.service';

@Component({
  selector: 'app-screenshot-import-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Dialog, Checkbox, ProgressSpinner, FormsModule],
  template: `
    <p-dialog
      header="Import Meetings from Screenshot"
      [visible]="visible()"
      (visibleChange)="onVisibleChange($event)"
      [modal]="true"
      [style]="{ width: '480px' }"
      [breakpoints]="{ '640px': '95vw' }"
      [closable]="importService.state() !== 'extracting' && importService.state() !== 'importing'"
    >
      @switch (importService.state()) {
        @case ('idle') {
          <!-- Upload/Paste state -->
          <div
            class="border-2 border-dashed border-border rounded-xl p-8 text-center cursor-pointer hover:border-accent-solid/50 hover:bg-surface-muted/50 transition-colors"
            (click)="fileInput.click()"
            (dragover)="onDragOver($event)"
            (drop)="onDrop($event)"
            (paste)="onPaste($event)"
            tabindex="0"
            (keydown.enter)="fileInput.click()"
            (keydown.space)="fileInput.click(); $event.preventDefault()"
            role="button"
            aria-label="Upload or paste a calendar screenshot"
          >
            <i class="pi pi-image text-3xl text-foreground-muted mb-3"></i>
            <p class="text-sm font-medium text-foreground mb-1">Drop or paste a screenshot</p>
            <p class="text-xs text-foreground-muted">PNG, JPG, or WebP of your calendar view</p>
            <p class="text-xs text-foreground-muted mt-2">
              <kbd class="px-1.5 py-0.5 bg-surface border border-border rounded text-foreground-muted">Ctrl+V</kbd>
              to paste from clipboard
            </p>
            <input
              #fileInput
              type="file"
              accept="image/png,image/jpeg,image/webp"
              class="hidden"
              (change)="onFileSelected($event)"
              aria-label="Select screenshot file"
            >
          </div>
        }

        @case ('extracting') {
          <div class="flex flex-col items-center py-8">
            <p-progressSpinner [style]="{ width: '48px', height: '48px' }" strokeWidth="3" />
            <p class="text-sm text-foreground-muted mt-4">Analyzing calendar screenshot...</p>
          </div>
        }

        @case ('preview') {
          <!-- Preview extracted meetings -->
          <div>
            <div class="flex items-center justify-between mb-3">
              <span class="text-sm text-foreground-muted">{{ selectedCount() }} of {{ importService.events().length }} selected</span>
              <button
                type="button"
                class="text-xs text-accent-solid hover:underline"
                (click)="toggleAll()"
                aria-label="Toggle all meetings"
              >
                {{ allSelected() ? 'Deselect all' : 'Select all' }}
              </button>
            </div>
            <div class="space-y-2 max-h-64 overflow-y-auto">
              @for (event of importService.events(); track $index) {
                <label class="flex items-start gap-3 p-3 bg-surface-muted rounded-lg cursor-pointer hover:bg-surface-muted/80 transition-colors">
                  <p-checkbox
                    [ngModel]="event.selected"
                    (ngModelChange)="importService.toggleEvent($index)"
                    [binary]="true"
                    styleClass="mt-0.5"
                  />
                  <div class="flex-1 min-w-0">
                    <p class="text-sm font-medium text-foreground truncate">{{ event.title }}</p>
                    <p class="text-xs text-foreground-muted">{{ formatDateTime(event.startTime) }} - {{ formatTime(event.endTime) }}</p>
                    @if (event.attendees) {
                      <p class="text-xs text-foreground-muted mt-0.5">
                        <i class="pi pi-users text-xs mr-1"></i>{{ event.attendees }}
                      </p>
                    }
                  </div>
                </label>
              }
            </div>
            <div class="flex justify-end gap-2 mt-4">
              <button
                type="button"
                class="px-4 py-2 text-sm text-foreground-secondary bg-surface-muted rounded-md hover:bg-surface-muted/80 transition-colors"
                (click)="importService.reset()"
              >
                Back
              </button>
              <button
                type="button"
                class="px-4 py-2 text-sm font-medium text-white bg-accent-solid rounded-md hover:opacity-90 transition-opacity disabled:opacity-50"
                [disabled]="selectedCount() === 0"
                (click)="importSelected()"
                aria-label="Import selected meetings"
              >
                Import {{ selectedCount() }} {{ selectedCount() === 1 ? 'Meeting' : 'Meetings' }}
              </button>
            </div>
          </div>
        }

        @case ('importing') {
          <div class="flex flex-col items-center py-8">
            <p-progressSpinner [style]="{ width: '48px', height: '48px' }" strokeWidth="3" />
            <p class="text-sm text-foreground-muted mt-4">
              Importing {{ importService.importedCount() }} of {{ selectedCount() }}...
            </p>
          </div>
        }

        @case ('done') {
          <div class="flex flex-col items-center py-8">
            <i class="pi pi-check-circle text-4xl text-done-text mb-3"></i>
            <p class="text-sm font-medium text-foreground">{{ importService.importedCount() }} meetings imported</p>
            <button
              type="button"
              class="mt-4 px-4 py-2 text-sm font-medium text-white bg-accent-solid rounded-md hover:opacity-90 transition-opacity"
              (click)="close()"
            >
              Done
            </button>
          </div>
        }

        @case ('error') {
          <div class="flex flex-col items-center py-8">
            <i class="pi pi-exclamation-triangle text-4xl text-danger mb-3"></i>
            <p class="text-sm text-danger text-center">{{ importService.error() }}</p>
            <button
              type="button"
              class="mt-4 px-4 py-2 text-sm font-medium text-white bg-accent-solid rounded-md hover:opacity-90 transition-opacity"
              (click)="importService.reset()"
            >
              Try Again
            </button>
          </div>
        }
      }
    </p-dialog>
  `,
})
export class ScreenshotImportDialogComponent {
  readonly importService = inject(ScreenshotImportService);

  readonly visible = signal(false);
  readonly onImported = output<void>();

  readonly selectedCount = computed(() => this.importService.events().filter(e => e.selected).length);
  readonly allSelected = computed(() => {
    const events = this.importService.events();
    return events.length > 0 && events.every(e => e.selected);
  });

  private lastSelectedCount = 0;

  open(): void {
    this.importService.reset();
    this.visible.set(true);
  }

  close(): void {
    this.visible.set(false);
    if (this.importService.state() === 'done') {
      this.onImported.emit();
    }
    this.importService.reset();
  }

  onVisibleChange(visible: boolean): void {
    this.visible.set(visible);
    if (!visible) {
      this.importService.reset();
    }
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    const file = event.dataTransfer?.files[0];
    if (file && file.type.startsWith('image/')) {
      this.processFile(file);
    }
  }

  onPaste(event: ClipboardEvent): void {
    const items = event.clipboardData?.items;
    if (!items) return;

    for (const item of Array.from(items)) {
      if (item.type.startsWith('image/')) {
        const file = item.getAsFile();
        if (file) {
          this.processFile(file);
          return;
        }
      }
    }
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) {
      this.processFile(file);
      input.value = '';
    }
  }

  private processFile(file: File): void {
    const reader = new FileReader();
    reader.onload = () => {
      const dataUrl = reader.result as string;
      // Extract base64 and media type from data URL
      const [header, base64Data] = dataUrl.split(',');
      const mediaType = header.match(/data:(.*?);/)?.[1] ?? 'image/png';
      this.importService.extractFromImage(base64Data, mediaType);
    };
    reader.readAsDataURL(file);
  }

  toggleAll(): void {
    this.importService.toggleAll(!this.allSelected());
  }

  importSelected(): void {
    this.lastSelectedCount = this.selectedCount();
    this.importService.importSelected(() => {
      // Each meeting created triggers a reload
    });
  }

  formatDateTime(iso: string): string {
    const date = new Date(iso);
    return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' }) +
      ' ' + date.toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' });
  }

  formatTime(iso: string): string {
    return new Date(iso).toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' });
  }
}
