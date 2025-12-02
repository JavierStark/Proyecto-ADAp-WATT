import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
// Importamos tu servicio de autenticación
import { AuthService } from '../services/auth.service';

export const publicGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  // Verificamos si ya tiene sesión iniciada
  // (Asegúrate de tener la función isLoggedIn en tu AuthService, si no la tienes, avísame)
  if (authService.isLoggedIn()) {
    console.log('Ya estás logueado, redirigiendo al home...');
    alert('Ya has iniciado sesión. Redirigiéndote al inicio...');
    router.navigate(['/']); // Lo mandamos a home
    return false; // Bloqueamos la entrada al login
  }

  return true; // Si NO está logueado, adelante, puede entrar
};