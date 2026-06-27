import { Injectable, signal, inject } from '@angular/core';
import { ApiService } from './api.service';
import { AuthService } from '../auth/auth.service';

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
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly _preferences = signal<UserPreferences>(this.load());
  readonly preferences = this._preferences.asReadonly();

  /** Apply the persisted preferences. Call once during app initialization. */
  initialize(): void {
    this.apply(this._preferences());
  }

  /**
   * When a user is logged in, the database is the source of truth. Load their
   * stored settings, merge over the local defaults, and apply. Settings are
   * stored as a generic JSON blob so new preference keys can be added without a
   * schema change.
   */
  loadFromDb(): void {
    if (!this.auth.isAuthenticated) return;
    this.api.getUserSettings().subscribe({
      next: res => {
        try {
          const parsed = res?.settingsJson ? JSON.parse(res.settingsJson) : {};
          const next = { ...this._preferences(), ...parsed } as UserPreferences;
          this._preferences.set(next);
          this.persist(next);
          this.apply(next);
        } catch {
          // Ignore malformed stored settings.
        }
      },
      error: () => { /* not logged in / unavailable — keep local */ },
    });
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
    this.writeThrough(next);
  }

  /** Persist to the database when authenticated (write-through). */
  private writeThrough(prefs: UserPreferences): void {
    if (!this.auth.isAuthenticated) return;
    this.api.saveUserSettings(JSON.stringify(prefs)).subscribe({
      error: () => { /* ignore transient write failures */ },
    });
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

    // Accent drives every "blue" element through CSS custom properties. The raw
    // accent is contrast-adjusted per theme: in dark mode it is lightened so it
    // stays legible against dark surfaces, and headings get an even lighter
    // shade. A consistently dark shade is exposed for surfaces that always carry
    // white text (footer, badges).
    const base = prefs.accentColor;
    const isDark = prefs.theme === 'dark';

    const accent = isDark ? lighten(base, 0.34) : base;
    const heading = isDark ? lighten(base, 0.5) : darken(base, 0.12);
    const accentDark = darken(base, 0.18);

    root.style.setProperty('--app-accent', accent);
    root.style.setProperty('--app-accent-dark', accentDark);
    root.style.setProperty('--app-heading', heading);
    root.style.setProperty('--app-on-accent', readableOn(accent));
    root.style.setProperty('--mat-sys-primary', accent);
    root.style.setProperty('--mat-sys-on-primary', readableOn(accent));
    root.style.setProperty('--mat-sys-on-primary-container', isDark ? lighten(base, 0.6) : darken(base, 0.25));
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

function hexToRgb(hex: string): [number, number, number] {
  const h = hex.replace('#', '');
  const full = h.length === 3 ? h.split('').map(c => c + c).join('') : h;
  return [
    parseInt(full.slice(0, 2), 16),
    parseInt(full.slice(2, 4), 16),
    parseInt(full.slice(4, 6), 16),
  ];
}

function rgbToHex(rgb: [number, number, number]): string {
  return '#' + rgb.map(v => Math.max(0, Math.min(255, Math.round(v))).toString(16).padStart(2, '0')).join('');
}

/** Mix the colour toward white by `amount` (0..1). */
function lighten(hex: string, amount: number): string {
  const [r, g, b] = hexToRgb(hex);
  return rgbToHex([r + (255 - r) * amount, g + (255 - g) * amount, b + (255 - b) * amount]);
}

/** Mix the colour toward black by `amount` (0..1). */
function darken(hex: string, amount: number): string {
  const [r, g, b] = hexToRgb(hex);
  return rgbToHex([r * (1 - amount), g * (1 - amount), b * (1 - amount)]);
}

/** Pick a readable foreground (near-black or white) for the given background. */
function readableOn(hex: string): string {
  const [r, g, b] = hexToRgb(hex).map(v => {
    const c = v / 255;
    return c <= 0.03928 ? c / 12.92 : Math.pow((c + 0.055) / 1.055, 2.4);
  });
  const luminance = 0.2126 * r + 0.7152 * g + 0.0722 * b;
  return luminance > 0.45 ? '#1a1a1a' : '#ffffff';
}
