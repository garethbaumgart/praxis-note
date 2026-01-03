import { Injectable, signal, computed, isDevMode } from '@angular/core';

export interface MockUser {
  email: string;
  name: string;
  userId: string;
}

@Injectable({ providedIn: 'root' })
export class MockAuthService {
  private readonly STORAGE_KEY = 'praxisnote.mockAuth.enabled';
  private readonly USER_KEY = 'praxisnote.mockAuth.user';

  private readonly _enabled = signal(this.readEnabled());
  private readonly _user = signal<MockUser | null>(this.readUser());

  readonly enabled = this._enabled.asReadonly();
  readonly user = this._user.asReadonly();
  readonly isLoggedIn = computed(() => this._enabled() && this._user() !== null);

  readonly isDevMode = isDevMode();

  enable(): void {
    if (!this.isDevMode) return;
    localStorage.setItem(this.STORAGE_KEY, 'true');
    this._enabled.set(true);
  }

  disable(): void {
    localStorage.removeItem(this.STORAGE_KEY);
    localStorage.removeItem(this.USER_KEY);
    this._enabled.set(false);
    this._user.set(null);
  }

  login(email: string, name: string): void {
    if (!this.isDevMode || !this._enabled()) return;

    const mockUser: MockUser = {
      email,
      name,
      userId: `mock-${email}`,
    };
    localStorage.setItem(this.USER_KEY, JSON.stringify(mockUser));
    this._user.set(mockUser);
  }

  logout(): void {
    localStorage.removeItem(this.USER_KEY);
    this._user.set(null);
  }

  getMockHeader(): string | null {
    const user = this._user();
    if (!this._enabled() || !user) return null;
    return `${user.email}|${user.name}|${user.userId}`;
  }

  private readEnabled(): boolean {
    if (!isDevMode()) return false;
    return localStorage.getItem(this.STORAGE_KEY) === 'true';
  }

  private readUser(): MockUser | null {
    if (!isDevMode()) return null;
    const stored = localStorage.getItem(this.USER_KEY);
    if (!stored) return null;
    try {
      return JSON.parse(stored);
    } catch {
      return null;
    }
  }
}
