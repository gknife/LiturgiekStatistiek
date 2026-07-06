import { Component, OnInit, Optional, Inject, HostBinding } from '@angular/core';
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
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import {
  CongregationSummary, PreacherSummary, ListItem, ServiceDetail,
  BibleBook, ParsedServiceData,
} from '../../core/models/api.models';
import { debounceTime, switchMap, of, map, throwError, Observable, forkJoin } from 'rxjs';
import { FormControl } from '@angular/forms';

export interface AddDialogData {
  serviceId?: string;
}

interface ElementSong {
  bundleId: string;
  section: string;
  number: number | null;
  versesText: string;
}

interface ServiceElementModel {
  position: number;
  elementType: number;
  labelId: string;
  notes: string;
  songs: ElementSong[];
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
    MatChipsModule, MatSlideToggleModule, FormsModule, ReactiveFormsModule,
  ],
  templateUrl: './add.component.html',
  styleUrl: './add.component.scss',
})
export class AddComponent implements OnInit {
  // Form
  metadataForm!: FormGroup;
  congregationControl = new FormControl('');
  preacherControl = new FormControl('');

  // Autocomplete
  congregationSuggestions: CongregationSummary[] = [];
  preacherSuggestions: PreacherSummary[] = [];

  /** City parsed from paste/URL import, used when auto-creating a new congregation. */
  private parsedCity: string | null = null;

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
      churchCalendarSundayId: [''],
      bibleTranslationId: [''],
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

    // Load dropdown lists. Edit-mode prefill maps existing labels/bundles back to
    // their ids, so it must run only AFTER these lists are available — otherwise the
    // Onderdeel/bundle dropdowns render empty. forkJoin gates prefill on the loads.
    forkJoin({
      bundles: this.api.getListByName('SongBundles'),
      specialOccasions: this.api.getListByName('SpecialOccasions'),
      bibleTranslations: this.api.getListByName('BibleTranslations'),
      churchCalendarSundays: this.api.getListByName('ChurchCalendarSundays'),
      musicalAccompaniments: this.api.getListByName('MusicalAccompaniment'),
      liturgicalLabels: this.api.getListByName('LiturgicalLabels'),
    }).subscribe(lists => {
      this.bundles = lists.bundles.items;
      this.specialOccasions = lists.specialOccasions.items;
      this.bibleTranslations = lists.bibleTranslations.items;
      this.churchCalendarSundays = lists.churchCalendarSundays.items;
      this.musicalAccompaniments = lists.musicalAccompaniments.items;
      this.liturgicalLabels = lists.liturgicalLabels.items;

      this.loadBibleBooks();

      // Edit mode: load the existing service and prefill the form once the lists
      // needed to resolve labels/bundles/books are present.
      if (this.dialogData?.serviceId) {
        this.editingServiceId = this.dialogData.serviceId;
        this.api.getService(this.editingServiceId).subscribe(service => this.prefill(service));
      }
    });

    // Reload book names when the translation changes.
    this.metadataForm.get('bibleTranslationId')!.valueChanges.subscribe(() => this.loadBibleBooks());

    // Autocomplete for congregation
    this.congregationControl.valueChanges.pipe(
      debounceTime(300),
      switchMap(val => val && val.length > 1 ? this.api.searchCongregations(val) : of([]))
    ).subscribe(results => this.congregationSuggestions = results);

