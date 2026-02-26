import { Injectable, inject, signal, NgZone } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { DriveConnectionStatus, DriveFolder, DriveSyncResult } from '../models/drive-connection.model';
import { ToastService } from './toast.service';

@Injectable({ providedIn: 'root' })
export class DriveService {
  private readonly http = inject(HttpClient);
  private readonly ngZone = inject(NgZone);
  private readonly toast = inject(ToastService);

  private readonly _status = signal<DriveConnectionStatus | null>(null);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _lastDisconnected = signal(false);
  private readonly _folders = signal<DriveFolder[]>([]);
  private readonly _loadingFolders = signal(false);
  private readonly _folderLoadError = signal<string | null>(null);
  private readonly _saving = signal(false);
  private readonly _syncing = signal(false);
  private readonly _pendingReviewCount = signal(0);

  readonly status = this._status.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly lastDisconnected = this._lastDisconnected.asReadonly();
  readonly folders = this._folders.asReadonly();
  readonly loadingFolders = this._loadingFolders.asReadonly();
  readonly folderLoadError = this._folderLoadError.asReadonly();
  readonly saving = this._saving.asReadonly();
  readonly syncing = this._syncing.asReadonly();
  readonly pendingReviewCount = this._pendingReviewCount.asReadonly();

  loadConnectionStatus(): void {
    this._loading.set(true);
    this._error.set(null);

    this.http.get<DriveConnectionStatus>('/api/drive/status').subscribe({
      next: status => {
        this._status.set(status);
        this._pendingReviewCount.set(status.pendingReviewCount ?? 0);
        this._loading.set(false);
      },
      error: () => {
        this._error.set('Failed to load Drive connection status.');
        this._loading.set(false);
      },
    });
  }

  connectGoogleDrive(): void {
    // Redirect to OAuth flow - browser handles the redirect
    window.location.href = '/api/drive/connect/google';
  }

  disconnectDrive(): void {
    this._loading.set(true);
    this._error.set(null);
    this._lastDisconnected.set(false);

    this.http.post('/api/drive/disconnect', {}).subscribe({
      next: () => {
        this._status.set({
          isConnected: false, provider: null, connectedAt: null,
          lastSyncedAt: null, folderName: null, folderId: null,
          initialImportCutoffDate: null, syncFrequencyMinutes: null,
          autoAcceptTags: false, isConfigured: false,
          lastSyncAt: null, nextSyncAt: null,
          lastSyncFilesDiscovered: 0, lastSyncFilesImported: 0,
          lastSyncFilesPendingReview: 0, lastSyncFilesErrored: 0,
          lastSyncError: null, isSyncPaused: false, pendingReviewCount: 0,
        });
        this._pendingReviewCount.set(0);
        this._lastDisconnected.set(true);
        this._loading.set(false);
      },
      error: () => {
        this._error.set('Failed to disconnect. Please try again.');
        this._loading.set(false);
      },
    });
  }

  acknowledgeDisconnected(): void {
    this._lastDisconnected.set(false);
  }

  loadFolders(search?: string): void {
    this._loadingFolders.set(true);
    this._folderLoadError.set(null);

    const params = search ? `?search=${encodeURIComponent(search)}` : '';
    this.http.get<DriveFolder[]>(`/api/drive/folders${params}`).subscribe({
      next: folders => {
        this._folders.set(folders);
        this._loadingFolders.set(false);
      },
      error: () => {
        this._folders.set([]);
        this._folderLoadError.set('Failed to load folders. Please try again.');
        this._loadingFolders.set(false);
      },
    });
  }

  saveSettings(
    settings: {
      folderId: string;
      folderName: string;
      initialImportCutoffDate: string | null;
      syncFrequencyMinutes: number;
      autoAcceptTags: boolean;
    },
    onSuccess?: () => void,
    onError?: (message: string) => void,
  ): void {
    this._saving.set(true);

    this.http.put('/api/drive/settings', settings).subscribe({
      next: () => {
        this._saving.set(false);
        onSuccess?.();
      },
      error: (err) => {
        this._saving.set(false);
        const message = err?.error?.detail ?? err?.error?.title ?? 'Failed to save settings. Please try again.';
        onError?.(message);
      },
    });
  }

  syncNow(): void {
    this._syncing.set(true);

    this.http.post<DriveSyncResult>('/api/drive/sync', {}).subscribe({
      next: result => {
        this._syncing.set(false);
        // Reload status to get updated sync info
        this.loadConnectionStatus();

        if (result.error) {
          this.toast.error('Drive sync error', result.error);
        } else if (result.filesDiscovered === 0) {
          this.toast.success({ summary: 'Drive sync complete', detail: 'No new files found.' });
        } else {
          const parts: string[] = [];
          if (result.filesImported > 0) parts.push(`${result.filesImported} imported`);
          if (result.filesPendingReview > 0) parts.push(`${result.filesPendingReview} pending review`);
          if (result.filesErrored > 0) parts.push(`${result.filesErrored} errors`);
          this.toast.success({
            summary: 'Drive sync complete',
            detail: `${result.filesDiscovered} files found: ${parts.join(', ')}`,
          });
        }
      },
      error: () => {
        this._syncing.set(false);
        this.toast.error('Sync failed', 'Could not sync with Google Drive. Please try again.');
      },
    });
  }

  loadPendingCount(): void {
    this.http.get<{ count: number }>('/api/drive/pending-count').subscribe({
      next: result => this._pendingReviewCount.set(result.count),
    });
  }

  /** Called from NotificationService SSE listener when a drive-sync event arrives. */
  handleDriveSyncEvent(data: { type: string; count?: number; message?: string }): void {
    if (data.type === 'pending_review') {
      this._pendingReviewCount.update(c => c + (data.count ?? 0));
    } else if (data.type === 'auto_imported') {
      this.toast.success({
        summary: 'Drive sync complete',
        detail: data.message ?? 'Files auto-imported from Drive',
      });
      // Reload status to reflect new counts
      this.loadConnectionStatus();
    } else if (data.type === 'error') {
      this.toast.error('Drive sync error', data.message ?? 'An error occurred during Drive sync.');
    }
  }
}
