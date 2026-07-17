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

### Default characteristics

Besides the onderdelen, a template also stores **standaardkenmerken** that pre-fill the
dienst-metadata when the template is chosen:

| Field | Source | Notes |
|-------|--------|-------|
| `MusicalAccompanimentId` | `MusicalAccompaniment` list | Default muzikale begeleiding |
| `DefaultBibleTranslationId` | `BibleTranslations` list | Pre-fills the translation on reading onderdelen |
| `DefaultSongBundleId` | `SongBundles` list | Pre-fills the liedbundel on song onderdelen (standaard liedbundel) |
| `IsReadingService` | bool | Leesdienst |
| `HasBeamerLiturgy` / `HasBeamerTexts` / `HasBeamerSongs` | bool | Beamer-opties |

The default **Bijbelvertaling** and default **liedbundel** are applied to *every* lezing- resp.
lied-onderdeel that still lacks one — whether it comes from the scaffold, is added manually, or
is produced by pasting text / importing a Kerkdienstgemist URL.

The **Kerkgenootschap** selector also doubles as a default: when the dienst chooser creates a
brand-new gemeente, it is stamped with the template's kerkgenootschap. For an existing gemeente
the field is read-only and the gemeente's own kerkgenootschap is kept.

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

### Duplicating a template

The Sjablonen-lijst has a **Dupliceren** action (copy icon) next to Bewerken. It loads the full
template into the editor as a *new, unsaved* copy — every field and onderdeel is prefilled and
the name becomes `"<naam> (kopie)"`. Saving creates a separate template; the original is left
untouched. This is the quickest way to derive a variant (e.g. an avonddienst from a
morgendienst) without re-entering the whole orde van dienst.

## Use in data entry

Creating a dienst now **starts** with a template chooser: before the Handmatig / Plakken /
URL-import tabs appear, the user picks a sjabloon (or **Zonder sjabloon (leeg)**). The chosen
template pre-fills the metadata standaardkenmerken and lays out the onderdelen scaffold, with
each onderdeel's performer defaulting to **Voorganger** (a template performer override wins,
and manual override is always possible). Editing or **duplicating** an existing dienst skips
the chooser (the source dienst acts as the template).

The **Sjabloon toepassen** button on the onderdelen-tab calls `instantiate` for the chosen
gemeente/dagdeel/gelegenheid. If onderdelen were already parsed or entered, they are
**reconciled** into the template scaffold (label match fills slots, empties stay, extras are
appended). This is also how pasted text and Kerkdienstgemist imports land in the standard
structure. It is only shown while **creating a new dienst** — once a dienst exists, editing it
no longer offers *Sjabloon toepassen* (a template is a starting point, not a re-apply on
existing data). See [data-entry.md](./data-entry.md).
