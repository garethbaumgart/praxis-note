import { Component, inject, computed, signal } from '@angular/core';
import { Button } from 'primeng/button';
import { AuthService } from './auth';

@Component({
  selector: 'app-root',
  imports: [Button],
  templateUrl: './app.html',
})
export class App {
  protected readonly auth = inject(AuthService);
  protected readonly sidebarOpen = signal(false);

  protected readonly firstName = computed(() => {
    const name = this.auth.user()?.name;
    return name?.split(' ')[0] ?? '';
  });

  toggleSidebar(): void {
    this.sidebarOpen.update(open => !open);
  }

  login(): void {
    this.auth.login();
  }

  logout(): void {
    this.auth.logout();
  }
}
