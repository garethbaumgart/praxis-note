import { Pipe, PipeTransform, inject } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

@Pipe({
  name: 'highlight',
  standalone: true,
  pure: true,
})
export class HighlightPipe implements PipeTransform {
  private readonly sanitizer = inject(DomSanitizer);

  transform(text: string | null | undefined, searchTerm: string | null | undefined): SafeHtml {
    if (!text) {
      return this.sanitizer.bypassSecurityTrustHtml('');
    }

    const escaped = this.escapeHtml(text);

    if (!searchTerm?.trim()) {
      // Return escaped text to prevent HTML interpretation
      return this.sanitizer.bypassSecurityTrustHtml(escaped);
    }

    const regex = new RegExp(`(${this.escapeRegex(searchTerm.trim())})`, 'gi');
    const highlighted = escaped.replace(
      regex,
      '<mark class="search-highlight">$1</mark>'
    );

    return this.sanitizer.bypassSecurityTrustHtml(highlighted);
  }

  private escapeHtml(str: string): string {
    return str
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#039;')
      .replace(/\n/g, '<br>');
  }

  private escapeRegex(str: string): string {
    return str.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  }
}
