import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection } from '@angular/core';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { providePrimeNG } from 'primeng/config';
import { MessageService } from 'primeng/api';
import { definePreset } from '@primeng/themes';
import Aura from '@primeng/themes/aura';
import { routes } from './app.routes';
import { mockAuthInterceptor } from './auth/mock-auth.interceptor';
import { authInterceptor } from './auth/auth.interceptor';

// Nord Frost-inspired color palette for PrimeNG
const PraxisNoteTheme = definePreset(Aura, {
  semantic: {
    primary: {
      50: '#f0f4f8',    // Lightest frost tint
      100: '#d8e2ed',   // Very light frost
      200: '#b8cce0',   // Light frost
      300: '#88c0d0',   // Nord Frost cyan
      400: '#81a1c1',   // Nord Frost blue-gray
      500: '#5e81ac',   // Nord Frost blue (main)
      600: '#4c6c94',   // Darker frost
      700: '#3b5277',   // Deeper frost
      800: '#2e4260',   // Dark frost
      900: '#243448',   // Very dark frost
      950: '#1a2633',   // Darkest frost
    },
  },
});

export const appConfig: ApplicationConfig = {
  providers: [
    provideZonelessChangeDetection(),
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([mockAuthInterceptor, authInterceptor])),
    provideAnimationsAsync(),
    providePrimeNG({
      theme: {
        preset: PraxisNoteTheme,
        options: {
          darkModeSelector: '[data-theme="dark"]',
        },
      },
    }),
    MessageService,
  ],
};
