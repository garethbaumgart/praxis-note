import { Component, ChangeDetectionStrategy, input, signal, computed } from '@angular/core';

@Component({
  selector: 'app-meeting-section',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="section-card" [style.border-left-color]="borderColor()">
      <button
        type="button"
        class="section-header"
        [style.color]="headerColor()"
        [attr.aria-expanded]="expanded()"
        [attr.aria-controls]="sectionId()"
        (click)="toggle()"
        (keydown.enter)="toggle()"
      >
        <span class="flex items-center gap-1.5">
          <i [class]="iconClasses()"></i>
          {{ title() }}
        </span>
        <i
          class="pi collapse-chevron"
          [class.pi-chevron-down]="!expanded()"
          [class.pi-chevron-up]="expanded()"
          aria-hidden="true"
        ></i>
      </button>

      <!-- Collapsed summary -->
      @if (!expanded()) {
        <div class="collapsed-summary" (click)="toggle()" role="button" tabindex="0" (keydown.enter)="toggle()">
          <ng-content select="[summary]" />
        </div>
      }

      <!-- Expanded content -->
      <div
        [id]="sectionId()"
        class="section-body"
        [class.section-body-visible]="expanded()"
        [class.section-body-hidden]="!expanded()"
      >
        <ng-content />
      </div>
    </div>
  `,
  styles: [`
    .section-card {
      background: var(--color-bg-subtle);
      border: 1px solid var(--color-border-default);
      border-radius: 8px;
      padding: 16px;
      margin-bottom: 12px;
      border-left: 3px solid transparent;
    }

    .section-header {
      all: unset;
      display: flex;
      justify-content: space-between;
      align-items: center;
      width: 100%;
      font-size: 12px;
      font-weight: 600;
      text-transform: uppercase;
      cursor: pointer;
      box-sizing: border-box;
    }

    .section-header:focus-visible {
      outline: 2px solid var(--color-primary-solid);
      outline-offset: 2px;
      border-radius: 4px;
    }

    .collapse-chevron {
      font-size: 10px;
      transition: transform 0.2s ease;
    }

    .collapsed-summary {
      margin-top: 8px;
      cursor: pointer;
    }

    .section-body {
      overflow: hidden;
      transition: opacity 0.2s ease, max-height 0.2s ease;
    }

    .section-body-visible {
      opacity: 1;
      max-height: 5000px;
      margin-top: 12px;
    }

    .section-body-hidden {
      opacity: 0;
      max-height: 0;
      margin-top: 0;
    }
  `],
})
export class MeetingSectionComponent {
  readonly title = input.required<string>();
  readonly icon = input.required<string>();
  readonly borderColor = input.required<string>();
  readonly headerColor = input.required<string>();
  readonly sectionId = input<string>('section');
  readonly collapsible = input(true);
  readonly initialExpanded = input(true);

  /** Combines the base 'pi' class with the specific icon class */
  readonly iconClasses = computed(() => `pi ${this.icon()}`);

  readonly expanded = signal(true);

  private initialized = false;

  toggle(): void {
    if (!this.collapsible()) return;
    this.expanded.set(!this.expanded());
  }

  expand(): void {
    this.expanded.set(true);
  }

  collapse(): void {
    if (!this.collapsible()) return;
    this.expanded.set(false);
  }

  /** Called by parent to set initial expanded state after construction */
  setExpanded(value: boolean): void {
    this.expanded.set(value);
  }
}
