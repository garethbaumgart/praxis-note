import { Injectable, inject, signal, computed, isDevMode, DestroyRef } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { MockAuthService } from '../auth/mock-auth.service';
import { ProfileService } from '../profiles/profile.service';
import { Notification } from './notification.model';

const POLL_INTERVAL_MS = 30_000; // 30 seconds

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly mockAuth = inject(MockAuthService);
  private readonly profileService = inject(ProfileService);
  private readonly destroyRef = inject(DestroyRef);

  private pollTimer: ReturnType<typeof setInterval> | null = null;
  private visibilityHandler: (() => void) | null = null;

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

    // Fetch immediately on start
    this.pollUnseenCount();

    // Set up interval
    this.pollTimer = setInterval(() => this.pollUnseenCount(), POLL_INTERVAL_MS);

    // Pause/resume on tab visibility
    this.visibilityHandler = () => {
      if (document.hidden) {
        this.clearPollTimer();
      } else {
        // Fetch immediately on tab focus, then resume interval
        this.pollUnseenCount();
        this.pollTimer = setInterval(() => this.pollUnseenCount(), POLL_INTERVAL_MS);
      }
    };
    document.addEventListener('visibilitychange', this.visibilityHandler);

    // Clean up on destroy
    this.destroyRef.onDestroy(() => this.stopPolling());
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
    try {
      const headers: Record<string, string> = {};

      if (isDevMode()) {
        const mockHeader = this.mockAuth.getMockHeader();
        if (mockHeader) {
          headers['X-Mock-User'] = mockHeader;
        }
      }

      const profileId = this.profileService.activeProfileId();
      if (profileId) {
        headers['X-Profile-Id'] = profileId;
      }

      const response = await fetch('/api/notifications/count', {
        headers,
        credentials: 'include',
      });

      if (response.ok) {
        const data = await response.json();
        this._unseenCount.set(data.count);
      }
      // Silently ignore errors — non-critical polling
    } catch {
      // Network error — silently ignore, will retry on next interval
    }
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
