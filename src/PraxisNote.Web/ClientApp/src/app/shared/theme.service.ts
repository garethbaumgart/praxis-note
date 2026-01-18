import { Injectable, signal, effect, inject, PLATFORM_ID, DestroyRef } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

type Theme = 'light' | 'dark';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly destroyRef = inject(DestroyRef);

  readonly theme = signal<Theme>(this.getSystemTheme());

  constructor() {
    if (isPlatformBrowser(this.platformId)) {
      // Listen for system preference changes
      const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
      const handler = (e: MediaQueryListEvent) => {
        this.theme.set(e.matches ? 'dark' : 'light');
      };

      // Use addEventListener with fallback for older browsers (Safari < 14)
      if (mediaQuery.addEventListener) {
        mediaQuery.addEventListener('change', handler);
        this.destroyRef.onDestroy(() => mediaQuery.removeEventListener('change', handler));
      } else {
        // Deprecated but needed for older Safari
        mediaQuery.addListener(handler);
        this.destroyRef.onDestroy(() => mediaQuery.removeListener(handler));
      }
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
