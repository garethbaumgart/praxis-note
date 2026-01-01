import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { catchError, of, tap } from 'rxjs';
import { User } from './user.model';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  private readonly _user = signal<User | null>(null);
  private readonly _loading = signal(true);
  private readonly _initialized = signal(false);

  readonly user = this._user.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly isAuthenticated = computed(() => this._user() !== null);

  constructor() {
    this.checkAuth();
  }

  login(): void {
    window.location.href = '/api/auth/login/google';
  }

  logout(): void {
    this._loading.set(true);
    this.http.post('/api/auth/logout', {}).subscribe({
      next: () => {
        this._user.set(null);
        this._loading.set(false);
      },
      error: () => {
        this._user.set(null);
        this._loading.set(false);
      },
    });
  }

  private checkAuth(): void {
    if (this._initialized()) {
      return;
    }

    this._loading.set(true);
    this.http
      .get<User>('/api/auth/me')
      .pipe(
        tap((user) => {
          this._user.set(user);
          this._loading.set(false);
          this._initialized.set(true);
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
