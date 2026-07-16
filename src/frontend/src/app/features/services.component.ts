import { Component, OnInit, signal, inject, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule, MatTable } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ActivatedRoute } from '@angular/router';
import { ApiService } from '../core/services/api.service';
import { AuthService } from '../core/auth/auth.service';
import { ServiceSummary, ServiceDetail, ServiceElement, ServiceElementSong, SermonTextReference } from '../core/models/api.models';
import { AddComponent, AddDialogData } from './add/add.component';

interface Option {
  id: string;
  label: string;
}

@Component({
  selector: 'app-services',
  standalone: true,
  imports: [
    CommonModule, FormsModule, MatCardModule, MatTableModule, MatPaginatorModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatAutocompleteModule, MatButtonModule,
    MatIconModule, MatCheckboxModule, MatProgressSpinnerModule, MatTooltipModule,
    MatDialogModule,
  ],
  templateUrl: './services.component.html',
  styleUrl: './services.component.scss',
})
export class ServicesComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly auth = inject(AuthService);
  private readonly dialog = inject(MatDialog);
  private readonly route = inject(ActivatedRoute);

  @ViewChild(MatTable) private table?: MatTable<ServiceSummary>;

  readonly services = signal<ServiceSummary[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly congregations = signal<Option[]>([]);
  readonly denominations = signal<Option[]>([]);
  readonly preachers = signal<Option[]>([]);
  readonly selectedIds = signal<Set<string>>(new Set());

  readonly expandedId = signal<string | null>(null);
  readonly loadingDetailId = signal<string | null>(null);
  private readonly detailCache = signal<Record<string, ServiceDetail>>({});

  page = 1;
  pageSize = 20;

  filterCongregationId = '';
  filterCongregationText = '';
  filterDenominationId = '';
  filterPreacherId = '';
  filterPreacherText = '';
  filterFromDate = '';
  filterToDate = '';
  includeConcepts = true;

  bulkField = 'timeOfDay';
  bulkValue = '';

  readonly timeOfDayOptions = [
    { value: '0', label: 'Morgen' },
    { value: '1', label: 'Middag' },
    { value: '2', label: 'Avond' },
  ];

  readonly bulkFields = [
    { value: 'timeOfDay', label: 'Dagdeel' },
    { value: 'isReadingService', label: 'Leesdienst' },
    { value: 'congregationId', label: 'Gemeente' },
  ];

  get isAuthenticated(): boolean {
    return this.auth.isAuthenticated;
  }

  get isAdmin(): boolean {
    return this.auth.isAuthenticated;
  }

  get displayedColumns(): string[] {
    const base = ['date', 'timeOfDay', 'denomination', 'congregation', 'city', 'preacher', 'specialOccasion', 'status', 'elementCount', 'broadcast'];
    if (this.isAuthenticated) return ['select', ...base, 'audit', 'actions'];
    return base;
  }

  ngOnInit(): void {
    this.api.getCongregations({ pageSize: 1000 }).subscribe({
      next: res => {
        this.congregations.set(res.items.map(c => ({ id: c.id, label: `${c.name} (${c.city})` })));
        this.syncFilterLabels();
      },
    });
    this.api.getPreachers({ pageSize: 1000 }).subscribe({
      next: res => {
        this.preachers.set(res.items.map(p => ({ id: p.id, label: this.formatPreacher(p.title, p.fullName, p.city) })));
        this.syncFilterLabels();
      },
    });
    this.api.getListByName('Denominations').subscribe({
      next: def => this.denominations.set(def.items.map(i => ({ id: i.id, label: i.value }))),
    });

    this.route.queryParamMap.subscribe(params => {
      const congregationId = params.get('congregationId');
      const preacherId = params.get('preacherId');
      if (congregationId) this.filterCongregationId = congregationId;
      if (preacherId) this.filterPreacherId = preacherId;
      this.syncFilterLabels();
      this.page = 1;
      this.load();
    });
  }

  // Keep the autocomplete input text in sync with the selected filter ids
  // (e.g. when a filter is pre-set from a query param or after options load).
  private syncFilterLabels(): void {
    if (this.filterCongregationId) {
      const c = this.congregations().find(x => x.id === this.filterCongregationId);
      if (c) this.filterCongregationText = c.label;
    }
    if (this.filterPreacherId) {
      const p = this.preachers().find(x => x.id === this.filterPreacherId);
      if (p) this.filterPreacherText = p.label;
    }
  }

  get filteredCongregations(): Option[] {
    const q = this.filterCongregationText.trim().toLowerCase();
    const list = this.congregations();
    return q ? list.filter(c => c.label.toLowerCase().includes(q)) : list;
  }

  get filteredPreachers(): Option[] {
    const q = this.filterPreacherText.trim().toLowerCase();
    const list = this.preachers();
    return q ? list.filter(p => p.label.toLowerCase().includes(q)) : list;
  }

  onCongregationInput(): void {
    // Typing clears any prior selection until an option is explicitly chosen.
    this.filterCongregationId = '';
  }

  onCongregationSelected(option: Option | null): void {
    this.filterCongregationId = option?.id ?? '';
    this.filterCongregationText = option?.label ?? '';
  }

  onPreacherInput(): void {
    this.filterPreacherId = '';
  }

  onPreacherSelected(option: Option | null): void {
    this.filterPreacherId = option?.id ?? '';
    this.filterPreacherText = option?.label ?? '';
  }

  load(): void {
    this.loading.set(true);
    this.selectedIds.set(new Set());
    this.expandedId.set(null);
    this.api.getServices({
      page: this.page,
      pageSize: this.pageSize,
      congregationId: this.filterCongregationId || undefined,
      denominationId: this.filterDenominationId || undefined,
      preacherId: this.filterPreacherId || undefined,
      fromDate: this.filterFromDate || undefined,
      toDate: this.filterToDate || undefined,
      includeConcepts: this.includeConcepts,
    }).subscribe({
      next: res => {
        this.services.set(res.items);
        this.totalCount.set(res.totalCount);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  applyFilters(): void {
    this.page = 1;
    this.load();
  }

  clearFilters(): void {
    this.filterCongregationId = '';
    this.filterCongregationText = '';
    this.filterDenominationId = '';
    this.filterPreacherId = '';
    this.filterPreacherText = '';
    this.filterFromDate = '';
    this.filterToDate = '';
    this.includeConcepts = true;
    this.page = 1;
    this.load();
  }

  onPage(e: PageEvent): void {
    this.page = e.pageIndex + 1;
    this.pageSize = e.pageSize;
    this.load();
  }

  timeLabel(value: string): string {
    switch (value) {
      case 'Morning': return 'Morgen';
      case 'Afternoon': return 'Middag';
      case 'Evening': return 'Avond';
      default: return value;
    }
  }

  // --- Selection ---
  isSelected(id: string): boolean {
    return this.selectedIds().has(id);
  }

  toggleSelect(id: string): void {
    const next = new Set(this.selectedIds());
    if (next.has(id)) {
      next.delete(id);
    } else {
      next.add(id);
    }
    this.selectedIds.set(next);
  }

  toggleSelectAll(): void {
    const current = this.selectedIds();
    if (current.size === this.services().length) {
      this.selectedIds.set(new Set());
    } else {
      this.selectedIds.set(new Set(this.services().map(s => s.id)));
    }
  }

  get allSelected(): boolean {
    return this.services().length > 0 && this.selectedIds().size === this.services().length;
  }

  // --- Expandable Onderdelen detail ---
  readonly isExpandedRow = (_index: number, row: ServiceSummary): boolean =>
    this.expandedId() === row.id;

  toggleExpand(row: ServiceSummary): void {
    if (this.expandedId() === row.id) {
      this.expandedId.set(null);
      this.table?.renderRows();
      return;
    }
    this.expandedId.set(row.id);
    this.table?.renderRows();
    if (!this.detailCache()[row.id]) {
      this.loadingDetailId.set(row.id);
      this.api.getService(row.id).subscribe({
        next: detail => {
          this.detailCache.update(c => ({ ...c, [row.id]: detail }));
          this.loadingDetailId.set(null);
        },
        error: () => this.loadingDetailId.set(null),
      });
    }
  }

  detailFor(id: string): ServiceDetail | null {
    return this.detailCache()[id] ?? null;
  }

  songLabel(song: ServiceElementSong): string {
    const bundle = song.bundleAbbreviation || song.bundleName;
    const section = song.section ? `${song.section} ` : '';
    const verses = song.verses?.length ? `:${song.verses.join(',')}` : '';
    return `${bundle} ${section}${song.songNumber}${verses}`;
  }

  private readonly elementTypeLabels: Record<string, string> = {
    Song: 'Lied',
    LiturgicalAct: 'Liturgische handeling',
    Reading: 'Lezing',
    Prayer: 'Gebed',
    Other: 'Overig',
  };

  elementHeading(el: ServiceElement): string {
    return el.label || this.elementTypeLabels[el.elementType] || el.elementType || 'Onderdeel';
  }

  formatPreacher(title: string | null | undefined, name: string | null | undefined, city: string | null | undefined): string {
    const base = [title, name].filter(v => v && v.trim()).join(' ');
    if (!base) return '';
    return city && city.trim() ? `${base} (${city})` : base;
  }

  preacherDisplay(row: ServiceSummary): string {
    return this.formatPreacher(row.preacherTitle, row.preacherName, row.preacherCity);
  }

  sermonTextLabel(detail: ServiceDetail): string {
    if (detail.sermonText?.trim()) return detail.sermonText.trim();
    return (detail.sermonTextReferences ?? [])
      .map(r => this.sermonRefLabel(r))
      .filter(s => s)
      .join('; ');
  }

  private sermonRefLabel(r: SermonTextReference): string {
    if (!r.bookName) return '';
    let s = r.bookName;
    if (r.chapter != null) {
      s += ` ${r.chapter}`;
      if (r.verseStart != null) {
        s += `:${r.verseStart}`;
        if (r.verseEnd != null && r.verseEnd !== r.verseStart) s += `-${r.verseEnd}`;
      }
    }
    return s;
  }

  readingRefsLabel(el: ServiceElement): string {
    if (!el.readingReferences?.length) return '';
    return el.readingReferences
      .map(r => {
        const verses = r.verseStart
          ? `:${r.verseStart}${r.verseEnd && r.verseEnd !== r.verseStart ? '-' + r.verseEnd : ''}`
          : '';
        return `${r.bookName} ${r.chapter ?? ''}${verses}`.trim();
      })
      .join('; ');
  }

  // --- Add / edit overlay ---
  openAdd(): void {
    const ref = this.dialog.open(AddComponent, {
      width: '900px',
      maxWidth: '95vw',
      maxHeight: '90vh',
      data: {} as AddDialogData,
    });
    ref.afterClosed().subscribe(saved => { if (saved) this.load(); });
  }

  openEdit(service: ServiceSummary): void {
    const ref = this.dialog.open(AddComponent, {
      width: '900px',
      maxWidth: '95vw',
      maxHeight: '90vh',
      data: { serviceId: service.id } as AddDialogData,
    });
    ref.afterClosed().subscribe(saved => { if (saved) this.load(); });
  }

  duplicate(service: ServiceSummary): void {
    const ref = this.dialog.open(AddComponent, {
      width: '900px',
      maxWidth: '95vw',
      maxHeight: '90vh',
      data: { duplicateFromId: service.id } as AddDialogData,
    });
    ref.afterClosed().subscribe(saved => { if (saved) this.load(); });
  }

  // --- Bulk operations ---
  get canApplyBulk(): boolean {
    return this.selectedIds().size > 0 && !!this.bulkField;
  }

  applyBulk(): void {
    if (!this.canApplyBulk) return;
    const value = this.bulkField === 'isReadingService'
      ? (this.bulkValue || 'false')
      : this.bulkValue;

    this.api.bulkUpdateServices({
      serviceIds: Array.from(this.selectedIds()),
      field: this.bulkField,
      value: value || null,
    }).subscribe({
      next: () => this.load(),
      error: (err: any) => alert('Fout bij bulk-bewerking: ' + (err?.error?.message ?? err.message)),
    });
  }

  bulkDelete(): void {
    if (this.selectedIds().size === 0) return;
    if (!confirm(`Weet u zeker dat u ${this.selectedIds().size} diensten wilt verwijderen?`)) return;

    this.api.bulkDeleteServices({ serviceIds: Array.from(this.selectedIds()) }).subscribe({
      next: () => this.load(),
      error: (err: any) => alert('Fout bij verwijderen: ' + err.message),
    });
  }
}
