# Retro

## What went well
- Four unrelated fixes shipped together cleanly; each verified end-to-end in the browser.
- The `Preacher.Title` removal turned out fully safe: the displayed prefixes ("ds.", "prop.")
  live in `Preacher.FullName`, not `Title`, so dropping the column caused no visible change.
- New vitest coverage locks in the onderdeel filtering/prefill behaviour that the user
  specifically called out.

## What was tricky
- **CDK overlay vs. dialog backdrop.** Dispatching a click on `.cdk-overlay-backdrop` to
  close a `mat-select` overlay also closed the surrounding dialog, which briefly made the
  layout inspection run against the wrong page. Fix: re-open the flow and inspect without
  touching the backdrop.
- **Dev server restart.** The previously running frontend dev server was stalled mid-build;
  a clean restart was needed before Chrome MCP verification.

## Follow-ups
- Maintainer to apply `RemovePreacherTitle` to Azure SQL.
- Consider seeding at least one unclassified label if we want to demonstrate the
  "unclassified labels visible under every type" branch in the real UI (currently all seeded
  labels are classified, so that branch is only exercised by unit tests).
