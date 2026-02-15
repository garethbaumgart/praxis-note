import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { DOCS_URL } from '../constants';

@Component({
  selector: 'app-help-link',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <a
      class="inline-flex items-center gap-1 text-xs text-foreground-muted hover:underline"
      [href]="docsUrl + '/' + path()"
      target="_blank"
      rel="noopener noreferrer"
      [attr.aria-label]="label() + ' (opens documentation in new tab)'"
    >
      <i class="pi pi-question-circle" style="font-size: 10px;" aria-hidden="true"></i>
      {{ label() }}
    </a>
  `,
})
export class HelpLinkComponent {
  readonly path = input.required<string>();
  readonly label = input('Learn more');
  protected readonly docsUrl = DOCS_URL;
}
