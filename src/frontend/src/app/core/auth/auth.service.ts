import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  PublicClientApplication,
  InteractionRequiredAuthError,
  AccountInfo,
  AuthenticationResult,
} from '@azure/msal-browser';

export interface UserProfile {
  name: string;
  email: string;
  roles: string[];
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private _isAuthenticated = new BehaviorSubject<boolean>(false);
  private _user = new BehaviorSubject<UserProfile | null>(null);
  private msalInstance: PublicClientApplication;
  private initialized = false;

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

  constructor() {
    this.msalInstance = new PublicClientApplication({
      auth: {
        clientId: environment.msalConfig.auth.clientId,
        authority: environment.msalConfig.auth.authority,
        redirectUri: environment.msalConfig.auth.redirectUri,
      },
      cache: {
        cacheLocation: 'localStorage',
      },
    });
  }

  async initialize(): Promise<void> {
    if (this.initialized) return;
    await this.msalInstance.initialize();

    // Handle redirect response (if returning from login)
    const response = await this.msalInstance.handleRedirectPromise();
    if (response) {
      this.handleAuthResult(response);
    } else {
      // Check if already logged in
      const accounts = this.msalInstance.getAllAccounts();
      if (accounts.length > 0) {
        this.msalInstance.setActiveAccount(accounts[0]);
        this.setUserFromAccount(accounts[0]);
      }
    }
    this.initialized = true;
  }

  async login(): Promise<void> {
    await this.initialize();
    // Use redirect — user stays in the same window
    await this.msalInstance.loginRedirect({
      scopes: environment.msalConfig.scopes,
    });
  }

  async logout(): Promise<void> {
    await this.initialize();
    this._isAuthenticated.next(false);
    this._user.next(null);
    await this.msalInstance.logoutRedirect();
  }

  async getAccessToken(): Promise<string | null> {
    await this.initialize();
    const account = this.msalInstance.getActiveAccount();
    if (!account) return null;

    try {
      const response = await this.msalInstance.acquireTokenSilent({
        scopes: environment.msalConfig.scopes,
        account,
      });
      return response.accessToken;
    } catch (error) {
      if (error instanceof InteractionRequiredAuthError) {
        try {
          const response = await this.msalInstance.acquireTokenPopup({
            scopes: environment.msalConfig.scopes,
          });
          return response.accessToken;
        } catch (popupError) {
          console.error('Token acquisition failed:', popupError);
          return null;
        }
      }
      console.error('Token acquisition failed:', error);
      return null;
    }
  }

  private handleAuthResult(response: AuthenticationResult): void {
    if (response?.account) {
      this.msalInstance.setActiveAccount(response.account);
      this.setUserFromAccount(response.account);
    }
  }

  private setUserFromAccount(account: AccountInfo): void {
    // Roles come from the ID token claims (set via App Roles in Entra)
    const idTokenClaims = account.idTokenClaims as Record<string, unknown> | undefined;
    const roles = (idTokenClaims?.['roles'] as string[]) ?? [];

    this._isAuthenticated.next(true);
    this._user.next({
      name: account.name ?? account.username,
      email: account.username,
      roles,
    });
  }
}
