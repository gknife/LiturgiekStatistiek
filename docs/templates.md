# Sjablonen — service-structure templates

Templates let you predefine a standard **orde van dienst** so that creating a service
pre-fills the onderdelen instead of starting from an empty list. They cover the recurring
structure per kerkgenootschap/gemeente and per gelegenheid (regulier, Doop, Avondmaal,
Belijdenis, bevestiging ambtsdragers, …).

UI: the **Sjablonen** tab (`TemplatesComponent`, authenticated only).
Backend: `ITemplateService` / `TemplateService`, exposed via `TemplatesController`.

## Model

A `ServiceTemplate` has a **Name** plus optional **selector tags**:

| Selector | Source | Notes |
|----------|--------|-------|
| `DenominationId` | `Denominations` list | Kerkgenootschap-level default |
| `CongregationId` | Congregation | Optional per-gemeente override |
| `TimeOfDay` | enum (ochtend/middag/avond) | Optional |
| `OccasionId` | `ServiceOccasion` list | Doop, Avondmaal, Belijdenis, … |

Each `ServiceTemplateElement` carries a label + element type and optional defaults:
**performer**, **beurtzang** and a **fixed scripture reference** (for standing readings).
No week-specific songs or sermon text are stored.

## Matching (`ResolveAsync`)

Given the selectors of the service being created, the resolver hard-filters on any
non-null template selector, then picks the highest **specificity score**:

```
congregation +8   occasion +4   timeOfDay +2   denomination +1
```

So a congregation-specific template beats a denomination-only one, and an occasion-specific
template (e.g. Avondmaal) beats a generic one. `InstantiateAsync` returns the winning
template's onderdelen as ready-to-use `CreateServiceElementRequest`s.

## Endpoints

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET`  | `/api/templates` | authenticated | List template summaries |
| `GET`  | `/api/templates/{id}` | authenticated | Full template with elements |
| `POST` | `/api/templates` | authenticated | Create |
| `PUT`  | `/api/templates/{id}` | authenticated | Update (delete-then-insert element graph) |
| `DELETE` | `/api/templates/{id}` | authenticated | Delete |
| `POST` | `/api/templates/instantiate` | authenticated | Best-match onderdelen for given selectors |

## Use in data entry

On the **Dienst toevoegen** page, **Sjabloon toepassen** calls `instantiate` for the chosen
gemeente/dagdeel/gelegenheid. If onderdelen were already parsed or entered, they are
**reconciled** into the template scaffold (label match fills slots, empties stay, extras are
appended). This is also how pasted text and Kerkdienstgemist imports land in the standard
structure. See [data-entry.md](./data-entry.md).
