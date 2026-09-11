import { inject } from '@angular/core';
import {
  CanActivateFn,
  Router,
} from '@angular/router';

import { Auth } from '../services/auth';

export const adminGuard: CanActivateFn = () => {
  const auth = inject(Auth);
  const router = inject(Router);

  return auth.currentEmployee()?.role ===
    'Administrator'
    ? true
    : router.createUrlTree(['/dashboard']);
};