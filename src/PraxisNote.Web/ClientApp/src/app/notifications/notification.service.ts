import { Injectable, inject, signal, computed, isDevMode, DestroyRef } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { MockAuthService } from '../auth/mock-auth.service';
import { Notification } from './notification.model';

const POLL_INTERVAL_MS = 30_000; // 30 seconds

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly mockAuth = inject(MockAuthService);
  private readonly destroyRef = inject(DestroyRef);

  private pollTimer: ReturnType<typeof setInterval> | null = null;
  private visibilityHandler: (() => void) | null = null;
  private pollInFlight = false;
  private destroyRegistered = false;
  private panelOpen = false;

  private readonly _notifications = signal<Notification[]>([]);
  private readonly _unseenCount = signal(0);
  private readonly _loading = signal(false);

  readonly notifications = this._notifications.asReadonly();
  readonly unseenCount = this._unseenCount.asReadonly();
  readonly loading = this._loading.asReadonly();

  readonly newNotifications = computed(() =>
    this._notifications().filter(n => !n.isSeen)
  );

  readonly historyNotifications = computed(() =>
    this._notifications().filter(n => n.isSeen)
  );

  /**
   * Start polling for unseen notification count every 30s.
   * Pauses when tab is hidden, resumes on focus.
   * Uses fetch() instead of HttpClient — notification count is non-critical
   * and should NOT trigger auth interceptor page reload on 401.
   */
  startPolling(): void {
    if (this.pollTimer) return;

    // Register destroy handler only once across login/logout cycles
    if (!this.destroyRegistered) {
      this.destroyRef.onDestroy(() => this.stopPolling());
      this.destroyRegistered = true;
    }

    // Pause/resume on tab visibility
    this.visibilityHandler = () => {
      if (document.hidden) {
        this.clearPollTimer();
      } else if (!this.pollTimer) {
        // Fetch immediately on tab focus, then resume interval
        this.pollUnseenCount();
        this.pollTimer = setInterval(() => this.pollUnseenCount(), POLL_INTERVAL_MS);
      }
    };
    document.addEventListener('visibilitychange', this.visibilityHandler);

    // If the tab is currently visible, fetch immediately and start interval;
    // otherwise wait for a visibilitychange event to start polling.
    if (!document.hidden) {
      this.pollUnseenCount();
      this.pollTimer = setInterval(() => this.pollUnseenCount(), POLL_INTERVAL_MS);
    }
  }

  stopPolling(): void {
    this.clearPollTimer();
    if (this.visibilityHandler) {
      document.removeEventListener('visibilitychange', this.visibilityHandler);
      this.visibilityHandler = null;
    }
  }

  private clearPollTimer(): void {
    if (this.pollTimer) {
      clearInterval(this.pollTimer);
      this.pollTimer = null;
    }
  }

  private async pollUnseenCount(): Promise<void> {
    // Guard against overlapping requests — skip if a request is already in flight
    if (this.pollInFlight) return;
    this.pollInFlight = true;

    try {
      const headers: Record<string, string> = {};

      if (isDevMode()) {
        const mockHeader = this.mockAuth.getMockHeader();
        if (mockHeader) {
          headers['X-Mock-User'] = mockHeader;
        }
      }

      // Notifications are user-scoped (not profile-scoped); do not send X-Profile-Id

      const response = await fetch('/api/notifications/count', {
        headers,
        credentials: 'include',
      });

      if (response.ok) {
        const data = await response.json();
        const newCount = data.count as number;
        const previousCount = this._unseenCount();
        this._unseenCount.set(newCount);

        // If the notification panel is currently open and the count changed,
        // refresh the list so it stays up-to-date without a manual reopen.
        if (newCount !== previousCount && this.panelOpen) {
          this.loadNotifications();
        }
      }
      // Silently ignore errors — non-critical polling
    } catch {
      // Network error — silently ignore, will retry on next interval
    } finally {
      this.pollInFlight = false;
    }
  }

  /** Track panel visibility so polling only refreshes the list when the panel is open. */
  setPanelOpen(open: boolean): void {
    this.panelOpen = open;
  }

  /** Load full notification list (called when panel opens). Uses HttpClient — this is a user-initiated action. */
  loadNotifications(): void {
    this._loading.set(true);
    this.http.get<Notification[]>('/api/notifications').subscribe({
      next: (notifications) => {
        this._notifications.set(notifications);
        this._loading.set(false);
      },
      error: () => this._loading.set(false),
    });
  }

  markAllAsSeen(): void {
    const notifications = this._notifications();
    if (notifications.length === 0) return;

    const maxId = Math.max(...notifications.map(n => n.id));

    // Optimistic update
    this._notifications.update(list =>
      list.map(n => ({ ...n, isSeen: true }))
    );
    this._unseenCount.set(0);

    this.http.post('/api/notifications/seen', { lastSeenNotificationId: maxId }).subscribe({
      error: () => this.loadNotifications(),
    });
  }
}
