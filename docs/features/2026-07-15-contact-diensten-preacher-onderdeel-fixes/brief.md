# Feature brief — Contact / Diensten-filters / Voorganger-prefix / Onderdeel-dropdowns

**Date:** 2026-07-15
**Author:** gk (@ppgerhardvanderknijff)

## Context

Four independent UI/data clean-ups reported after the Azure-hosted database went live:

1. **Contact page — "Bijdragen" section.** The contact page showed a contribution
   ("Bijdragen") card and the intro invited the reader to "draag bij aan het project".
   This is not wanted; the page should only offer contact with the onderzoeker.
2. **Diensten filters stacked vertically.** On the *Diensten* overview the filter fields
   rendered one below the other because `.filter-card` set `display:flex; flex-wrap:wrap`
   but no `flex-direction`, so the `mat-mdc-card` column default won. They should sit
   horizontally and only wrap when they do not fit.
3. **Voorganger "prefix" column.** `Preacher.Title` (a prefix such as *ds.*/*prop.*) existed
   in the entity, DTOs, service and seeder but was never surfaced or set in the UI. It was
   dead data. Decision: remove it entirely.
4. **Onderdeel dropdowns.** On adding an onderdeel the second dropdown (*Onderdeel*) should
   be correctly filtered by the *Type* dropdown, be laid out cleanly, and be pre-filled where
   a single option applies.

## Goal

- Remove the Bijdragen card and the "draag bij aan het project" wording from contact.
- Lay the diensten filters out horizontally, wrapping only when needed.
- Remove `Preacher.Title` from the whole stack (entity → migration → DTOs → service →
  seeder → frontend models) with an EF migration dropping the column.
- Keep the onderdeel *Onderdeel* dropdown strictly filtered to labels classified for the
  selected type (plus unclassified labels), auto-select the sole matching classified label,
  and improve the row layout.

## Constraints

- Existing production data may be outdated or incorrect; changes must not assume clean data.
- The database is Azure-hosted; the `RemovePreacherTitle` column-drop migration is applied
  to Azure SQL manually by the maintainer.
