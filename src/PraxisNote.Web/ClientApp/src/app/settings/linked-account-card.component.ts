import { Component, ChangeDetectionStrategy, input, output, computed, inject } from '@angular/core';
import { Menu } from 'primeng/menu';
import { MenuItem } from 'primeng/api';
import { LinkedIdentity } from './linked-accounts.model';
import { LinkedAccountsService } from './linked-accounts.service';
import { Profile } from '../profiles/profile.model';

@Component({
  selector: 'app-linked-account-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Menu],
  template: `
    <div class="flex items-center gap-3 px-4 py-3 rounded-lg border border-border bg-surface hover:bg-surface-muted/50 transition-colors">
      <!-- Provider icon -->
      <div class="w-9 h-9 rounded-lg bg-surface-muted flex items-center justify-center shrink-0">
        <i class="pi pi-google text-foreground-secondary text-sm" aria-hidden="true"></i>
      </div>

      <!-- Account info -->
      <div class="flex-1 min-w-0">
        <p class="text-sm font-medium text-foreground truncate">{{ identity().email }}</p>
        @if (identity().defaultProfileId) {
          <p class="text-xs text-foreground-muted truncate">Default: {{ defaultProfileName() }}</p>
        }
      </div>

      <!-- Actions menu or Primary badge -->
      @if (menuItems().length > 0) {
        <button type="button"
          class="touch-target w-7 h-7 flex items-center justify-center rounded text-foreground-muted hover:bg-surface-muted transition"
          (click)="menu.toggle($event); $event.stopPropagation()"
          [attr.aria-label]="'Actions for ' + identity().email">
          <i class="pi pi-ellipsis-v text-xs" aria-hidden="true"></i>
        </button>
        <p-menu #menu [model]="menuItems()" [popup]="true" appendTo="body" />
      } @else {
        <span class="text-[10px] font-semibold text-accent-foreground bg-accent px-1.5 py-0.5 rounded">Primary</span>
      }
    </div>
  `,
})
export class LinkedAccountCardComponent {
  private readonly linkedAccountsService = inject(LinkedAccountsService);

  readonly identity = input.required<LinkedIdentity>();
  readonly profiles = input<Profile[]>([]);
  readonly canUnlink = input(true);

  readonly onUnlink = output<LinkedIdentity>();

  readonly defaultProfileName = computed(() => {
    const profileId = this.identity().defaultProfileId;
    if (!profileId) return '';
    return this.profiles().find(p => p.id === profileId)?.name ?? 'Unknown';
  });

  readonly menuItems = computed<MenuItem[]>(() => {
    const items: MenuItem[] = [];
    const profiles = this.profiles();

    if (profiles.length > 0) {
      items.push({
        label: 'Set default profile',
        icon: 'pi pi-user',
        items: [
          ...profiles.map(p => ({
            label: p.name,
            icon: p.id === this.identity().defaultProfileId ? 'pi pi-check' : (p.icon ? `pi ${p.icon}` : 'pi pi-user'),
            command: () => this.linkedAccountsService.setDefaultProfile(this.identity().id, p.id),
          })),
          { separator: true },
          {
            label: 'None',
            icon: !this.identity().defaultProfileId ? 'pi pi-check' : 'pi pi-minus',
            command: () => this.linkedAccountsService.setDefaultProfile(this.identity().id, null),
          },
        ],
      });
    }

    if (this.canUnlink()) {
      if (items.length > 0) items.push({ separator: true });
      items.push({
        label: 'Unlink',
        icon: 'pi pi-times',
        styleClass: 'text-danger',
        command: () => this.onUnlink.emit(this.identity()),
      });
    }

    return items;
  });
}
