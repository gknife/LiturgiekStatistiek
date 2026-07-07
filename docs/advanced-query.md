# Advanced Query Builder

The advanced query builder lets any user (authenticated or not) build custom queries
over services and their songs using composable building blocks, without writing SQL.

UI: the **"Geavanceerd"** tab on `/zoeken` (`AdvancedQueryComponent`).
Backend: `IAdvancedQueryService` / `AdvancedQueryService`, exposed via `QueriesController`.

## Safety

The endpoint is **anonymous-facing**, so it never builds raw SQL. Field keys and
operators are whitelisted server-side and translated into safe EF Core LINQ. Anything
not on the whitelist is ignored or rejected. Aggregates are capped at 100 groups and
list output is paginated (default 50, max 200 per page).

## Endpoints

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `GET`  | `/api/queries/advanced/schema`  | anon | Whitelisted fields, operators & group-by options for the builder UI |
| `POST` | `/api/queries/advanced/execute` | anon | Execute one query (list or aggregate) |
| `POST` | `/api/queries/advanced/compare` | anon | Execute & combine N named queries |
| `GET/POST/DELETE` | `/api/queries/saved` | **authenticated** | Save / load / delete personal queries |
| `POST` | `/api/export/advanced-excel`    | anon | Excel export of an executed query |

## Filters (building blocks)

Each filter is one AND-combined block: `{ field, operator, value, value2, ...songSequence }`.

| Field | Type | Operators |
|-------|------|-----------|
| `date` | date | `between`, `before`, `after`, `eq` |
| `congregation` | congregation | `eq`, `in` |
| `city` | text | `eq`, `contains` |
| `denomination` | text | `eq`, `contains` |
| `preacher` | preacher | `eq` |
| `timeOfDay` | enum | `eq`, `in` |
| `bibleTranslation` | text | `eq`, `contains` |
| `specialOccasion` | text | `eq`, `contains` |
| `isReadingService` | bool | `isTrue`, `isFalse` |
| `beurtzang` | bool | `isTrue`, `isFalse` |
| `performer` | text | `eq`, `contains` (voorganger/ouderling/gemeentelid) |
| `sermonTheme` | text | `contains` |
| `sermonText` | text | `contains` |
| `songUsed` | song | `eq` (bundle + number) |
| `songComplete` | song | `eq` — service where the song (bundle + number) was sung **as a whole** |
| `songSequence` | songSequence | `seqBefore`, `seqAfter`, `seqDirectlyBefore`, `seqDirectlyAfter` |

The builder UI is **schema-driven**: it renders whatever `/api/queries/advanced/schema`
returns, so new whitelisted fields appear automatically. `beurtzang`/`songComplete`/
`performer`/`bibleTranslation`/`denomination` cover the per-reading translation,
kerkgenootschap and "sung as a whole" dimensions. Draft (`Concept`) services are always
excluded — advanced queries only see `Gepubliceerd` services.

### Song-completeness filter

`songComplete` keeps only services where every catalog verse of the given song was sung
(union across all onderdelen in that service), using `SongCompletenessCalculator`. In the
**natural-language** search this maps to the `song-completeness` template — e.g.
*"wanneer wordt Ps1773 93 volledig gezongen"* lists every service where Psalm 93 was sung
in full.

### Song-sequence operators

Song-sequence filters compare the order of two songs within a single service using
`ServiceElement.Position`. Because they depend on element ordering, they are evaluated
**in memory** after the EF-translatable filters have reduced the candidate set.
"Directly before/after" requires adjacent positions; "before/after" only requires the
relative order. Songs are matched by bundle (Value or Abbreviation, case-insensitive)
plus song number.

## Output modes

- **list** — paginated table of matching services (`ChartType = "table"`).
- **aggregate** — group-by + count, with a chart (`bar`/`line`/`pie`/`doughnut`).
  Group-by fields: congregation, city, denomination, preacher, timeOfDay,
  bibleTranslation, specialOccasion, year.

## Chart formatting

Count-based charts (aantal diensten, aantal keer gezongen, …) render **integer** ticks and
tooltips (1, 2, 3 — never 1.0/0.5/0.3). The chart component detects this automatically when
every data point is a whole number; averages and percentages keep their decimals. See
`ResultChartComponent`.

## Compare

`CompareAsync` combines multiple named queries:
- If all queries are aggregates sharing the same group-by, results are aligned into one
  multi-dataset chart over the union of group labels.
- Otherwise it falls back to comparing the **number of matching services** per query.
