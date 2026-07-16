import { Component, OnInit, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatButtonModule } from '@angular/material/button';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatSelectModule } from '@angular/material/select';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/auth/auth.service';
import {
  ListDefinition, ListItem, Congregation, Preacher, CongregationPastorInput,
} from '../../core/models/api.models';

const LITURGICAL_LABELS_LIST = 'LiturgicalLabels';

interface Option {
  id: string;
  label: string;
}

interface CongregationEdit {
  id: string | null;
  name: string;
  city: string;
  locationDetail: string;
  denominationId: string | null;
  modality: string | null;
  pastorIds: string[];
  primaryPastorId: string | null;
}

interface PreacherEdit {
  id: string | null;
  fullName: string;
  city: string;
  denominationId: string | null;
  titleId: string | null;
}

@Component({
  selector: 'app-lists',
  standalone: true,
  imports: [
    MatCardModule, MatTableModule, MatExpansionModule,
    MatChipsModule, MatIconModule, MatInputModule,
    MatFormFieldModule, MatButtonModule, MatTooltipModule, MatDialogModule,
    MatSnackBarModule, MatSelectModule, FormsModule,
  ],
  templateUrl: './lists.component.html',
  styleUrl: './lists.component.scss',
})
export class ListsComponent implements OnInit {
  private allLists: ListDefinition[] = [];
  readonly filteredLists = signal<ListDefinition[]>([]);
  searchQuery = '';
  readonly loading = signal(true);

  // Managed reference entities
  readonly congregations = signal<Congregation[]>([]);
  readonly preachers = signal<Preacher[]>([]);
  readonly denominations = signal<Option[]>([]);
  readonly titles = signal<Option[]>([]);

  editingCongregation: CongregationEdit | null = null;
  editingPreacher: PreacherEdit | null = null;

  readonly elementTypes = [
    { value: 0, label: 'Lied' },
    { value: 1, label: 'Liturgische handeling' },
    { value: 2, label: 'Lezing' },
    { value: 3, label: 'Gebed' },
    { value: 4, label: 'Overig' },
  ];

  // Inline editing state
  editingItem: { listId: string; itemId: string | null; value: string; abbreviation: string; liturgicalElementType: number | null } | null = null;

  constructor(
    private api: ApiService,
    private snackBar: MatSnackBar,
    private router: Router,
    public auth: AuthService,
  ) {}

  ngOnInit(): void {
    this.loadLists();
    this.loadEntities();
  }

  loadLists(): void {
    this.loading.set(true);
    this.api.getAllLists().subscribe({
      next: (lists) => {
        this.allLists = lists.sort((a, b) =>
          (a.description ?? a.name).localeCompare(b.description ?? b.name, 'nl')
        );
        this.applyFilter();
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      },
    });
  }

  getDisplayName(list: ListDefinition): string {
    return list.description ?? list.name;
  }

  isLabelsList(list: ListDefinition): boolean {
    return list.name === LITURGICAL_LABELS_LIST;
  }

  displayedColumns(list: ListDefinition): string[] {
    const cols = ['value', 'abbreviation'];
    if (this.isLabelsList(list)) cols.push('type');
    if (this.auth.isAuthenticated) cols.push('actions');
    return cols;
  }

  elementTypeLabel(value: number | null | undefined): string {
    if (value === null || value === undefined) return '';
    return this.elementTypes.find(t => t.value === value)?.label ?? '';
  }

  auditTooltip(item: ListItem): string {
    const parts: string[] = [];
    const fmt = (d?: string | null) => d ? new Date(d).toLocaleString('nl-NL') : '';
    if (item.createdBy) parts.push(`Toegevoegd door ${item.createdBy}${item.createdAt ? ' op ' + fmt(item.createdAt) : ''}`);
    if (item.modifiedBy) parts.push(`Laatst bewerkt door ${item.modifiedBy}${item.modifiedAt ? ' op ' + fmt(item.modifiedAt) : ''}`);
    return parts.join('\n');
  }

  filterLists(): void {
    this.applyFilter();
  }

  private applyFilter(): void {
    const q = this.searchQuery.toLowerCase();
    if (!q) {
      this.filteredLists.set([...this.allLists]);
      return;
    }
    this.filteredLists.set(
      this.allLists.filter(
        (l) =>
          (l.description ?? l.name).toLowerCase().includes(q) ||
          l.items.some(
            (i) =>
              i.value.toLowerCase().includes(q) ||
              (i.abbreviation && i.abbreviation.toLowerCase().includes(q))
          )
      )
    );
  }

  // --- Edit functionality ---

