import { inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';

import { Auth } from '../services/auth';

export const authInterceptor: HttpInterceptorFn = (
  request,
  next,
) => {
  const auth = inject(Auth);
  const token = auth.getAccessToken();

  const isBillingRequest = request.url.startsWith(
    'http://localhost:5002',
  );

  const isLoginRequest = request.url.endsWith(
    '/api/auth/login',
  );

  if (!token || !isBillingRequest || isLoginRequest) {
    return next(request);
  }

  const authenticatedRequest = request.clone({
    setHeaders: {
      Authorization: `Bearer ${token}`,
    },
  });

  return next(authenticatedRequest);
};