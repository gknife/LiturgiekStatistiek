import { Component, OnInit, signal, inject } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RouterLink } from '@angular/router';
import { AsyncPipe } from '@angular/common';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/auth/auth.service';
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
  imports: [MatCardModule, MatIconModule, MatButtonModule, MatProgressSpinnerModule, RouterLink, AsyncPipe],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
})
export class HomeComponent implements OnInit {
  private readonly api = inject(ApiService);
  readonly auth = inject(AuthService);

  readonly stats = signal<HomeStats>({ services: 0, congregations: 0, songs: 0, preachers: 0 });
  readonly contentHtml = signal('');
  readonly loggingIn = signal(false);

  ngOnInit(): void {
    this.auth.isAuthenticated$.subscribe(isAuth => {
      if (isAuth) this.loadDashboard();
    });
  }

  private loadDashboard(): void {
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

  async login(): Promise<void> {
    this.loggingIn.set(true);
    try {
      await this.auth.login();
    } finally {
      this.loggingIn.set(false);
    }
  }
}
