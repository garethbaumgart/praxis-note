import { Component, input, output, ChangeDetectionStrategy } from '@angular/core';

/**
 * Reusable delete confirmation button with visual countdown.
 * Shows "Confirm?" with a shrinking progress bar that indicates remaining time.
 * Respects prefers-reduced-motion for accessibility.
 */
@Component({
  selector: 'app-delete-confirm-button',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col items-end" [class.shrink-0]="shrink()">
      <button
        type="button"
        class="flex items-center gap-1 text-danger text-xs"
        (click)="onConfirm.emit(); $event.stopPropagation()"
        [attr.aria-label]="ariaLabel()"
      >
        <i class="pi pi-trash"></i>
        <span>Confirm?</span>
      </button>
      <div class="h-0.5 bg-danger/30 rounded-full mt-0.5 w-full overflow-hidden">
        <div class="h-full bg-danger rounded-full delete-countdown"></div>
      </div>
    </div>
  `,
})
export class DeleteConfirmButtonComponent {
  /** Accessible label for the confirm button */
  readonly ariaLabel = input('Confirm delete');

  /** Whether to apply shrink-0 class (useful in flex layouts) */
  readonly shrink = input(false);

  /** Emitted when user clicks the confirm button */
  readonly onConfirm = output<void>();
}
