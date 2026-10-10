import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { UserRole } from './api.models';
import { AuthService } from './auth.service';

export function roleGuard(allowedRoles: UserRole[]): CanActivateFn {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);
    const user = auth.currentUser();

    if (!user || !auth.isAuthenticated()) {
      return router.createUrlTree(['/login']);
    }

    if (!allowedRoles.includes(user.role)) {
      return router.createUrlTree(['/']);
    }

    return true;
  };
}
