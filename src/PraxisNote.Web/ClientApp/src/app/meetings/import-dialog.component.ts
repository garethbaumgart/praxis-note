import { Component, ChangeDetectionStrategy, inject, signal, computed, output } from '@angular/core';
import { Dialog } from 'primeng/dialog';
import { Checkbox } from 'primeng/checkbox';
import { ProgressSpinner } from 'primeng/progressspinner';
import { FormsModule } from '@angular/forms';
import { ScreenshotImportService } from './screenshot-import.service';
import { CalendarService } from '../shared/services/calendar.service';
import { formatDateTime as sharedFormatDateTime, formatLocaleTime, formatShortDate } from '../shared/date-utils';

@Component({
  selector: 'app-import-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Dialog, Checkbox, ProgressSpinner, FormsModule],
  template: `
    <p-dialog
      header="Import Meetings"
      [visible]="visible()"
      (visibleChange)="onVisibleChange($event)"
      [modal]="true"
      [style]="{ width: '480px' }"
      [breakpoints]="{ '640px': '95vw' }"
      [closable]="closable()"
    >
      <!-- Tab bar -->
      <div class="flex border-b border-border-muted mb-4">
        <button
          type="button"
          class="flex-1 py-3 text-sm font-medium transition-colors"
          [class.text-accent-foreground]="activeTab() === 'calendar'"
          [class.text-foreground-muted]="activeTab() !== 'calendar'"
          [style.border-bottom]="activeTab() === 'calendar' ? '2px solid var(--color-primary-solid)' : '2px solid transparent'"
          (click)="activeTab.set('calendar')"
        >
          <i class="pi pi-google mr-1.5 text-xs"></i>
          Google Calendar
        </button>
        <button
          type="button"
          class="flex-1 py-3 text-sm font-medium transition-colors"
          [class.text-accent-foreground]="activeTab() === 'screenshot'"
          [class.text-foreground-muted]="activeTab() !== 'screenshot'"
          [style.border-bottom]="activeTab() === 'screenshot' ? '2px solid var(--color-primary-solid)' : '2px solid transparent'"
          (click)="activeTab.set('screenshot')"
        >
          <i class="pi pi-image mr-1.5 text-xs"></i>
          Screenshot
        </button>
      </div>

      @switch (activeTab()) {
        @case ('calendar') {
          @if (calendarService.status()?.isConnected) {
            <!-- Connected state -->
            <div>
              <div class="flex items-center gap-2 mb-4">
                <div class="w-6 h-6 rounded-full flex items-center justify-center bg-done/30">
                  <i class="pi pi-check text-xs text-done-foreground"></i>
                </div>
                <span class="text-sm text-foreground font-medium">Connected to Google Calendar</span>
              </div>
              <div class="bg-surface-muted rounded-lg p-4 mb-4">
                <div class="flex items-center justify-between mb-2">
                  <span class="text-sm font-medium text-foreground">Sync window</span>
                  <span class="text-xs px-2 py-0.5 bg-accent rounded text-accent-foreground font-medium">Next 7 days</span>
                </div>
                <p class="text-xs text-foreground-muted">
                  Imports meetings from today through {{ syncEndDate() }}. Events beyond this range are not included.
                </p>
              </div>
              <div class="flex items-center gap-2 text-xs text-foreground-muted mb-5">
                <i class="pi pi-clock text-xs"></i>
                <span>Last synced: {{ lastSyncFormatted() }}</span>
              </div>
              <button type="button"
                class="w-full py-2.5 bg-accent-solid text-white rounded-lg text-sm font-medium flex items-center justify-center gap-2 hover:opacity-90 transition-opacity"
                [disabled]="calendarService.syncing()"
                (click)="syncNow()"
              >
                @if (calendarService.syncing()) {
                  <i class="pi pi-spin pi-spinner text-xs"></i>
                  Syncing...
                } @else {
                  <i class="pi pi-sync text-xs"></i>
                  Sync Now
                }
              </button>
            </div>
          } @else {
            <!-- Not connected state -->
            <div class="py-4 text-center">
              <div class="w-14 h-14 rounded-full bg-surface-muted flex items-center justify-center mx-auto mb-4">
                <i class="pi pi-calendar text-2xl text-foreground-muted"></i>
              </div>
              <p class="text-base font-medium text-foreground mb-1">Connect your calendar</p>
              <p class="text-sm text-foreground-muted mb-5">
                Automatically import meetings for the next 7 days.<br>Only titles, times, and attendees are synced.
              </p>
              <button type="button"
                class="px-5 py-2.5 bg-accent-solid text-white rounded-lg text-sm font-medium inline-flex items-center gap-2 hover:opacity-90 transition-opacity"
                (click)="connectCalendar()"
              >
                <i class="pi pi-google text-xs"></i>
                Connect Google Calendar
              </button>
            </div>
          }
        }
        @case ('screenshot') {
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
        }
      }
    </p-dialog>
  `,
})
export class ImportDialogComponent {
  readonly importService = inject(ScreenshotImportService);
  readonly calendarService = inject(CalendarService);

  private readonly supportedTypes = ['image/png', 'image/jpeg', 'image/webp'];

  readonly visible = signal(false);
  readonly activeTab = signal<'calendar' | 'screenshot'>('screenshot');
  readonly onImported = output<void>();

  readonly closable = computed(() =>
    this.importService.state() !== 'extracting' && this.importService.state() !== 'importing'
  );

  readonly selectedCount = computed(() => this.importService.events().filter(e => e.selected).length);
  readonly allSelected = computed(() => {
    const events = this.importService.events();
    return events.length > 0 && events.every(e => e.selected);
  });

  readonly syncEndDate = computed(() => {
    const d = new Date();
    d.setDate(d.getDate() + 7);
    return formatShortDate(d);
  });

  readonly lastSyncFormatted = computed(() => {
    const lastSync = this.calendarService.status()?.lastSyncedAt;
    if (!lastSync) return 'Never';
    return sharedFormatDateTime(lastSync);
  });

  open(): void {
    this.importService.reset();
    this.activeTab.set(this.calendarService.status()?.isConnected ? 'calendar' : 'screenshot');
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

  syncNow(): void {
    this.calendarService.syncEvents();
  }

  connectCalendar(): void {
    this.calendarService.connectGoogleCalendar();
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    const file = event.dataTransfer?.files[0];
    if (file && this.supportedTypes.includes(file.type)) {
      this.processFile(file);
    } else if (file) {
      this.importService.error.set('Unsupported image format. Please use PNG, JPG, or WebP.');
      this.importService.state.set('error');
    }
  }

  onPaste(event: ClipboardEvent): void {
    const items = event.clipboardData?.items;
    if (!items) return;

    for (const item of Array.from(items)) {
      if (this.supportedTypes.includes(item.type)) {
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
    reader.onerror = () => {
      this.importService.error.set('Failed to read the file. Please try again.');
      this.importService.state.set('error');
    };
    reader.readAsDataURL(file);
  }

  toggleAll(): void {
    this.importService.toggleAll(!this.allSelected());
  }

  importSelected(): void {
    this.importService.importSelected(() => {
      // Each meeting created triggers a reload
    });
  }

  formatDateTime(iso: string): string {
    return sharedFormatDateTime(iso);
  }

  formatTime(iso: string): string {
    return formatLocaleTime(iso);
  }
}
