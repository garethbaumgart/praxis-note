import { Component, ChangeDetectionStrategy, inject, OnInit, OnDestroy, effect, untracked, signal, computed, viewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { DatePipe } from '@angular/common';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { CalendarService } from '../shared/services/calendar.service';
import { ToastService } from '../shared/services/toast.service';
import { ContextualHeaderService } from '../shared/services/contextual-header.service';
import { PageContentComponent } from '../shared/components/page-content.component';
import { ProfileService } from '../profiles/profile.service';
import { Profile } from '../profiles/profile.model';
import { ProfileCardComponent } from './profile-card.component';
import { CreateProfileDialogComponent } from './create-profile-dialog.component';
import { LinkedAccountsService } from './linked-accounts.service';
import { LinkAccountPanelComponent } from './link-account-panel.component';

const MAX_PROFILES = 5;

@Component({
  selector: 'app-settings-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, DatePipe, Dialog, PageContentComponent, ProfileCardComponent, CreateProfileDialogComponent, LinkAccountPanelComponent],
  template: `
    <app-page-content maxWidth="narrow">
      <h1 class="sr-only">Settings</h1>

      <div class="space-y-6">
        <!-- Profiles Section -->
        <section class="bg-surface border border-border rounded-xl p-6">
          <div class="flex items-center gap-3 mb-4">
            <div class="w-10 h-10 rounded-lg bg-surface-muted flex items-center justify-center">
              <i class="pi pi-users text-lg text-foreground-secondary" aria-hidden="true"></i>
            </div>
            <div>
              <h2 class="text-lg font-semibold text-foreground">Profiles</h2>
              <p class="text-sm text-foreground-secondary">Organize your data across separate contexts like Work and Personal.</p>
            </div>
          </div>

          @if (profileService.loading()) {
            <div class="flex items-center gap-3 py-4" role="status" aria-label="Loading profiles">
              <i class="pi pi-spin pi-spinner text-sm text-foreground-muted" aria-hidden="true"></i>
              <span class="text-sm text-foreground-muted" aria-hidden="true">Loading profiles...</span>
              <span class="sr-only">Loading profiles...</span>
            </div>
          } @else {
            <div class="space-y-2 mb-4">
              @for (profile of profileService.profiles(); track profile.id) {
                <app-profile-card
                  [profile]="profile"
                  [isActive]="profile.id === profileService.activeProfileId()"
                  (onEdit)="openEditDialog($event)"
                  (onDelete)="startDeleteProfile($event)"
                />
              }
            </div>

            <!-- New Profile button -->
            <button
              type="button"
              class="w-full flex items-center justify-center gap-2 py-2.5 border border-dashed border-border rounded-lg text-sm text-foreground-muted hover:text-foreground hover:border-foreground-muted transition-colors"
              [class.opacity-50]="atMaxProfiles()"
              [class.cursor-not-allowed]="atMaxProfiles()"
              [disabled]="atMaxProfiles()"
              (click)="openCreateDialog()"
              [attr.aria-label]="atMaxProfiles() ? 'Maximum of ' + maxProfiles + ' profiles reached' : 'Create new profile'"
            >
              <i class="pi pi-plus text-xs" aria-hidden="true"></i>
              <span>{{ atMaxProfiles() ? 'Maximum ' + maxProfiles + ' profiles' : 'New Profile' }}</span>
            </button>
          }
        </section>

        <!-- Linked Accounts Section -->
        <section class="bg-surface border border-border rounded-xl p-6">
          <div class="flex items-center gap-3 mb-4">
            <div class="w-10 h-10 rounded-lg bg-surface-muted flex items-center justify-center">
              <i class="pi pi-link text-lg text-foreground-secondary" aria-hidden="true"></i>
            </div>
            <div>
              <h2 class="text-lg font-semibold text-foreground">Linked Accounts</h2>
              <p class="text-sm text-foreground-secondary">Sign in from any of your linked accounts to access this data.</p>
            </div>
          </div>

          @if (linkedAccountsService.loading()) {
            <div class="flex items-center gap-3 py-4" role="status" aria-label="Loading linked accounts">
              <i class="pi pi-spin pi-spinner text-sm text-foreground-muted" aria-hidden="true"></i>
              <span class="text-sm text-foreground-muted" aria-hidden="true">Loading linked accounts...</span>
              <span class="sr-only">Loading linked accounts...</span>
            </div>
          } @else {
            <div class="space-y-2 mb-4">
              @for (identity of linkedAccountsService.identities(); track identity.id) {
                <div class="flex items-center gap-3 px-4 py-3 rounded-lg border border-border bg-surface">
                  <!-- Provider icon -->
                  <div class="w-8 h-8 rounded-full bg-surface-muted flex items-center justify-center shrink-0">
                    <i class="pi pi-google text-foreground-secondary text-sm" aria-hidden="true"></i>
                  </div>

                  <!-- Account info -->
                  <div class="flex-1 min-w-0">
                    <p class="text-sm font-medium text-foreground truncate">{{ identity.email }}</p>
                    @if (identity.defaultProfileId) {
                      <p class="text-xs text-foreground-muted">Default: {{ getProfileName(identity.defaultProfileId) }}</p>
                    }
                  </div>

                  <!-- Primary badge or Unlink button -->
                  @if (linkedAccountsService.identities().length <= 1) {
                    <span class="text-[10px] font-semibold text-accent-foreground bg-accent px-1.5 py-0.5 rounded">Primary</span>
                  } @else {
                    <button
                      type="button"
                      class="px-3 py-1 text-xs text-danger hover:text-white hover:bg-danger border border-danger rounded transition-colors"
                      (click)="startUnlinkIdentity(identity.id, identity.email)"
                      [attr.aria-label]="'Unlink ' + identity.email"
                    >
                      Unlink
                    </button>
                  }
                </div>
              }
            </div>

            <!-- Link Another Account -->
            @if (!showLinkPanel()) {
              <button
                type="button"
                class="w-full flex items-center justify-center gap-2 py-2.5 border border-dashed border-border rounded-lg text-sm text-foreground-muted hover:text-foreground hover:border-foreground-muted transition-colors"
                (click)="showLinkPanel.set(true)"
                aria-label="Link another account"
              >
                <i class="pi pi-plus text-xs" aria-hidden="true"></i>
                <span>Link Another Account</span>
              </button>
            } @else {
              <app-link-account-panel
                [profiles]="profileService.profiles()"
                (onClose)="showLinkPanel.set(false)"
                (onLinked)="onAccountLinked()"
              />
            }
          }
        </section>

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
            <div class="flex items-center gap-3 py-4" role="status" aria-label="Loading calendar connection status">
              <i class="pi pi-spin pi-spinner text-sm text-foreground-muted" aria-hidden="true"></i>
              <span class="text-sm text-foreground-muted" aria-hidden="true">Loading connection status...</span>
              <span class="sr-only">Loading calendar connection status...</span>
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
    </app-page-content>

    <!-- Create/Edit Profile Dialog -->
    <app-create-profile-dialog
      (onSave)="saveProfile($event)"
    />

    <!-- Delete Profile Confirmation Dialog -->
    <p-dialog
      [visible]="showDeleteDialog()"
      (visibleChange)="showDeleteDialog.set($event)"
      [modal]="true"
      [draggable]="false"
      [resizable]="false"
      [dismissableMask]="true"
      [closable]="true"
      [style]="{ width: '24rem' }"
      [breakpoints]="{ '640px': '95vw' }"
      header="Delete Profile"
    >
      <p class="text-sm text-foreground-secondary">
        Are you sure you want to delete <strong class="text-foreground">{{ profileToDelete()?.name }}</strong>?
        This action cannot be undone.
      </p>
      @if (deleteError()) {
        <div class="mt-3 py-2 px-4 bg-danger/10 border border-danger/30 rounded-lg">
          <p class="text-sm text-danger">{{ deleteError() }}</p>
        </div>
      }
      <div class="flex justify-end gap-3 px-5 py-4 border-t border-border mt-4 -mx-5 -mb-5">
        <button
          type="button"
          class="px-4 py-1.5 text-sm text-foreground-secondary hover:text-foreground transition-colors"
          (click)="showDeleteDialog.set(false)"
        >
          Cancel
        </button>
        <button
          type="button"
          class="px-4 py-2 text-sm bg-danger text-white rounded-lg font-medium hover:opacity-90 transition"
          (click)="confirmDeleteProfile()"
        >
          Delete
        </button>
      </div>
    </p-dialog>

    <!-- Unlink Account Confirmation Dialog -->
    <p-dialog
      [visible]="showUnlinkDialog()"
      (visibleChange)="showUnlinkDialog.set($event)"
      [modal]="true"
      [draggable]="false"
      [resizable]="false"
      [dismissableMask]="true"
      [closable]="true"
      [style]="{ width: '24rem' }"
      [breakpoints]="{ '640px': '95vw' }"
      header="Unlink Account"
    >
      <p class="text-sm text-foreground-secondary">
        Are you sure you want to unlink <strong class="text-foreground">{{ unlinkEmail() }}</strong>?
        You will no longer be able to sign in with this account.
      </p>
      <div class="flex justify-end gap-3 px-5 py-4 border-t border-border mt-4 -mx-5 -mb-5">
        <button
          type="button"
          class="px-4 py-1.5 text-sm text-foreground-secondary hover:text-foreground transition-colors"
          (click)="showUnlinkDialog.set(false)"
        >
          Cancel
        </button>
        <button
          type="button"
          class="px-4 py-2 text-sm bg-danger text-white rounded-lg font-medium hover:opacity-90 transition"
          (click)="confirmUnlinkIdentity()"
        >
          Unlink
        </button>
      </div>
    </p-dialog>
  `,
})
export class SettingsPage implements OnInit, OnDestroy {
  readonly calendarService = inject(CalendarService);
  readonly profileService = inject(ProfileService);
  readonly linkedAccountsService = inject(LinkedAccountsService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly headerService = inject(ContextualHeaderService);

  private readonly profileDialog = viewChild(CreateProfileDialogComponent);

  readonly maxProfiles = MAX_PROFILES;
  readonly atMaxProfiles = computed(() => this.profileService.profiles().length >= MAX_PROFILES);

  readonly showDeleteDialog = signal(false);
  readonly profileToDelete = signal<Profile | null>(null);
  readonly deleteError = signal<string | null>(null);

  readonly showLinkPanel = signal(false);

  readonly showUnlinkDialog = signal(false);
  readonly unlinkIdentityId = signal<string | null>(null);
  readonly unlinkEmail = signal('');

  constructor() {
    // Show toast when sync completes successfully
    effect(() => {
      const result = this.calendarService.lastSyncResult();
      if (result) {
        untracked(() => {
          const imported = result.importedCount;
          const skipped = result.skippedCount;
          const detail = imported > 0
            ? `Imported ${imported} meeting${imported !== 1 ? 's' : ''} for the next 7 days${skipped > 0 ? `, ${skipped} already existed` : ''}`
            : `No new meetings found${skipped > 0 ? ` (${skipped} already existed)` : ''}`;
          this.toast.success({ summary: 'Calendar synced', detail });
          this.calendarService.clearLastSyncResult();
        });
      }
    });

    // Show toast when calendar is disconnected successfully
    effect(() => {
      if (this.calendarService.lastDisconnected()) {
        untracked(() => {
          this.toast.success({ summary: 'Google Calendar disconnected.' });
          this.calendarService.acknowledgeDisconnected();
        });
      }
    });
  }

  ngOnInit(): void {
    this.headerService.breadcrumb.set([{ label: 'Settings' }]);
    this.calendarService.loadConnectionStatus();
    this.profileService.loadProfiles();
    this.linkedAccountsService.loadIdentities();

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

  ngOnDestroy(): void {
    this.headerService.clearContext();
  }

  // --- Profile actions ---

  openCreateDialog(): void {
    this.profileDialog()?.open();
  }

  openEditDialog(profile: Profile): void {
    this.profileDialog()?.open({ id: profile.id, name: profile.name, icon: profile.icon });
  }

  saveProfile(event: { name: string; icon: string | null; editId?: string }): void {
    if (event.editId) {
      this.profileService.updateProfile(event.editId, event.name, event.icon);
    } else {
      this.profileService.createProfile(event.name, event.icon);
    }
  }

  startDeleteProfile(profile: Profile): void {
    this.profileToDelete.set(profile);
    this.deleteError.set(null);
    this.showDeleteDialog.set(true);
  }

  confirmDeleteProfile(): void {
    const profile = this.profileToDelete();
    if (!profile) return;

    this.deleteError.set(null);
    this.profileService.deleteProfile(
      profile.id,
      () => {
        this.showDeleteDialog.set(false);
        this.profileToDelete.set(null);
      },
      (message) => {
        this.deleteError.set(message);
      },
    );
  }

  // --- Linked Accounts actions ---

  getProfileName(profileId: string): string {
    return this.profileService.profiles().find(p => p.id === profileId)?.name ?? 'Unknown';
  }

  startUnlinkIdentity(identityId: string, email: string): void {
    this.unlinkIdentityId.set(identityId);
    this.unlinkEmail.set(email);
    this.showUnlinkDialog.set(true);
  }

  confirmUnlinkIdentity(): void {
    const id = this.unlinkIdentityId();
    if (!id) return;
    this.linkedAccountsService.unlinkIdentity(id);
    this.showUnlinkDialog.set(false);
  }

  onAccountLinked(): void {
    this.showLinkPanel.set(false);
    this.linkedAccountsService.loadIdentities();
    this.toast.success({ summary: 'Account linked successfully' });
  }

  // --- Calendar actions ---

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
