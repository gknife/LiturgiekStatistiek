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
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { ListDefinition, ListItem } from '../../core/models/api.models';

const LITURGICAL_LABELS_LIST = 'LiturgicalLabels';

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
    public auth: AuthService,
  ) {}

  ngOnInit(): void {
    this.loadLists();
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
}
