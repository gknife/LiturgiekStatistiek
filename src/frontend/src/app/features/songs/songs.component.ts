import { Component, OnInit, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { ListItem, Song } from '../../core/models/api.models';

@Component({
  selector: 'app-songs',
  standalone: true,
  imports: [
    MatCardModule, MatTableModule, MatSelectModule, MatFormFieldModule,
    MatInputModule, MatButtonModule, MatIconModule, MatPaginatorModule, FormsModule,
  ],
  templateUrl: './songs.component.html',
  styleUrl: './songs.component.scss',
})
export class SongsComponent implements OnInit {
  readonly bundles = signal<ListItem[]>([]);
  selectedBundleId: string | null = null;
  readonly songs = signal<Song[]>([]);
  displayedColumns = ['number', 'title', 'numberOfVerses'];
  readonly totalCount = signal(0);
  page = 1;
  pageSize = 50;
  readonly loading = signal(false);
  searchQuery = '';

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.api.getListByName('SongBundles').subscribe({
      next: (list) => {
        this.bundles.set(list.items);
        if (list.items.length > 0) {
          this.selectedBundleId = list.items[0].id;
          this.loadSongs();
        }
      },
    });
  }

  onBundleChange(): void {
    this.page = 1;
    this.loadSongs();
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
}
