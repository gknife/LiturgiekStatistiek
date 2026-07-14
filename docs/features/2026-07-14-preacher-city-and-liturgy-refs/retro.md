# Retro

## What went well
- The bug turned out to be a single root cause behind two reported symptoms
  (preektekst + schriftlezing), because a PUT carrying both a sermon ref and a reading
  element failed as a unit. Fixing the sermon-ref re-add resolved both.
- The method already documented the exact EF identity-resolution pitfall for the Elements
  path; the sermon-ref path simply hadn't been migrated to the DbSet pattern. The comment
  made the fix obvious once reproduced.

## What was tricky
- The failure was invisible in the UI: a 500 with no CORS header surfaces as
  `net::ERR_FAILED`, and the autosave handler swallowed it. Static reading of the backend
  looked correct — only live reproduction (API + Chrome) exposed it.
- The sermon-ref update path had **no** test coverage; the existing update tests always
  passed `SermonTextReferences: null`.

## Follow-ups / lessons
- Prefer the DbSet-add pattern consistently for any delete-then-re-add graph in
  `UpdateServiceAsync`; the tracked-navigation add is a trap after a delete flush.
- Autosave now surfaces failures — consider extending the same treatment to any other
  fire-and-forget calls so silent data loss can't recur.
- When adding a child-collection to a request DTO, add an update-with-that-collection test
  at the same time.
