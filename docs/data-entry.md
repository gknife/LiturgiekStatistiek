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

## 2. Plakken & verwerken (paste)

Posts raw text to `POST /api/parse/liturgy` `{ text, title? }`. A **deterministic**
rule-based parser (`LiturgyParser`) returns structured `ParsedServiceData`; the Azure
OpenAI parser is only used as a fallback when configured. The parsed result pre-fills the
manual form for review before saving.

## 3. URL importeren (url)

Posts a URL to `POST /api/parse/url` `{ url }`. `UrlImportService` fetches the page
server-side, extracts `og:title` / `<title>` and the meta description, HTML-decodes them
and feeds them to the same `LiturgyParser`. The broadcast URL is stored on the service.
Verified against kerkdienstgemist.nl, whose liturgy lives in the meta description and
title (no JavaScript required).

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

The save path is shared by all three flows. When the **Gemeente** or **Voorganger**
field holds a name that was selected from the autocomplete, its id is used directly.
When the name was typed manually or supplied by the paste/URL parser and does not yet
exist, it is resolved on save: an existing match (case-insensitive) is reused, otherwise
a new `Congregation` / `Preacher` is created automatically (`POST /api/congregations`,
`POST /api/preachers`) and its id is used. A parsed city fills the required *City* field;
when none is available (e.g. kerkdienstgemist titles without a city) it falls back to
`Onbekend`. This keeps the URL-import flow working without manual pre-registration; new
records can be edited or de-duplicated afterwards under *Lijsten*.

## Related endpoints

| Endpoint | Purpose |
| --- | --- |
| `GET /api/bible/books?translation=` | 66 canonical books + versification, names per translation |
| `POST /api/parse/liturgy` | Parse pasted text |
| `POST /api/parse/url` | Fetch + parse a URL |
| `GET/PUT /api/usersettings` | Per-user settings JSON blob (`[Authorize]`) |
| `GET/POST/DELETE /api/recent-searches` | Last 10 NL searches per user (`[Authorize]`) |
