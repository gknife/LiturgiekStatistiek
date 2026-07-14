import { Component, OnInit, Optional, Inject, HostBinding, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTabsModule } from '@angular/material/tabs';
import { MatStepperModule } from '@angular/material/stepper';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import {
  CongregationSummary, PreacherSummary, ListItem, ServiceDetail,
  BibleBook, ParsedServiceData, ServiceTemplateSummary, ServiceTemplate,
} from '../../core/models/api.models';
import { debounceTime, switchMap, of, map, throwError, Observable, forkJoin, tap, catchError } from 'rxjs';
import { FormControl } from '@angular/forms';

export interface AddDialogData {
  serviceId?: string;
  duplicateFromId?: string;
}

interface ElementSong {
  bundleId: string;
  section: string;
  number: number | null;
  versesText: string;
  sungInFull: boolean;
  /** Catalog verse count for bundle+number, fetched lazily for the live badge. */
  catalogVerseCount?: number | null;
}

interface ReadingRefModel {
  bibleBookId: string;
  chapter: number | null;
  verseStart: number | null;
  verseEnd: number | null;
}

interface ServiceElementModel {
  position: number;
  elementType: number;
  labelId: string;
  notes: string;
  songs: ElementSong[];
  performerId: string;
  isBeurtzang: boolean;
  bibleTranslationId: string;
  readingRefs: ReadingRefModel[];
}

interface SermonRefModel {
  bibleBookId: string;
  chapter: number | null;
  verseStart: number | null;
  verseEnd: number | null;
}

@Component({
  selector: 'app-add',
  standalone: true,
  imports: [
    DatePipe,
    MatCardModule, MatTabsModule, MatStepperModule, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatDatepickerModule, MatNativeDateModule,
    MatAutocompleteModule, MatButtonModule, MatIconModule, MatCheckboxModule,
    MatChipsModule, MatSlideToggleModule, MatTooltipModule, FormsModule, ReactiveFormsModule,
  ],
  templateUrl: './add.component.html',
  styleUrl: './add.component.scss',
})
export class AddComponent implements OnInit {
  // Form
  metadataForm!: FormGroup;
  congregationControl = new FormControl('');
  congregationCityControl = new FormControl('');
  preacherControl = new FormControl('');
  preacherCityControl = new FormControl('');

  // Autocomplete
  congregationSuggestions: CongregationSummary[] = [];
  preacherSuggestions: PreacherSummary[] = [];

  // Lists
  timeOfDayOptions = [
    { value: 0, label: 'Ochtend' },
    { value: 1, label: 'Middag' },
    { value: 2, label: 'Avond' },
  ];
  bundles: ListItem[] = [];
  specialOccasions: ListItem[] = [];
  bibleTranslations: ListItem[] = [];
  churchCalendarSundays: ListItem[] = [];
  musicalAccompaniments: ListItem[] = [];
  liturgicalLabels: ListItem[] = [];
  performers: ListItem[] = [];
  serviceOccasions: ListItem[] = [];
  denominations: ListItem[] = [];

  // Defaults carried over from the chosen template, applied to onderdelen added
  // later (manually or via paste/URL import), not just the initial scaffold.
  private templateDefaultTranslationId = '';
  private templateDefaultBundleId = '';

  // Cache of catalog verse counts keyed by "bundleId|number" for the live badge.
  private catalogVerseCountCache = new Map<string, number | null>();

  // Element type ids (mirror the backend ElementType enum).
  readonly ELEMENT_SONG = 0;
  readonly ELEMENT_READING = 2;

  // Draft/publish + save-guard state.
  saving = false;
  publishing = false;
  autoSaving = false;
  /** Set when the last autosave failed, so the UI can surface it instead of losing data silently. */
  autosaveFailed = false;
  status = 1; // 0 = Concept, 1 = Gepubliceerd
  private autosaveTimer: ReturnType<typeof setTimeout> | null = null;
  private lastSavedSnapshot = '';
  /** Timestamp of the last successful (auto)save, shown per tab. */
  lastSavedAt: Date | null = null;

  // Bible reference data for the structured Preektekst editor.
  bibleBooks: BibleBook[] = [];
  sermonRefs: SermonRefModel[] = [];

  // Paste-to-parse
  pasteText = '';
  parseMessage = '';
  parsing = false;

  // URL import
  importUrl = '';
  importMessage = '';
  importing = false;

  // Tab control (so parse/import can jump to the manual review tab).
  selectedTabIndex = 0;

  elements: ServiceElementModel[] = [];

  editingServiceId: string | null = null;

  // Template-first flow (add mode only): the user picks a sjabloon (or "leeg")
  // before entering data on any of the tabs.
  availableTemplates = signal<ServiceTemplateSummary[]>([]);
  templateChosen = signal(false);
  chosenTemplateName = signal<string | null>(null);

  get isEditMode(): boolean {
    return this.editingServiceId !== null;
  }

  get isInDialog(): boolean {
    return this.dialogRef !== null;
  }

  @HostBinding('class.in-dialog') get inDialogClass(): boolean {
    return this.isInDialog;
  }

  constructor(
    private fb: FormBuilder,
    private api: ApiService,
    @Optional() private dialogRef: MatDialogRef<AddComponent> | null,
    @Optional() @Inject(MAT_DIALOG_DATA) private dialogData: AddDialogData | null,
  ) {}

