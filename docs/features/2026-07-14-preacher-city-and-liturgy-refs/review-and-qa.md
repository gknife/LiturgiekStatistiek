# Review & QA

## Automated tests

### Backend (`dotnet test` — IntegrationTests)
- **39 passed, 0 failed.** New tests:
  - `ServiceServiceUpdateIntegrationTests.UpdateServiceAsync_AddingSermonReferences_PersistsThem`
    — regression for the `DbUpdateConcurrencyException` (bugs #2/#3).
  - `…_ReplacingSermonReferences_ReplacesNotAppends` — replace semantics.
  - `…_WithSermonRefAndReadingElement_PersistsBoth` — the coupled preektekst +
    schriftlezing path.
  - `PreacherServiceCityIntegrationTests.CreatePreacherAsync_PersistsCity`.
  - `…SearchPreachersAsync_IncludesCity` — city surfaced in the search summary.

### Frontend (`ng test` — vitest)
- **17 passed, 0 failed.** New `add.component.city.spec.ts`:
  - Existing preacher selection shows city read-only (disabled) + sets `preacherId`.
  - Typing a new name re-enables the city field and clears `preacherId`.
  - `snapshot()` includes `preacherCity` (so autosave still fires on city change).

## Manual / API reproduction
- Pre-fix: `PUT /api/services/{id}` with a `sermonTextReferences` entry → **HTTP 500**
  (`DbUpdateConcurrencyException` at `ServiceService.cs`). A PUT with both a sermon ref and a
  reading element → 500 (both lost).
- Post-fix: same PUTs → **200**, values persist on GET.

## Chrome MCP end-to-end
- New dienst, new gemeente "Testgemeente MCP" / Teststad, **new voorganger "ds. MCP Tester"
  with woonplaats "Testdorp"**, preektekst **Genesis 1:1-3**, schriftlezing element
  **Exodus 2:1-5**.
- Every autosave `PUT` returned **200** (no `net::ERR_FAILED`).
- `GET` after save returned:
  `preacher.city = "Testdorp"`, `sermonTextReferences = ["Genesis 1:1-3"]`,
  `readingReferences = ["Exodus 2:1-5"]`.
- Reopening the saved dienst: woonplaats field shows "Testdorp", **disabled**, hint
  "Woonplaats van de bestaande voorganger (niet wijzigbaar hier)." — read-only path confirmed.

## Risks / follow-ups
- Existing production preachers have no city; the read-only field simply shows blank until a
  preacher is edited under *Lijsten → Voorgangers* (out of scope here).
- Autosave failures are now surfaced via the indicator; explicit save/publish still alert.
