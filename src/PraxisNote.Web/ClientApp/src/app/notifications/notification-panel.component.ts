import { Component, inject, input, output, signal, ChangeDetectionStrategy, effect } from '@angular/core';
import { DatePipe } from '@angular/common';
import { NotificationService } from './notification.service';

@Component({
  selector: 'app-notification-panel',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe],
  template: `
    @if (visible()) {
      <div class="fixed inset-0 z-50">
        <!-- Overlay -->
        <div
          class="absolute inset-0 bg-gray-900/60"
          (click)="close()"
          aria-hidden="true"
        ></div>

        <!-- Panel (slides from right) -->
        <aside
          class="absolute inset-y-0 right-0 w-96 max-w-full bg-surface shadow-2xl flex flex-col animate-slide-in-right"
          role="dialog"
          aria-label="Notifications"
        >
          <!-- Header -->
          <div class="h-14 flex items-center justify-between px-4 border-b border-border">
            <h2 class="font-semibold text-foreground">What's New</h2>
            <button
              class="w-8 h-8 flex items-center justify-center hover:bg-surface-muted rounded-lg"
              (click)="close()"
              aria-label="Close notifications"
            >
              <i class="pi pi-times text-foreground-secondary" aria-hidden="true"></i>
            </button>
          </div>

          <!-- Tabs -->
          <div class="flex border-b border-border">
            <button
              class="flex-1 px-4 py-3 text-sm font-medium transition-colors"
              [class.text-accent-foreground]="activeTab() === 'new'"
              [class.border-b-2]="activeTab() === 'new'"
              [class.border-accent-foreground]="activeTab() === 'new'"
              [class.text-foreground-muted]="activeTab() !== 'new'"
              (click)="setTab('new')"
            >
              New
              @if (notificationService.newNotifications().length > 0) {
                <span class="ml-2 px-2 py-0.5 text-xs rounded-full bg-accent text-accent-foreground">
                  {{ notificationService.newNotifications().length }}
                </span>
              }
            </button>
            <button
              class="flex-1 px-4 py-3 text-sm font-medium transition-colors"
              [class.text-accent-foreground]="activeTab() === 'history'"
              [class.border-b-2]="activeTab() === 'history'"
              [class.border-accent-foreground]="activeTab() === 'history'"
              [class.text-foreground-muted]="activeTab() !== 'history'"
              (click)="setTab('history')"
            >
              History
            </button>
          </div>

          <!-- Content -->
          <div class="flex-1 overflow-y-auto">
            @if (notificationService.loading()) {
              <div class="flex items-center justify-center h-32">
                <i class="pi pi-spin pi-spinner text-2xl text-foreground-muted"></i>
              </div>
            } @else {
              @let items = activeTab() === 'new'
                ? notificationService.newNotifications()
                : notificationService.historyNotifications();

              @if (items.length === 0) {
                <div class="flex flex-col items-center justify-center h-32 text-foreground-muted">
                  <i class="pi pi-inbox text-3xl mb-2"></i>
                  <p class="text-sm">
                    {{ activeTab() === 'new' ? 'All caught up!' : 'No history yet' }}
                  </p>
                </div>
              } @else {
                <div class="divide-y divide-border">
                  @for (notification of items; track notification.id) {
                    <div class="p-4 hover:bg-surface-muted/50 transition-colors">
                      <div class="flex items-start gap-3">
                        <span
                          class="px-2 py-0.5 text-xs font-medium rounded-full whitespace-nowrap"
                          [class]="getTypeColor(notification.type)"
                        >
                          {{ getTypeLabel(notification.type) }}
                        </span>
                        <div class="flex-1 min-w-0">
                          <h3 class="font-medium text-foreground text-sm">
                            {{ notification.title }}
                          </h3>
                          <p class="text-sm text-foreground-secondary mt-1">
                            {{ notification.summary }}
                          </p>
                          <div class="flex items-center gap-3 mt-2">
                            <span class="text-xs text-foreground-muted">
                              {{ notification.createdAt | date:'MMM d, yyyy' }}
                            </span>
                            @if (notification.issueUrl) {
                              <a
                                [href]="notification.issueUrl"
                                target="_blank"
                                rel="noopener noreferrer"
                                class="text-xs text-accent-foreground hover:underline"
                              >
                                View details
                              </a>
                            }
                          </div>
                        </div>
                      </div>
                    </div>
                  }
                </div>
              }
            }
          </div>
        </aside>
      </div>
    }
  `,
})
export class NotificationPanelComponent {
  protected readonly notificationService = inject(NotificationService);

  readonly visible = input(false);
  readonly visibleChange = output<boolean>();

  protected readonly activeTab = signal<'new' | 'history'>('new');

  constructor() {
    // Load notifications and auto-mark as seen when panel opens
    effect(() => {
      if (this.visible()) {
        this.notificationService.loadNotifications();
        // Auto-mark as seen after a brief delay
        setTimeout(() => this.notificationService.markAllAsSeen(), 1000);
      }
    });
  }

  protected close(): void {
    this.visibleChange.emit(false);
  }

  protected setTab(tab: 'new' | 'history'): void {
    this.activeTab.set(tab);
  }

  protected getTypeColor(type: string): string {
    switch (type) {
      case 'Feature': return 'bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-200';
      case 'BugFix': return 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200';
      case 'Improvement': return 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200';
      default: return 'bg-gray-100 text-gray-800';
    }
  }

  protected getTypeLabel(type: string): string {
    switch (type) {
      case 'BugFix': return 'Bug Fix';
      default: return type;
    }
  }
}
