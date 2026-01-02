import { Component, inject, signal } from '@angular/core';
import { RouterOutlet, Router } from '@angular/router';
import { AuthService } from './auth';
import { ThemeService } from './shared/theme.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
})
export class App {
  protected readonly auth = inject(AuthService);
  protected readonly router = inject(Router);
  protected readonly themeService = inject(ThemeService);
  protected readonly sidebarOpen = signal(false);

  toggleSidebar(): void {
    this.sidebarOpen.update(open => !open);
  }

  navigate(path: string): void {
    this.router.navigate([path]);
    this.sidebarOpen.set(false);
  }

  isActive(path: string): boolean {
    return this.router.url === path || this.router.url.startsWith(path + '/');
  }

  login(): void {
    this.auth.login();
  }

  logout(): void {
    this.auth.logout();
  }
}
