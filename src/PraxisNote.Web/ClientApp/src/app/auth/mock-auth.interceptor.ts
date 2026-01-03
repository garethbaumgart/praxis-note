import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { MockAuthService } from './mock-auth.service';

export const mockAuthInterceptor: HttpInterceptorFn = (req, next) => {
  // Only add header to our own API requests
  if (!req.url.startsWith('/api/')) {
    return next(req);
  }

  const mockAuth = inject(MockAuthService);

  // Only add header if mock auth is enabled and user is logged in
  const mockHeader = mockAuth.getMockHeader();
  if (mockHeader) {
    req = req.clone({
      setHeaders: {
        'X-Mock-User': mockHeader,
      },
    });
  }

  return next(req);
};
