# Data Entry — Diensten toevoegen

The **Dienst toevoegen** page (`add.component`) offers three ways to add a service. All
three converge on the same save path so the resulting data is identical and queryable.

## 1. Handmatig (manual)

- **Datum** uses a Dutch date picker (`nl-NL`, `dd-MM-yyyy`).
- **Orde van dienst** — each element's *Onderdeel* is chosen from a fixed dropdown of the
  33 liturgical labels (seeded as `LiturgicalLabels`, editable under *Lijsten*). The
  element is saved against its `LabelId`. Song elements additionally capture **bundle**,
  **nummer** and **verzen**.
- **Preektekst** — a structured editor: pick a Bible **book** (names resolve to the chosen
  *Bijbelvertaling* via `GET /api/bible/books?translation=...`), **chapter** and one or more
  **verses**. Multiple references are supported, and a free-text field covers cross-chapter
  cases such as *Genesis 1:laatste vers – Genesis 2:eerste vers*. References are stored as
  `SermonTextReference` rows (book ordinal + chapter + verse start/end) so they are
  queryable, with `Service.SermonText` kept as a raw fallback.

### Per-element details

Each onderdeel carries a **type** (Lied, Liturgische handeling, Schriftlezing, Gebed,
Overig). The editor adapts to the type:

- **Wie doet dit onderdeel** (performer) — on every non-song onderdeel you can pick who
  leads it (voorganger/ouderling/gemeentelid, seeded as the `ServicePerformer` list and
  editable under *Lijsten*). Stored on `ServiceElement.PerformerId`.
- **Beurtzang** — a checkbox on song onderdelen (`ServiceElement.IsBeurtzang`), queryable.
- **Schriftlezing** — a reading onderdeel holds **one Bijbelvertaling** (per reading, no
  longer service-wide) plus **one or more structured scripture references** (book +
  chapter + verse start/end), reusing the Preektekst editor pattern. Stored as
  `ReadingReference` rows on the element, with `ServiceElement.BibleTranslationId` for the
  translation.

### Volledig gezongen (completeness)

Whether a song is "sung as a whole" is **computed automatically** — never a manual flag —
by comparing the sung verse numbers against the song's catalog verse count
(`Song.NumberOfVerses`). Two states are surfaced with a **volledig** badge in the diensten
survey: complete **within one onderdeel** (e.g. `Ps1773 93:1,2,3,4`) and complete **across
the whole service** (e.g. `93:1,2` in one onderdeel and `93:3,4` in another). When the
catalog count is unknown the song is never counted as complete. See
`SongCompletenessCalculator`.

### Concept, autosave & publiceren

Editing works against a **server-side draft**. While the dialog is open, changes are
**autosaved** (debounced ~2s) as a `Concept`. Concept services show a *Concept* badge in the
diensten list, are visible to all signed-in users, and are **excluded from all queries and
statistics** until published. Use **Publiceren** to flip the status to `Gepubliceerd`. The
Save/Publiceren buttons disable while a request is in flight, preventing duplicate saves.

### Sjabloon toepassen (templates)

**Sjabloon toepassen** pre-fills the onderdelen from the best-matching template for the
chosen gemeente/kerkgenootschap, dagdeel and gelegenheid (Doop, Avondmaal, …). When you
already parsed or entered onderdelen, applying a template **reconciles** them into the
template scaffold: matching labels fill the template slots, empty slots stay, and extra
onderdelen are appended. Templates are managed on the **Sjablonen** page (see
[templates.md](./templates.md)).

## 2. Plakken & verwerken (paste)

Posts raw text to `POST /api/parse/liturgy` `{ text, title? }`. A **deterministic**
rule-based parser (`LiturgyParser`) returns structured `ParsedServiceData`; the Azure
OpenAI parser is only used as a fallback when configured. The parsed result pre-fills the
manual form for review before saving.

## 3. URL importeren (url)

Posts a URL to `POST /api/parse/url` `{ url }`. `UrlImportService` fetches the page
server-side, extracts `og:title` / `<title>` and the meta description and HTML-decodes them.

- **kerkdienstgemist.nl** gets a dedicated extractor (`UrlImportService.ParseKerkdienstgemist`).
  The page is a SPA (no liturgy in the static HTML), but the metadata sits in predictable
  places:
  - The **date & time-of-day** come from the recording id in the path, which encodes the
    start time as `{unixSeconds}{stationId:D5}` (e.g. `…/recording/178326870001060` →
    unix `1783268700` → `2026-07-05`, converted to Europe/Amsterdam).
  - The **church name** is the last ` - ` segment of `og:title`; it is split into
    *Gemeente* (all but the last word) and *Plaats* (last word).
  - The **preacher** is the first ` - ` segment when it starts with `ds.`/`drs.`/… (else the
    first segment is a service type such as *Avonddienst* → Evening); with 3+ segments the
    middle segment is the **sermon theme**, and a scripture reference in the description
    (e.g. `Handelingen 9: 1-22`) becomes the **preektekst**.
- **Other sites** fall back to the generic `LiturgyParser` over the title + meta description.

The broadcast URL is stored on the service.

## Parser rules (summary)

- **Labels** — keyword → label, first match wins (e.g. `votum` → *Votum*, `schriftlezing`
  → *Schriftlezing(en)*, `tekst`/`preektekst` → *Preektekst*).
- **Song references** — `Ps. 32 : 1 en 3`, `LvdK 91: 1, 2, 4`, `Opw. 220 : 1-3` →
  bundle + number + verses. Verses split on `,` / `en`; ranges (`1-3`) are expanded.
- **Bundle abbreviations** — `Ps./Psalm/Gez. → Ps1773`, `LvdK/Lied`, `Opw.`, `WK`,
  `WKPs`, `GK`.
- **Schriftlezing** → reading element with the reference in *Notes*.
- **Preektekst** → sets `SermonText` and adds a *Preektekst* element.
- **Heuristics** — the first/last otherwise-unlabeled song become *Openingslied* /
  *Slotlied*.
- **Title** — splits on ` - ` to derive preacher (`ds.`/`drs.`/…), congregation, Dutch
  date (`27 juni`) and time-of-day (`<12` Morning, `<17` Afternoon, else Evening).
- Whitespace is collapsed so values like `HC zondag  40` normalise cleanly.

## Saving — congregation & preacher resolution

The save path is shared by all three flows. **Gemeente** (name, with autocomplete) and
**Plaats** (city) are separate fields; the preacher is a single autocomplete field. When a
value is selected from the autocomplete its id is used directly; editing either the name or
the city clears that id so it is re-resolved on save. On save, an existing congregation is
matched **case-insensitively on both name and city** (so "Hervormde gemeente" in Randwijk
and in Ederveen stay distinct), otherwise a new `Congregation` / `Preacher` is created
automatically (`POST /api/congregations`, `POST /api/preachers`) and its id is used. An
empty city falls back to `Onbekend`. This keeps the URL-import flow working without manual
pre-registration; new records can be edited or de-duplicated afterwards under *Lijsten*.

## Related endpoints

| Endpoint | Purpose |
| --- | --- |
| `GET /api/bible/books?translation=` | 66 canonical books + versification, names per translation |
| `POST /api/parse/liturgy` | Parse pasted text |
| `POST /api/parse/url` | Fetch + parse a URL |
| `GET/PUT /api/usersettings` | Per-user settings JSON blob (`[Authorize]`) |
| `GET/POST/DELETE /api/recent-searches` | Last 10 NL searches per user (`[Authorize]`) |
