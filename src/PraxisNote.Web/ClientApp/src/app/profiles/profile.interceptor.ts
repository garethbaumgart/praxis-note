import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { ProfileService } from './profile.service';

export const profileInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.startsWith('/api/')) return next(req);

  const profileService = inject(ProfileService);
  const profileId = profileService.activeProfileId();

  if (profileId) {
    req = req.clone({
      headers: req.headers.set('X-Profile-Id', profileId),
    });
  }

  return next(req);
};
