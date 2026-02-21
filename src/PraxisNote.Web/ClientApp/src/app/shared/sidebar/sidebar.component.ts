import { Component, inject, input, output, computed, ChangeDetectionStrategy, signal, OnInit, DestroyRef } from '@angular/core';
import { Router, NavigationEnd } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs';
import { Tooltip } from 'primeng/tooltip';
import { Menu } from 'primeng/menu';
import { MenuItem } from 'primeng/api';
import { SidebarActivityService } from './sidebar-activity.service';
import { SidebarService } from './sidebar.service';
import { ProfileService } from '../../profiles/profile.service';
import { MeetingService } from '../../meetings/meeting.service';
import { DOCS_URL } from '../constants';

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
  imports: [Tooltip, Menu],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './sidebar.component.html',
  host: { class: 'contents' },
  styles: [`
    .recording-sidebar-item {
      display: flex;
      align-items: center;
      gap: 6px;
      height: 30px;
      padding: 0 12px;
      margin: 2px 12px;
      border-radius: 6px;
      font-size: 12px;
      background: var(--color-danger-bg);
      border: 1px solid color-mix(in srgb, var(--color-danger-base) 20%, transparent);
      color: var(--color-danger-base);
      cursor: pointer;
      transition: all 0.15s;
      width: calc(100% - 24px);
    }

    .recording-sidebar-item:hover {
      background: var(--color-danger-base);
      color: var(--color-surface);
    }

    .pulse-dot {
      width: 6px;
      height: 6px;
      border-radius: 50%;
      background: var(--color-danger-base);
      animation: sidebar-pulse 1.5s ease-in-out infinite;
      flex-shrink: 0;
    }

    .recording-sidebar-item:hover .pulse-dot {
      background: var(--color-surface);
    }

    @keyframes sidebar-pulse {
      0%, 100% { opacity: 1; }
      50% { opacity: 0.3; }
    }

    .toggle-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 28px;
      height: 28px;
      border-radius: 6px;
      color: var(--color-text-muted);
      cursor: pointer;
      transition: all 0.15s;
    }

    .toggle-btn:hover {
      background: var(--color-bg-muted);
      color: var(--color-text-secondary);
    }
  `],
})
export class SidebarComponent implements OnInit {
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly activity = inject(SidebarActivityService);
  private readonly sidebarService = inject(SidebarService);
  protected readonly profileService = inject(ProfileService);
  private readonly meetingService = inject(MeetingService);

  readonly collapsed = this.sidebarService.collapsed;
  protected readonly docsUrl = DOCS_URL;

  // Section collapse state
  protected readonly inProgressCollapsed = this.sidebarService.inProgressCollapsed;
  protected readonly upNextCollapsed = this.sidebarService.upNextCollapsed;
  protected readonly contextCollapsed = this.sidebarService.contextCollapsed;

  protected readonly activeProfile = this.profileService.activeProfile;

  protected readonly profileMenuItems = computed<MenuItem[]>(() => {
    const profiles = this.profileService.profiles();
    const activeId = this.profileService.activeProfileId();
    const items: MenuItem[] = profiles.map(p => ({
      label: p.name,
      icon: p.id === activeId ? 'pi pi-check' : (p.icon ? `pi ${p.icon}` : 'pi pi-user'),
      command: () => this.profileService.switchProfile(p.id),
      styleClass: p.id === activeId ? 'font-semibold' : '',
    }));

    items.push(
      { separator: true },
      {
        label: 'Manage profiles',
        icon: 'pi pi-cog',
        command: () => {
          this.router.navigate(['/settings']);
          this.closeMobile.emit();
        },
      }
    );

    return items;
  });

  readonly user = input.required<User>();
  readonly mobileOpen = input(false);

  readonly closeMobile = output<void>();
  readonly onLogout = output<void>();

  // Reactive signal for current path to support zoneless change detection
  private readonly currentPath = signal('');

  protected readonly navItems: NavItem[] = [
    { path: '/home', label: 'Home', icon: 'pi-home', enabled: true },
    { path: '/tasks', label: 'Tasks', icon: 'pi-check-square', enabled: true },
    { path: '/notes', label: 'Notes', icon: 'pi-file-edit', enabled: true },
    { path: '/meetings', label: 'Meetings', icon: 'pi-comments', enabled: true },
    { path: '/tags', label: 'Tag Hub', icon: 'pi-tags', enabled: true },
    { path: '/insights', label: 'Insights', icon: 'pi-chart-line', enabled: true },
  ];

  readonly hasUnreflectedMeetings = computed(() => {
    const meetings = this.meetingService.meetings();
    return meetings.some(m =>
      (m.status === 'Ready' || m.status === 'Reviewed') &&
      m.behavioralAnalysis !== null &&
      m.reflectionData === null
    );
  });

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

  protected navigateTo(route: string[], queryParams?: Record<string, string>): void {
    this.router.navigate(route, queryParams ? { queryParams } : undefined);
    this.closeMobile.emit();
  }

  protected navigateToTask(taskId: string): void {
    this.router.navigate(['/tasks'], { queryParams: { highlight: taskId } });
    this.closeMobile.emit();
  }

  protected returnToRecording(): void {
    const meetingId = this.activity.recorder.activeMeetingId();
    if (meetingId) {
      this.navigateTo(['/meetings', meetingId]);
    }
  }

  protected toggleCollapse(): void {
    this.sidebarService.toggle();
  }

  protected toggleInProgress(): void {
    this.sidebarService.toggleSection(this.sidebarService.inProgressCollapsed, this.sidebarService.sectionKeys.inProgress);
  }

  protected toggleUpNext(): void {
    this.sidebarService.toggleSection(this.sidebarService.upNextCollapsed, this.sidebarService.sectionKeys.upNext);
  }

  protected toggleContext(): void {
    this.sidebarService.toggleSection(this.sidebarService.contextCollapsed, this.sidebarService.sectionKeys.context);
  }

  protected logout(): void {
    this.onLogout.emit();
  }
}