  ngOnInit(): void {
    this.metadataForm = this.fb.group({
      date: [null, Validators.required],
      timeOfDay: [0, Validators.required],
      congregationId: [''],
      preacherId: [''],
      denominationId: [''],
      churchCalendarSundayId: [''],
      musicalAccompanimentId: [''],
      specialOccasionId: [''],
      isReadingService: [false],
      broadcastUrl: [''],
      sermonText: [''],
      sermonTheme: [''],
      hasBeamerLiturgy: [false],
      hasBeamerTexts: [false],
      hasBeamerSongs: [false],
    });

    // Edit mode: capture the id synchronously so the dialog title and the
    // buildRequest path are correct even if a list load fails.
    if (this.dialogData?.serviceId) {
      this.editingServiceId = this.dialogData.serviceId;
    }

    // Editing an existing service skips the template-first step. Duplicating a
    // service also skips it (the source dienst acts as the template). For a new
    // service the user must first choose a sjabloon (or "leeg").
    if (this.isEditMode || this.dialogData?.duplicateFromId) {
      this.templateChosen.set(true);
    } else {
      this.api.getTemplates().subscribe({
        next: templates => this.availableTemplates.set(templates.filter(t => t.isActive)),
        error: () => this.availableTemplates.set([]),
      });
    }

    // Load dropdown lists. Each list is loaded resiliently: a missing list
    // (404) must NOT blank every other dropdown or block edit-prefill, so each
    // call falls back to an empty item list instead of failing the whole join.
    forkJoin({
      bundles: this.loadListItems('SongBundles'),
      specialOccasions: this.loadListItems('SpecialOccasions'),
      bibleTranslations: this.loadListItems('BibleTranslations'),
      churchCalendarSundays: this.loadListItems('ChurchCalendarSundays'),
      musicalAccompaniments: this.loadListItems('MusicalAccompaniment'),
      liturgicalLabels: this.loadListItems('LiturgicalLabels'),
      performers: this.loadListItems('ServicePerformer'),
      serviceOccasions: this.loadListItems('ServiceOccasion'),
      denominations: this.loadListItems('Denominations'),
    }).subscribe(lists => {
      this.bundles = lists.bundles;
      this.specialOccasions = lists.specialOccasions;
      this.bibleTranslations = lists.bibleTranslations;
      this.churchCalendarSundays = lists.churchCalendarSundays;
      this.musicalAccompaniments = lists.musicalAccompaniments;
      this.liturgicalLabels = lists.liturgicalLabels;
      this.performers = lists.performers;
      this.serviceOccasions = lists.serviceOccasions;
      this.denominations = lists.denominations;

      this.loadBibleBooks();

      // Edit mode: load the existing service and prefill the form once the lists
      // needed to resolve labels/bundles/books are present.
      if (this.editingServiceId) {
        this.api.getService(this.editingServiceId).subscribe(service => this.prefill(service));
      } else if (this.dialogData?.duplicateFromId) {
        // Duplicate: prefill from the source dienst but keep it a new concept
        // (no editingServiceId), clear the date and force Concept status.
        this.api.getService(this.dialogData.duplicateFromId).subscribe(service => {
          this.prefill(service);
          this.status = 0;
          this.metadataForm.patchValue({ date: null }, { emitEvent: false });
          this.lastSavedSnapshot = this.snapshot();
        });
      }
    });

    // Autocomplete for congregation (search on the name field).
    this.congregationControl.valueChanges.pipe(
      debounceTime(300),
      switchMap(val => val && val.length > 1 ? this.api.searchCongregations(val) : of([]))
    ).subscribe(results => this.congregationSuggestions = results);

    // Editing either the name or the city means the previously resolved congregation
    // id is stale, so clear it and let it be re-resolved (name + city) on save. These
    // fire only on user input; programmatic prefill/select use { emitEvent: false }.
    this.congregationControl.valueChanges.subscribe(() => {
      this.metadataForm.patchValue({ congregationId: '' }, { emitEvent: false });
      // Typing a (new) gemeente name re-enables the kerkgenootschap dropdown.
      this.metadataForm.get('denominationId')?.enable({ emitEvent: false });
      this.scheduleAutosave();
    });
    this.congregationCityControl.valueChanges.subscribe(() => {
      this.metadataForm.patchValue({ congregationId: '' }, { emitEvent: false });
      this.metadataForm.get('denominationId')?.enable({ emitEvent: false });
      this.scheduleAutosave();
    });

    // Autocomplete for preacher
    this.preacherControl.valueChanges.pipe(
      debounceTime(300),
      switchMap(val => val && val.length > 1 ? this.api.searchPreachers(val) : of([]))
    ).subscribe(results => this.preacherSuggestions = results);

    this.preacherControl.valueChanges.subscribe(() => {
      this.metadataForm.patchValue({ preacherId: '' }, { emitEvent: false });
      // Typing a (new) voorganger name means we're entering a new preacher, so
      // the woonplaats becomes editable again.
      this.preacherCityControl.enable({ emitEvent: false });
      this.scheduleAutosave();
    });

    this.preacherCityControl.valueChanges.subscribe(() => this.scheduleAutosave());

    // Autosave: debounced draft save whenever the metadata form changes.
    this.metadataForm.valueChanges.subscribe(() => this.scheduleAutosave());
  }

  /** Load a list's items, tolerating a missing list (404) by returning []. */
  private loadListItems(name: string): Observable<ListItem[]> {
    return this.api.getListByName(name).pipe(
      map(def => def.items),
      catchError(() => of<ListItem[]>([])),
    );
  }

  private loadBibleBooks(): void {
    // Book/chapter/verse structure is translation-independent for the structured
    // editor; per-reading translation is captured separately on each reading onderdeel.
    this.api.getBibleBooks().subscribe(books => this.bibleBooks = books);
  }

