import { Component, ChangeDetectionStrategy, DestroyRef, inject, signal, output } from '@angular/core';
import { Dialog } from 'primeng/dialog';
import { DriveService } from '../shared/services/drive.service';
import { ToastService } from '../shared/services/toast.service';
import { DriveConnectionStatus, DriveFolder } from '../shared/models/drive-connection.model';

@Component({
  selector: 'app-drive-setup-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Dialog],
  template: `
    <p-dialog
      [visible]="visible()"
      (visibleChange)="onVisibleChange($event)"
      [modal]="true"
      [draggable]="false"
      [resizable]="false"
      [dismissableMask]="true"
      [closable]="true"
      [style]="{ width: '30rem' }"
      [breakpoints]="{ '640px': '95vw' }"
      [header]="step() === 1 ? 'Select Drive Folder' : 'Configure Import'"
    >
      @if (step() === 1) {
        <!-- Step 1: Folder Picker -->
        <div class="space-y-3">
          <input
            type="text"
            class="w-full px-3 py-2 text-sm border border-border rounded-lg bg-surface text-foreground placeholder:text-foreground-muted focus:outline-none focus:ring-2 focus:ring-accent/50"
            placeholder="Search folders..."
            aria-label="Search folders"
            [value]="searchQuery()"
            (input)="onSearchInput($any($event.target).value)"
          />

          @if (driveService.loadingFolders()) {
            <div class="flex items-center gap-3 py-8 justify-center" role="status" aria-label="Loading folders">
              <i class="pi pi-spin pi-spinner text-sm text-foreground-muted" aria-hidden="true"></i>
              <span class="text-sm text-foreground-muted">Loading folders...</span>
              <span class="sr-only">Loading folders...</span>
            </div>
          } @else if (driveService.folderLoadError()) {
            <div class="flex flex-col items-center py-8 text-foreground-muted">
              <i class="pi pi-exclamation-circle text-2xl mb-2 text-danger" aria-hidden="true"></i>
              <p class="text-sm text-danger mb-2">{{ driveService.folderLoadError() }}</p>
              <button type="button" class="text-sm text-accent underline" (click)="driveService.loadFolders(searchQuery() || undefined)">Try again</button>
            </div>
          } @else if (driveService.folders().length === 0) {
            <div class="flex flex-col items-center py-8 text-foreground-muted">
              <i class="pi pi-inbox text-2xl mb-2" aria-hidden="true"></i>
              <p class="text-sm">No folders found</p>
            </div>
          } @else {
            <div class="max-h-60 overflow-y-auto space-y-1" role="listbox" aria-label="Drive folders">
              @for (folder of driveService.folders(); track folder.id) {
                <button
                  type="button"
                  role="option"
                  class="w-full flex items-center gap-2.5 px-3 py-2 rounded-lg text-left transition-colors"
                  [class.bg-accent]="selectedFolderId() === folder.id"
                  [class.border]="selectedFolderId() === folder.id"
                  [class.border-accent-solid]="selectedFolderId() === folder.id"
                  (click)="selectFolder(folder)"
                  [attr.aria-selected]="selectedFolderId() === folder.id"
                >
                  <i class="pi pi-folder text-sm" style="color: #ebcb8b;" aria-hidden="true"></i>
                  <span class="text-sm text-foreground truncate">{{ folder.name }}</span>
                  @if (selectedFolderId() === folder.id) {
                    <i class="pi pi-check text-xs text-accent-foreground ml-auto" aria-hidden="true"></i>
                  }
                </button>
              }
            </div>
          }
        </div>

        <!-- Step 1 footer -->
        <div class="flex justify-end gap-3 px-5 py-4 border-t border-border mt-4 -mx-5 -mb-5">
          <button type="button" class="px-4 py-1.5 text-sm text-foreground-secondary hover:text-foreground transition-colors" (click)="visible.set(false)">Cancel</button>
          <button type="button" class="px-4 py-1.5 text-sm text-white bg-accent-solid hover:opacity-90 rounded-md transition-opacity disabled:opacity-50" [disabled]="!selectedFolderId()" (click)="step.set(2)">Next: Configure</button>
        </div>
      } @else {
        <!-- Step 2: Configure Import -->
        <div class="space-y-4">
          <!-- Selected folder badge -->
          <div class="flex items-center gap-2 px-3 py-2 bg-accent rounded-lg">
            <i class="pi pi-folder text-sm text-accent-foreground" aria-hidden="true"></i>
            <span class="text-sm font-medium text-accent-foreground truncate">{{ selectedFolderName() }}</span>
            <button type="button" class="ml-auto text-xs text-accent-foreground underline" (click)="step.set(1)">Change</button>
          </div>

          <!-- Cutoff date -->
          <div>
            <label for="cutoffDate" class="block text-sm font-medium text-foreground mb-1">Import files from</label>
            <input id="cutoffDate" type="date" class="w-full px-3 py-2 text-sm border border-border rounded-lg bg-surface text-foreground focus:outline-none focus:ring-2 focus:ring-accent/50" [value]="cutoffDate()" (input)="cutoffDate.set($any($event.target).value)" />
            <p class="text-xs text-foreground-muted mt-1">Files older than this date will be skipped</p>
          </div>

          <!-- Sync frequency -->
          <div>
            <label for="syncFreq" class="block text-sm font-medium text-foreground mb-1">Sync frequency</label>
            <select id="syncFreq" class="w-full px-3 py-2 text-sm border border-border rounded-lg bg-surface text-foreground focus:outline-none focus:ring-2 focus:ring-accent/50" [value]="syncFrequency()" (change)="syncFrequency.set(+$any($event.target).value)">
              <option [value]="15">Every 15 minutes</option>
              <option [value]="30">Every 30 minutes</option>
              <option [value]="60">Every hour</option>
              <option [value]="0">Manual only</option>
            </select>
          </div>

          <!-- Auto-accept tags -->
          <div class="flex items-center justify-between">
            <div>
              <div class="text-sm font-medium text-foreground">Auto-accept tags</div>
              <div class="text-xs text-foreground-muted">Automatically apply AI-suggested tags to imported files</div>
            </div>
            <button type="button" role="switch" [attr.aria-checked]="autoAcceptTags()" class="relative w-9 h-5 rounded-full transition-colors" [class.bg-accent-solid]="autoAcceptTags()" [class.bg-surface-muted]="!autoAcceptTags()" (click)="toggleAutoAcceptTags()" aria-label="Toggle auto-accept tags">
              <span class="block w-4 h-4 rounded-full bg-white shadow absolute top-0.5 transition-transform" [class.translate-x-4]="autoAcceptTags()" [class.translate-x-0.5]="!autoAcceptTags()"></span>
            </button>
          </div>
        </div>

        <!-- Step 2 footer -->
        <div class="flex justify-end gap-3 px-5 py-4 border-t border-border mt-4 -mx-5 -mb-5">
          <button type="button" class="px-4 py-1.5 text-sm text-foreground-secondary hover:text-foreground transition-colors" (click)="step.set(1)">Back</button>
          <button type="button" class="px-4 py-1.5 text-sm text-white bg-accent-solid hover:opacity-90 rounded-md transition-opacity disabled:opacity-50" [disabled]="driveService.saving()" (click)="saveAndImport()">
            @if (driveService.saving()) {
              <i class="pi pi-spin pi-spinner text-xs mr-1" aria-hidden="true"></i>
            }
            Save &amp; Start Import
          </button>
        </div>
      }
    </p-dialog>
  `,
})
export class DriveSetupDialogComponent {
  readonly driveService = inject(DriveService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  readonly visible = signal(false);
  readonly step = signal(1);
  readonly searchQuery = signal('');
  readonly selectedFolderId = signal<string | null>(null);
  readonly selectedFolderName = signal('');
  readonly cutoffDate = signal('');
  readonly syncFrequency = signal(15);
  readonly autoAcceptTags = signal(false);

  readonly onSaved = output<void>();

  private searchTimeout: ReturnType<typeof setTimeout> | null = null;

  constructor() {
    this.destroyRef.onDestroy(() => this.clearSearchTimeout());
  }

  open(existingConfig?: DriveConnectionStatus): void {
    this.step.set(1);
    this.searchQuery.set('');

    if (existingConfig?.isConfigured) {
      this.selectedFolderId.set(existingConfig.folderId);
      this.selectedFolderName.set(existingConfig.folderName ?? '');
      this.cutoffDate.set(existingConfig.initialImportCutoffDate ?? this.defaultCutoffDate());
      this.syncFrequency.set(existingConfig.syncFrequencyMinutes ?? 15);
      this.autoAcceptTags.set(existingConfig.autoAcceptTags);
    } else {
      this.selectedFolderId.set(null);
      this.selectedFolderName.set('');
      this.cutoffDate.set(this.defaultCutoffDate());
      this.syncFrequency.set(15);
      this.autoAcceptTags.set(false);
    }

    this.visible.set(true);
    this.driveService.loadFolders();
  }

  onVisibleChange(visible: boolean): void {
    this.visible.set(visible);
    if (!visible) {
      this.clearSearchTimeout();
    }
  }

  toggleAutoAcceptTags(): void {
    this.autoAcceptTags.set(!this.autoAcceptTags());
  }

  selectFolder(folder: DriveFolder): void {
    this.selectedFolderId.set(folder.id);
    this.selectedFolderName.set(folder.name);
  }

  onSearchInput(value: string): void {
    this.searchQuery.set(value);
    this.clearSearchTimeout();

    this.searchTimeout = setTimeout(() => {
      this.driveService.loadFolders(value || undefined);
    }, 300);
  }

  saveAndImport(): void {
    const folderId = this.selectedFolderId();
    const folderName = this.selectedFolderName();
    if (!folderId || !folderName) return;

    this.driveService.saveSettings(
      {
        folderId,
        folderName,
        initialImportCutoffDate: this.cutoffDate() || null,
        syncFrequencyMinutes: this.syncFrequency(),
        autoAcceptTags: this.autoAcceptTags(),
      },
      () => {
        this.visible.set(false);
        this.onSaved.emit();
      },
      (message) => {
        this.toast.error(message);
      },
    );
  }

  private clearSearchTimeout(): void {
    if (this.searchTimeout) {
      clearTimeout(this.searchTimeout);
      this.searchTimeout = null;
    }
  }

  private defaultCutoffDate(): string {
    const date = new Date();
    date.setDate(date.getDate() - 30);
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
