import { Component, OnInit, Optional, Inject, HostBinding, signal, ViewChild } from '@angular/core';
import { DatePipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTabsModule } from '@angular/material/tabs';
import { MatStepperModule, MatStepper } from '@angular/material/stepper';
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
  Congregation, Preacher, CongregationPastor, BundleSection,
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

interface PreacherOption extends PreacherSummary {
  isAssociatedPastor?: boolean;
  isPrimaryPastor?: boolean;
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
  @ViewChild('stepper') stepper?: MatStepper;

  // Form
  metadataForm!: FormGroup;
  congregationControl = new FormControl('');
  congregationCityControl = new FormControl('');
  preacherControl = new FormControl('');
  preacherCityControl = new FormControl('');
  quickAddCongregationForm!: FormGroup;
  quickAddPreacherForm!: FormGroup;

  // Autocomplete
  congregationSuggestions: CongregationSummary[] = [];
  preacherSuggestions: PreacherSummary[] = [];
  selectedCongregation: CongregationSummary | null = null;
  selectedPreacher: PreacherSummary | null = null;
  showQuickAddCongregation = false;
  showQuickAddPreacher = false;
  creatingCongregation = false;
  creatingPreacher = false;

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
  preacherTitles: ListItem[] = [];

  // Defaults carried over from the chosen template, applied to onderdelen added
  // later (manually or via paste/URL import), not just the initial scaffold.
  private templateDefaultTranslationId = '';
  private templateDefaultBundleId = '';
  // Denomination of the chosen template; congregations of this denomination are
  // surfaced at the top of the gemeente autocomplete.
  private templateDenominationId = '';

  // Cache of catalog verse counts keyed by "bundleId|number" for the live badge.
  private catalogVerseCountCache = new Map<string, number | null>();

  // Cache of rubrieken (categorieën) per liedbundel, for the per-song dropdown.
  sectionsByBundle: Record<string, BundleSection[]> = {};

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
      date: [new Date(), Validators.required],
      timeOfDay: [0, Validators.required],
      congregationId: ['', Validators.required],
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
    this.quickAddCongregationForm = this.fb.group({
      name: ['', Validators.required],
      city: ['', Validators.required],
      denominationId: [''],
      locationDetail: [''],
    });
    this.quickAddPreacherForm = this.fb.group({
      titleId: [''],
      fullName: ['', Validators.required],
      city: [''],
      denominationId: [''],
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
      preacherTitles: this.loadListItems('PreacherTitles'),
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
      this.preacherTitles = lists.preacherTitles;

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
      switchMap(val => typeof val === 'string' && val.length > 1 ? this.api.searchCongregations(val) : of([]))
    ).subscribe(results => this.congregationSuggestions = this.prioritizeCongregationsByDenomination(results));

    // The autocomplete is select-only: typing filters choices but clears any
    // previously selected id until the user explicitly chooses an option.
    // A selected option writes the object (not a string) to the control, so we
    // only clear on string values (i.e. the user is typing).
    this.congregationControl.valueChanges.subscribe(value => {
      if (typeof value !== 'string') return;
      this.selectedCongregation = null;
      this.metadataForm.patchValue({ congregationId: '' }, { emitEvent: false });
      this.congregationCityControl.setValue('', { emitEvent: false });
      this.metadataForm.patchValue({ denominationId: '' }, { emitEvent: false });
      this.scheduleAutosave();
    });

    // Autocomplete for preacher
    this.preacherControl.valueChanges.pipe(
      debounceTime(300),
      switchMap(val => typeof val === 'string' && val.length > 1 ? this.api.searchPreachers(val) : of([]))
    ).subscribe(results => {
      this.preacherSuggestions = results;
      this.prioritizePastorsInSuggestions();
    });

    this.preacherControl.valueChanges.subscribe(value => {
      if (typeof value !== 'string') return;
      this.selectedPreacher = null;
      this.metadataForm.patchValue({ preacherId: '' }, { emitEvent: false });
      this.preacherCityControl.setValue('', { emitEvent: false });
      this.scheduleAutosave();
    });

    // Autosave: debounced draft save whenever the metadata form changes.
    this.metadataForm.valueChanges.subscribe(() => this.scheduleAutosave());

    // Leesdienst: a reading service has no preacher, so clear + disable the
    // voorganger fields when the toggle is on (enforced again on save).
    this.metadataForm.get('isReadingService')!.valueChanges.subscribe((v: boolean) =>
      this.applyReadingServiceState(!!v));
  }

