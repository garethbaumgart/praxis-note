import { Component, inject, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { toSignal } from '@angular/core/rxjs-interop';
import { catchError, map, of, startWith } from 'rxjs';
import { Card } from 'primeng/card';
import { Tag } from 'primeng/tag';
import { Button } from 'primeng/button';
import { Avatar } from 'primeng/avatar';
import { AuthService } from './auth';

type ApiStatus = 'checking...' | 'healthy' | 'error';

@Component({
  selector: 'app-root',
  imports: [Card, Tag, Button, Avatar],
  templateUrl: './app.html',
})
export class App {
  private readonly http = inject(HttpClient);
  protected readonly auth = inject(AuthService);

  protected readonly title = 'PraxisNote';
  protected readonly apiStatus = toSignal(
    this.http.get<{ status: string }>('/api/health').pipe(
      map((res) => res.status as ApiStatus),
      catchError(() => of('error' as ApiStatus)),
      startWith('checking...' as ApiStatus)
    ),
    { initialValue: 'checking...' as ApiStatus }
  );
  protected readonly apiSeverity = computed(() => {
    const status = this.apiStatus();
    if (status === 'healthy') return 'success';
    if (status === 'error') return 'danger';
    return 'info';
  });

  login(): void {
    this.auth.login();
  }

  logout(): void {
    this.auth.logout();
  }
}
