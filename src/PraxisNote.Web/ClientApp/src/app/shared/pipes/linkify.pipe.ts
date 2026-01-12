import { Pipe, PipeTransform, inject } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';

// URL regex that matches http://, https://, and www. URLs
const URL_REGEX = /(?:https?:\/\/|www\.)[^\s<>"')\]]+/gi;

// Maximum display length for URLs before truncation
const MAX_URL_DISPLAY_LENGTH = 40;

@Pipe({
  name: 'linkify',
  standalone: true,
  pure: true,
})
export class LinkifyPipe implements PipeTransform {
  private readonly sanitizer = inject(DomSanitizer);

  transform(text: string | null | undefined): SafeHtml {
    if (!text) {
      return '';
    }

    // Find all URLs and their positions
    const matches: { index: number; length: number; url: string }[] = [];
    let match: RegExpExecArray | null;

    // Reset regex lastIndex
    URL_REGEX.lastIndex = 0;

    while ((match = URL_REGEX.exec(text)) !== null) {
      matches.push({
        index: match.index,
        length: match[0].length,
        url: match[0],
      });
    }

    // If no URLs found, escape and return the text
    if (matches.length === 0) {
      return this.escapeHtml(text);
    }

    // Build the result by processing segments
    let result = '';
    let lastIndex = 0;

    for (const { index, length, url } of matches) {
      // Add escaped text before this URL
      if (index > lastIndex) {
        result += this.escapeHtml(text.substring(lastIndex, index));
      }

      // Add the link
      result += this.createLink(url);
      lastIndex = index + length;
    }

    // Add any remaining text after the last URL
    if (lastIndex < text.length) {
      result += this.escapeHtml(text.substring(lastIndex));
    }

    // Mark as safe HTML (we've escaped user content and only added our own markup)
    return this.sanitizer.bypassSecurityTrustHtml(result);
  }

  private createLink(url: string): string {
    // Ensure URL has protocol for href
    const href = url.startsWith('www.') ? `https://${url}` : url;

    // Create truncated display text
    const displayText = this.truncateUrl(url);

    // Escape any HTML in the URL itself (for the href and title attributes)
    const safeHref = this.escapeAttr(href);
    const safeTitle = this.escapeAttr(url);
    const safeDisplay = this.escapeHtml(displayText);

    return `<a href="${safeHref}" target="_blank" rel="noopener noreferrer" title="${safeTitle}" class="text-primary hover:underline break-all">${safeDisplay}</a>`;
  }

  private truncateUrl(url: string): string {
    if (url.length <= MAX_URL_DISPLAY_LENGTH) {
      return url;
    }

    // Remove protocol for display
    let display = url.replace(/^https?:\/\//, '');

    if (display.length <= MAX_URL_DISPLAY_LENGTH) {
      return display;
    }

    // Truncate with ellipsis
    return display.substring(0, MAX_URL_DISPLAY_LENGTH - 3) + '...';
  }

  private escapeHtml(text: string): string {
    return text
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#039;')
      .replace(/\n/g, '<br>');
  }

  private escapeAttr(text: string): string {
    return text
      .replace(/&/g, '&amp;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#039;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;');
  }
}
