import { Injectable, signal, effect, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

type Theme = 'light' | 'dark';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly platformId = inject(PLATFORM_ID);

  readonly theme = signal<Theme>(this.getSystemTheme());

  constructor() {
    if (isPlatformBrowser(this.platformId)) {
      // Listen for system preference changes
      const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
      mediaQuery.addEventListener('change', (e) => {
        this.theme.set(e.matches ? 'dark' : 'light');
      });
    }

    // Apply theme to document when it changes
    effect(() => {
      const theme = this.theme();
      if (isPlatformBrowser(this.platformId)) {
        document.documentElement.setAttribute('data-theme', theme);
      }
    });
  }

  private getSystemTheme(): Theme {
    if (!isPlatformBrowser(this.platformId)) {
      return 'light';
    }
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }
}
