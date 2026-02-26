import { Component, ChangeDetectionStrategy, inject, OnInit, OnDestroy, effect, untracked, signal, computed, viewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { DatePipe } from '@angular/common';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { CalendarService } from '../shared/services/calendar.service';
import { DriveService } from '../shared/services/drive.service';
import { JiraService } from '../shared/services/jira.service';
import { ToastService } from '../shared/services/toast.service';
import { ContextualHeaderService } from '../shared/services/contextual-header.service';
import { PageContentComponent } from '../shared/components/page-content.component';
import { ProfileService } from '../profiles/profile.service';
import { Profile } from '../profiles/profile.model';
import { ProfileCardComponent } from './profile-card.component';
import { CreateProfileDialogComponent } from './create-profile-dialog.component';
import { LinkedAccountsService } from './linked-accounts.service';
import { LinkedAccountCardComponent } from './linked-account-card.component';
import { LinkAccountPanelComponent } from './link-account-panel.component';
import { ApiKeyService } from './api-key.service';
import { ApiKeyDto } from './api-key.model';
import { ApiKeyCardComponent } from './api-key-card.component';
import { DriveSetupDialogComponent } from './drive-setup-dialog.component';

const MAX_PROFILES = 5;
const MAX_API_KEYS = 5;

@Component({
  selector: 'app-settings-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, DatePipe, Dialog, PageContentComponent, ProfileCardComponent, CreateProfileDialogComponent, LinkedAccountCardComponent, LinkAccountPanelComponent, ApiKeyCardComponent, DriveSetupDialogComponent],
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
            <div class="flex flex-col gap-3 mb-4">
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
            <div class="flex flex-col gap-3 mb-4">
              @for (identity of linkedAccountsService.identities(); track identity.id) {
                <app-linked-account-card
                  [identity]="identity"
                  [profiles]="profileService.profiles()"
                  [canUnlink]="linkedAccountsService.identities().length > 1"
                  (onUnlink)="startUnlinkIdentity($event.id, $event.email)"
                />
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

        <!-- Google Drive Integration Section -->
        <section class="bg-surface border border-border rounded-xl p-6">
          <div class="flex items-center gap-3 mb-4">
            <div class="w-10 h-10 rounded-lg bg-surface-muted flex items-center justify-center">
              <i class="pi pi-folder text-lg text-foreground-secondary" aria-hidden="true"></i>
            </div>
            <div>
              <h2 class="text-lg font-semibold text-foreground">Google Drive</h2>
              <p class="text-sm text-foreground-secondary">Import meeting notes and documents from Google Drive.</p>
            </div>
          </div>

          @if (driveService.loading()) {
            <div class="flex items-center gap-3 py-4" role="status" aria-label="Loading Drive connection status">
              <i class="pi pi-spin pi-spinner text-sm text-foreground-muted" aria-hidden="true"></i>
              <span class="text-sm text-foreground-muted" aria-hidden="true">Loading connection status...</span>
              <span class="sr-only">Loading Drive connection status...</span>
            </div>
          } @else if (driveService.status()?.isConnected) {
            <div class="space-y-4">
              <div class="flex items-center gap-2 py-3 px-4 bg-done/30 border border-done rounded-lg">
                <i class="pi pi-check-circle text-done-foreground" aria-hidden="true"></i>
                <span class="text-sm font-medium text-done-foreground">Connected to Google Drive</span>
              </div>

              @if (driveService.status()?.isConfigured) {
                <!-- Configured state: show folder, sync frequency, last sync -->
                <div class="grid grid-cols-1 sm:grid-cols-2 gap-4 text-sm">
                  @if (driveService.status()?.folderName) {
                    <div>
                      <span class="text-foreground-muted">Linked folder</span>
                      <p class="font-medium text-foreground">{{ driveService.status()!.folderName }}</p>
                    </div>
                  }
                  <div>
                    <span class="text-foreground-muted">Sync frequency</span>
                    <p class="font-medium text-foreground">{{ formatSyncFrequency(driveService.status()?.syncFrequencyMinutes) }}</p>
                  </div>
                  @if (driveService.status()?.lastSyncedAt) {
                    <div>
                      <span class="text-foreground-muted">Last synced</span>
                      <p class="font-medium text-foreground">{{ driveService.status()!.lastSyncedAt | date:'medium' }}</p>
                    </div>
                  }
                </div>
              } @else {
                <!-- Not yet configured: prompt to set up -->
                <p class="text-sm text-foreground-secondary">
                  Select a folder and configure import settings to get started.
                </p>
              }

              @if (driveService.error()) {
                <div class="py-2 px-4 bg-danger/10 border border-danger/30 rounded-lg">
                  <p class="text-sm text-danger">{{ driveService.error() }}</p>
                </div>
              }

              <div class="flex items-center gap-3 pt-2">
                <p-button
                  [label]="driveService.status()?.isConfigured ? 'Configure' : 'Set Up Folder'"
                  [icon]="driveService.status()?.isConfigured ? 'pi pi-cog' : 'pi pi-folder'"
                  (onClick)="openDriveSetup()"
                  severity="secondary"
                  size="small"
                />
                <p-button
                  label="Disconnect"
                  icon="pi pi-times"
                  (onClick)="disconnectDrive()"
                  severity="danger"
                  [outlined]="true"
                  size="small"
                />
              </div>
            </div>
          } @else {
            <div class="space-y-4">
              <p class="text-sm text-foreground-secondary">
                Connect your Google Drive to import meeting notes and documents.
                Only read-only access is requested — PraxisNote will never modify your Drive files.
              </p>

              @if (driveService.error()) {
                <div class="py-2 px-4 bg-danger/10 border border-danger/30 rounded-lg">
                  <p class="text-sm text-danger">{{ driveService.error() }}</p>
                </div>
              }

              <p-button
                label="Connect Google Drive"
                icon="pi pi-google"
                (onClick)="connectGoogleDrive()"
              />
            </div>
          }
        </section>

        <!-- Jira Integration Section -->
        <section class="bg-surface border border-border rounded-xl p-6">
          <div class="flex items-center gap-3 mb-4">
            <div class="w-10 h-10 rounded-lg bg-surface-muted flex items-center justify-center">
              <i class="pi pi-external-link text-lg text-foreground-secondary" aria-hidden="true"></i>
            </div>
            <div>
              <h2 class="text-lg font-semibold text-foreground">Jira Integration</h2>
              <p class="text-sm text-foreground-secondary">Link Jira issues in your notes as rich inline chips.</p>
            </div>
          </div>

          @if (jiraService.loading()) {
            <div class="flex items-center gap-3 py-4" role="status" aria-label="Loading Jira connection status">
              <i class="pi pi-spin pi-spinner text-sm text-foreground-muted" aria-hidden="true"></i>
              <span class="text-sm text-foreground-muted" aria-hidden="true">Loading connection status...</span>
              <span class="sr-only">Loading Jira connection status...</span>
            </div>
          } @else if (jiraService.status()?.isConnected) {
            <div class="space-y-4">
              <div class="flex items-center gap-2 py-3 px-4 bg-done/30 border border-done rounded-lg">
                <i class="pi pi-check-circle text-done-foreground" aria-hidden="true"></i>
                <span class="text-sm font-medium text-done-foreground">Connected to Jira</span>
              </div>

              <div class="text-sm">
                @if (jiraService.status()?.siteUrl) {
                  <div>
                    <span class="text-foreground-muted">Site</span>
                    <p class="font-medium text-foreground">{{ jiraService.status()!.siteUrl }}</p>
                  </div>
                }
                @if (jiraService.status()?.connectedAt) {
                  <div class="mt-2">
                    <span class="text-foreground-muted">Connected since</span>
                    <p class="font-medium text-foreground">{{ jiraService.status()!.connectedAt | date:'mediumDate' }}</p>
                  </div>
                }
              </div>

              @if (jiraService.error()) {
                <div class="py-2 px-4 bg-danger/10 border border-danger/30 rounded-lg">
                  <p class="text-sm text-danger">{{ jiraService.error() }}</p>
                </div>
              }

              <div class="flex items-center gap-3 pt-2">
                <p-button
                  label="Disconnect"
                  icon="pi pi-times"
                  (onClick)="disconnectJira()"
                  severity="danger"
                  [outlined]="true"
                  size="small"
                />
              </div>
            </div>
          } @else {
            <div class="space-y-4">
              <p class="text-sm text-foreground-secondary">
                Connect your Jira Cloud account to paste issue URLs in notes and see them as rich inline chips with status, type, and summary.
              </p>

              @if (jiraService.error()) {
                <div class="py-2 px-4 bg-danger/10 border border-danger/30 rounded-lg">
                  <p class="text-sm text-danger">{{ jiraService.error() }}</p>
                </div>
              }

              <p-button
                label="Connect Jira"
                icon="pi pi-external-link"
                (onClick)="connectJira()"
              />
            </div>
          }
        </section>

        <!-- API Keys Section -->
        <section class="bg-surface border border-border rounded-xl p-6">
          <div class="flex items-center gap-3 mb-2">
            <div class="w-10 h-10 rounded-lg bg-surface-muted flex items-center justify-center">
              <i class="pi pi-key text-lg text-foreground-secondary" aria-hidden="true"></i>
            </div>
            <div>
              <h2 class="text-lg font-semibold text-foreground">API Keys</h2>
              <p class="text-sm text-foreground-secondary">Manage keys for the MCP server and API access.</p>
            </div>
          </div>

          <!-- MCP connection info -->
          <div class="mb-4 py-3 px-4 bg-surface-subtle border border-border rounded-lg">
            <p class="text-xs text-foreground-muted mb-1.5">MCP Server Endpoint</p>
            <div class="flex items-center gap-2">
              <code class="text-sm text-foreground font-mono flex-1 truncate">{{ mcpEndpointUrl() }}</code>
              <button
                type="button"
                class="p-1.5 text-foreground-muted hover:text-foreground transition-colors rounded"
                (click)="copyToClipboard(mcpEndpointUrl())"
                aria-label="Copy MCP endpoint URL"
              >
                <i class="pi pi-copy text-xs" aria-hidden="true"></i>
              </button>
            </div>
          </div>

          @if (apiKeyService.loading()) {
            <div class="flex items-center gap-3 py-4" role="status" aria-label="Loading API keys">
              <i class="pi pi-spin pi-spinner text-sm text-foreground-muted" aria-hidden="true"></i>
              <span class="text-sm text-foreground-muted" aria-hidden="true">Loading API keys...</span>
              <span class="sr-only">Loading API keys...</span>
            </div>
          } @else if (apiKeyService.error()) {
            <div class="py-2 px-4 bg-danger/10 border border-danger/30 rounded-lg mb-4">
              <p class="text-sm text-danger">{{ apiKeyService.error() }}</p>
              <button
                type="button"
                class="text-sm text-accent underline mt-1"
                (click)="apiKeyService.loadKeys()"
              >
                Try again
              </button>
            </div>
          } @else {
            @if (activeKeys().length > 0) {
              <div class="flex flex-col gap-2 mb-4">
                @for (key of activeKeys(); track key.id) {
                  <app-api-key-card
                    [apiKey]="key"
                    [profiles]="profileService.profiles()"
                    (onRevoke)="startRevokeKey(findKey($event))"
                  />
                }
              </div>
            } @else {
              <div class="flex flex-col items-center justify-center py-8 text-foreground-muted mb-4">
                <i class="pi pi-key text-2xl mb-2" aria-hidden="true"></i>
                <p class="text-sm">No API keys yet</p>
                <p class="text-xs mt-1">Create a key to connect MCP clients like Claude Code or Cursor.</p>
              </div>
            }

            <button
              type="button"
              class="w-full flex items-center justify-center gap-2 py-2.5 border border-dashed border-border rounded-lg text-sm text-foreground-muted hover:text-foreground hover:border-foreground-muted transition-colors"
              [class.opacity-50]="atMaxKeys()"
              [class.cursor-not-allowed]="atMaxKeys()"
              [disabled]="atMaxKeys()"
              (click)="openCreateKeyDialog()"
              [attr.aria-label]="atMaxKeys() ? 'Maximum of 5 API keys reached' : 'Create new API key'"
            >
              <i class="pi pi-plus text-xs" aria-hidden="true"></i>
              <span>{{ atMaxKeys() ? 'Maximum 5 keys' : 'New API Key' }}</span>
            </button>
          }
        </section>
      </div>
    </app-page-content>

    <app-drive-setup-dialog (onSaved)="onDriveSetupSaved()" />

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

    <!-- Create API Key Dialog -->
    <p-dialog
      [visible]="showCreateKeyDialog()"
      (visibleChange)="showCreateKeyDialog.set($event)"
      [modal]="true"
      [draggable]="false"
      [resizable]="false"
      [dismissableMask]="!newCreatedKey()"
      [closable]="!newCreatedKey()"
      [style]="{ width: '30rem' }"
      [breakpoints]="{ '640px': '95vw' }"
      [header]="newCreatedKey() ? 'API Key Created' : 'Create API Key'"
    >
      @if (newCreatedKey()) {
        <!-- Key created view -->
        <div class="space-y-4">
          <div class="py-3 px-4 bg-done/20 border border-done/30 rounded-lg">
            <div class="flex items-center gap-2 mb-1">
              <i class="pi pi-check-circle text-done-foreground text-sm" aria-hidden="true"></i>
              <span class="text-sm font-medium text-done-foreground">Key created successfully</span>
            </div>
            <p class="text-xs text-foreground-muted">Copy this key now. You will not be able to see it again.</p>
          </div>

          <div class="py-3 px-4 bg-surface-muted rounded-lg">
            <div class="flex items-center gap-2">
              <code class="text-sm text-foreground font-mono flex-1 break-all select-all">{{ newCreatedKey() }}</code>
              <button
                type="button"
                class="shrink-0 p-2 text-foreground-muted hover:text-foreground transition-colors rounded"
                (click)="copyKeyToClipboard(newCreatedKey()!)"
                aria-label="Copy API key"
              >
                <i class="pi pi-copy text-sm" aria-hidden="true"></i>
              </button>
            </div>
          </div>
        </div>

        <div class="flex justify-end px-5 py-4 border-t border-border mt-4 -mx-5 -mb-5">
          <button
            type="button"
            class="px-4 py-1.5 text-sm text-white bg-accent-solid hover:opacity-90 rounded-md transition-opacity"
            (click)="dismissNewKey()"
          >
            Done
          </button>
        </div>
      } @else {
        <!-- Create form -->
        <div class="space-y-4">
          <div>
            <label for="apiKeyName" class="block text-sm font-medium text-foreground mb-1">Key name</label>
            <input
              id="apiKeyName"
              type="text"
              class="w-full px-3 py-2 text-sm border border-border rounded-lg bg-surface text-foreground placeholder:text-foreground-muted focus:outline-none focus:ring-2 focus:ring-accent/50"
              placeholder="e.g. Claude Code, Cursor"
              [value]="newKeyName()"
              (input)="newKeyName.set($any($event.target).value)"
              (keydown.enter)="createApiKey()"
              maxlength="100"
            />
          </div>

          <div>
            <label for="apiKeyExpiry" class="block text-sm font-medium text-foreground mb-1">Expiry (optional)</label>
            <select
              id="apiKeyExpiry"
              class="w-full px-3 py-2 text-sm border border-border rounded-lg bg-surface text-foreground focus:outline-none focus:ring-2 focus:ring-accent/50"
              [value]="newKeyExpiry()"
              (change)="newKeyExpiry.set($any($event.target).value)"
            >
              <option value="">Never expires</option>
              <option value="30">30 days</option>
              <option value="60">60 days</option>
              <option value="90">90 days</option>
              <option value="365">1 year</option>
            </select>
          </div>
        </div>

        <div class="flex justify-end gap-3 px-5 py-4 border-t border-border mt-4 -mx-5 -mb-5">
          <button
            type="button"
            class="px-4 py-1.5 text-sm text-foreground-secondary hover:text-foreground transition-colors"
            [disabled]="apiKeyService.creating()"
            (click)="showCreateKeyDialog.set(false)"
          >
            Cancel
          </button>
          <button
            type="button"
            class="px-4 py-1.5 text-sm text-white bg-accent-solid hover:opacity-90 rounded-md transition-opacity disabled:opacity-50"
            [disabled]="!newKeyName().trim() || apiKeyService.creating()"
            (click)="createApiKey()"
          >
            @if (apiKeyService.creating()) {
              <i class="pi pi-spin pi-spinner text-xs mr-1" aria-hidden="true"></i>
            }
            Create
          </button>
        </div>
      }
    </p-dialog>

    <!-- Revoke API Key Confirmation Dialog -->
    <p-dialog
      [visible]="showRevokeKeyDialog()"
      (visibleChange)="showRevokeKeyDialog.set($event)"
      [modal]="true"
      [draggable]="false"
      [resizable]="false"
      [dismissableMask]="true"
      [closable]="true"
      [style]="{ width: '24rem' }"
      [breakpoints]="{ '640px': '95vw' }"
      header="Revoke API Key"
    >
      <p class="text-sm text-foreground-secondary">
        Are you sure you want to revoke <strong class="text-foreground">{{ keyToRevoke()?.name }}</strong>?
        Any MCP clients using this key will lose access immediately.
      </p>
      <div class="flex justify-end gap-3 px-5 py-4 border-t border-border mt-4 -mx-5 -mb-5">
        <button
          type="button"
          class="px-4 py-1.5 text-sm text-foreground-secondary hover:text-foreground transition-colors"
          (click)="showRevokeKeyDialog.set(false)"
        >
          Cancel
        </button>
        <button
          type="button"
          class="px-4 py-2 text-sm bg-danger text-white rounded-lg font-medium hover:opacity-90 transition"
          (click)="confirmRevokeKey()"
        >
          Revoke
        </button>
      </div>
    </p-dialog>
  `,
})
export class SettingsPage implements OnInit, OnDestroy {
  readonly calendarService = inject(CalendarService);
  readonly driveService = inject(DriveService);
  readonly jiraService = inject(JiraService);
  readonly profileService = inject(ProfileService);
  readonly linkedAccountsService = inject(LinkedAccountsService);
  readonly apiKeyService = inject(ApiKeyService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly headerService = inject(ContextualHeaderService);

  private readonly profileDialog = viewChild(CreateProfileDialogComponent);
  private readonly driveSetupDialog = viewChild(DriveSetupDialogComponent);

  readonly maxProfiles = MAX_PROFILES;
  readonly atMaxProfiles = computed(() => this.profileService.profiles().length >= MAX_PROFILES);

  readonly showDeleteDialog = signal(false);
  readonly profileToDelete = signal<Profile | null>(null);
  readonly deleteError = signal<string | null>(null);

  readonly showLinkPanel = signal(false);

  readonly showUnlinkDialog = signal(false);
  readonly unlinkIdentityId = signal<string | null>(null);
  readonly unlinkEmail = signal('');

  // --- API Key state ---
  readonly showCreateKeyDialog = signal(false);
  readonly showRevokeKeyDialog = signal(false);
  readonly keyToRevoke = signal<ApiKeyDto | null>(null);
  readonly newKeyName = signal('');
  readonly newKeyExpiry = signal('');
  readonly newCreatedKey = signal<string | null>(null);

  readonly activeKeys = computed(() => this.apiKeyService.keys().filter(k => !k.isRevoked));
  readonly atMaxKeys = computed(() => this.activeKeys().length >= MAX_API_KEYS);
  readonly mcpEndpointUrl = computed(() => `${window.location.origin}/mcp`);

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

    // Show toast when Drive is disconnected successfully
    effect(() => {
      if (this.driveService.lastDisconnected()) {
        untracked(() => {
          this.toast.success({ summary: 'Google Drive disconnected.' });
          this.driveService.acknowledgeDisconnected();
        });
      }
    });

    // Show toast when Jira is disconnected successfully
    effect(() => {
      if (this.jiraService.lastDisconnected()) {
        untracked(() => {
          this.toast.success({ summary: 'Jira disconnected.' });
          this.jiraService.acknowledgeDisconnected();
        });
      }
    });

    // Surface raw key from service to local signal for the create dialog
    effect(() => {
      const rawKey = this.apiKeyService.lastCreatedRawKey();
      if (rawKey) {
        untracked(() => {
          this.newCreatedKey.set(rawKey);
        });
      }
    });
  }

  ngOnInit(): void {
    this.headerService.breadcrumb.set([{ label: 'Settings' }]);
    this.calendarService.loadConnectionStatus();
    this.driveService.loadConnectionStatus();
    this.jiraService.loadConnectionStatus();
    this.profileService.loadProfiles();
    this.linkedAccountsService.loadIdentities();
    this.apiKeyService.loadKeys();

    // Check for OAuth redirect success
    const params = this.route.snapshot.queryParams;
    if (params['connected'] === 'true') {
      this.toast.success({ summary: 'Google Calendar connected successfully!' });
    }
    if (params['drive_connected'] === 'true') {
      this.toast.success({ summary: 'Google Drive connected successfully!' });
      // Auto-open folder picker after initial connection
      setTimeout(() => this.openDriveSetup(), 500);
    }
    if (params['jira_connected'] === 'true') {
      this.toast.success({ summary: 'Jira connected successfully!' });
    }
    if (params['error']) {
      const errorMessages: Record<string, string> = {
        auth_denied: 'Calendar access was denied. Please try again.',
        no_code: 'Authorization failed. Please try again.',
        not_authenticated: 'Please log in first, then connect your calendar.',
        token_exchange_failed: 'Failed to connect. Please try again.',
        no_refresh_token: 'Could not get full access. Please revoke PraxisNote access in your Google account settings and try again.',
        drive_auth_denied: 'Drive access was denied. Please try again.',
        drive_no_code: 'Drive authorization failed. Please try again.',
        drive_token_exchange_failed: 'Failed to connect to Drive. Please try again.',
        drive_no_refresh_token: 'Could not get full access to Drive. Please revoke PraxisNote access in your Google account settings and try again.',
        jira_auth_denied: 'Jira access was denied. Please try again.',
        jira_no_code: 'Jira authorization failed. Please try again.',
        jira_token_exchange_failed: 'Failed to connect to Jira. Please try again.',
        jira_no_resources: 'No accessible Jira sites found. Ensure you have access to a Jira Cloud instance.',
      };
      this.toast.error(
        errorMessages[params['error']] ?? 'An error occurred during connection.',
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

  // --- Drive actions ---

  connectGoogleDrive(): void {
    this.driveService.connectGoogleDrive();
  }

  disconnectDrive(): void {
    this.driveService.disconnectDrive();
  }

  openDriveSetup(): void {
    this.driveSetupDialog()?.open(this.driveService.status() ?? undefined);
  }

  onDriveSetupSaved(): void {
    this.driveService.loadConnectionStatus();
    this.toast.success({ summary: 'Drive import settings saved', detail: 'Initial import will start shortly.' });
  }

  formatSyncFrequency(minutes: number | null | undefined): string {
    if (minutes == null) return 'Not set';
    if (minutes === 0) return 'Manual only';
    if (minutes === 60) return 'Every hour';
    return `Every ${minutes} minutes`;
  }

  // --- Jira actions ---

  connectJira(): void {
    this.jiraService.connectJira();
  }

  disconnectJira(): void {
    this.jiraService.disconnectJira();
  }

  // --- API Key actions ---

  openCreateKeyDialog(): void {
    this.newKeyName.set('');
    this.newKeyExpiry.set('');
    this.newCreatedKey.set(null);
    this.apiKeyService.clearLastCreatedKey();
    this.showCreateKeyDialog.set(true);
  }

  createApiKey(): void {
    if (this.apiKeyService.creating()) return;
    const name = this.newKeyName().trim();
    if (!name) return;

    let expiresAt: string | undefined;
    const days = this.newKeyExpiry();
    if (days) {
      const date = new Date();
      date.setDate(date.getDate() + parseInt(days, 10));
      expiresAt = date.toISOString();
    }

    this.apiKeyService.createKey(name, expiresAt);
  }

  copyToClipboard(text: string): void {
    navigator.clipboard.writeText(text).then(
      () => this.toast.success({ summary: 'Copied to clipboard' }),
      () => this.toast.error('Failed to copy to clipboard'),
    );
  }

  copyKeyToClipboard(key: string): void {
    navigator.clipboard.writeText(key).then(
      () => this.toast.success({ summary: 'API key copied to clipboard' }),
      () => this.toast.error('Failed to copy to clipboard'),
    );
  }

  dismissNewKey(): void {
    this.newCreatedKey.set(null);
    this.apiKeyService.clearLastCreatedKey();
    this.showCreateKeyDialog.set(false);
  }

  findKey(id: string): ApiKeyDto | null {
    return this.apiKeyService.keys().find(k => k.id === id) ?? null;
  }

  startRevokeKey(key: ApiKeyDto | null): void {
    if (!key) return;
    this.keyToRevoke.set(key);
    this.showRevokeKeyDialog.set(true);
  }

  confirmRevokeKey(): void {
    const key = this.keyToRevoke();
    if (!key) return;
    this.apiKeyService.revokeKey(key.id);
    this.showRevokeKeyDialog.set(false);
    this.keyToRevoke.set(null);
  }
}
