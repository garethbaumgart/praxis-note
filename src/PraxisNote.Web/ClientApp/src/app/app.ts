import { Component, inject, signal, effect, ChangeDetectionStrategy } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Toast } from 'primeng/toast';
import { AuthService } from './auth';
import { MockAuthToolbarComponent } from './auth/mock-auth-toolbar.component';
import { LoginComponent } from './shared/login/login.component';
import { SidebarComponent } from './shared/sidebar/sidebar.component';
import { NotificationService } from './notifications/notification.service';
import { NotificationPanelComponent } from './notifications/notification-panel.component';
import { ThemeService } from './shared/theme.service';

@Component({
  selector: 'app-root',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, Toast, SidebarComponent, LoginComponent, MockAuthToolbarComponent, NotificationPanelComponent],
  templateUrl: './app.html',
})
export class App {
  protected readonly auth = inject(AuthService);
  protected readonly notificationService = inject(NotificationService);
  private readonly themeService = inject(ThemeService); // Initialize theme detection at app startup
  protected readonly sidebarOpen = signal(false);
  protected readonly notificationPanelOpen = signal(false);

  constructor() {
    // Connect SSE when authenticated
    effect(() => {
      if (this.auth.isAuthenticated()) {
        this.notificationService.connectSse();
      } else {
        this.notificationService.disconnectSse();
      }
    });
  }

  toggleSidebar(): void {
    this.sidebarOpen.update(open => !open);
  }

  closeSidebar(): void {
    this.sidebarOpen.set(false);
  }

  toggleNotificationPanel(): void {
    this.notificationPanelOpen.update(open => !open);
  }

  login(): void {
    this.auth.login();
  }

  logout(): void {
    this.auth.logout();
  }
}
