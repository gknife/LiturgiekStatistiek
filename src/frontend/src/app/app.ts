import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';

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
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  navItems = [
    { path: '/', label: 'Home', icon: 'home' },
    { path: '/zoeken', label: 'Zoeken', icon: 'search' },
    { path: '/toevoegen', label: 'Toevoegen', icon: 'add_circle' },
    { path: '/lijsten', label: 'Lijsten', icon: 'list' },
    { path: '/liedcatalogus', label: 'Liedcatalogus', icon: 'music_note' },
    { path: '/instellingen', label: 'Instellingen', icon: 'settings' },
    { path: '/contact', label: 'Contact', icon: 'mail' },
  ];
}