  startAdd(list: ListDefinition): void {
    this.editingItem = { listId: list.id, itemId: null, value: '', abbreviation: '', liturgicalElementType: null };
  }

  startEdit(list: ListDefinition, item: ListItem): void {
    this.editingItem = {
      listId: list.id,
      itemId: item.id,
      value: item.value,
      abbreviation: item.abbreviation ?? '',
      liturgicalElementType: item.liturgicalElementType ?? null,
    };
  }

  cancelEdit(): void {
    this.editingItem = null;
  }

  saveItem(): void {
    if (!this.editingItem || !this.editingItem.value.trim()) return;

    if (this.editingItem.itemId) {
      this.api.updateListItem(this.editingItem.itemId, {
        value: this.editingItem.value.trim(),
        abbreviation: this.editingItem.abbreviation.trim() || null,
        sortOrder: 0,
        isActive: true,
        liturgicalElementType: this.editingItem.liturgicalElementType,
      }).subscribe({
        next: () => {
          this.snackBar.open('Item bijgewerkt', 'OK', { duration: 2000 });
          this.editingItem = null;
          this.loadLists();
        },
        error: () => this.snackBar.open('Fout bij opslaan', 'OK', { duration: 3000 }),
      });
    } else {
      const list = this.allLists.find(l => l.id === this.editingItem!.listId);
      const maxSort = list ? Math.max(0, ...list.items.map(i => i.sortOrder)) : 0;
      this.api.addListItem({
        listDefinitionId: this.editingItem.listId,
        value: this.editingItem.value.trim(),
        abbreviation: this.editingItem.abbreviation.trim() || null,
        sortOrder: maxSort + 1,
      }).subscribe({
        next: () => {
          this.snackBar.open('Item toegevoegd', 'OK', { duration: 2000 });
          this.editingItem = null;
          this.loadLists();
        },
        error: () => this.snackBar.open('Fout bij toevoegen', 'OK', { duration: 3000 }),
      });
    }
  }

  deleteItem(item: ListItem): void {
    if (!confirm(`Weet u zeker dat u "${item.value}" wilt verwijderen?`)) return;
    this.api.deleteListItem(item.id).subscribe({
      next: () => {
        this.snackBar.open('Item verwijderd', 'OK', { duration: 2000 });
        this.loadLists();
      },
      error: () => this.snackBar.open('Fout bij verwijderen', 'OK', { duration: 3000 }),
    });
  }

  // --- Managed reference entities (Gemeenten & Voorgangers) ---

  private loadEntities(): void {
    this.refreshEntities();
    this.api.getListByName('Denominations').subscribe({
      next: def => this.denominations.set(def.items.map(i => ({ id: i.id, label: i.value }))),
    });
    this.api.getListByName('PreacherTitles').subscribe({
      next: def => this.titles.set(def.items.map(i => ({ id: i.id, label: i.value }))),
    });
  }

  private refreshEntities(): void {
    forkJoin({
      congregations: this.api.getCongregations({ pageSize: 1000 }),
      preachers: this.api.getPreachers({ pageSize: 1000 }),
    }).subscribe({
      next: ({ congregations, preachers }) => {
        this.congregations.set(
          congregations.items.sort((a, b) => a.name.localeCompare(b.name, 'nl'))
        );
        this.preachers.set(
          preachers.items.sort((a, b) => a.fullName.localeCompare(b.fullName, 'nl'))
        );
      },
    });
  }

  denominationLabel(id: string | null): string {
    if (!id) return '';
    return this.denominations().find(d => d.id === id)?.label ?? '';
  }

  titleLabel(id: string | null): string {
    if (!id) return '';
    return this.titles().find(t => t.id === id)?.label ?? '';
  }

  preacherName(id: string): string {
    return this.preachers().find(p => p.id === id)?.fullName ?? '';
  }

  // Navigate to the diensten overview filtered by this entity.
  viewCongregationServices(c: Congregation): void {
    if (c.serviceCount > 0) {
      this.router.navigate(['/diensten'], { queryParams: { congregationId: c.id } });
    }
  }

  viewPreacherServices(p: Preacher): void {
    if (p.serviceCount > 0) {
      this.router.navigate(['/diensten'], { queryParams: { preacherId: p.id } });
    }
  }

  // --- Gemeente edit ---

  startAddCongregation(): void {
    this.editingCongregation = {
      id: null, name: '', city: '', locationDetail: '',
      denominationId: null, modality: null, pastorIds: [], primaryPastorId: null,
    };
  }

