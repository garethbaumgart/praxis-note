import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CalendarConnectionStatus, SyncResult } from '../models/calendar-connection.model';

@Injectable({ providedIn: 'root' })
export class CalendarService {
  private readonly http = inject(HttpClient);

  private readonly _status = signal<CalendarConnectionStatus | null>(null);
  private readonly _loading = signal(false);
  private readonly _syncing = signal(false);
  private readonly _error = signal<string | null>(null);
  private readonly _lastSyncResult = signal<SyncResult | null>(null);
  private readonly _lastDisconnected = signal(false);

  readonly status = this._status.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly syncing = this._syncing.asReadonly();
  readonly error = this._error.asReadonly();
  readonly lastSyncResult = this._lastSyncResult.asReadonly();
  readonly lastDisconnected = this._lastDisconnected.asReadonly();

  loadConnectionStatus(): void {
    this._loading.set(true);
    this._error.set(null);

    this.http.get<CalendarConnectionStatus>('/api/calendar/status').subscribe({
      next: status => {
        this._status.set(status);
        this._loading.set(false);
      },
      error: () => {
        this._error.set('Failed to load calendar connection status.');
        this._loading.set(false);
      },
    });
  }

  connectGoogleCalendar(): void {
    // Redirect to OAuth flow - browser handles the redirect
    window.location.href = '/api/calendar/connect/google';
  }

  syncEvents(): void {
    this._syncing.set(true);
    this._error.set(null);
    this._lastSyncResult.set(null);

    this.http.post<SyncResult>('/api/calendar/sync', {}).subscribe({
      next: result => {
        this._syncing.set(false);
        this._lastSyncResult.set(result);
        // Refresh status to update lastSyncedAt
        this.loadConnectionStatus();
      },
      error: (err) => {
        this._syncing.set(false);
        this._error.set(err.error?.error ?? 'Sync failed. Please try again.');
      },
    });
  }

  acknowledgeDisconnected(): void {
    this._lastDisconnected.set(false);
  }

  disconnectCalendar(): void {
    this._loading.set(true);
    this._error.set(null);
    this._lastDisconnected.set(false);

    this.http.post('/api/calendar/disconnect', {}).subscribe({
      next: () => {
        this._status.set({ isConnected: false, provider: null, connectedAt: null, lastSyncedAt: null });
        this._lastDisconnected.set(true);
        this._loading.set(false);
      },
      error: () => {
        this._error.set('Failed to disconnect. Please try again.');
        this._loading.set(false);
      },
    });
  }
}
