import { Component, ChangeDetectionStrategy, input, output, computed, signal, inject } from '@angular/core';
import { Menu } from 'primeng/menu';
import { MenuItem } from 'primeng/api';
import { Profile } from '../profiles/profile.model';
import { ProfileService } from '../profiles/profile.service';

@Component({
  selector: 'app-profile-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Menu],
  template: `
    <div class="flex items-center gap-3 px-4 py-3 rounded-lg border border-border bg-surface hover:bg-surface-muted/50 transition-colors">
      <!-- Profile icon -->
      <div class="w-9 h-9 rounded-lg bg-surface-muted flex items-center justify-center shrink-0">
        <i class="pi {{ profile().icon ?? 'pi-user' }} text-foreground-secondary" aria-hidden="true"></i>
      </div>

      <!-- Profile info -->
      <div class="flex-1 min-w-0">
        <div class="flex items-center gap-2">
          <p class="text-sm font-medium text-foreground truncate">{{ profile().name }}</p>
          @if (profile().isDefault) {
            <span class="text-[10px] font-semibold text-accent-foreground bg-accent px-1.5 py-0.5 rounded">Default</span>
          }
          @if (isActive()) {
            <span class="text-[10px] font-semibold text-done-foreground bg-done px-1.5 py-0.5 rounded">Active</span>
          }
        </div>
      </div>

      <!-- Actions menu -->
      <button
        type="button"
        class="touch-target w-7 h-7 flex items-center justify-center rounded text-foreground-muted hover:bg-surface-muted transition"
        (click)="menu.toggle($event); $event.stopPropagation()"
        [attr.aria-label]="'Actions for ' + profile().name"
      >
        <i class="pi pi-ellipsis-v text-xs" aria-hidden="true"></i>
      </button>
      <p-menu #menu [model]="menuItems()" [popup]="true" appendTo="body" />
    </div>
  `,
})
export class ProfileCardComponent {
  private readonly profileService = inject(ProfileService);

  readonly profile = input.required<Profile>();
  readonly isActive = input(false);

  readonly onEdit = output<Profile>();
  readonly onDelete = output<Profile>();

  readonly menuItems = computed<MenuItem[]>(() => {
    const p = this.profile();
    const items: MenuItem[] = [
      {
        label: 'Edit',
        icon: 'pi pi-pencil',
        command: () => this.onEdit.emit(p),
      },
    ];

    if (!p.isDefault) {
      items.push(
        {
          label: 'Set as default',
          icon: 'pi pi-star',
          command: () => this.profileService.setDefaultProfile(p.id),
        },
        {
          label: 'Delete',
          icon: 'pi pi-trash',
          command: () => this.onDelete.emit(p),
        },
      );
    }

    return items;
  });
}
