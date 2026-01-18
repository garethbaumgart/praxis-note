import { Pipe, PipeTransform, inject } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

@Pipe({
  name: 'highlight',
  standalone: true,
  pure: true,
})
export class HighlightPipe implements PipeTransform {
  private readonly sanitizer = inject(DomSanitizer);

  transform(text: string | null | undefined, searchTerm: string | null | undefined): SafeHtml | string {
    if (!text) {
      return '';
    }

    if (!searchTerm?.trim()) {
      return text;
    }

    const escaped = this.escapeHtml(text);
    const regex = new RegExp(`(${this.escapeRegex(searchTerm.trim())})`, 'gi');
    const highlighted = escaped.replace(
      regex,
      '<mark class="bg-primary/20 text-foreground rounded-sm px-0.5">$1</mark>'
    );

    return this.sanitizer.bypassSecurityTrustHtml(highlighted);
  }

  private escapeHtml(str: string): string {
    return str
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#039;');
  }

  private escapeRegex(str: string): string {
    return str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  }
}
