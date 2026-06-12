import { inject } from '@angular/core';
import { Router, CanActivateFn } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  await auth.initialize();

  if (auth.isAuthenticated) {
    return true;
  }

  router.navigate(['/']);
  return false;
};

export const researcherGuard: CanActivateFn = async () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  await auth.initialize();

  if (auth.isResearcher) {
    return true;
  }

  router.navigate(['/']);
  return false;
};
