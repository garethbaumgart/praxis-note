import { Component, ChangeDetectionStrategy, input } from '@angular/core';

@Component({
  selector: 'app-page-content',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div
      class="mx-auto px-6 md:px-8 py-8 md:py-10"
      [class.max-w-6xl]="maxWidth() === 'default'"
      [class.max-w-3xl]="maxWidth() === 'narrow'"
    >
      <ng-content />
    </div>
  `,
})
export class PageContentComponent {
  readonly maxWidth = input<'default' | 'narrow'>('default');
}
