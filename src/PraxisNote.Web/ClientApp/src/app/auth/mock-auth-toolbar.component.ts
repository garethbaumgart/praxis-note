import { Component, inject, isDevMode, signal, ChangeDetectionStrategy } from '@angular/core';
import { MockAuthService } from './mock-auth.service';

@Component({
  selector: 'app-mock-auth-toolbar',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (isDevMode) {
      <div class="fixed bottom-4 right-4 z-[9999] flex flex-col items-end gap-2 font-sans text-xs">
        <button
          class="px-3 py-2 border-none rounded-md text-white cursor-pointer font-semibold shadow-lg transition-colors"
          [class.bg-gray-500]="!mockAuth.enabled()"
          [class.hover:bg-gray-600]="!mockAuth.enabled()"
          [class.bg-emerald-500]="mockAuth.enabled()"
          [class.hover:bg-emerald-600]="mockAuth.enabled()"
          (click)="toggleMockMode()"
        >
          Mock: {{ mockAuth.enabled() ? 'ON' : 'OFF' }}
        </button>

        @if (mockAuth.enabled()) {
          <div class="bg-surface rounded-lg p-3 shadow-lg border border-border">
            @if (mockAuth.isLoggedIn()) {
              <div class="flex items-center gap-2">
                <span class="text-foreground max-w-[150px] overflow-hidden text-ellipsis whitespace-nowrap">
                  {{ mockAuth.user()?.email }}
                </span>
                <button
                  class="px-3 py-2 border-none rounded bg-rose-600 text-white cursor-pointer font-medium transition-colors hover:bg-rose-700"
                  (click)="mockLogout()"
                >
                  Logout
                </button>
              </div>
            } @else {
              <div class="flex flex-col gap-2">
                <input
                  type="email"
                  [value]="email()"
                  (input)="email.set(asInput($event).value)"
                  placeholder="Email"
                  class="px-3 py-2 border border-border rounded text-xs w-[180px] bg-surface text-foreground focus:outline-none focus:border-emerald-500 focus:ring-2 focus:ring-emerald-500/20"
                />
                <input
                  type="text"
                  [value]="name()"
                  (input)="name.set(asInput($event).value)"
                  placeholder="Name"
                  class="px-3 py-2 border border-border rounded text-xs w-[180px] bg-surface text-foreground focus:outline-none focus:border-emerald-500 focus:ring-2 focus:ring-emerald-500/20"
                />
                <button
                  class="px-3 py-2 border-none rounded bg-emerald-500 text-white cursor-pointer font-medium transition-colors hover:bg-emerald-600"
                  (click)="mockLogin()"
                >
                  Login
                </button>
              </div>
            }
          </div>
        }
      </div>
    }
  `,
})
export class MockAuthToolbarComponent {
  protected readonly mockAuth = inject(MockAuthService);
  protected readonly isDevMode = isDevMode();

  readonly email = signal('dev@test.com');
  readonly name = signal('Dev User');

  /** Type-safe helper for accessing input value from events */
  asInput(event: Event): HTMLInputElement {
    return event.target as HTMLInputElement;
  }

  toggleMockMode(): void {
    if (this.mockAuth.enabled()) {
      this.mockAuth.disable();
      // Force page reload to clear any cached auth state
      window.location.reload();
    } else {
      this.mockAuth.enable();
    }
  }

  mockLogin(): void {
    this.mockAuth.login(this.email(), this.name());
    // Reload to trigger auth check with new mock user
    window.location.reload();
  }

  mockLogout(): void {
    this.mockAuth.logout();
    // Reload to trigger auth check without mock user
    window.location.reload();
  }
}
