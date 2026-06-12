import { HttpInterceptorFn } from '@angular/common/http';
import { inject, NgZone } from '@angular/core';
import { from, switchMap } from 'rxjs';
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

  return from(zone.run(() => auth.getAccessToken())).pipe(
    switchMap((token) => {
      if (token) {
        const authReq = req.clone({
          setHeaders: { Authorization: `Bearer ${token}` },
        });
        return next(authReq);
      }
      return next(req);
    })
  );
};