  startEditCongregation(c: Congregation): void {
    const pastors = c.pastors ?? [];
    this.editingCongregation = {
      id: c.id,
      name: c.name,
      city: c.city,
      locationDetail: c.locationDetail ?? '',
      denominationId: c.denominationId,
      modality: c.modality,
      pastorIds: pastors.map(p => p.preacherId),
      primaryPastorId: pastors.find(p => p.isPrimary)?.preacherId ?? null,
    };
  }

  cancelCongregationEdit(): void {
    this.editingCongregation = null;
  }

  saveCongregation(): void {
    const e = this.editingCongregation;
    if (!e || !e.name.trim() || !e.city.trim()) return;

    const pastors: CongregationPastorInput[] = e.pastorIds.map(id => ({
      preacherId: id,
      isPrimary: id === e.primaryPastorId,
    }));

    const request = {
      name: e.name.trim(),
      city: e.city.trim(),
      locationDetail: e.locationDetail.trim() || null,
      denominationId: e.denominationId,
      modality: e.modality,
      latitude: null,
      longitude: null,
      pastors,
    };

    const done = (msg: string) => {
      this.snackBar.open(msg, 'OK', { duration: 2000 });
      this.editingCongregation = null;
      this.refreshEntities();
    };
    const fail = () => this.snackBar.open('Fout bij opslaan', 'OK', { duration: 3000 });

    if (e.id) {
      this.api.updateCongregation(e.id, request).subscribe({ next: () => done('Gemeente bijgewerkt'), error: fail });
    } else {
      this.api.createCongregation(request).subscribe({ next: () => done('Gemeente toegevoegd'), error: fail });
    }
  }

  deleteCongregation(c: Congregation): void {
    if (c.serviceCount > 0) return;
    if (!confirm(`Weet u zeker dat u gemeente "${c.name}" wilt verwijderen?`)) return;
    this.api.deleteCongregation(c.id).subscribe({
      next: () => {
        this.snackBar.open('Gemeente verwijderd', 'OK', { duration: 2000 });
        this.refreshEntities();
      },
      error: (err) => {
        const msg = err?.status === 409
          ? 'Gemeente heeft nog gekoppelde diensten en kan niet worden verwijderd.'
          : 'Fout bij verwijderen';
        this.snackBar.open(msg, 'OK', { duration: 3000 });
      },
    });
  }

  // --- Voorganger edit ---

  startAddPreacher(): void {
    this.editingPreacher = { id: null, fullName: '', city: '', denominationId: null, titleId: null };
  }

  startEditPreacher(p: Preacher): void {
    this.editingPreacher = {
      id: p.id,
      fullName: p.fullName,
      city: p.city ?? '',
      denominationId: p.denominationId,
      titleId: p.titleId,
    };
  }

  cancelPreacherEdit(): void {
    this.editingPreacher = null;
  }

  congregationColumns(): string[] {
    const cols = ['denomination', 'name', 'city', 'services'];
    if (this.auth.isAuthenticated) cols.push('actions');
    return cols;
  }

  preacherColumns(): string[] {
    const cols = ['title', 'name', 'city', 'denomination', 'services'];
    if (this.auth.isAuthenticated) cols.push('actions');
    return cols;
  }

  savePreacher(): void {
    const e = this.editingPreacher;
    if (!e || !e.fullName.trim()) return;

    const request = {
      fullName: e.fullName.trim(),
      city: e.city.trim() || null,
      denominationId: e.denominationId,
      titleId: e.titleId,
    };

    const done = (msg: string) => {
      this.snackBar.open(msg, 'OK', { duration: 2000 });
      this.editingPreacher = null;
      this.refreshEntities();
    };
    const fail = () => this.snackBar.open('Fout bij opslaan', 'OK', { duration: 3000 });

    if (e.id) {
      this.api.updatePreacher(e.id, request).subscribe({ next: () => done('Voorganger bijgewerkt'), error: fail });
    } else {
      this.api.createPreacher(request).subscribe({ next: () => done('Voorganger toegevoegd'), error: fail });
    }
  }

  deletePreacher(p: Preacher): void {
    if (p.serviceCount > 0) return;
    if (!confirm(`Weet u zeker dat u voorganger "${p.fullName}" wilt verwijderen?`)) return;
    this.api.deletePreacher(p.id).subscribe({
      next: () => {
        this.snackBar.open('Voorganger verwijderd', 'OK', { duration: 2000 });
        this.refreshEntities();
      },
      error: (err) => {
        const msg = err?.status === 409
          ? 'Voorganger heeft nog gekoppelde diensten en kan niet worden verwijderd.'
          : 'Fout bij verwijderen';
        this.snackBar.open(msg, 'OK', { duration: 3000 });
      },
    });
  }
}