  chaptersOf(bookId: string): number[] {
    const book = this.bibleBooks.find(b => b.id === bookId);
    if (!book) return [];
    return Array.from({ length: book.chapterCount }, (_, i) => i + 1);
  }

  versesOf(bookId: string, chapter: number | null): number[] {
    const book = this.bibleBooks.find(b => b.id === bookId);
    if (!book || !chapter || chapter < 1 || chapter > book.verseCounts.length) return [];
    return Array.from({ length: book.verseCounts[chapter - 1] }, (_, i) => i + 1);
  }

  addSermonRef(): void {
    this.sermonRefs.push({ bibleBookId: '', chapter: null, verseStart: null, verseEnd: null });
    this.scheduleAutosave();
  }

  removeSermonRef(index: number): void {
    this.sermonRefs.splice(index, 1);
    this.scheduleAutosave();
  }

  private prefill(service: ServiceDetail): void {
    this.status = service.statusValue ?? 1;
    this.metadataForm.patchValue({
      date: service.date ? new Date(service.date) : null,
      timeOfDay: service.timeOfDayValue,
      congregationId: service.congregation.id,
      preacherId: service.preacher?.id ?? '',
      churchCalendarSundayId: service.churchCalendarSundayId ?? '',
      musicalAccompanimentId: service.musicalAccompanimentId ?? '',
      specialOccasionId: service.specialOccasionId ?? '',
      isReadingService: service.isReadingService,
      broadcastUrl: service.broadcastUrl ?? '',
      sermonText: service.sermonText ?? '',
      sermonTheme: service.sermonTheme ?? '',
      hasBeamerLiturgy: service.hasBeamerLiturgy,
      hasBeamerTexts: service.hasBeamerTexts,
      hasBeamerSongs: service.hasBeamerSongs,
    });

    this.congregationControl.setValue(service.congregation.name, { emitEvent: false });
    this.congregationCityControl.setValue(service.congregation.city, { emitEvent: false });
    // Existing gemeente: reflect its kerkgenootschap (by abbreviation) and lock the field.
    const denom = this.denominations.find(d => d.abbreviation === service.congregation.denominationAbbreviation);
    this.metadataForm.patchValue({ denominationId: denom?.id ?? '' }, { emitEvent: false });
    this.metadataForm.get('denominationId')?.disable({ emitEvent: false });
    if (service.preacher) {
      this.preacherControl.setValue(service.preacher.fullName, { emitEvent: false });
      this.preacherCityControl.setValue(service.preacher.city ?? '', { emitEvent: false });
      this.preacherCityControl.disable({ emitEvent: false });
    }

    this.elements = service.elements
      .sort((a, b) => a.position - b.position)
      .map(e => ({
        position: e.position,
        elementType: this.elementTypeValue(e.elementType),
        labelId: e.labelId ?? this.labelIdByValue(e.label),
        notes: e.notes ?? '',
        performerId: e.performerId ?? '',
        isBeurtzang: e.isBeurtzang ?? false,
        bibleTranslationId: e.bibleTranslationId ?? '',
        readingRefs: (e.readingReferences ?? []).map(r => ({
          bibleBookId: r.bibleBookId ?? '',
          chapter: r.chapter,
          verseStart: r.verseStart,
          verseEnd: r.verseEnd,
        })),
        songs: e.songs.map(s => ({
          bundleId: s.bundleId ?? this.bundleIdByAbbrev(s.bundleAbbreviation, s.bundleName),
          section: s.section,
          number: s.songNumber,
          versesText: s.verses.join(', '),
          sungInFull: s.sungInFull ?? false,
        })),
      }));

    this.sermonRefs = (service.sermonTextReferences ?? []).map(r => ({
      bibleBookId: r.bibleBookId ?? '',
      chapter: r.chapter,
      verseStart: r.verseStart,
      verseEnd: r.verseEnd,
    }));

    this.refreshAllCatalogCounts();
    this.lastSavedSnapshot = this.snapshot();
  }

  /** Load catalog verse counts for every song currently in the form (for the badge). */
  private refreshAllCatalogCounts(): void {
    for (const el of this.elements) {
      for (const song of el.songs) this.updateCatalogVerseCount(song);
    }
  }

  /** Map the backend ElementType string name to its numeric value. */
  private elementTypeValue(name: string): number {
    switch (name) {
      case 'Song': return 0;
      case 'LiturgicalAct': return 1;
      case 'Reading': return 2;
      case 'Prayer': return 3;
      case 'Other': return 4;
      default: return 0;
    }
  }

  private labelIdByValue(value: string | null): string {
    if (!value) return '';
    return this.liturgicalLabels.find(l => l.value === value)?.id ?? '';
  }

  private elementTypeForLabel(label: string | null): number {
    const l = (label ?? '').toLowerCase();
    if (l.includes('schriftlezing') || l.includes('lezing')) return 2; // Reading
    if (l.includes('gebed')) return 3; // Prayer
    if (l.includes('lied') || l.includes('zang') || l.includes('psalm')) return 0; // Song
    if (
      l.includes('votum') || l.includes('groet') || l.includes('zegen') || l.includes('collecte') ||
      l.includes('vermaan') || l.includes('belijd') || l.includes('mededeling') || l.includes('muziek') ||
      l.includes('kindermoment')
    ) {
      return 1; // LiturgicalAct
    }
    return 4; // Other
  }