  /** Clear and lock the voorganger fields for a leesdienst; unlock otherwise. */
  private applyReadingServiceState(isReading: boolean): void {
    if (isReading) {
      this.selectedPreacher = null;
      this.metadataForm.patchValue({ preacherId: '' }, { emitEvent: false });
      this.preacherControl.setValue('', { emitEvent: false });
      this.preacherControl.disable({ emitEvent: false });
      this.preacherCityControl.setValue('', { emitEvent: false });
      this.preacherCityControl.disable({ emitEvent: false });
    } else {
      this.preacherControl.enable({ emitEvent: false });
      this.preacherCityControl.enable({ emitEvent: false });
    }
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

    this.selectedCongregation = service.congregation;
    this.congregationControl.setValue(service.congregation.name, { emitEvent: false });
    this.congregationCityControl.setValue(service.congregation.city, { emitEvent: false });
    this.metadataForm.patchValue({ denominationId: service.congregation.denominationId ?? '' }, { emitEvent: false });
    if (service.preacher) {
      this.selectedPreacher = service.preacher;
      this.preacherControl.setValue(service.preacher.fullName, { emitEvent: false });
      this.preacherCityControl.setValue(service.preacher.city ?? '', { emitEvent: false });
    } else {
      this.selectedPreacher = null;
      this.preacherControl.setValue('', { emitEvent: false });
      this.preacherCityControl.setValue('', { emitEvent: false });
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
      for (const song of el.songs) {
        this.updateCatalogVerseCount(song);
        if (song.bundleId) this.loadSectionsForBundle(song.bundleId);
      }
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
    this.selectedCongregation = congregation;
    this.congregationControl.setValue(congregation.name, { emitEvent: false });
    this.congregationCityControl.setValue(congregation.city, { emitEvent: false });
    this.metadataForm.patchValue({
      congregationId: congregation.id,
      denominationId: congregation.denominationId ?? '',
    });
    this.prioritizePastorsInSuggestions();
    if (!this.nullableGuid(this.metadataForm.value.preacherId) && !(this.preacherControl.value || '').trim()) {
      const primaryPastor = congregation.pastors?.find(p => p.isPrimary);
      if (primaryPastor) this.selectPastor(primaryPastor);
    }
  }

  displayCongregation = (value: CongregationSummary | string | null): string =>
    value ? (typeof value === 'string' ? value : value.name) : '';

  displayPreacher = (value: PreacherOption | PreacherSummary | string | null): string =>
    value ? (typeof value === 'string' ? value : value.fullName) : '';

  preacherTitlePrefix(p: PreacherOption): string {
    return p.title ? `${p.title} ` : '';
  }

  get congregationDenominationLabel(): string {
    const id = this.selectedCongregation?.denominationId ?? this.metadataForm?.value.denominationId ?? '';
    const item = this.denominations.find(d => d.id === id);
    return item?.abbreviation || item?.value || this.selectedCongregation?.denominationAbbreviation || '—';
  }

  get preacherTitleLabel(): string {
    return this.selectedPreacher?.title || '—';
  }

  get preacherDenominationLabel(): string {
    return this.selectedPreacher?.denomination || '—';
  }

  /** Formatted "Voorganger" value for the review card (or a leesdienst marker). */
  get preacherReviewLabel(): string {
    if (this.metadataForm?.value.isReadingService) return 'Leesdienst (geen voorganger)';
    return (this.preacherControl.value || '').trim() || '—';
  }

  /** Human-readable summary of the structured preektekst references (review card). */
  get sermonRefsSummary(): string {
    return this.sermonRefs
      .map(r => {
        const book = this.bibleBooks.find(b => b.id === r.bibleBookId);
        if (!book) return '';
        let s = book.name;
        if (r.chapter != null) {
          s += ` ${r.chapter}`;
          if (r.verseStart != null) {
            s += `:${r.verseStart}`;
            if (r.verseEnd != null && r.verseEnd !== r.verseStart) s += `-${r.verseEnd}`;
          }
        }
        return s;
      })
      .filter(s => s)
      .join('; ');
  }

  selectPreacher(preacher: PreacherSummary): void {
    this.selectedPreacher = preacher;
    this.preacherControl.setValue(preacher.fullName, { emitEvent: false });
    this.metadataForm.patchValue({ preacherId: preacher.id });
    this.preacherCityControl.setValue(preacher.city ?? '', { emitEvent: false });
  }

  selectPreacherOption(preacher: PreacherOption): void {
    if (preacher.isAssociatedPastor && (!preacher.title || !preacher.denomination)) {
      this.api.getPreacher(preacher.id).subscribe({
        next: full => this.selectPreacher(this.preacherToSummary(full)),
        error: () => this.selectPreacher(preacher),
      });
      return;
    }
    this.selectPreacher(preacher);
  }

  preacherOptions(): PreacherOption[] {
    const raw = this.preacherControl.value;
    const typed = (typeof raw === 'string' ? raw : '').toLowerCase().trim();
    const pastors = (this.selectedCongregation?.pastors ?? [])
      .filter(p => !typed || p.fullName.toLowerCase().includes(typed))
      .map(p => this.pastorToPreacherOption(p));
    const pastorIds = new Set(pastors.map(p => p.id));
    return [
      ...pastors,
      ...this.preacherSuggestions
        .filter(p => !pastorIds.has(p.id))
        .map(p => ({ ...p })),
    ];
  }

  private pastorToPreacherOption(pastor: CongregationPastor): PreacherOption {
    return {
      id: pastor.preacherId,
      fullName: pastor.fullName,
      city: pastor.city,
      title: null,
      denomination: null,
      isAssociatedPastor: true,
      isPrimaryPastor: pastor.isPrimary,
    };
  }

  private selectPastor(pastor: CongregationPastor): void {
    this.api.getPreacher(pastor.preacherId).subscribe({
      next: preacher => this.selectPreacher(this.preacherToSummary(preacher)),
      error: () => this.selectPreacher(this.pastorToPreacherOption(pastor)),
    });
  }

  private prioritizePastorsInSuggestions(): void {
    const pastors = this.selectedCongregation?.pastors ?? [];
    if (!pastors.length || !this.preacherSuggestions.length) return;
    const order = new Map(pastors.map((p, i) => [p.preacherId, i]));
    this.preacherSuggestions = this.preacherSuggestions
      .slice()
      .sort((a, b) => (order.get(a.id) ?? Number.MAX_SAFE_INTEGER) - (order.get(b.id) ?? Number.MAX_SAFE_INTEGER));
  }

  // Surface congregations of the chosen template's denomination at the top of the list.
  private prioritizeCongregationsByDenomination(list: CongregationSummary[]): CongregationSummary[] {
    if (!this.templateDenominationId) return list;
    return list
      .slice()
      .sort((a, b) => {
        const aMatch = a.denominationId === this.templateDenominationId ? 0 : 1;
        const bMatch = b.denominationId === this.templateDenominationId ? 0 : 1;
        return aMatch - bMatch;
      });
  }

  private congregationToSummary(congregation: Congregation): CongregationSummary {
    return {
      id: congregation.id,
      name: congregation.name,
      city: congregation.city,
      denominationAbbreviation: congregation.denominationAbbreviation,
      denominationId: congregation.denominationId,
      pastors: congregation.pastors,
    };
  }

  private preacherToSummary(preacher: Preacher): PreacherSummary {
    return {
      id: preacher.id,
      fullName: preacher.fullName,
      city: preacher.city,
      title: preacher.title,
      denomination: preacher.denomination,
    };
  }

  openQuickAddCongregation(): void {
    this.quickAddCongregationForm.reset({
      name: (this.congregationControl.value || '').trim(),
      city: '',
      denominationId: this.metadataForm.value.denominationId ?? '',
      locationDetail: '',
    });
    this.showQuickAddCongregation = true;
  }

  createQuickCongregation(): void {
    if (this.quickAddCongregationForm.invalid || this.creatingCongregation) {
      this.quickAddCongregationForm.markAllAsTouched();
      return;
    }
    const value = this.quickAddCongregationForm.value;
    this.creatingCongregation = true;
    this.api.createCongregation({
      name: (value.name || '').trim(),
      city: (value.city || '').trim(),
      denominationId: this.nullableGuid(value.denominationId),
      locationDetail: (value.locationDetail || '').trim() || null,
      modality: null,
      latitude: null,
      longitude: null,
    }).subscribe({
      next: congregation => {
        const summary = this.congregationToSummary(congregation);
        this.congregationSuggestions = [summary, ...this.congregationSuggestions.filter(c => c.id !== summary.id)];
        this.selectCongregation(summary);
        this.showQuickAddCongregation = false;
        this.creatingCongregation = false;
      },
      error: err => {
        this.creatingCongregation = false;
        alert('Fout bij aanmaken gemeente: ' + (err.message ?? err));
      },
    });
  }

  openQuickAddPreacher(): void {
    this.quickAddPreacherForm.reset({
      titleId: '',
      fullName: (this.preacherControl.value || '').trim(),
      city: '',
      denominationId: this.metadataForm.value.denominationId ?? '',
    });
    this.showQuickAddPreacher = true;
  }

  createQuickPreacher(): void {
    if (this.quickAddPreacherForm.invalid || this.creatingPreacher) {
      this.quickAddPreacherForm.markAllAsTouched();
      return;
    }
    const value = this.quickAddPreacherForm.value;
    this.creatingPreacher = true;
    this.api.createPreacher({
      fullName: (value.fullName || '').trim(),
      city: (value.city || '').trim() || null,
      denominationId: this.nullableGuid(value.denominationId),
      titleId: this.nullableGuid(value.titleId),
    }).subscribe({
      next: preacher => {
        const summary = this.preacherToSummary(preacher);
        this.preacherSuggestions = [summary, ...this.preacherSuggestions.filter(p => p.id !== summary.id)];
        this.selectPreacher(summary);
        this.showQuickAddPreacher = false;
        this.creatingPreacher = false;
      },
      error: err => {
        this.creatingPreacher = false;
        alert('Fout bij aanmaken voorganger: ' + (err.message ?? err));
      },
    });
  }

  addElement(): void {
    const element: ServiceElementModel = {
      position: this.elements.length + 1,
      elementType: 0,
      labelId: '',
      songs: [],
      notes: '',
      performerId: '',
      isBeurtzang: false,
      bibleTranslationId: this.templateDefaultTranslationId,
      readingRefs: [],
    };
    this.autoSelectLabel(element);
    this.elements.push(element);
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
    this.autoSelectLabel(element);
    // Default the Bijbelvertaling on a reading onderdeel when none is set yet.
    if (this.isReadingElement(element) && !element.bibleTranslationId) {
      element.bibleTranslationId = this.templateDefaultTranslationId;
    }
    this.scheduleAutosave();
  }

  /**
   * Prefill the Onderdeel when exactly one classified label matches the selected
   * Type, so common single-option types (e.g. a lone reading label) fill in.
   */
  private autoSelectLabel(element: ServiceElementModel): void {
    if (element.labelId) return;
    const matches = this.liturgicalLabels.filter(
      l => l.liturgicalElementType === element.elementType,
    );
    if (matches.length === 1) {
      element.labelId = matches[0].id;
    }
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
    const song: ElementSong = {
      bundleId: this.templateDefaultBundleId,
      section: '',
      number: null,
      versesText: '',
      sungInFull: false,
    };
    element.songs.push(song);
    if (song.bundleId) {
      this.loadSectionsForBundle(song.bundleId, () => this.applyDefaultSection(song));
    }
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
    if (song.bundleId) {
      this.loadSectionsForBundle(song.bundleId, () => {
        if (!(song.section || '').trim()) this.applyDefaultSection(song);
      });
    }
    this.updateCatalogVerseCount(song);
    this.scheduleAutosave();
  }

  /** Rubrieken (categorieën) available for a song's bundle. */
  sectionsFor(song: ElementSong): BundleSection[] {
    return this.sectionsByBundle[song.bundleId] ?? [];
  }

  /** Lazily load (and cache) the rubrieken for a bundle, then run an optional callback. */
  private loadSectionsForBundle(bundleId: string, done?: () => void): void {
    if (this.sectionsByBundle[bundleId]) {
      done?.();
      return;
    }
    this.api.getBundleSections(bundleId).subscribe({
      next: (sections) => {
        this.sectionsByBundle[bundleId] = sections;
        done?.();
      },
      error: () => {
        this.sectionsByBundle[bundleId] = [];
        done?.();
      },
    });
  }

  /** Prefill the song's rubriek with the bundle's default rubriek, if any. */
  private applyDefaultSection(song: ElementSong): void {
    const def = (this.sectionsByBundle[song.bundleId] ?? []).find(s => s.isDefault);
    if (def && !(song.section || '').trim()) song.section = def.value;
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
      // Valid tokens: a number, a range (1-3), or a named vers label (e.g. Voorzang).
      .some(t => !/^\d+$/.test(t) && !/^\d+\s*[-–]\s*\d+$/.test(t) && !/^[A-Za-zÀ-ÿ][A-Za-zÀ-ÿ0-9 '.-]*$/.test(t));
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
    const congregationId = this.nullableGuid(this.metadataForm.value.congregationId);
    if (!congregationId) {
      this.metadataForm.get('congregationId')?.markAsTouched();
      return throwError(() => new Error('Selecteer een bestaande gemeente.'));
    }
    const preacherId = this.nullableGuid(this.metadataForm.value.preacherId);
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
    if (!this.nullableGuid(this.metadataForm.value.congregationId)) return;
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
    this.applyReadingServiceState(template.isReadingService ?? false);

    // Remember the template defaults so onderdelen added later (manually or via
    // paste/URL import) are prefilled too, not just the initial scaffold.
    this.templateDefaultTranslationId = template.defaultBibleTranslationId ?? '';
    this.templateDefaultBundleId = template.defaultSongBundleId ?? '';
    this.templateDenominationId = template.denominationId ?? '';

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
    const denominationId = this.selectedCongregation?.denominationId ?? this.nullableGuid(this.metadataForm.value.denominationId);
    const timeOfDay = this.metadataForm.value.timeOfDay;
    const occasionId = this.nullableGuid(this.metadataForm.value.specialOccasionId);
    this.api.resolveTemplate({ denominationId, congregationId, timeOfDay, occasionId }).subscribe({
      next: (template) => {
        if (!template) {
          alert('Geen passend sjabloon gevonden voor deze gemeente/tijdstip.');
          return;
        }
        // Reuse the full-template path so the standaard liedbundel/vertaling are
        // captured too, and are applied to onderdelen added afterwards.
        this.applyTemplateDto(template);
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

  // --- Persistent footer navigation (drives the Handmatig stepper) ---

  /** True when the footer step navigation applies (Handmatig tab active). */
  get onManualTab(): boolean {
    return this.selectedTabIndex === 0;
  }

  get canStepBack(): boolean {
    return this.onManualTab && !!this.stepper && this.stepper.selectedIndex > 0;
  }

  get canStepNext(): boolean {
    return this.onManualTab && !!this.stepper && this.stepper.selectedIndex < this.stepper.steps.length - 1;
  }

  stepBack(): void {
    this.stepper?.previous();
  }

  stepNext(): void {
    this.stepper?.next();
  }
}
