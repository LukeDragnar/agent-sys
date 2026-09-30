import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

export const adminGuard: CanActivateFn = () => {
  const token = localStorage.getItem('token');
  if (!token) return inject(Router).createUrlTree(['/login']);
  try {
    const payload = JSON.parse(atob(token.split('.')[1]));
    const role = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
    if (role === 'admin') return true;
    return inject(Router).createUrlTree(['/']);
  } catch {
    return inject(Router).createUrlTree(['/login']);
  }
};
