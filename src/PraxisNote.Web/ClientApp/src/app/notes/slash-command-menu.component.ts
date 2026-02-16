import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  ElementRef,
  effect,
  viewChild,
} from '@angular/core';
import { SlashCommandItem } from './extensions/slash-command-items';

@Component({
  selector: 'app-slash-command-menu',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      #menuContainer
      class="slash-menu"
      [style.top.px]="position().top"
      [style.left.px]="position().left"
      role="listbox"
      aria-label="Slash commands"
    >
      @for (group of groups(); track group.name) {
        <div class="slash-menu-group">
          <div class="slash-menu-group-label">{{ group.name }}</div>
          @for (item of group.items; track item.label) {
            <button
              type="button"
              class="slash-menu-item"
              [class.slash-menu-item-active]="item.flatIndex === selectedIndex()"
              [attr.data-index]="item.flatIndex"
              role="option"
              [attr.aria-selected]="item.flatIndex === selectedIndex()"
              (mouseenter)="onMouseEnter(item.flatIndex)"
              (click)="selectItem.emit(item.flatIndex)"
            >
              <i [class]="item.icon + ' slash-menu-item-icon'" aria-hidden="true"></i>
              <span class="slash-menu-item-label">
                {{ item.label }}
                @if (item.aliases?.length) {
                  <span class="slash-menu-item-alias">{{ item.aliases![0] }}</span>
                }
              </span>
              @if (item.shortcut) {
                <span class="slash-menu-item-shortcut">{{ item.shortcut }}</span>
              }
            </button>
          }
        </div>
      }
      @if (items().length === 0) {
        <div class="slash-menu-empty">No matching commands</div>
      }
    </div>
  `,
  styles: [
    `
      .slash-menu {
        position: fixed;
        z-index: 1000;
        width: 280px;
        max-height: 320px;
        overflow-y: auto;
        background: var(--color-surface-default);
        border: 1px solid var(--color-border);
        border-radius: 8px;
        box-shadow: 0 4px 16px rgba(0, 0, 0, 0.12);
        padding: 4px 0;
      }

      .slash-menu-group {
        padding: 4px 0;
      }

      .slash-menu-group + .slash-menu-group {
        border-top: 1px solid var(--color-border);
      }

      .slash-menu-group-label {
        padding: 6px 12px 4px;
        font-size: 10px;
        font-weight: 600;
        text-transform: uppercase;
        letter-spacing: 0.05em;
        color: var(--color-foreground-muted);
      }

      .slash-menu-item {
        display: flex;
        align-items: center;
        gap: 8px;
        width: 100%;
        padding: 6px 12px;
        border: none;
        background: transparent;
        cursor: pointer;
        font-size: 13px;
        color: var(--color-foreground-default);
        text-align: left;
        transition: background 0.1s;
      }

      .slash-menu-item:hover,
      .slash-menu-item-active {
        background: var(--color-surface-hover);
      }

      .slash-menu-item-icon {
        width: 16px;
        font-size: 14px;
        color: var(--color-foreground-secondary);
        text-align: center;
        flex-shrink: 0;
      }

      .slash-menu-item-label {
        flex: 1;
      }

      .slash-menu-item-alias {
        font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
        font-size: 10px;
        color: var(--color-foreground-muted);
        background: var(--color-surface-hover);
        padding: 1px 5px;
        border-radius: 3px;
        margin-left: 4px;
      }

      .slash-menu-item-shortcut {
        font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
        font-size: 11px;
        color: var(--color-foreground-muted);
        flex-shrink: 0;
      }

      .slash-menu-empty {
        padding: 12px;
        text-align: center;
        font-size: 13px;
        color: var(--color-foreground-muted);
      }
    `,
  ],
})
export class SlashCommandMenuComponent {
  private readonly menuContainer = viewChild<ElementRef>('menuContainer');

  /** The filtered list of slash command items */
  readonly items = input.required<SlashCommandItem[]>();

  /** The currently highlighted item index (flat index across all groups) */
  readonly selectedIndex = input.required<number>();

  /** The position for the dropdown {top, left} in fixed coordinates */
  readonly position = input.required<{ top: number; left: number }>();

  /** Emitted when an item is selected (by click or mouseenter for index tracking) */
  readonly selectItem = output<number>();

  /** Emitted when mouse hovers over an item (to update the selected index) */
  readonly hoverItem = output<number>();

  /** Grouped items computed from the flat items list, with flat indices preserved */
  readonly groups = input.required<
    Array<{
      name: string;
      items: Array<SlashCommandItem & { flatIndex: number }>;
    }>
  >();

  constructor() {
    // Scroll the active item into view when selectedIndex changes
    effect(() => {
      const index = this.selectedIndex();
      const container = this.menuContainer()?.nativeElement as HTMLElement;
      if (!container) return;

      // Use requestAnimationFrame to ensure the DOM is updated
      requestAnimationFrame(() => {
        const activeItem = container.querySelector(`[data-index="${index}"]`) as HTMLElement;
        if (activeItem) {
          activeItem.scrollIntoView({ block: 'nearest' });
        }
      });
    });
  }

  onMouseEnter(index: number): void {
    this.hoverItem.emit(index);
  }
}
