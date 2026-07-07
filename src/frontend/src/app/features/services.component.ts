import { Component, OnInit, signal, inject, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule, MatTable } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ApiService } from '../core/services/api.service';
import { AuthService } from '../core/auth/auth.service';
import { ServiceSummary, ServiceDetail, ServiceElement, ServiceElementSong } from '../core/models/api.models';
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
    MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule,
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

  @ViewChild(MatTable) private table?: MatTable<ServiceSummary>;

  readonly services = signal<ServiceSummary[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly congregations = signal<Option[]>([]);
  readonly denominations = signal<Option[]>([]);
  readonly selectedIds = signal<Set<string>>(new Set());

  readonly expandedId = signal<string | null>(null);
  readonly loadingDetailId = signal<string | null>(null);
  private readonly detailCache = signal<Record<string, ServiceDetail>>({});

  page = 1;
  pageSize = 20;

  filterCongregationId = '';
  filterDenominationId = '';
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
    if (this.isAuthenticated) return ['select', ...base, 'actions'];
    return base;
  }

  ngOnInit(): void {
    this.load();
    this.api.getCongregations({ pageSize: 1000 }).subscribe({
      next: res => this.congregations.set(res.items.map(c => ({ id: c.id, label: `${c.name} (${c.city})` }))),
    });
    this.api.getListByName('Denominations').subscribe({
      next: def => this.denominations.set(def.items.map(i => ({ id: i.id, label: i.value }))),
    });
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
    this.filterDenominationId = '';
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

  elementHeading(el: ServiceElement): string {
    return el.label || el.elementType || 'Onderdeel';
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
