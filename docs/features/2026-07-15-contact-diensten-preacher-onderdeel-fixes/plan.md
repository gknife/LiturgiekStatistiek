# Implementation plan

## (a) Contact — remove "Bijdragen"
- `contact.component.html`: drop the third (`group_add` / "Bijdragen") `mat-card`; change
  intro to "Neem contact op met de onderzoeker." (removes "of draag bij aan het project").

## (b) Diensten filters horizontal
- `services.component.scss` `.filter-card`: add `flex-direction: row`; per-field min-widths
  (selects ~200px, date fields ~150px); keep `flex-wrap: wrap` so fields wrap only when they
  do not fit; Filteren/Wissen stay on the same row.

## (c) Remove `Preacher.Title`
- Backend: remove `Title` from `Preacher` entity, `PreacherConfiguration`, `PreacherDto`,
  `PreacherSummaryDto`, `CreatePreacherRequest`, `UpdatePreacherRequest`, `PreacherService`
  (projections/create/update/search), `ServiceService` preacher summary and `DataSeeder`
  seeds.
- EF migration `RemovePreacherTitle` drops the `nvarchar(20)` column (Down re-adds it).
- Frontend: remove `title` from `Preacher` / `PreacherSummary` models; update specs
  and the integration test that passed `Title: null`.

## (d) Onderdeel dropdowns
- `labelsForType` keeps unclassified (null) labels visible under every type and strictly
  filters classified labels to the selected type — already correct, verified with tests.
- Prefill: `autoSelectLabel` selects the label when exactly one *classified* label matches
  the type; called from `onElementTypeChange` and `addElement`.
- Layout (`add.component.scss`): `.element-type-field` fixed `flex: 0 0 170px`,
  `.element-label-field` wider (`flex: 2 1 220px`), `.element-notes-field` flexible
  (`flex: 1 1 160px`), and `.element-header` wraps (`flex-wrap: wrap`) so fields never cramp.

## Tests
- Backend: existing PreacherService/ServiceService/City tests compile without `Title`.
- Frontend (vitest): `add.component.onderdeel.spec.ts` — strict filtering, auto-select for a
  single match, no auto-select for multiple matches, clear-on-mismatch; existing city spec
  updated.

## Docs
- This feature-delivery set + `release-notes.md` + `data-entry.md` update.

## Verification (Chrome MCP)
- Contact card gone; diensten filters horizontal; onderdeel filter+prefill+layout; preacher
  create/save no regression.
