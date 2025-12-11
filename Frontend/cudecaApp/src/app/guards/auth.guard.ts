import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Verificar si está logueado
  if (!authService.isLoggedIn()) {
    alert('Debes iniciar sesión para ver tu perfil');
    router.navigate(['/log-in']);
    return false;
  }

  // Verificar si el token está expirado
  if (authService.isTokenExpired()) {
    authService.forceLogout('Tu sesión ha expirado. Por favor, inicia sesión nuevamente.');
    router.navigate(['/log-in']);
    return false;
  }

  return true;
};