  private bundleIdByAbbrev(abbrev: string | null, name?: string): string {
    return this.bundles.find(b => b.abbreviation === abbrev)?.id
      ?? this.bundles.find(b => b.value === name)?.id
      ?? '';
  }

  selectCongregation(congregation: CongregationSummary): void {
    this.congregationControl.setValue(congregation.name, { emitEvent: false });
    this.congregationCityControl.setValue(congregation.city, { emitEvent: false });
    this.metadataForm.patchValue({ congregationId: congregation.id });
    // Reflect the existing gemeente's kerkgenootschap (matched by abbreviation) for
    // display; it is disabled and ignored on save for an existing gemeente.
    const denom = this.denominations.find(d => d.abbreviation === congregation.denominationAbbreviation);
    this.metadataForm.patchValue({ denominationId: denom?.id ?? '' }, { emitEvent: false });
    this.metadataForm.get('denominationId')?.disable({ emitEvent: false });
  }

  /**
   * The kerkgenootschap dropdown is only editable when creating a NEW gemeente (no
   * existing congregation resolved yet); for an existing gemeente it is read-only.
   */
  get isNewCongregation(): boolean {
    return !this.nullableGuid(this.metadataForm?.value.congregationId);
  }

  selectPreacher(preacher: PreacherSummary): void {
    this.preacherControl.setValue(preacher.fullName, { emitEvent: false });
    this.metadataForm.patchValue({ preacherId: preacher.id });
    // Existing voorganger: show its woonplaats read-only (edit it via beheer).
    this.preacherCityControl.setValue(preacher.city ?? '', { emitEvent: false });
    this.preacherCityControl.disable({ emitEvent: false });
  }

  addElement(): void {
    this.elements.push({
      position: this.elements.length + 1,
      elementType: 0,
      labelId: '',
      songs: [],
      notes: '',
      performerId: '',
      isBeurtzang: false,
      bibleTranslationId: this.templateDefaultTranslationId,
      readingRefs: [],
    });
    this.scheduleAutosave();
  }

  removeElement(index: number): void {
    this.elements.splice(index, 1);
    this.elements.forEach((el, i) => el.position = i + 1);
    this.scheduleAutosave();
  }

  /** Move an onderdeel up or down; positions are renumbered. */
  moveElement(index: number, direction: -1 | 1): void {
    const target = index + direction;
    if (target < 0 || target >= this.elements.length) return;
    const [item] = this.elements.splice(index, 1);
    this.elements.splice(target, 0, item);
    this.elements.forEach((el, i) => el.position = i + 1);
    this.scheduleAutosave();
  }

  isSongElement(el: ServiceElementModel): boolean {
    return el.elementType === this.ELEMENT_SONG;
  }

  isReadingElement(el: ServiceElementModel): boolean {
    return el.elementType === this.ELEMENT_READING;
  }

  /**
   * Onderdeel options filtered by the element's selected Type. Labels without a
   * classification are always shown so nothing is hidden unexpectedly.
   */
  labelsForType(elementType: number | null | undefined): ListItem[] {
    if (elementType === null || elementType === undefined) return this.liturgicalLabels;
    return this.liturgicalLabels.filter(
      l => l.liturgicalElementType === elementType ||
        l.liturgicalElementType === null ||
        l.liturgicalElementType === undefined,
    );
  }

  /** When the Type changes, clear a selected label that no longer matches. */
  onElementTypeChange(element: ServiceElementModel): void {
    if (element.labelId) {
      const label = this.liturgicalLabels.find(l => l.id === element.labelId);
      const type = label?.liturgicalElementType;
      if (label && type !== null && type !== undefined && type !== element.elementType) {
        element.labelId = '';
      }
    }
    // Default the Bijbelvertaling on a reading onderdeel when none is set yet.
    if (this.isReadingElement(element) && !element.bibleTranslationId) {
      element.bibleTranslationId = this.templateDefaultTranslationId;
    }
    this.scheduleAutosave();
  }

  addReadingRef(element: ServiceElementModel): void {
    element.readingRefs.push({ bibleBookId: '', chapter: null, verseStart: null, verseEnd: null });
    this.scheduleAutosave();
  }

  removeReadingRef(element: ServiceElementModel, index: number): void {
    element.readingRefs.splice(index, 1);
    this.scheduleAutosave();
  }

  addSong(element: ServiceElementModel): void {
    element.songs.push({
      bundleId: this.templateDefaultBundleId,
      section: '',
      number: null,
      versesText: '',
      sungInFull: false,
    });
    this.scheduleAutosave();
  }

  removeSong(element: ServiceElementModel, index: number): void {
    element.songs.splice(index, 1);
    this.scheduleAutosave();
  }

  /**
   * Called when a song's bundle or number changes: refresh its cached catalog verse
   * count (for the live "volledig" badge and hele-lied auto-fill) and autosave.
   */
  onSongIdentityChange(song: ElementSong): void {
    this.updateCatalogVerseCount(song);
    this.scheduleAutosave();
  }

  /** Lazily fetch (and cache) the catalog verse count for a song's bundle+number. */
  private updateCatalogVerseCount(song: ElementSong): void {
    if (!song.bundleId || song.number == null) {
      song.catalogVerseCount = null;
      return;
    }
    const key = `${song.bundleId}|${song.number}`;
    if (this.catalogVerseCountCache.has(key)) {
      song.catalogVerseCount = this.catalogVerseCountCache.get(key) ?? null;
      this.autofillFullVerses(song);
      return;
    }
    this.api.getSongByNumber(song.bundleId, song.number).subscribe({
      next: s => {
        const n = s?.numberOfVerses ?? null;
        this.catalogVerseCountCache.set(key, n);
        song.catalogVerseCount = n;
        this.autofillFullVerses(song);
      },
      error: () => {
        this.catalogVerseCountCache.set(key, null);
        song.catalogVerseCount = null;
      },
    });
  }

