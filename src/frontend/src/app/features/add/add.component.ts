import { Component, OnInit, Optional, Inject, HostBinding } from '@angular/core';
import { DatePipe, JsonPipe } from '@angular/common';
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
import { CongregationSummary, PreacherSummary, ListItem, ServiceDetail } from '../../core/models/api.models';
import { debounceTime, switchMap, of } from 'rxjs';
import { FormControl } from '@angular/forms';

export interface AddDialogData {
  serviceId?: string;
}

@Component({
  selector: 'app-add',
  standalone: true,
  imports: [
    DatePipe, JsonPipe,
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

  // Paste-to-parse
  pasteText = '';
  parsedResult: any = null;
  parsing = false;

  // URL import
  importUrl = '';
  importing = false;

  // Elements
  elements: Array<{
    position: number;
    elementType: number;
    label: string;
    songs: Array<{ bundle: string; section: string; number: number; verses: string[] }>;
    notes: string;
  }> = [];

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

    // Load dropdown lists
    this.api.getListByName('SongBundles').subscribe(l => this.bundles = l.items);
    this.api.getListByName('SpecialOccasions').subscribe(l => this.specialOccasions = l.items);
    this.api.getListByName('BibleTranslations').subscribe(l => this.bibleTranslations = l.items);
    this.api.getListByName('ChurchCalendarSundays').subscribe(l => this.churchCalendarSundays = l.items);
    this.api.getListByName('MusicalAccompaniment').subscribe(l => this.musicalAccompaniments = l.items);

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

    // Edit mode: load the existing service and prefill the form.
    if (this.dialogData?.serviceId) {
      this.editingServiceId = this.dialogData.serviceId;
      this.api.getService(this.editingServiceId).subscribe(service => this.prefill(service));
    }
  }

  private prefill(service: ServiceDetail): void {
    this.metadataForm.patchValue({
      date: service.date,
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
        label: e.label ?? '',
        notes: e.notes ?? '',
        songs: e.songs.map(s => ({
          bundle: s.bundleAbbreviation ?? s.bundleName,
          section: s.section,
          number: s.songNumber,
          verses: s.verses,
        })),
      }));
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
      label: '',
      songs: [],
      notes: '',
    });
  }

  removeElement(index: number): void {
    this.elements.splice(index, 1);
    this.elements.forEach((el, i) => el.position = i + 1);
  }

  submitPaste(): void {
    this.parsing = true;
    // TODO: Send to LLM parse endpoint
    setTimeout(() => {
      this.parsing = false;
      this.parsedResult = { message: 'Verwerking voltooid (demo)' };
    }, 1500);
  }

  submitUrl(): void {
    this.importing = true;
    // TODO: Send URL to parse endpoint
    setTimeout(() => {
      this.importing = false;
    }, 1500);
  }

  saveService(): void {
    if (!this.metadataForm.valid) return;
    const request = {
      ...this.metadataForm.value,
      elements: this.elements,
    };

    if (this.isEditMode && this.editingServiceId) {
      this.api.updateService(this.editingServiceId, request).subscribe({
        next: () => this.finish(true),
        error: (err: any) => alert('Fout bij opslaan: ' + err.message),
      });
    } else {
      this.api.createService(request).subscribe({
        next: () => this.finish(true),
        error: (err: any) => alert('Fout bij opslaan: ' + err.message),
      });
    }
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
