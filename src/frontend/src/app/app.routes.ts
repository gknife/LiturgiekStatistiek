import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/home/home.component').then(m => m.HomeComponent),
  },
  {
    path: 'zoeken',
    loadComponent: () => import('./features/query/query.component').then(m => m.QueryComponent),
    canActivate: [authGuard],
  },
  {
    path: 'diensten',
    loadComponent: () => import('./features/services.component').then(m => m.ServicesComponent),
    canActivate: [authGuard],
  },
  {
    path: 'toevoegen',
    redirectTo: 'diensten',
  },
  {
    path: 'sjablonen',
    loadComponent: () => import('./features/templates/templates.component').then(m => m.TemplatesComponent),
    canActivate: [authGuard],
  },
  {
    path: 'lijsten',
    loadComponent: () => import('./features/lists/lists.component').then(m => m.ListsComponent),
    canActivate: [authGuard],
  },
  {
    path: 'liedcatalogus',
    loadComponent: () => import('./features/songs/songs.component').then(m => m.SongsComponent),
    canActivate: [authGuard],
  },
  {
    path: 'instellingen',
    loadComponent: () => import('./features/settings/settings.component').then(m => m.SettingsComponent),
    canActivate: [authGuard],
  },
  {
    path: 'contact',
    loadComponent: () => import('./features/contact/contact.component').then(m => m.ContactComponent),
    canActivate: [authGuard],
  },
  {
    path: 'beheer',
    loadComponent: () => import('./features/admin/admin.component').then(m => m.AdminComponent),
    canActivate: [authGuard],
  },
  {
    path: '**',
    redirectTo: '',
  },
];
