import { Component, OnInit } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { ListDefinition } from '../../core/models/api.models';

@Component({
  selector: 'app-lists',
  standalone: true,
  imports: [
    MatCardModule, MatTableModule, MatExpansionModule,
    MatChipsModule, MatIconModule, MatInputModule,
    MatFormFieldModule, FormsModule,
  ],
  templateUrl: './lists.component.html',
  styleUrl: './lists.component.scss',
})
export class ListsComponent implements OnInit {
  lists: ListDefinition[] = [];
  filteredLists: ListDefinition[] = [];
  searchQuery = '';
  loading = true;

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.api.getAllLists().subscribe({
      next: (lists) => {
        this.lists = lists;
        this.filteredLists = lists;
        this.loading = false;
      },
      error: () => (this.loading = false),
    });
  }

  filterLists(): void {
    const q = this.searchQuery.toLowerCase();
    this.filteredLists = this.lists.filter(
      (l) =>
        l.name.toLowerCase().includes(q) ||
        l.items.some(
          (i) =>
            i.value.toLowerCase().includes(q) ||
            (i.abbreviation && i.abbreviation.toLowerCase().includes(q))
        )
    );
  }
}
