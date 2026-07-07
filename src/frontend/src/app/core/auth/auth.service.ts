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
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private _isAuthenticated = new BehaviorSubject<boolean>(false);
  private _user = new BehaviorSubject<UserProfile | null>(null);
  private msalInstance: PublicClientApplication;
  private initPromise: Promise<void> | null = null;
  private readonly devBypass = environment.devBypass === true;

  isAuthenticated$ = this._isAuthenticated.asObservable();
  user$ = this._user.asObservable();

  get isAuthenticated(): boolean {
    return this._isAuthenticated.value;
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
    if (!this.initPromise) {
      this.initPromise = this.doInitialize();
    }
    return this.initPromise;
  }

  private async doInitialize(): Promise<void> {
    // Development bypass: treat the local user as a signed-in editor without Entra.
    if (this.devBypass) {
      this._isAuthenticated.next(true);
      this._user.next({ name: 'Ontwikkelaar', email: 'dev@localhost' });
      return;
    }

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
  }

  async login(): Promise<void> {
    if (this.devBypass) {
      await this.initialize();
      return;
    }
    await this.initialize();
    // Use redirect — user stays in the same window
    await this.msalInstance.loginRedirect({
      scopes: environment.msalConfig.scopes,
    });
  }

  async logout(): Promise<void> {
    if (this.devBypass) {
      return;
    }
    await this.initialize();
    this._isAuthenticated.next(false);
    this._user.next(null);
    await this.msalInstance.logoutRedirect();
  }

  async getAccessToken(): Promise<string | null> {
    if (this.devBypass) {
      return null;
    }
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
    this._isAuthenticated.next(true);
    this._user.next({
      name: account.name ?? account.username,
      email: account.username,
    });
  }
}