    // Autocomplete for preacher
    this.preacherControl.valueChanges.pipe(
      debounceTime(300),
      switchMap(val => val && val.length > 1 ? this.api.searchPreachers(val) : of([]))
    ).subscribe(results => this.preacherSuggestions = results);
  }

  private loadBibleBooks(): void {
    const translationId = this.metadataForm?.get('bibleTranslationId')?.value;
    const abbr = this.bibleTranslations.find(t => t.id === translationId)?.abbreviation || undefined;
    this.api.getBibleBooks(abbr).subscribe(books => this.bibleBooks = books);
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
  }

  removeSermonRef(index: number): void {
    this.sermonRefs.splice(index, 1);
  }

  private prefill(service: ServiceDetail): void {
    this.metadataForm.patchValue({
      date: service.date ? new Date(service.date) : null,
      timeOfDay: service.timeOfDayValue,
      congregationId: service.congregation.id,
      preacherId: service.preacher?.id ?? '',
      churchCalendarSundayId: service.churchCalendarSundayId ?? '',
      bibleTranslationId: service.bibleTranslationId ?? '',
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

    this.congregationControl.setValue(`${service.congregation.name} — ${service.congregation.city}`);
    if (service.preacher) {
      this.preacherControl.setValue(service.preacher.fullName);
    }

    this.elements = service.elements
      .sort((a, b) => a.position - b.position)
      .map(e => ({
        position: e.position,
        elementType: 0,
        labelId: this.labelIdByValue(e.label),
        notes: e.notes ?? '',
        songs: e.songs.map(s => ({
          bundleId: this.bundleIdByAbbrev(s.bundleAbbreviation, s.bundleName),
          section: s.section,
          number: s.songNumber,
          versesText: s.verses.join(', '),
        })),
      }));

    this.sermonRefs = (service.sermonTextReferences ?? []).map(r => ({
      bibleBookId: r.bibleBookId ?? '',
      chapter: r.chapter,
      verseStart: r.verseStart,
      verseEnd: r.verseEnd,
    }));
  }

  private labelIdByValue(value: string | null): string {
    if (!value) return '';
    return this.liturgicalLabels.find(l => l.value === value)?.id ?? '';
  }

  private bundleIdByAbbrev(abbrev: string | null, name?: string): string {
    return this.bundles.find(b => b.abbreviation === abbrev)?.id
      ?? this.bundles.find(b => b.value === name)?.id
      ?? '';
  }

  selectCongregation(congregation: CongregationSummary): void {
    this.metadataForm.patchValue({ congregationId: congregation.id });
    this.congregationControl.setValue(congregation.name + ' — ' + congregation.city);
  }

  selectPreacher(preacher: PreacherSummary): void {
    this.metadataForm.patchValue({ preacherId: preacher.id });
    this.preacherControl.setValue(preacher.fullName);
  }

  addElement(): void {
    this.elements.push({
      position: this.elements.length + 1,
      elementType: 0,
      labelId: '',
      songs: [],
      notes: '',
    });
  }

  removeElement(index: number): void {
    this.elements.splice(index, 1);
    this.elements.forEach((el, i) => el.position = i + 1);
  }

  addSong(element: ServiceElementModel): void {
    element.songs.push({ bundleId: '', section: '', number: null, versesText: '' });
  }

  removeSong(element: ServiceElementModel, index: number): void {
    element.songs.splice(index, 1);
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
    this.parsedCity = data.city ?? null;

    this.elements = data.elements.map((e, i) => ({
      position: e.position || i + 1,
      elementType: 0,
      labelId: this.labelIdByValue(e.label),
      notes: e.notes ?? '',
      songs: e.songNumber != null
        ? [{
            bundleId: this.bundleIdByAbbrev(e.songBundle),
            section: '',
            number: e.songNumber,
            versesText: (e.verses ?? []).join(', '),
          }]
        : [],
    }));
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
      songs: el.songs
        .filter(s => s.bundleId && s.number != null)
        .map((s, i) => ({
          bundleId: s.bundleId,
          section: s.section || null,
          songNumber: s.number,
          position: i + 1,
          verses: this.parseVerses(s.versesText),
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

    const raw = (this.congregationControl.value || '').trim();
    if (!raw) return throwError(() => new Error('Gemeente is verplicht.'));

    // Display value may be "Naam — Plaats (Denominatie)"; extract the bare name/city.
    const name = raw.split(' — ')[0].trim();
    const cityFromLabel = raw.includes(' — ')
      ? raw.split(' — ')[1].split(' (')[0].trim()
      : '';
    const city = this.parsedCity || cityFromLabel || 'Onbekend';

    return this.api.searchCongregations(name).pipe(
      switchMap(results => {
        const match = results.find(r => r.name.toLowerCase() === name.toLowerCase());
        if (match) return of(match.id);
        return this.api.createCongregation({ name, city }).pipe(map(c => c.id));
      })
    );
  }

  /** Resolve the preacher to an existing id, creating one when a new name was entered. */
  private resolvePreacherId(): Observable<string | null> {
    const existing = this.nullableGuid(this.metadataForm.value.preacherId);
    if (existing) return of(existing);

    const name = (this.preacherControl.value || '').trim();
    if (!name) return of(null);

    return this.api.searchPreachers(name).pipe(
      switchMap(results => {
        const match = results.find(r => r.fullName.toLowerCase() === name.toLowerCase());
        if (match) return of<string | null>(match.id);
        return this.api.createPreacher({ fullName: name }).pipe(map(p => p.id as string | null));
      })
    );
  }

  saveService(): void {
    if (!this.metadataForm.valid) return;

    this.resolveCongregationId().pipe(
      switchMap(congregationId =>
        this.resolvePreacherId().pipe(map(preacherId => ({ congregationId, preacherId })))
      )
    ).subscribe({
      next: ({ congregationId, preacherId }) => {
        const request = {
          ...this.metadataForm.value,
          congregationId,
          preacherId,
          date: this.toDateString(this.metadataForm.value.date),
          churchCalendarSundayId: this.nullableGuid(this.metadataForm.value.churchCalendarSundayId),
          bibleTranslationId: this.nullableGuid(this.metadataForm.value.bibleTranslationId),
          musicalAccompanimentId: this.nullableGuid(this.metadataForm.value.musicalAccompanimentId),
          specialOccasionId: this.nullableGuid(this.metadataForm.value.specialOccasionId),
          elements: this.buildElements(),
          sermonTextReferences: this.buildSermonRefs(),
        };

        const save$ = this.isEditMode && this.editingServiceId
          ? this.api.updateService(this.editingServiceId, request)
          : this.api.createService(request);

        save$.subscribe({
          next: () => this.finish(true),
          error: (err: any) => alert('Fout bij opslaan: ' + (err.message ?? err)),
        });
      },
      error: (err: any) => alert('Fout bij opslaan: ' + (err.message ?? err)),
    });
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
