import { Injectable, signal } from '@angular/core';

export type ThemeMode = 'light' | 'dark';
export type FontSize = 'small' | 'medium' | 'large';

export interface UserPreferences {
  theme: ThemeMode;
  fontSize: FontSize;
  accentColor: string;
}

const STORAGE_KEY = 'liturgiek-preferences';

const DEFAULT_PREFERENCES: UserPreferences = {
  theme: 'light',
  fontSize: 'medium',
  accentColor: '#2c5282',
};

const FONT_SIZES: Record<FontSize, string> = {
  small: '14px',
  medium: '16px',
  large: '18px',
};

/**
 * Central place that owns the user's visual preferences and applies them to the
 * document. Because the app runs zoneless, preferences are exposed as a signal
 * so the settings UI re-renders on change. Applying the preferences is a pure
 * DOM side effect (CSS custom properties / classes) and is done at app startup.
 */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly _preferences = signal<UserPreferences>(this.load());
  readonly preferences = this._preferences.asReadonly();

  /** Apply the persisted preferences. Call once during app initialization. */
  initialize(): void {
    this.apply(this._preferences());
  }

  setTheme(theme: ThemeMode): void {
    this.update({ theme });
  }

  setFontSize(fontSize: FontSize): void {
    this.update({ fontSize });
  }

  setAccentColor(accentColor: string): void {
    this.update({ accentColor });
  }

  private update(partial: Partial<UserPreferences>): void {
    const next = { ...this._preferences(), ...partial };
    this._preferences.set(next);
    this.persist(next);
    this.apply(next);
  }

  private apply(prefs: UserPreferences): void {
    const root = document.documentElement;

    // Theme: modern Angular Material emits its system tokens through the CSS
    // light-dark() function, so toggling color-scheme on the root flips the
    // whole palette. The class is used for non-token styling (e.g. the logo).
    root.style.colorScheme = prefs.theme;
    root.classList.toggle('dark-theme', prefs.theme === 'dark');

    // Font size scales the root rem unit.
    root.style.fontSize = FONT_SIZES[prefs.fontSize];

    // Accent overrides Material's primary system colour plus a convenience
    // variable used by bespoke elements (links, active nav underline, …).
    const accent = prefs.accentColor;
    root.style.setProperty('--app-accent', accent);
    root.style.setProperty('--mat-sys-primary', accent);
    root.style.setProperty('--mat-sys-on-primary', '#ffffff');
  }

  private load(): UserPreferences {
    try {
      const saved = localStorage.getItem(STORAGE_KEY);
      if (saved) {
        return { ...DEFAULT_PREFERENCES, ...JSON.parse(saved) };
      }
    } catch {
      // Corrupt/unavailable storage — fall back to defaults.
    }
    return { ...DEFAULT_PREFERENCES };
  }

  private persist(prefs: UserPreferences): void {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(prefs));
    } catch {
      // Ignore storage write failures (e.g. private mode quota).
    }
  }
}
