import { Component, computed, inject, input, output, ChangeDetectionStrategy } from '@angular/core';
import { Router } from '@angular/router';
import { ThemeService } from '../theme.service';

interface NavItem {
  path: string;
  label: string;
  icon: string;
  enabled: boolean;
}

interface User {
  name: string;
  avatarUrl?: string | null;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './sidebar.component.html',
  host: { class: 'contents' },
})
export class SidebarComponent {
  private readonly router = inject(Router);
  protected readonly themeService = inject(ThemeService);

  readonly user = input.required<User>();
  readonly mobileOpen = input(false);

  readonly closeMobile = output<void>();
  readonly onLogout = output<void>();

  protected readonly navItems: NavItem[] = [
    { path: '/home', label: 'Home', icon: 'pi-home', enabled: true },
    { path: '/notes', label: 'Notes', icon: 'pi-file-edit', enabled: false },
    { path: '/tasks', label: 'Tasks', icon: 'pi-check-square', enabled: true },
    { path: '/labels', label: 'Labels', icon: 'pi-tags', enabled: false },
  ];

  protected isActive(path: string): boolean {
    return this.router.url === path || this.router.url.startsWith(path + '/');
  }

  protected navigate(path: string): void {
    this.router.navigate([path]);
    this.closeMobile.emit();
  }

  protected logout(): void {
    this.onLogout.emit();
  }

  protected toggleTheme(): void {
    this.themeService.toggle();
  }
}
