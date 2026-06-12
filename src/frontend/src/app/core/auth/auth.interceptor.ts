import { HttpInterceptorFn } from '@angular/common/http';
import { inject, NgZone } from '@angular/core';
import { from, switchMap, Observable } from 'rxjs';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const zone = inject(NgZone);

  // Only attach token to our API requests
  if (!req.url.startsWith(environment.apiUrl)) {
    return next(req);
  }

  // If not authenticated, pass through without token
  if (!auth.isAuthenticated) {
    return next(req);
  }

  // Wrap the token acquisition in a new Observable that re-enters the Angular zone
  return new Observable(subscriber => {
    auth.getAccessToken().then(token => {
      zone.run(() => {
        const request = token
          ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
          : req;
        next(request).subscribe(subscriber);
      });
    }).catch(() => {
      zone.run(() => {
        next(req).subscribe(subscriber);
      });
    });
  });
};