  /** Toggle handler for the "Hele lied / alle verzen" checkbox. */
  onSungInFullChange(song: ElementSong): void {
    if (song.sungInFull) {
      this.updateCatalogVerseCount(song);
      this.autofillFullVerses(song);
    }
    this.scheduleAutosave();
  }

  /** When "hele lied" is set and the catalog count is known, fill verses "1-N". */
  private autofillFullVerses(song: ElementSong): void {
    if (song.sungInFull && song.catalogVerseCount && song.catalogVerseCount > 0) {
      song.versesText = song.catalogVerseCount === 1 ? '1' : `1-${song.catalogVerseCount}`;
    }
  }

  /** The verses field is locked when "hele lied" is on and the catalog count is known. */
  isVersesLocked(song: ElementSong): boolean {
    return song.sungInFull && !!song.catalogVerseCount && song.catalogVerseCount > 0;
  }

  /** Normalize the verses text to the canonical form (e.g. "1, 3, 5-7") on blur. */
  normalizeVersesText(song: ElementSong): void {
    const normalized = AddComponent.canonicalizeVerses(song.versesText);
    if (normalized !== song.versesText) {
      song.versesText = normalized;
      this.scheduleAutosave();
    }
  }

  /** True when the verses text contains tokens that aren't a number or a range. */
  hasInvalidVerses(song: ElementSong): boolean {
    return AddComponent.hasInvalidVerseTokens(song.versesText);
  }

  /**
   * Canonical verse format: comma+space separated items, hyphen (no spaces) for ranges,
   * original order preserved. Invalid tokens are kept verbatim so the user can fix them.
   */
  static canonicalizeVerses(text: string): string {
    if (!text) return '';
    return text
      .split(',')
      .map(t => t.trim())
      .filter(t => t.length > 0)
      .map(t => {
        const range = t.match(/^(\d+)\s*[-–]\s*(\d+)$/);
        if (range) return `${range[1]}-${range[2]}`;
        return t;
      })
      .join(', ');
  }

  private static hasInvalidVerseTokens(text: string): boolean {
    if (!text || !text.trim()) return false;
    return text
      .split(',')
      .map(t => t.trim())
      .filter(t => t.length > 0)
      .some(t => !/^\d+$/.test(t) && !/^\d+\s*[-–]\s*\d+$/.test(t));
  }

  /** Parse a verses text into the set of verse numbers (expanding ranges). */
  private verseNumbersOf(text: string): Set<number> {
    const result = new Set<number>();
    for (const raw of (text || '').split(',')) {
      const t = raw.trim();
      if (!t) continue;
      const range = t.match(/^(\d+)\s*[-–]\s*(\d+)$/);
      if (range) {
        const start = parseInt(range[1], 10);
        const end = parseInt(range[2], 10);
        if (start <= end && end - start < 500) {
          for (let v = start; v <= end; v++) result.add(v);
        }
        continue;
      }
      const single = t.match(/^\d+$/);
      if (single) result.add(parseInt(t, 10));
    }
    return result;
  }

  private sameSongKey(a: ElementSong, b: ElementSong): boolean {
    return a.bundleId === b.bundleId && (a.section || '') === (b.section || '') && a.number === b.number;
  }

  /**
   * Live "volledig" indicator mirroring the backend calculator: complete when the
   * song is explicitly marked "hele lied", or when the catalog verse count is known
   * and every verse is covered within this onderdeel or across the whole dienst.
   */
  isSongComplete(song: ElementSong): boolean {
    if (!song.bundleId || song.number == null) return false;

    // Explicit "hele lied" on any entry of the same song counts as complete.
    const sameKey = this.elements.flatMap(e => e.songs).filter(s => this.sameSongKey(s, song));
    if (sameKey.some(s => s.sungInFull)) return true;

    const n = song.catalogVerseCount;
    if (!n || n <= 0) return false;

    const full: number[] = Array.from({ length: n }, (_, i) => i + 1);
    const serviceVerses = new Set<number>();
    for (const s of sameKey) {
      for (const v of this.verseNumbersOf(s.versesText)) serviceVerses.add(v);
    }
    return full.every(v => serviceVerses.has(v));
  }

  submitPaste(): void {
    this.parsing = true;
    this.parseMessage = '';
    this.api.parseLiturgy(this.pasteText).subscribe({
      next: res => {
        this.parsing = false;
        if (res.success && res.data) {
          this.applyParsed(res.data);
          this.parseMessage = `Verwerkt: ${res.data.elements.length} onderdelen. Controleer en sla op in het tabblad "Handmatig invoeren".`;
          this.selectedTabIndex = 0;
        } else {
          this.parseMessage = res.errorMessage ?? 'Kon de tekst niet verwerken.';
        }
      },
      error: err => {
        this.parsing = false;
        this.parseMessage = 'Fout bij verwerken: ' + (err.message ?? err);
      },
    });
  }

