import { Component, OnInit, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/auth/auth.service';
import { ListItem, Song, SongVerse, BundleSection } from '../../core/models/api.models';

interface SongForm {
  id: string | null;
  section: string;
  number: number | null;
  title: string;
  verses: { number: number; title: string; label: string }[];
}

interface BundleForm {
  id: string | null;
  value: string;
  abbreviation: string;
  sortOrder: number;
}

@Component({
  selector: 'app-songs',
  standalone: true,
  imports: [
    MatCardModule, MatTableModule, MatSelectModule, MatFormFieldModule,
    MatInputModule, MatButtonModule, MatIconModule, MatTooltipModule,
    MatSnackBarModule, MatPaginatorModule, FormsModule,
  ],
  templateUrl: './songs.component.html',
  styleUrl: './songs.component.scss',
})
export class SongsComponent implements OnInit {
  readonly bundles = signal<ListItem[]>([]);
  selectedBundleId: string | null = null;
  bundleListDefId: string | null = null;
  readonly songs = signal<Song[]>([]);
  readonly expandedSongId = signal<string | null>(null);
  readonly versesBySong = signal<Record<string, SongVerse[]>>({});
  readonly totalCount = signal(0);
  page = 1;
  pageSize = 50;
  readonly loading = signal(false);
  searchQuery = '';

  readonly songForm = signal<SongForm | null>(null);
  readonly bundleForm = signal<BundleForm | null>(null);
  readonly saving = signal(false);

  readonly sections = signal<BundleSection[]>([]);
  readonly showSectionsPanel = signal(false);
  newSectionValue = '';

  constructor(
    private api: ApiService,
    private snackBar: MatSnackBar,
    public auth: AuthService,
  ) {}

  ngOnInit(): void {
    this.loadBundles();
  }

  loadBundles(selectId?: string): void {
    this.api.getListByName('SongBundles').subscribe({
      next: (list) => {
        this.bundleListDefId = list.id;
        this.bundles.set(list.items);
        if (list.items.length > 0) {
          this.selectedBundleId = selectId && list.items.some((b) => b.id === selectId)
            ? selectId
            : (list.items.some((b) => b.id === this.selectedBundleId) ? this.selectedBundleId : list.items[0].id);
          this.loadSongs();
          this.loadSections();
        } else {
          this.selectedBundleId = null;
          this.songs.set([]);
          this.sections.set([]);
          this.totalCount.set(0);
        }
      },
    });
  }

  get canEdit(): boolean {
    return this.auth.isAuthenticated;
  }

  get selectedBundle(): ListItem | undefined {
    return this.bundles().find((b) => b.id === this.selectedBundleId);
  }

  get hasSections(): boolean {
    return this.songs().some((s) => !!s.section);
  }

  get displayedColumns(): string[] {
    const cols: string[] = [];
    if (this.hasSections) cols.push('section');
    cols.push('number', 'title', 'numberOfVerses');
    if (this.canEdit) cols.push('actions');
    return cols;
  }

  onBundleChange(): void {
    this.page = 1;
    this.expandedSongId.set(null);
    this.showSectionsPanel.set(false);
    this.loadSongs();
    this.loadSections();
  }

  onPageChange(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.loadSongs();
  }

  loadSongs(): void {
    if (!this.selectedBundleId) return;
    this.loading.set(true);
    this.api.getSongsByBundle(this.selectedBundleId, this.page, this.pageSize).subscribe({
      next: (result) => {
        this.songs.set(result.items);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  get filteredSongs(): Song[] {
    const songs = this.songs();
    if (!this.searchQuery) return songs;
    const q = this.searchQuery.toLowerCase();
    return songs.filter(
      (s) => s.number.toString().includes(q) || (s.title && s.title.toLowerCase().includes(q))
    );
  }

  toggleVerses(song: Song): void {
    if (this.expandedSongId() === song.id) {
      this.expandedSongId.set(null);
      return;
    }
    this.expandedSongId.set(song.id);
    if (this.versesBySong()[song.id]) return;
    this.api.getSong(song.id).subscribe({
      next: (full) => this.versesBySong.update((m) => ({ ...m, [song.id]: full.verses ?? [] })),
      error: () => this.versesBySong.update((m) => ({ ...m, [song.id]: [] })),
    });
  }

  versesOf(song: Song): SongVerse[] {
    return this.versesBySong()[song.id] ?? [];
  }

  // --- Song CRUD ---
  openAddSong(): void {
    this.bundleForm.set(null);
    this.songForm.set({
      id: null,
      section: this.defaultSectionValue,
      number: null,
      title: '',
      verses: [],
    });
  }

  openEditSong(song: Song): void {
    this.bundleForm.set(null);
    this.api.getSong(song.id).subscribe({
      next: (full) => {
        this.songForm.set({
          id: full.id,
          section: full.section ?? '',
          number: full.number,
          title: full.title ?? '',
          verses: (full.verses ?? []).map((v) => ({ number: v.number, title: v.title ?? '', label: v.label ?? '' })),
        });
      },
    });
  }

  cancelSongForm(): void {
    this.songForm.set(null);
  }

  addVerseRow(): void {
    const form = this.songForm();
    if (!form) return;
    const next = form.verses.length > 0 ? Math.max(...form.verses.map((v) => v.number)) + 1 : 1;
    this.songForm.set({ ...form, verses: [...form.verses, { number: next, title: '', label: '' }] });
  }

  addNamedVerseRow(): void {
    const form = this.songForm();
    if (!form) return;
    this.songForm.set({ ...form, verses: [...form.verses, { number: 0, title: '', label: 'Voorzang' }] });
  }

  removeVerseRow(index: number): void {
    const form = this.songForm();
    if (!form) return;
    const verses = form.verses.filter((_, i) => i !== index);
    this.songForm.set({ ...form, verses });
  }

  saveSong(): void {
    const form = this.songForm();
    if (!form || !this.selectedBundleId) return;
    if (form.number == null) {
      this.snackBar.open('Nummer is verplicht', 'OK', { duration: 3000 });
      return;
    }
    const verses: SongVerse[] = form.verses.map((v) => ({
      number: v.number,
      title: v.title || null,
      label: v.label?.trim() ? v.label.trim() : null,
    }));
    this.saving.set(true);
    const done = (msg: string) => {
      this.snackBar.open(msg, 'OK', { duration: 2000 });
      const editedId = form.id;
      this.songForm.set(null);
      this.saving.set(false);
      if (editedId) {
        this.versesBySong.update((m) => {
          const copy = { ...m };
          delete copy[editedId];
          return copy;
        });
      }
      this.loadSongs();
    };
    const fail = () => {
      this.snackBar.open('Fout bij opslaan', 'OK', { duration: 3000 });
      this.saving.set(false);
    };

    if (form.id) {
      this.api.updateSong(form.id, {
        section: form.section,
        number: form.number,
        title: form.title || undefined,
        numberOfVerses: verses.length || undefined,
        verses,
      }).subscribe({ next: () => done('Lied bijgewerkt'), error: fail });
    } else {
      this.api.createSong({
        bundleId: this.selectedBundleId,
        section: form.section,
        number: form.number,
        title: form.title || undefined,
        numberOfVerses: verses.length || undefined,
        verses,
      }).subscribe({ next: () => done('Lied toegevoegd'), error: fail });
    }
  }

  deleteSong(song: Song): void {
    const label = song.section ? `${song.section} ${song.number}` : `nr. ${song.number}`;
    if (!confirm(`Weet u zeker dat u "${label}" wilt verwijderen?`)) return;
    this.api.deleteSong(song.id).subscribe({
      next: () => {
        this.snackBar.open('Lied verwijderd', 'OK', { duration: 2000 });
        this.loadSongs();
      },
      error: () => this.snackBar.open('Fout bij verwijderen', 'OK', { duration: 3000 }),
    });
  }

  // --- Rubriek (section) management ---
  loadSections(): void {
    if (!this.selectedBundleId) {
      this.sections.set([]);
      return;
    }
    this.api.getBundleSections(this.selectedBundleId).subscribe({
      next: (s) => this.sections.set(s),
      error: () => this.sections.set([]),
    });
  }

  get defaultSectionValue(): string {
    return this.sections().find((s) => s.isDefault)?.value ?? '';
  }

  toggleSectionsPanel(): void {
    this.songForm.set(null);
    this.bundleForm.set(null);
    this.showSectionsPanel.update((v) => !v);
  }

  addSection(): void {
    const val = this.newSectionValue.trim();
    if (!val || !this.selectedBundleId) return;
    const isFirst = this.sections().length === 0;
    this.api.createBundleSection(this.selectedBundleId, {
      value: val,
      sortOrder: this.sections().length,
      isDefault: isFirst,
    }).subscribe({
      next: () => {
        this.newSectionValue = '';
        this.loadSections();
        this.snackBar.open('Rubriek toegevoegd', 'OK', { duration: 2000 });
      },
      error: () => this.snackBar.open('Fout bij opslaan (bestaat de rubriek al?)', 'OK', { duration: 3000 }),
    });
  }

  renameSection(section: BundleSection, newValue: string): void {
    const val = newValue.trim();
    if (!val || val === section.value) return;
    this.api.updateBundleSection(section.id, {
      value: val,
      sortOrder: section.sortOrder,
      isDefault: section.isDefault,
      isActive: section.isActive,
    }).subscribe({
      next: () => {
        this.loadSections();
        this.loadSongs();
        this.snackBar.open('Rubriek hernoemd', 'OK', { duration: 2000 });
      },
      error: () => this.snackBar.open('Fout bij opslaan', 'OK', { duration: 3000 }),
    });
  }

  setDefaultSection(section: BundleSection): void {
    if (section.isDefault) return;
    this.api.updateBundleSection(section.id, {
      value: section.value,
      sortOrder: section.sortOrder,
      isDefault: true,
      isActive: section.isActive,
    }).subscribe({
      next: () => this.loadSections(),
      error: () => this.snackBar.open('Fout bij opslaan', 'OK', { duration: 3000 }),
    });
  }

  deleteSection(section: BundleSection): void {
    if (!confirm(`Rubriek "${section.value}" verwijderen? Bestaande liederen behouden hun categorie.`)) return;
    this.api.deleteBundleSection(section.id).subscribe({
      next: () => {
        this.loadSections();
        this.snackBar.open('Rubriek verwijderd', 'OK', { duration: 2000 });
      },
      error: () => this.snackBar.open('Fout bij verwijderen', 'OK', { duration: 3000 }),
    });
  }

  // --- Bundle CRUD ---
  openAddBundle(): void {
    this.songForm.set(null);
    const nextOrder = this.bundles().length > 0 ? Math.max(...this.bundles().map((b) => b.sortOrder)) + 1 : 1;
    this.bundleForm.set({ id: null, value: '', abbreviation: '', sortOrder: nextOrder });
  }

  openEditBundle(): void {
    const b = this.selectedBundle;
    if (!b) return;
    this.songForm.set(null);
    this.bundleForm.set({ id: b.id, value: b.value, abbreviation: b.abbreviation ?? '', sortOrder: b.sortOrder });
  }

  cancelBundleForm(): void {
    this.bundleForm.set(null);
  }

  saveBundle(): void {
    const form = this.bundleForm();
    if (!form || !this.bundleListDefId) return;
    if (!form.value.trim()) {
      this.snackBar.open('Naam is verplicht', 'OK', { duration: 3000 });
      return;
    }
    this.saving.set(true);
    const fail = () => {
      this.snackBar.open('Fout bij opslaan', 'OK', { duration: 3000 });
      this.saving.set(false);
    };
    if (form.id) {
      this.api.updateListItem(form.id, {
        value: form.value.trim(),
        abbreviation: form.abbreviation.trim() || null,
        sortOrder: form.sortOrder,
        isActive: true,
      }).subscribe({
        next: () => {
          this.snackBar.open('Bundel bijgewerkt', 'OK', { duration: 2000 });
          const id = form.id!;
          this.bundleForm.set(null);
          this.saving.set(false);
          this.loadBundles(id);
        },
        error: fail,
      });
    } else {
      this.api.addListItem({
        listDefinitionId: this.bundleListDefId,
        value: form.value.trim(),
        abbreviation: form.abbreviation.trim() || null,
        sortOrder: form.sortOrder,
      }).subscribe({
        next: (created) => {
          this.snackBar.open('Bundel toegevoegd', 'OK', { duration: 2000 });
          this.bundleForm.set(null);
          this.saving.set(false);
          this.loadBundles(created.id);
        },
        error: fail,
      });
    }
  }

  deleteBundle(): void {
    const b = this.selectedBundle;
    if (!b) return;
    if (!confirm(`Weet u zeker dat u bundel "${b.value}" en alle bijbehorende liederen wilt verwijderen?`)) return;
    this.api.deleteListItem(b.id).subscribe({
      next: () => {
        this.snackBar.open('Bundel verwijderd', 'OK', { duration: 2000 });
        this.selectedBundleId = null;
        this.loadBundles();
      },
      error: () => this.snackBar.open('Fout bij verwijderen', 'OK', { duration: 3000 }),
    });
  }
}
