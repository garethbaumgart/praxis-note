import { Injectable } from '@angular/core';

/**
 * Service to manage delete confirmation state with auto-cancel behavior.
 * Intentionally handles one confirmation at a time - starting a new confirmation
 * automatically cancels any existing one via cleanup().
 */
@Injectable({
  providedIn: 'root',
})
export class DeleteConfirmationService {
  private timeoutId: ReturnType<typeof setTimeout> | null = null;
  private clickHandler: (() => void) | null = null;

  /** Default timeout for delete confirmation in milliseconds */
  static readonly DEFAULT_TIMEOUT_MS = 5000;

  /**
   * Start a delete confirmation with auto-cancel after timeout or click outside.
   * Call cleanup() first if starting a new confirmation while one is active.
   */
  start(onCancel: () => void, timeoutMs = DeleteConfirmationService.DEFAULT_TIMEOUT_MS): void {
    this.cleanup();

    // Auto-cancel after timeout
    this.timeoutId = setTimeout(() => {
      onCancel();
      this.cleanup();
    }, timeoutMs);

    // Cancel on any click outside (after current event completes)
    setTimeout(() => {
      this.clickHandler = () => {
        onCancel();
        this.cleanup();
      };
      document.addEventListener('click', this.clickHandler, { once: true });
    }, 0);
  }

  cleanup(): void {
    if (this.timeoutId) {
      clearTimeout(this.timeoutId);
      this.timeoutId = null;
    }
    if (this.clickHandler) {
      document.removeEventListener('click', this.clickHandler);
      this.clickHandler = null;
    }
  }
}
