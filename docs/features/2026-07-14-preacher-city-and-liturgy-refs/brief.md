# Feature brief — Voorganger woonplaats + preektekst/schriftlezing opslaan

**Date:** 2026-07-14
**Author:** gk (@ppgerhardvanderknijff)

## Context

Three issues were reported against the *Dienst toevoegen/bewerken* screen after the
Azure-hosted database went live:

1. **Voorganger woonplaats not settable.** `Preacher.City` exists in the database and the
   backend already accepts `CreatePreacherRequest.City`, but the UI never captured or sent a
   city. New preachers were therefore always created without a woonplaats.
2. **Preektekst not saved.** The preektekst (sermon text) Bible reference dropdowns worked in
   the UI, but the value never persisted — silently lost on reopen, with no error shown.
3. **Schriftlezingen not saved.** Same symptom for the schriftlezing (scripture reading)
   references.

## Goal

- Let the user set a **woonplaats** for a *brand-new* voorganger; show it read-only for an
  existing one.
- Fix the silent data loss so preektekst and schriftlezing references persist on save.
- Make a failed autosave visible instead of silently swallowed.

## Constraints

- Existing production data may be outdated or incorrect; changes must not assume clean data
  (defensive null handling on city and references).
- No behavioural change to the create flow, which already worked.
