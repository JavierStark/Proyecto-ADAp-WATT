import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // LÓGICA INVERSA AL OTRO GUARDIA:
  if (authService.isLoggedIn()) {
    return true; 
  } else {
    alert('Debes iniciar sesión para ver tu perfil');
    router.navigate(['/log-in']);
    return false;
  }
};