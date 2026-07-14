# Implementation plan

## Root cause (bugs #2 + #3 — one bug)

`ServiceService.UpdateServiceAsync` deletes the existing sermon references, flushes, then
re-adds the new ones. The re-add went through the **tracked navigation**
(`service.SermonTextReferences.Add(...)`). After the delete flush, EF identity resolution
treats a freshly added row on the tracked parent as an *existing* row to UPDATE, so the
second `SaveChangesAsync()` throws
`DbUpdateConcurrencyException: Attempted to update or delete an entity that does not exist in
the store`. The HTTP layer returns **500** with no CORS header, so the browser reports
`net::ERR_FAILED` and the autosave handler swallowed it.

A single PUT carrying **both** a sermon reference and a reading element 500s, losing both —
which is why preektekst *and* schriftlezing were reported together. The Elements path was
already correct (it re-adds via the `DbSet`).

## Changes

### A. Backend fix
- `UpdateServiceAsync`: re-add `SermonTextReferences` via
  `_context.SermonTextReferences.Add(...)` (DbSet), mirroring the Elements path. Documented
  with a comment.

### B. Backend support for read-only preacher city
- Add `City` to `PreacherSummaryDto`.
- Populate it in `PreacherService.SearchPreachersAsync` and in
  `ServiceService.GetServiceByIdAsync` (the `service.Preacher` summary).

### C. Frontend preacher city
- `PreacherSummary` model: add `city`.
- New `preacherCityControl`; "Woonplaats voorganger" input beside Voorganger.
- Editable for a new name; read-only + prefilled when an existing preacher is selected or
  loaded. Sent via `createPreacher({ fullName, city })`. Added to `snapshot()`.

### D. Frontend hardening
- Autosave error handler sets `autosaveFailed` and surfaces a non-blocking indicator instead
  of silently swallowing the failure.

## Tests
- Integration (SQLite): add sermon refs on update; replace sermon refs; both sermon ref +
  reading element together.
- Integration: preacher create persists city; search summary includes city.
- Frontend (vitest): existing preacher shows read-only city; typing a new name re-enables;
  snapshot includes preacher city.

## Verification
- API-level create → PUT reproduction (pre-fix 500, post-fix 200 + persists).
- Chrome MCP end-to-end.
