# Theming, Logo & Favicon

## ThemeService

`ThemeService` is applied at **app startup** and controls three user-configurable
settings, persisted to `localStorage`:

- **Theme** — light / dark (toggles a class on the document root and `color-scheme`).
- **Accent colour** — overrides Material 3 system variables (`--mat-sys-primary` and
  related) so toolbar, buttons, links and active states pick up the accent.
- **Font size** — base font scaling.

Settings are reachable from the user menu **and** an always-visible settings icon, and
apply live (no reload needed).

### Why startup application matters

Because the app is zoneless and Material 3 reads CSS system variables, the theme must be
written to the document before the first render. Applying it via the `ThemeService` at
startup avoids a flash of default styling and ensures the accent is honoured everywhere.

The previous hardcoded `color-scheme: light` in `styles.scss` was removed because it
fought the dark-mode toggle.

## Logo

The original full-colour logo is shown on the light toolbar. In dark mode a white logo
variant is used (switched via the theme class / `prefers-color-scheme`) so it stays
legible against the dark toolbar.

## Favicon

The favicon is served from the app's `public/` (or `src/`) assets and referenced from
`index.html`. Verify it loads via DevTools → Network (look for `favicon.ico` / the
configured icon returning `200`). If a browser caches an old icon, hard-reload.
