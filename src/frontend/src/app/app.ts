import { Component, OnInit } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { AuthService } from './core/auth/auth.service';
import { AsyncPipe } from '@angular/common';

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
    AsyncPipe,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  navItems = [
    { path: '/', label: 'Home', icon: 'home' },
    { path: '/zoeken', label: 'Zoeken', icon: 'search' },
    { path: '/toevoegen', label: 'Toevoegen', icon: 'add_circle' },
    { path: '/lijsten', label: 'Lijsten', icon: 'list' },
    { path: '/liedcatalogus', label: 'Liedcatalogus', icon: 'music_note' },
    { path: '/instellingen', label: 'Instellingen', icon: 'settings' },
    { path: '/contact', label: 'Contact', icon: 'mail' },
  ];

  constructor(public auth: AuthService) {}

  async ngOnInit(): Promise<void> {
    await this.auth.initialize();
  }

  login(): void {
    this.auth.login();
  }

  logout(): void {
    this.auth.logout();
  }
}