  submitUrl(): void {
    this.importing = true;
    this.importMessage = '';
    this.api.importLiturgyUrl(this.importUrl).subscribe({
      next: res => {
        this.importing = false;
        if (res.success && res.data) {
          this.applyParsed(res.data);
          this.importMessage = `Geïmporteerd: ${res.data.elements.length} onderdelen. Controleer en sla op in het tabblad "Handmatig invoeren".`;
          this.selectedTabIndex = 0;
        } else {
          this.importMessage = res.errorMessage ?? 'Kon de liturgie niet importeren.';
        }
      },
      error: err => {
        this.importing = false;
        this.importMessage = 'Fout bij importeren: ' + (err.message ?? err);
      },
    });
  }

  /** Map a parsed liturgy onto the manual form for review. */
  private applyParsed(data: ParsedServiceData): void {
    const timeMap: Record<string, number> = { Morning: 0, Afternoon: 1, Evening: 2 };

    this.metadataForm.patchValue({
      date: data.date ? new Date(data.date) : this.metadataForm.value.date,
      timeOfDay: data.timeOfDay && timeMap[data.timeOfDay] !== undefined ? timeMap[data.timeOfDay] : this.metadataForm.value.timeOfDay,
      sermonText: data.sermonText ?? this.metadataForm.value.sermonText,
      sermonTheme: data.sermonTheme ?? this.metadataForm.value.sermonTheme,
      broadcastUrl: data.broadcastUrl ?? this.metadataForm.value.broadcastUrl,
    });

    if (data.congregation) this.congregationControl.setValue(data.congregation);
    if (data.city) this.congregationCityControl.setValue(data.city);
    if (data.preacher) this.preacherControl.setValue(data.preacher);

    this.elements = data.elements.map((e, i) => ({
      position: e.position || i + 1,
      elementType: e.songNumber != null ? this.ELEMENT_SONG : this.elementTypeForLabel(e.label),
      labelId: this.labelIdByValue(e.label),
      notes: e.notes ?? '',
      performerId: '',
      isBeurtzang: false,
      bibleTranslationId:
        (e.songNumber == null && this.elementTypeForLabel(e.label) === this.ELEMENT_READING)
          ? this.templateDefaultTranslationId
          : '',
      readingRefs: [],
      songs: e.songNumber != null
        ? [{
            bundleId: this.bundleIdByAbbrev(e.songBundle) || this.templateDefaultBundleId,
            section: '',
            number: e.songNumber,
            versesText: (e.verses ?? []).join(', '),
            sungInFull: false,
          }]
        : [],
    }));
    this.refreshAllCatalogCounts();
  }

  /** Convert a Date (or ISO string) to a 'yyyy-MM-dd' string for the API (DateOnly). */
  private toDateString(value: unknown): string | null {
    if (!value) return null;
    const d = value instanceof Date ? value : new Date(value as string);
    if (isNaN(d.getTime())) return null;
    const y = d.getFullYear();
    const m = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    return `${y}-${m}-${day}`;
  }

  private parseVerses(text: string): string[] {
    return text
      .split(',')
      .map(v => v.trim())
      .filter(v => v.length > 0);
  }

  private nullableGuid(value: string | null | undefined): string | null {
    return value && value.length > 0 ? value : null;
  }

  private buildElements() {
    return this.elements.map(el => ({
      position: el.position,
      elementType: el.elementType,
      labelId: this.nullableGuid(el.labelId),
      scriptureReference: null,
      notes: el.notes || null,
      performerId: this.isSongElement(el) ? null : this.nullableGuid(el.performerId),
      isBeurtzang: this.isSongElement(el) ? el.isBeurtzang : false,
      bibleTranslationId: this.isReadingElement(el) ? this.nullableGuid(el.bibleTranslationId) : null,
      readingReferences: this.isReadingElement(el)
        ? el.readingRefs
            .filter(r => r.bibleBookId)
            .map((r, i) => {
              const book = this.bibleBooks.find(b => b.id === r.bibleBookId);
              return {
                bibleBookId: r.bibleBookId,
                bookName: book?.name ?? '',
                chapter: r.chapter,
                verseStart: r.verseStart,
                verseEnd: r.verseEnd,
                position: i + 1,
              };
            })
        : [],
      songs: el.songs
        .filter(s => s.bundleId && s.number != null)
        .map((s, i) => ({
          bundleId: s.bundleId,
          section: s.section || null,
          songNumber: s.number,
          position: i + 1,
          verses: this.parseVerses(s.versesText),
          sungInFull: s.sungInFull,
        })),
    }));
  }

  private buildSermonRefs() {
    return this.sermonRefs
      .filter(r => r.bibleBookId)
      .map((r, i) => {
        const book = this.bibleBooks.find(b => b.id === r.bibleBookId);
        return {
          bibleBookId: r.bibleBookId,
          bookName: book?.name ?? '',
          chapter: r.chapter,
          verseStart: r.verseStart,
          verseEnd: r.verseEnd,
          position: i + 1,
        };
      });
  }

  /**
   * Resolve the congregation to an existing id, creating a new congregation when
   * the user typed (or a paste/URL import produced) a name that isn't yet in the
   * database. This keeps the paste and URL flows working end-to-end.
   */
  private resolveCongregationId(): Observable<string> {
    const existing = this.nullableGuid(this.metadataForm.value.congregationId);
    if (existing) return of(existing);

    const name = (this.congregationControl.value || '').trim();
    if (!name) return throwError(() => new Error('Gemeente is verplicht.'));

    const city = (this.congregationCityControl.value || '').trim() || 'Onbekend';

    // Match an existing congregation on BOTH name and city so, e.g., "Hervormde
    // Gemeente" in Randwijk and in Ederveen stay distinct; otherwise create it.
    return this.api.searchCongregations(name).pipe(
      switchMap(results => {
        const match = results.find(r =>
          r.name.toLowerCase() === name.toLowerCase() &&
          (r.city || '').toLowerCase() === city.toLowerCase());
        if (match) return of(match.id);
        const denominationId = this.nullableGuid(this.metadataForm.value.denominationId);
        return this.api.createCongregation({ name, city, denominationId }).pipe(map(c => c.id));
      })
    );
  }

