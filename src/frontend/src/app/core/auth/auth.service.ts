import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface UserProfile {
  name: string;
  email: string;
  roles: string[];
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private _isAuthenticated = new BehaviorSubject<boolean>(false);
  private _user = new BehaviorSubject<UserProfile | null>(null);

  isAuthenticated$ = this._isAuthenticated.asObservable();
  user$ = this._user.asObservable();

  get isAuthenticated(): boolean {
    return this._isAuthenticated.value;
  }

  get isAdmin(): boolean {
    return this._user.value?.roles.includes('Admin') ?? false;
  }

  get isResearcher(): boolean {
    const roles = this._user.value?.roles ?? [];
    return roles.includes('Admin') || roles.includes('Researcher');
  }

  async login(): Promise<void> {
    // In production, this uses MSAL. For dev, we simulate authentication.
    if (!environment.production) {
      this._isAuthenticated.next(true);
      this._user.next({
        name: 'Onderzoeker (Dev)',
        email: 'dev@liturgiekstatistiek.nl',
        roles: ['Admin', 'Researcher'],
      });
      return;
    }

    // TODO: MSAL login flow
    // const msalInstance = ...
    // const response = await msalInstance.loginPopup({ scopes: environment.msalConfig.scopes });
  }

  logout(): void {
    this._isAuthenticated.next(false);
    this._user.next(null);
  }
}
