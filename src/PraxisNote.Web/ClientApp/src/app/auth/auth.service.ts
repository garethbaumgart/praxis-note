import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { catchError, of, Subscription, tap } from 'rxjs';
import { User } from './user.model';
import { ProfileService } from '../profiles/profile.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly profileService = inject(ProfileService);

  private readonly _user = signal<User | null>(null);
  private readonly _loading = signal(true);
  private readonly _initialized = signal(false);
  private readonly _loginPending = signal(false);
  private authCheckSub?: Subscription;

  readonly user = this._user.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly isAuthenticated = computed(() => this._user() !== null);
  readonly loginPending = this._loginPending.asReadonly();

  constructor() {
    this.checkAuth();
  }

  login(): void {
    if (this._loginPending()) return;
    this._loginPending.set(true);
    window.location.href = '/api/auth/login/google';
    // Reset after 5s in case the redirect fails or user navigates back
    setTimeout(() => this._loginPending.set(false), 5000);
  }

  logout(): void {
    this._loading.set(true);
    this.http.post('/api/auth/logout', {}).subscribe({
      next: () => {
        this._user.set(null);
        this._loading.set(false);
        this._initialized.set(false);
      },
      error: () => {
        this._user.set(null);
        this._loading.set(false);
        this._initialized.set(false);
      },
    });
  }

  recheckAuth(): void {
    this._initialized.set(false);
    this.checkAuth();
  }

  private checkAuth(): void {
    if (this._initialized()) {
      return;
    }

    this._loading.set(true);
    this.authCheckSub?.unsubscribe();
    this.authCheckSub = this.http
      .get<User>('/api/auth/me')
      .pipe(
        tap((user) => {
          this._user.set(user);
          this._loading.set(false);
          this._initialized.set(true);

          // Initialize profiles from the auth response
          if (user.profiles?.length) {
            this.profileService.initFromUser(
              user.profiles.map(p => ({
                id: p.id,
                name: p.name,
                icon: p.icon,
                isDefault: p.isDefault,
                createdAt: '',
              }))
            );
          }
        }),
        catchError((error: HttpErrorResponse) => {
          if (error.status === 401) {
            this._user.set(null);
          }
          this._loading.set(false);
          this._initialized.set(true);
          return of(null);
        })
      )
      .subscribe();
  }
}
