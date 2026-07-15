# Release notes

## 2026-07-15 — Contactpagina, diensten-filters, voorganger-opschoning & onderdeel-dropdowns

### Changed
- **Contactpagina.** De sectie "Bijdragen" is verwijderd; de pagina nodigt nu enkel uit om
  contact op te nemen met de onderzoeker.
- **Diensten — filters.** De filtervelden staan nu horizontaal naast elkaar en breken alleen
  af naar een volgende regel wanneer ze niet passen (voorheen stonden ze onder elkaar).
- **Onderdeel toevoegen.** De **Onderdeel**-keuzelijst wordt strikt gefilterd op het gekozen
  **Type** (plus niet-geclassificeerde labels) en wordt automatisch ingevuld wanneer er
  precies één passend label is. De rij is netter uitgelijnd (vaste breedte voor *Type*,
  ruimere *Onderdeel*, flexibele *Opmerking*).

### Removed
- **Voorganger-prefix.** De ongebruikte prefix-kolom (`Preacher.Title`, bijv. *ds.*) is
  volledig verwijderd uit de applicatie en database (migratie `RemovePreacherTitle`). Deze
  werd nergens in de UI getoond of ingevuld.

## 2026-07-14 — Voorganger woonplaats & opslaan preektekst/schriftlezing

### Fixed
- **Preektekst werd niet opgeslagen.** Bij het bewerken van een dienst ging de preektekst
  (bijbelverwijzing) verloren zonder foutmelding. De oorzaak was een fout in de update van
  de dienst (`ServiceService.UpdateServiceAsync`) die een HTTP 500 gaf. Opgelost.
- **Schriftlezingen werden niet opgeslagen.** Dezelfde oorzaak: een dienst met zowel een
  preektekst als een schriftlezing sloeg beide niet op. Beide worden nu correct bewaard.
- **Stille opslagfouten.** Een mislukte automatische opslag werd niet getoond. Er verschijnt
  nu een duidelijke indicator ("Automatisch opslaan mislukt — sla handmatig op").

### Added
- **Woonplaats voorganger.** Bij het invoeren van een *nieuwe* voorganger kan nu een
  woonplaats worden opgegeven. Bij een bestaande voorganger wordt de woonplaats
  alleen-lezen getoond (wijzig deze via *Lijsten → Voorgangers*).
