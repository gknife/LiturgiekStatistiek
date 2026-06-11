import { Component, OnInit } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/services/api.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [MatCardModule, MatIconModule, MatButtonModule, RouterLink],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
})
export class HomeComponent implements OnInit {
  stats = { services: 0, congregations: 0, songs: 0, preachers: 0 };
  contentHtml = '';

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.api.getServices({ page: 1, pageSize: 1 }).subscribe({
      next: (r) => (this.stats.services = r.totalCount),
      error: () => {},
    });
    this.api.getCongregations({ page: 1, pageSize: 1 }).subscribe({
      next: (r) => (this.stats.congregations = r.totalCount),
      error: () => {},
    });
    this.api.getPreachers({ page: 1, pageSize: 1 }).subscribe({
      next: (r) => (this.stats.preachers = r.totalCount),
      error: () => {},
    });
  }
}
