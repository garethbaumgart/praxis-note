import { HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  return next(req).pipe(
    catchError((error) => {
      if (error.status === 401 && !req.url.includes('/api/auth/me')) {
        // Session expired - force full page reload to reset app state
        window.location.href = '/';
      }
      return throwError(() => error);
    })
  );
};
