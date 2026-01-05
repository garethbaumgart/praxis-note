import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);

  return next(req).pipe(
    catchError((error) => {
      if (error.status === 401 && !req.url.includes('/api/auth/me')) {
        // Session expired - redirect to home which will show login
        router.navigate(['/']);
        // Force page reload to reset app state
        window.location.href = '/';
      }
      return throwError(() => error);
    })
  );
};
