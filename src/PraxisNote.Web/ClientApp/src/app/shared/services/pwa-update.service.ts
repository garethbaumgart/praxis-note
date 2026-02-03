import { DestroyRef, Injectable, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { SwUpdate, VersionReadyEvent } from '@angular/service-worker';
import { filter, interval } from 'rxjs';

/** Session storage key to track dismissed version hashes */
const DISMISSED_VERSION_KEY = 'pwa-dismissed-version';

@Injectable({
  providedIn: 'root',
})
export class PwaUpdateService {
  private readonly swUpdate = inject(SwUpdate);
  private readonly destroyRef = inject(DestroyRef);

  /** Whether a new version is available and ready to install */
  readonly updateAvailable = signal(false);

  /** Hash of the currently detected update version */
  private currentVersionHash: string | null = null;

  constructor() {
    if (!this.swUpdate.isEnabled) {
      return;
    }

    // Listen for new versions
    this.swUpdate.versionUpdates
      .pipe(
        filter((event): event is VersionReadyEvent => event.type === 'VERSION_READY'),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((event) => {
        // Only show notification if user hasn't dismissed this specific version
        const dismissedVersion = sessionStorage.getItem(DISMISSED_VERSION_KEY);
        if (dismissedVersion !== event.latestVersion.hash) {
          this.currentVersionHash = event.latestVersion.hash;
          this.updateAvailable.set(true);
        }
      });

    // Check for updates every 30 minutes while the app is open
    interval(30 * 60 * 1000)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.checkForUpdate();
      });

    // Check for updates when user returns to the app
    const visibilityHandler = () => {
      if (document.visibilityState === 'visible') {
        this.checkForUpdate();
      }
    };
    document.addEventListener('visibilitychange', visibilityHandler);
    this.destroyRef.onDestroy(() => {
      document.removeEventListener('visibilitychange', visibilityHandler);
    });
  }

  /** Manually check for updates */
  async checkForUpdate(): Promise<void> {
    if (!this.swUpdate.isEnabled) {
      return;
    }

    try {
      await this.swUpdate.checkForUpdate();
    } catch (err) {
      console.error('Failed to check for updates:', err);
    }
  }

  /** Reload the app to apply the update */
  applyUpdate(): void {
    document.location.reload();
  }

  /** Dismiss the update notification (user chooses to update later) */
  dismissUpdate(): void {
    // Remember this version was dismissed so we don't show it again this session
    if (this.currentVersionHash) {
      sessionStorage.setItem(DISMISSED_VERSION_KEY, this.currentVersionHash);
    }
    this.updateAvailable.set(false);
  }
}
