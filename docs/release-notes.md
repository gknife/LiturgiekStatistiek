# Release notes

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
