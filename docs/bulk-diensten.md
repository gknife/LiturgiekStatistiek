# Diensten — Bulk View & Edit

The `/diensten` route (`ServicesComponent`) provides a paginated, filterable grid of
services. Viewing is **public**; editing controls are only shown to authenticated users.

This page replaces the old standalone "Toevoegen" navigation item: `/toevoegen`
redirects to `/diensten`, and the add/edit form opens as a **dialog overlay**
(`MatDialog` wrapping `AddComponent`) for both create and edit.

## Permissions

| Action | Who |
|--------|-----|
| View grid (paginated/filtered) | Everyone |
| Inline per-row edit (flat fields) | Authenticated |
| Multi-select bulk apply (one field to many) | Authenticated |
| Bulk delete | Admin only |
| Create / edit via dialog | Authenticated |

## Backend endpoints

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET`  | `/api/services` | anon | Paginated/filtered list |
| `PUT`  | `/api/services/{id}` | authenticated | Full update (incl. elements) |
| `POST` | `/api/services/bulk-update` | authenticated | Apply one whitelisted field to many services |
| `POST` | `/api/services/bulk-delete` | Admin | Delete many services |

### Bulk update — whitelisted fields

`BulkUpdateAsync` applies a single field to every selected service. Only these fields
are accepted (anything else throws `ArgumentException`):

`timeOfDay`, `congregationId`, `preacherId`, `bibleTranslationId`,
`specialOccasionId`, `musicalAccompanimentId`, `isReadingService`.

This keeps bulk editing safe and avoids the complexity of full nested-element updates
for the common "change one thing across many services" workflow.

## Notes

- The grid maps `TimeOfDay` enum values to Dutch labels (Morning→Morgen,
  Afternoon→Middag, Evening→Avond).
- Full nested-element editing happens through the create/edit dialog (`PUT /api/services/{id}`),
  which uses a replace-all strategy for a service's elements.