  /** Resolve the preacher to an existing id, creating one when a new name was entered. */
  private resolvePreacherId(): Observable<string | null> {
    const existing = this.nullableGuid(this.metadataForm.value.preacherId);
    if (existing) return of(existing);

    const name = (this.preacherControl.value || '').trim();
    if (!name) return of(null);

    const city = (this.preacherCityControl.value || '').trim() || null;

    return this.api.searchPreachers(name).pipe(
      switchMap(results => {
        const match = results.find(r => r.fullName.toLowerCase() === name.toLowerCase());
        if (match) return of<string | null>(match.id);
        return this.api.createPreacher({ fullName: name, city }).pipe(map(p => p.id as string | null));
      })
    );
  }

  private buildRequest(congregationId: string, preacherId: string | null, status: number) {
    return {
      ...this.metadataForm.value,
      congregationId,
      preacherId,
      date: this.toDateString(this.metadataForm.value.date),
      churchCalendarSundayId: this.nullableGuid(this.metadataForm.value.churchCalendarSundayId),
      musicalAccompanimentId: this.nullableGuid(this.metadataForm.value.musicalAccompanimentId),
      specialOccasionId: this.nullableGuid(this.metadataForm.value.specialOccasionId),
      status,
      elements: this.buildElements(),
      sermonTextReferences: this.buildSermonRefs(),
    };
  }

  private performSave(status: number): Observable<ServiceDetail> {
    return this.resolveCongregationId().pipe(
      switchMap(congregationId =>
        this.resolvePreacherId().pipe(map(preacherId => ({ congregationId, preacherId })))
      ),
      switchMap(({ congregationId, preacherId }) => {
        const request = this.buildRequest(congregationId, preacherId, status);
        const save$ = this.isEditMode && this.editingServiceId
          ? this.api.updateService(this.editingServiceId, request)
          : this.api.createService(request);
        return save$.pipe(tap(saved => {
          this.editingServiceId = saved.id;
          this.status = saved.statusValue ?? status;
          this.lastSavedSnapshot = this.snapshot();
          this.lastSavedAt = new Date();
        }));
      })
    );
  }

  /** Snapshot of the editable state, used to skip no-op autosaves. */
  private snapshot(): string {
    return JSON.stringify({
      form: this.metadataForm.value,
      elements: this.elements,
      sermonRefs: this.sermonRefs,
      congregation: this.congregationControl.value,
      city: this.congregationCityControl.value,
      preacher: this.preacherControl.value,
      preacherCity: this.preacherCityControl.value,
    });
  }

  /** Debounced autosave: persists the in-progress service as a Concept draft. */
  scheduleAutosave(): void {
    if (this.autosaveTimer) clearTimeout(this.autosaveTimer);
    this.autosaveTimer = setTimeout(() => this.autosave(), 2000);
  }

  /** Human-readable status shown per tab, e.g. "Laatst opgeslagen om 14:03". */
  get lastSavedLabel(): string {
    if (this.autoSaving) return 'Bezig met opslaan…';
    if (this.autosaveFailed) return 'Automatisch opslaan mislukt — sla handmatig op';
    if (!this.lastSavedAt) return 'Nog niet opgeslagen';
    const hh = String(this.lastSavedAt.getHours()).padStart(2, '0');
    const mm = String(this.lastSavedAt.getMinutes()).padStart(2, '0');
    return `Laatst opgeslagen om ${hh}:${mm}`;
  }

  private autosave(): void {
    if (this.saving || this.publishing) return;
    if (!this.metadataForm.get('date')?.value) return;
    if (!(this.congregationControl.value || this.metadataForm.value.congregationId)) return;
    if (this.snapshot() === this.lastSavedSnapshot) return;

    this.autoSaving = true;
    this.autosaveFailed = false;
    // A brand-new service becomes a Concept draft; an existing one keeps its status.
    this.performSave(this.isEditMode ? this.status : 0).subscribe({
      next: () => { this.autoSaving = false; },
      error: () => { this.autoSaving = false; this.autosaveFailed = true; },
    });
  }

  /** Save (as Concept for a new service; keeps status for an existing one). */
  saveDraft(): void {
    if (this.saving || this.publishing) return;
    if (!this.metadataForm.valid) { this.metadataForm.markAllAsTouched(); return; }
    this.saving = true;
    this.performSave(this.isEditMode ? this.status : 0).subscribe({
      next: () => { this.saving = false; this.finish(true); },
      error: (err: any) => { this.saving = false; alert('Fout bij opslaan: ' + (err.message ?? err)); },
    });
  }

  /** Publish: persists the service and flips it to Gepubliceerd. */
  publish(): void {
    if (this.saving || this.publishing) return;
    if (!this.metadataForm.valid) { this.metadataForm.markAllAsTouched(); return; }
    this.publishing = true;
    this.performSave(1).subscribe({
      next: () => { this.publishing = false; this.finish(true); },
      error: (err: any) => { this.publishing = false; alert('Fout bij opslaan: ' + (err.message ?? err)); },
    });
  }

  /** Back-compat entry point for the primary action button. */
  saveService(): void {
    this.publish();
  }

