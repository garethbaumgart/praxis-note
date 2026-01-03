import { Component, inject, signal, isDevMode } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MockAuthService } from './mock-auth.service';
import { AuthService } from './auth.service';

@Component({
  selector: 'app-mock-auth-toolbar',
  standalone: true,
  imports: [FormsModule],
  template: `
    @if (isDevMode) {
      <div class="mock-toolbar">
        <button
          class="mock-toggle"
          [class.enabled]="mockAuth.enabled()"
          (click)="toggleMockMode()"
        >
          Mock: {{ mockAuth.enabled() ? 'ON' : 'OFF' }}
        </button>

        @if (mockAuth.enabled()) {
          <div class="mock-panel">
            @if (mockAuth.isLoggedIn()) {
              <div class="mock-user">
                <span class="mock-user-info">{{ mockAuth.user()?.email }}</span>
                <button class="mock-btn logout" (click)="mockLogout()">Logout</button>
              </div>
            } @else {
              <div class="mock-form">
                <input
                  type="email"
                  [(ngModel)]="email"
                  placeholder="Email"
                  class="mock-input"
                />
                <input
                  type="text"
                  [(ngModel)]="name"
                  placeholder="Name"
                  class="mock-input"
                />
                <button class="mock-btn login" (click)="mockLogin()">Login</button>
              </div>
            }
          </div>
        }
      </div>
    }
  `,
  styles: `
    .mock-toolbar {
      position: fixed;
      bottom: 16px;
      right: 16px;
      z-index: 9999;
      display: flex;
      flex-direction: column;
      align-items: flex-end;
      gap: 8px;
      font-family: system-ui, -apple-system, sans-serif;
      font-size: 12px;
    }

    .mock-toggle {
      padding: 8px 12px;
      border: none;
      border-radius: 6px;
      background: #6b7280;
      color: white;
      cursor: pointer;
      font-weight: 600;
      box-shadow: 0 2px 8px rgba(0, 0, 0, 0.15);
      transition: background 0.2s;
    }

    .mock-toggle:hover {
      background: #4b5563;
    }

    .mock-toggle.enabled {
      background: #10b981;
    }

    .mock-toggle.enabled:hover {
      background: #059669;
    }

    .mock-panel {
      background: white;
      border-radius: 8px;
      padding: 12px;
      box-shadow: 0 4px 16px rgba(0, 0, 0, 0.15);
      border: 1px solid #e5e7eb;
    }

    .mock-form {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .mock-input {
      padding: 8px 12px;
      border: 1px solid #d1d5db;
      border-radius: 4px;
      font-size: 12px;
      width: 180px;
    }

    .mock-input:focus {
      outline: none;
      border-color: #10b981;
      box-shadow: 0 0 0 2px rgba(16, 185, 129, 0.2);
    }

    .mock-btn {
      padding: 8px 12px;
      border: none;
      border-radius: 4px;
      cursor: pointer;
      font-weight: 500;
      transition: background 0.2s;
    }

    .mock-btn.login {
      background: #10b981;
      color: white;
    }

    .mock-btn.login:hover {
      background: #059669;
    }

    .mock-btn.logout {
      background: #ef4444;
      color: white;
    }

    .mock-btn.logout:hover {
      background: #dc2626;
    }

    .mock-user {
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .mock-user-info {
      color: #374151;
      max-width: 150px;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    :host-context(.dark-mode) .mock-panel {
      background: #1f2937;
      border-color: #374151;
    }

    :host-context(.dark-mode) .mock-input {
      background: #374151;
      border-color: #4b5563;
      color: white;
    }

    :host-context(.dark-mode) .mock-user-info {
      color: #d1d5db;
    }
  `,
})
export class MockAuthToolbarComponent {
  protected readonly mockAuth = inject(MockAuthService);
  private readonly auth = inject(AuthService);

  protected readonly isDevMode = isDevMode();

  protected email = signal('dev@test.com');
  protected name = signal('Dev User');

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
