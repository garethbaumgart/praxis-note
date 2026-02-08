import { Component, input, output, computed, ChangeDetectionStrategy } from '@angular/core';
import { MenuItem } from 'primeng/api';
import { Menu } from 'primeng/menu';
import { SortMode } from './task.model';

@Component({
  selector: 'app-sort-menu-dropdown',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Menu],
  template: `
    <button
      type="button"
      class="touch-target w-7 h-7 flex items-center justify-center rounded transition-colors ml-1"
      [class.bg-interactive]="isSortActive()"
      [class.text-interactive-foreground]="isSortActive()"
      [class.text-foreground-muted]="!isSortActive()"
      [class.hover:text-foreground]="!isSortActive()"
      [class.hover:bg-surface-hover]="!isSortActive()"
      (click)="sortMenu.toggle($event)"
      aria-label="Sort options"
      aria-haspopup="true"
    >
      <i class="pi pi-sort-alt text-xs"></i>
    </button>
    <p-menu #sortMenu [model]="sortMenuItems()" [popup]="true" appendTo="body" />
  `,
})
export class SortMenuDropdownComponent {
  readonly sortMode = input.required<SortMode>();
  readonly onModeChange = output<SortMode>();

  readonly isSortActive = computed(() => this.sortMode() !== 'manual');

  readonly sortMenuItems = computed<MenuItem[]>(() => {
    const current = this.sortMode();
    return [
      {
        label: 'Manual order',
        icon: current === 'manual' ? 'pi pi-check' : 'pi pi-bars',
        command: () => this.onModeChange.emit('manual'),
      },
      {
        label: 'Due date',
        icon: current === 'dueDate' ? 'pi pi-check' : 'pi pi-calendar',
        command: () => this.onModeChange.emit('dueDate'),
      },
      {
        label: 'Priority',
        icon: current === 'priority' ? 'pi pi-check' : 'pi pi-flag',
        command: () => this.onModeChange.emit('priority'),
      },
    ];
  });
}
