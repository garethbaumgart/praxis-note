import { Injectable } from '@angular/core';

export interface DeleteConfirmationHandle {
  cancel: () => void;
}

@Injectable({
  providedIn: 'root',
})
export class DeleteConfirmationService {
  private timeoutId: ReturnType<typeof setTimeout> | null = null;
  private clickHandler: (() => void) | null = null;

  start(onCancel: () => void, timeoutMs = 3000): DeleteConfirmationHandle {
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

    return {
      cancel: () => {
        this.cleanup();
      },
    };
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
