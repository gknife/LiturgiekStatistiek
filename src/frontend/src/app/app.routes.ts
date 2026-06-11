import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/home/home.component').then(m => m.HomeComponent),
  },
  {
    path: 'zoeken',
    loadComponent: () => import('./features/query/query.component').then(m => m.QueryComponent),
  },
  {
    path: 'toevoegen',
    loadComponent: () => import('./features/add/add.component').then(m => m.AddComponent),
  },
  {
    path: 'lijsten',
    loadComponent: () => import('./features/lists/lists.component').then(m => m.ListsComponent),
  },
  {
    path: 'liedcatalogus',
    loadComponent: () => import('./features/songs/songs.component').then(m => m.SongsComponent),
  },
  {
    path: 'instellingen',
    loadComponent: () => import('./features/settings/settings.component').then(m => m.SettingsComponent),
  },
  {
    path: 'contact',
    loadComponent: () => import('./features/contact/contact.component').then(m => m.ContactComponent),
  },
  {
    path: 'beheer',
    loadComponent: () => import('./features/admin/admin.component').then(m => m.AdminComponent),
  },
  {
    path: '**',
    redirectTo: '',
  },
];
