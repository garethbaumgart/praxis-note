import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';

@Component({
  selector: 'app-error-state',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="flex flex-col items-center justify-center py-8" role="alert">
      <div
        class="rounded-xl flex items-center justify-center mb-3"
        [class.w-11]="size() === 'md'"
        [class.h-11]="size() === 'md'"
        [class.w-9]="size() === 'sm'"
        [class.h-9]="size() === 'sm'"
        [class.rounded-lg]="size() === 'sm'"
        [class.rounded-xl]="size() === 'md'"
        [class.bg-danger-bg]="true"
      >
        <i
          class="pi pi-exclamation-triangle text-danger"
          [class.text-lg]="size() === 'md'"
          [class.text-sm]="size() === 'sm'"
          aria-hidden="true"
        ></i>
      </div>
      <p
        class="font-semibold text-foreground"
        [class.text-sm]="size() === 'md'"
        [class.text-xs]="size() === 'sm'"
      >{{ title() }}</p>
      @if (message()) {
        <p
          class="text-foreground-muted mt-1"
          [class.text-xs]="true"
        >{{ message() }}</p>
      }
      @if (showRetry()) {
        <button
          type="button"
          class="mt-3 px-3.5 py-1.5 text-xs font-medium rounded-lg border border-border text-foreground-secondary hover:bg-surface-muted transition-colors cursor-pointer"
          (click)="retry.emit()"
        >
          Try again
        </button>
      }
    </div>
  `,
})
export class ErrorStateComponent {
  readonly title = input('Something went wrong');
  readonly message = input('');
  readonly size = input<'md' | 'sm'>('md');
  readonly showRetry = input(true);
  readonly retry = output<void>();
}
