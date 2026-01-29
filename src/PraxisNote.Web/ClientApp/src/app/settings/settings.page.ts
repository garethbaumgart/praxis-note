import { Component, ChangeDetectionStrategy, inject, OnInit, effect } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { DatePipe } from '@angular/common';
import { Button } from 'primeng/button';
import { CalendarService } from './calendar.service';
import { ToastService } from '../shared/services/toast.service';

@Component({
  selector: 'app-settings-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, DatePipe],
  template: `
    <div class="max-w-3xl mx-auto px-4 md:px-6 py-6 md:py-8">
      <!-- Header -->
      <div class="mb-8">
        <h1 class="text-2xl font-bold text-foreground">Settings</h1>
        <p class="text-foreground-secondary mt-1">Manage your integrations and preferences.</p>
      </div>

      <!-- Calendar Integration Section -->
      <section class="bg-surface border border-border rounded-xl p-6">
        <div class="flex items-center gap-3 mb-4">
          <div class="w-10 h-10 rounded-lg bg-surface-muted flex items-center justify-center">
            <i class="pi pi-calendar text-lg text-foreground-secondary" aria-hidden="true"></i>
          </div>
          <div>
            <h2 class="text-lg font-semibold text-foreground">Calendar Integration</h2>
            <p class="text-sm text-foreground-secondary">Import meetings from your calendar.</p>
          </div>
        </div>

        @if (calendarService.loading()) {
          <!-- Loading state -->
          <div class="flex items-center gap-3 py-4">
            <i class="pi pi-spin pi-spinner text-foreground-muted" aria-hidden="true"></i>
            <span class="text-sm text-foreground-muted">Loading connection status...</span>
          </div>
        } @else if (calendarService.status()?.isConnected) {
          <!-- Connected state -->
          <div class="space-y-4">
            <div class="flex items-center gap-2 py-3 px-4 bg-done/30 border border-done rounded-lg">
              <i class="pi pi-check-circle text-done-foreground" aria-hidden="true"></i>
              <span class="text-sm font-medium text-done-foreground">Connected to Google Calendar</span>
            </div>

            <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 text-sm">
              @if (calendarService.status()?.connectedAt) {
                <div>
                  <span class="text-foreground-muted">Connected since</span>
                  <p class="font-medium text-foreground">{{ calendarService.status()!.connectedAt | date:'mediumDate' }}</p>
                </div>
              }
              @if (calendarService.status()?.lastSyncedAt) {
                <div>
                  <span class="text-foreground-muted">Last synced</span>
                  <p class="font-medium text-foreground">{{ calendarService.status()!.lastSyncedAt | date:'medium' }}</p>
                </div>
              }
            </div>

            @if (calendarService.error()) {
              <div class="py-2 px-4 bg-danger/10 border border-danger/30 rounded-lg">
                <p class="text-sm text-danger">{{ calendarService.error() }}</p>
              </div>
            }

            @if (calendarService.lastSyncResult()) {
              <div class="py-2 px-4 bg-done/20 border border-done/30 rounded-lg">
                <p class="text-sm text-foreground">
                  Imported {{ calendarService.lastSyncResult()!.importedCount }} new meeting{{ calendarService.lastSyncResult()!.importedCount !== 1 ? 's' : '' }},
                  {{ calendarService.lastSyncResult()!.skippedCount }} already existed.
                </p>
              </div>
            }

            <div class="flex items-center gap-3 pt-2">
              <p-button
                label="Sync Now"
                icon="pi pi-sync"
                [loading]="calendarService.syncing()"
                (onClick)="syncCalendar()"
                severity="secondary"
                size="small"
              />
              <p-button
                label="Disconnect"
                icon="pi pi-times"
                (onClick)="disconnectCalendar()"
                severity="danger"
                [outlined]="true"
                size="small"
              />
            </div>
          </div>
        } @else {
          <!-- Disconnected state -->
          <div class="space-y-4">
            <p class="text-sm text-foreground-secondary">
              Connect your Google Calendar to automatically import upcoming meetings.
              Only event titles, times, and attendees are imported — no calendar data is stored beyond what appears in your meetings list.
            </p>

            @if (calendarService.error()) {
              <div class="py-2 px-4 bg-danger/10 border border-danger/30 rounded-lg">
                <p class="text-sm text-danger">{{ calendarService.error() }}</p>
              </div>
            }

            <p-button
              label="Connect Google Calendar"
              icon="pi pi-google"
              (onClick)="connectGoogleCalendar()"
            />
          </div>
        }
      </section>
    </div>
  `,
})
export class SettingsPage implements OnInit {
  readonly calendarService = inject(CalendarService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);

  constructor() {
    // Show toast when sync completes successfully
    effect(() => {
      const result = this.calendarService.lastSyncResult();
      if (result) {
        this.toast.success({ summary: 'Calendar synced!' });
      }
    });
  }

  ngOnInit(): void {
    this.calendarService.loadConnectionStatus();

    // Check for OAuth redirect success
    const params = this.route.snapshot.queryParams;
    if (params['connected'] === 'true') {
      this.toast.success({ summary: 'Google Calendar connected successfully!' });
    }
    if (params['error']) {
      const errorMessages: Record<string, string> = {
        auth_denied: 'Calendar access was denied. Please try again.',
        no_code: 'Authorization failed. Please try again.',
        not_authenticated: 'Please log in first, then connect your calendar.',
        token_exchange_failed: 'Failed to connect. Please try again.',
        no_refresh_token: 'Could not get full access. Please revoke PraxisNote access in your Google account settings and try again.',
      };
      this.toast.error(
        errorMessages[params['error']] ?? 'An error occurred connecting your calendar.',
      );
    }
  }

  connectGoogleCalendar(): void {
    this.calendarService.connectGoogleCalendar();
  }

  syncCalendar(): void {
    this.calendarService.syncEvents();
  }

  disconnectCalendar(): void {
    this.calendarService.disconnectCalendar();
  }
}