  /** Id of the "Voorganger" performer, used as the default for onderdelen. */
  private defaultPerformerId(): string {
    return this.performers.find(p => p.value === 'Voorganger')?.id ?? '';
  }

  /** Proceed without a template (empty service). */
  skipTemplate(): void {
    this.chosenTemplateName.set(null);
    this.templateChosen.set(true);
  }

  /** Choose a template: prefill metadata defaults + onderdelen scaffold, then continue. */
  chooseTemplate(templateId: string): void {
    this.api.getTemplate(templateId).subscribe({
      next: template => {
        this.applyTemplateDto(template);
        this.chosenTemplateName.set(template.name);
        this.templateChosen.set(true);
      },
      error: () => alert('Kon sjabloon niet laden.'),
    });
  }

  /** Prefill metadata defaults and build the onderdelen scaffold from a template. */
  private applyTemplateDto(template: ServiceTemplate): void {
    this.metadataForm.patchValue({
      timeOfDay: template.timeOfDay ?? this.metadataForm.value.timeOfDay,
      specialOccasionId: template.occasionId ?? this.metadataForm.value.specialOccasionId,
      musicalAccompanimentId: template.musicalAccompanimentId ?? '',
      denominationId: template.denominationId ?? '',
      isReadingService: template.isReadingService ?? false,
      hasBeamerLiturgy: template.hasBeamerLiturgy ?? false,
      hasBeamerTexts: template.hasBeamerTexts ?? false,
      hasBeamerSongs: template.hasBeamerSongs ?? false,
    }, { emitEvent: false });

    // Remember the template defaults so onderdelen added later (manually or via
    // paste/URL import) are prefilled too, not just the initial scaffold.
    this.templateDefaultTranslationId = template.defaultBibleTranslationId ?? '';
    this.templateDefaultBundleId = template.defaultSongBundleId ?? '';

    const defaultPerformer = this.defaultPerformerId();
    const defaultTranslation = this.templateDefaultTranslationId;
    const scaffold: ServiceElementModel[] = template.elements
      .slice()
      .sort((a, b) => a.position - b.position)
      .map((e, i) => ({
        position: i + 1,
        elementType: e.elementTypeValue,
        labelId: e.labelId ?? '',
        notes: '',
        // Template performer override wins; otherwise default to Voorganger.
        performerId: e.performerId ?? defaultPerformer,
        isBeurtzang: e.isBeurtzang ?? false,
        bibleTranslationId: e.elementTypeValue === this.ELEMENT_READING ? defaultTranslation : '',
        readingRefs: [],
        songs: [],
      }));
    this.elements = this.reconcileWithScaffold(scaffold, this.elements);
  }

  /** Re-apply a matching template based on the chosen gemeente/tijdstip (from the onderdelen tab). */
  applyTemplate(): void {
    const congregationId = this.nullableGuid(this.metadataForm.value.congregationId);
    const timeOfDay = this.metadataForm.value.timeOfDay;
    const occasionId = this.nullableGuid(this.metadataForm.value.specialOccasionId);
    this.api.instantiateTemplate({ congregationId, timeOfDay, occasionId }).subscribe({
      next: (instances) => {
        if (!instances || instances.length === 0) {
          alert('Geen passend sjabloon gevonden voor deze gemeente/tijdstip.');
          return;
        }
        const scaffold: ServiceElementModel[] = instances.map((inst, i) => ({
          position: inst.position || i + 1,
          elementType: inst.elementType,
          labelId: inst.labelId ?? '',
          notes: '',
          performerId: inst.performerId ?? '',
          isBeurtzang: inst.isBeurtzang ?? false,
          bibleTranslationId: inst.bibleTranslationId ?? '',
          readingRefs: [],
          songs: [],
        }));
        this.elements = this.reconcileWithScaffold(scaffold, this.elements);
        this.scheduleAutosave();
      },
      error: () => alert('Kon sjabloon niet laden.'),
    });
  }

  /**
   * Merge already-entered/parsed onderdelen into a template scaffold:
   * fill each empty scaffold slot with the first unused existing element that has the
   * same label, keep empty slots that had no match, and append any leftover existing
   * elements (that carried content) after the scaffold.
   */
  private reconcileWithScaffold(
    scaffold: ServiceElementModel[],
    existing: ServiceElementModel[],
  ): ServiceElementModel[] {
    const hasContent = (e: ServiceElementModel) =>
      (e.songs && e.songs.length > 0) || !!e.notes || (e.readingRefs && e.readingRefs.length > 0);
    const pool = existing.filter(hasContent);
    const used = new Set<ServiceElementModel>();

    for (const slot of scaffold) {
      const match = pool.find(e => !used.has(e) && e.labelId && e.labelId === slot.labelId);
      if (match) {
        used.add(match);
        slot.notes = match.notes || slot.notes;
        slot.songs = match.songs;
        if (match.performerId) slot.performerId = match.performerId;
        if (match.isBeurtzang) slot.isBeurtzang = match.isBeurtzang;
        if (match.bibleTranslationId) slot.bibleTranslationId = match.bibleTranslationId;
        if (match.readingRefs?.length) slot.readingRefs = match.readingRefs;
      }
    }

    const leftovers = pool.filter(e => !used.has(e));
    const result = [...scaffold, ...leftovers];
    result.forEach((e, i) => (e.position = i + 1));
    return result;
  }

  finish(saved: boolean): void {
    if (this.dialogRef) {
      this.dialogRef.close(saved);
    } else if (saved) {
      alert('Dienst opgeslagen!');
    }
  }

  cancel(): void {
    this.dialogRef?.close(false);
  }
}
