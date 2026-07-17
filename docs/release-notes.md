# Release notes

## 2026-07-17 — Rubrieken, voorzang, leesdienst & dienst-dialoog

### Added
- **Rubrieken per liedbundel.** De **Liedcatalogus** heeft een beheerpaneel voor de
  categorieën/rubrieken van een bundel (toevoegen, hernoemen, verwijderen, sorteren). Eén
  rubriek kan als **standaard** worden gemarkeerd; die wordt voorgevuld in de rubriek-keuze
  bij het toevoegen van een lied én bij een lied-onderdeel in een dienst. Hernoemen cascadeert
  naar bestaande liederen en dienst-liedverwijzingen in die bundel.
- **Genoemde coupletten (Voorzang).** Een lied kan een gelabeld couplet vóór couplet 1 hebben
  (bijv. de *Voorzang* van Psalm 18 in de bundel 1773). Gelabelde coupletten tellen niet mee in
  het aantal coupletten (`Song.NumberOfVerses`) en worden op volgorde (`SortOrder`) getoond.
- **Standaard datum.** De datum van een nieuwe dienst staat standaard op **vandaag**.

### Changed
- **Leesdienst zonder voorganger.** Bij een **Leesdienst** wordt het veld *Voorganger*
  uitgeschakeld en leeggemaakt; de serverlaag garandeert dat een leesdienst nooit een
  voorganger houdt (zowel bij aanmaken als bijwerken). In het diensten-overzicht toont de
  kolom *Voorganger* een **Leesdienst**-badge.
- **Standaard liedbundel uit sjabloon.** De standaard liedbundel van een sjabloon wordt nu ook
  daadwerkelijk voorgevuld wanneer je in de dienst-popup een lied toevoegt.
- **Dienst-dialoog opgeschoond.** Kop (titel + automatisch-opslaan-indicator) en voettoetsen
  (Terug/Volgende/Publiceren) blijven altijd zichtbaar; alleen de onderdelen scrollen, met de
  scrollbalk binnen de afgeronde hoeken. Publiceren/navigeren kan vanuit elke tab. De losse
  *Sluiten*-knop en de dubbele "laatst opgeslagen"-melding onderaan zijn verwijderd; sluiten
  gaat via het kruisje rechtsboven. De alleen-lezen velden (kerkgenootschap, plaats, titel,
  woonplaats) worden als tekst getoond in plaats van als invoerveld, en de
  kerkgenootschap-notitie overlapt de muzikale begeleiding niet meer.
- **Sjabloon toepassen alleen bij nieuw.** *Sjabloon toepassen* is enkel beschikbaar tijdens
  het aanmaken van een dienst, niet meer bij het bewerken van een bestaande dienst.

### Fixed
- **Preektekst in "Controleren & opslaan".** Een via de dropdowns ingestelde preektekst was
  niet zichtbaar op het controleren-tabblad; dit wordt nu correct getoond.
- **Vastlopen bij "Vergelijk kerkgenootschappen".** Het openen van dit zoek-sjabloon liet de
  browser vastlopen door een oneindige change-detection-lus op de meervoudige keuzelijst
  (`selectedDenominations` gaf elke cyclus een nieuwe array terug). De referentie wordt nu
  gecachet zolang de selectie ongewijzigd is.


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
