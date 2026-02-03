import { Injectable, inject, signal } from '@angular/core';
import { SwUpdate, VersionReadyEvent } from '@angular/service-worker';
import { filter, interval } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class PwaUpdateService {
  private readonly swUpdate = inject(SwUpdate);

  /** Whether a new version is available and ready to install */
  readonly updateAvailable = signal(false);

  constructor() {
    if (!this.swUpdate.isEnabled) {
      return;
    }

    // Listen for new versions
    this.swUpdate.versionUpdates
      .pipe(filter((event): event is VersionReadyEvent => event.type === 'VERSION_READY'))
      .subscribe(() => {
        this.updateAvailable.set(true);
      });

    // Check for updates every 30 minutes while the app is open
    interval(30 * 60 * 1000).subscribe(() => {
      this.checkForUpdate();
    });

    // Check for updates when user returns to the app
    document.addEventListener('visibilitychange', () => {
      if (document.visibilityState === 'visible') {
        this.checkForUpdate();
      }
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
    this.updateAvailable.set(false);
  }
}
