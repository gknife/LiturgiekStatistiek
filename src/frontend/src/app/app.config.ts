import {
  ApplicationConfig,
  provideBrowserGlobalErrorListeners,
  provideZonelessChangeDetection,
  provideAppInitializer,
  inject,
  LOCALE_ID,
} from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideAnimations } from '@angular/platform-browser/animations';
import { MAT_DATE_LOCALE, provideNativeDateAdapter } from '@angular/material/core';
import { registerLocaleData } from '@angular/common';
import localeNl from '@angular/common/locales/nl';

import { routes } from './app.routes';
import { authInterceptor } from './core/auth/auth.interceptor';
import { ThemeService } from './core/services/theme.service';

registerLocaleData(localeNl);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZonelessChangeDetection(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    provideAnimations(),
    provideNativeDateAdapter(),
    { provide: LOCALE_ID, useValue: 'nl-NL' },
    { provide: MAT_DATE_LOCALE, useValue: 'nl-NL' },
    provideAppInitializer(() => {
      inject(ThemeService).initialize();
    }),
  ],
};
