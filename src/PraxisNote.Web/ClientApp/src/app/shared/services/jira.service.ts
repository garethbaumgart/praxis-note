import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { JiraConnectionStatus, JiraIssue } from '../models/jira.model';

@Injectable({ providedIn: 'root' })
export class JiraService {
  private readonly http = inject(HttpClient);

  private readonly _status = signal<JiraConnectionStatus | null>(null);
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

    this.http.get<JiraConnectionStatus>('/api/jira/status').subscribe({
      next: status => {
        this._status.set(status);
        this._loading.set(false);
      },
      error: () => {
        this._error.set('Failed to load Jira connection status.');
        this._loading.set(false);
      },
    });
  }

  connectJira(): void {
    window.location.href = '/api/jira/connect';
  }

  disconnectJira(): void {
    this._loading.set(true);
    this._error.set(null);
    this._lastDisconnected.set(false);

    this.http.post('/api/jira/disconnect', {}).subscribe({
      next: () => {
        this._status.set({ isConnected: false, siteUrl: null, connectedAt: null });
        this._lastDisconnected.set(true);
        this._loading.set(false);
      },
      error: () => {
        this._error.set('Failed to disconnect Jira. Please try again.');
        this._loading.set(false);
      },
    });
  }

  acknowledgeDisconnected(): void {
    this._lastDisconnected.set(false);
  }

  resolveIssue(issueKey: string): Promise<JiraIssue> {
    return new Promise((resolve, reject) => {
      this.http.get<JiraIssue>(`/api/jira/issue/${encodeURIComponent(issueKey)}`).subscribe({
        next: issue => resolve(issue),
        error: err => reject(err),
      });
    });
  }
}
