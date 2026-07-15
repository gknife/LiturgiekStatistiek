# Review & QA

## Changes
- (a) `contact.component.html` — removed the "Bijdragen" card; intro reduced to
  "Neem contact op met de onderzoeker."
- (b) `services.component.scss` — `.filter-card` now `flex-direction: row` with per-field
  min-widths and `flex-wrap: wrap`.
- (c) `Preacher.Title` removed from entity, `PreacherConfiguration`, DTOs, `PreacherService`,
  `ServiceService`, `DataSeeder`; EF migration `20260715063722_RemovePreacherTitle`
  (DropColumn `Title`); frontend `Preacher`/`PreacherSummary` models + specs; integration
  test updated.
- (d) `add.component.ts` — added `autoSelectLabel`, called from `onElementTypeChange` and
  `addElement`; `add.component.scss` — `.element-type-field` fixed 170px, `.element-label-field`
  wider, `.element-notes-field` flexible, `.element-header` wraps.

## Automated tests
- Backend `dotnet test`: **70 unit + 39 integration passed**, build clean (confirms the
  `Title` removal compiles across all layers).
- Frontend `ng test`: **21 passed**, incl. new `add.component.onderdeel.spec.ts`
  (strict filtering, single-match auto-select, no auto-select on multiple matches,
  clear-on-mismatch).

## Manual QA (Chrome DevTools MCP, dev server)
- Contact `/contact`: 2 cards (E-mail, GitHub); no "Bijdragen", no "draag bij aan het
  project".
- Diensten `/diensten`: filter fields all on one row (`flex-direction: row`, 4 fields same
  `top`); Filteren/Wissen present.
- Add `/toevoegen` → Orde van dienst → nieuw onderdeel:
  - Type "Schriftlezing" → Onderdeel auto-filled "Schriftlezing(en)"; dropdown showed only
    the reading-classified label (strict filter).
  - Layout: Type 170px, Onderdeel 251px, Opmerking 175px, all on one wrapping row.
  - Metadata tab still shows "Woonplaats voorganger" and preacher list renders prefixes from
    `FullName` (e.g. "ds. Janneke Dekker") — no regression from removing `Title`.
- No console errors during the flow.

## Notes / risk
- Existing Azure data may be outdated; the `RemovePreacherTitle` column-drop migration is
  applied to Azure SQL manually by the maintainer. `Down` re-adds the nullable column.
- CI `npm ci` self-heal (from the same working session) unaffected by these changes.
