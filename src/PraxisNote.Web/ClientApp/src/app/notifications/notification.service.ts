import { Injectable, inject, signal, computed, NgZone } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Notification } from './notification.model';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly http = inject(HttpClient);
  private readonly ngZone = inject(NgZone);

  private eventSource: EventSource | null = null;
  private reconnectTimer: ReturnType<typeof setTimeout> | null = null;
  private reconnectAttempts = 0;
  private readonly maxReconnectDelay = 60000; // 1 minute max

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

  connectSse(): void {
    if (this.eventSource) return;

    this.ngZone.runOutsideAngular(() => {
      this.eventSource = new EventSource('/api/notifications/stream', {
        withCredentials: true
      });

      this.eventSource.addEventListener('count', (event: MessageEvent) => {
        this.ngZone.run(() => {
          try {
            const data = JSON.parse(event.data);
            this._unseenCount.set(data.count);
            this.reconnectAttempts = 0; // Reset on successful message
          } catch {
            // Ignore malformed JSON
          }
        });
      });

      this.eventSource.addEventListener('new', (event: MessageEvent) => {
        this.ngZone.run(() => {
          try {
            const notification = JSON.parse(event.data) as Notification;
            this._notifications.update(list => [notification, ...list]);
            this._unseenCount.update(c => c + 1);
            this.reconnectAttempts = 0; // Reset on successful message
          } catch {
            // Ignore malformed JSON
          }
        });
      });

      this.eventSource.onerror = () => {
        this.disconnectSse();
        // Exponential backoff: 1s, 2s, 4s, 8s, ... up to maxReconnectDelay
        const delay = Math.min(1000 * Math.pow(2, this.reconnectAttempts), this.maxReconnectDelay);
        this.reconnectAttempts++;
        this.reconnectTimer = setTimeout(() => this.connectSse(), delay);
      };
    });
  }

  disconnectSse(): void {
    if (this.reconnectTimer) {
      clearTimeout(this.reconnectTimer);
      this.reconnectTimer = null;
    }
    if (this.eventSource) {
      this.eventSource.close();
      this.eventSource = null;
    }
  }

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

  loadUnseenCount(): void {
    this.http.get<{ count: number }>('/api/notifications/count').subscribe({
      next: (result) => this._unseenCount.set(result.count),
    });
  }

  markAllAsSeen(): void {
    const unseenIds = this._notifications()
      .filter(n => !n.isSeen)
      .map(n => n.id);

    if (unseenIds.length === 0) return;

    // Optimistic update
    this._notifications.update(list =>
      list.map(n => unseenIds.includes(n.id) ? { ...n, isSeen: true } : n)
    );
    this._unseenCount.set(0);

    this.http.post('/api/notifications/seen', { notificationIds: unseenIds }).subscribe({
      error: () => this.loadNotifications(),
    });
  }
}
