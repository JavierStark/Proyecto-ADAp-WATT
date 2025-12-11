import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse
} from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {

  constructor(
    private authService: AuthService,
    private router: Router
  ) { }

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        // Si recibimos un 401 (No Autorizado), significa que el token expiró
        if (error.status === 401) {
          // Mostrar alerta de sesión expirada
          alert('Tu sesión ha expirado. Por favor, inicia sesión nuevamente.');
          
          // Cerrar sesión
          this.authService.logout();
          
          // Redirigir al login
          this.router.navigate(['/log-in']);
        }
        
        return throwError(() => error);
      })
    );
  }
}
