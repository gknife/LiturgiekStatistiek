import { Component, OnInit } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AuthService } from './core/auth/auth.service';
import { ThemeService } from './core/services/theme.service';
import { AsyncPipe } from '@angular/common';

interface NavItem {
  path: string;
  label: string;
  icon: string;
  requiresAuth?: boolean;
}

@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatSidenavModule,
    MatListModule,
    MatMenuModule,
    MatTooltipModule,
    AsyncPipe,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  allNavItems: NavItem[] = [
    { path: '/', label: 'Home', icon: 'home' },
    { path: '/zoeken', label: 'Zoeken', icon: 'search' },
    { path: '/diensten', label: 'Diensten', icon: 'church' },
    { path: '/sjablonen', label: 'Sjablonen', icon: 'dashboard_customize', requiresAuth: true },
    { path: '/lijsten', label: 'Lijsten', icon: 'list', requiresAuth: true },
    { path: '/liedcatalogus', label: 'Liedcatalogus', icon: 'music_note', requiresAuth: true },
    { path: '/contact', label: 'Contact', icon: 'mail' },
  ];

  constructor(public auth: AuthService, private theme: ThemeService) {}

  get visibleNavItems(): NavItem[] {
    return this.allNavItems.filter(
      item => !item.requiresAuth || this.auth.isAuthenticated
    );
  }

  async ngOnInit(): Promise<void> {
    await this.auth.initialize();
    // Once auth state is known, load DB-backed settings for logged-in users.
    this.theme.loadFromDb();
  }

  login(): void {
    this.auth.login();
  }

  logout(): void {
    this.auth.logout();
  }
}
