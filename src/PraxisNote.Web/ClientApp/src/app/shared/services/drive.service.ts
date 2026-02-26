import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { DriveConnectionStatus } from '../models/drive-connection.model';

@Injectable({ providedIn: 'root' })
export class DriveService {
  private readonly http = inject(HttpClient);

  private readonly _status = signal<DriveConnectionStatus | null>(null);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _lastDisconnected = signal(false);

  readonly status = this._status.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();
  readonly lastDisconnected = this._lastDisconnected.asReadonly();

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
        this._status.set({ isConnected: false, provider: null, connectedAt: null, lastSyncedAt: null, folderName: null });
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
}
