import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { DriveConnectionStatus, DriveFolder } from '../models/drive-connection.model';

@Injectable({ providedIn: 'root' })
export class DriveService {
  private readonly http = inject(HttpClient);

  private readonly _status = signal<DriveConnectionStatus | null>(null);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _lastDisconnected = signal(false);
  private readonly _folders = signal<DriveFolder[]>([]);
  private readonly _loadingFolders = signal(false);
  private readonly _saving = signal(false);

  readonly status = this._status.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly lastDisconnected = this._lastDisconnected.asReadonly();
  readonly folders = this._folders.asReadonly();
  readonly loadingFolders = this._loadingFolders.asReadonly();
  readonly saving = this._saving.asReadonly();

  loadConnectionStatus(): void {
    this._loading.set(true);
    this._error.set(null);

    this.http.get<DriveConnectionStatus>('/api/drive/status').subscribe({
      next: status => {
        this._status.set(status);
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
        });
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

    const params = search ? `?search=${encodeURIComponent(search)}` : '';
    this.http.get<DriveFolder[]>(`/api/drive/folders${params}`).subscribe({
      next: folders => {
        this._folders.set(folders);
        this._loadingFolders.set(false);
      },
      error: () => {
        this._folders.set([]);
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
    onError?: () => void,
  ): void {
    this._saving.set(true);

    this.http.put('/api/drive/settings', settings).subscribe({
      next: () => {
        this._saving.set(false);
        onSuccess?.();
      },
      error: () => {
        this._saving.set(false);
        onError?.();
      },
    });
  }
}
