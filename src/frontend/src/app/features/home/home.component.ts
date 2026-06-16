import { Component, OnInit, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/services/api.service';
import { marked } from 'marked';

interface HomeStats {
  services: number;
  congregations: number;
  songs: number;
  preachers: number;
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [MatCardModule, MatIconModule, MatButtonModule, RouterLink],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
})
export class HomeComponent implements OnInit {
  readonly stats = signal<HomeStats>({ services: 0, congregations: 0, songs: 0, preachers: 0 });
  readonly contentHtml = signal('');

  constructor(private api: ApiService) {}

  ngOnInit(): void {
    this.api.getServices({ page: 1, pageSize: 1 }).subscribe({
      next: (r) => this.stats.update((s) => ({ ...s, services: r.totalCount })),
      error: () => {},
    });
    this.api.getCongregations({ page: 1, pageSize: 1 }).subscribe({
      next: (r) => this.stats.update((s) => ({ ...s, congregations: r.totalCount })),
      error: () => {},
    });
    this.api.getPreachers({ page: 1, pageSize: 1 }).subscribe({
      next: (r) => this.stats.update((s) => ({ ...s, preachers: r.totalCount })),
      error: () => {},
    });
    this.api.getContent('homepage').subscribe({
      next: (page) => {
        this.contentHtml.set(marked(page.contentMarkdown) as string);
      },
      error: () => {},
    });
  }
}
