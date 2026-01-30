import { Component, inject, input, output, ChangeDetectionStrategy, signal, OnInit, DestroyRef } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs';

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
export class SidebarComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  readonly user = input.required<User>();
  readonly mobileOpen = input(false);

  readonly closeMobile = output<void>();
  readonly onLogout = output<void>();

  // Reactive signal for current path to support zoneless change detection
  private readonly currentPath = signal('');

  protected readonly navItems: NavItem[] = [
    { path: '/home', label: 'Home', icon: 'pi-home', enabled: true },
    { path: '/notes', label: 'Notes', icon: 'pi-file-edit', enabled: true },
    { path: '/tasks', label: 'Tasks', icon: 'pi-check-square', enabled: true },
    { path: '/meetings', label: 'Meetings', icon: 'pi-comments', enabled: true },
    { path: '/insights', label: 'Insights', icon: 'pi-chart-line', enabled: true },
    { path: '/tags', label: 'Tags', icon: 'pi-tags', enabled: false },
  ];

  ngOnInit(): void {
    // Set initial path
    this.updateCurrentPath();

    // Subscribe to navigation events to update the path signal
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => this.updateCurrentPath());
  }

  private updateCurrentPath(): void {
    // Extract pathname without query params or fragments
    this.currentPath.set(this.router.url.split('?')[0].split('#')[0]);
  }

  protected isActive(path: string): boolean {
    const current = this.currentPath();
    return current === path || current.startsWith(path + '/');
  }

  protected navigate(path: string): void {
    this.router.navigate([path]);
    this.closeMobile.emit();
  }

  protected logout(): void {
    this.onLogout.emit();
  }
}
