import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  // Tu URL de Azure (sin barra al final)
  private apiUrl = 'https://tu-proyecto.azurewebsites.net'; 

  constructor(private http: HttpClient) { }

  // 1. REGISTRO
  // Según PDF: POST /auth/register [cite: 2, 3]
  register(credentials: { email: string; password: string }): Observable<any> {
    return this.http.post(`${this.apiUrl}/auth/register`, credentials);
  }

  // 2. LOGIN
  // Según PDF: POST /auth/login [cite: 4, 5]
  login(credentials: { email: string; password: string }): Observable<any> {
    return this.http.post(`${this.apiUrl}/auth/login`, credentials)
      .pipe(
        // El operador 'tap' nos permite hacer algo con la respuesta sin modificarla
        tap((response: any) => this.setSession(response))
      );
  }

  // 3. LOGOUT (Cerrar Sesión)
  // Según PDF: POST /auth/logout [cite: 6, 7]
  // El PDF dice que hay que avisar al servidor de que nos vamos
  logout(): void {
    this.http.post(`${this.apiUrl}/auth/logout`, {}).subscribe({
      next: () => console.log('Sesión cerrada en servidor'),
      error: (e) => console.error('Error cerrando sesión', e)
    });
    
    // Limpiamos el navegador pase lo que pase
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
  }

  // --- MÉTODOS AUXILIARES ---

  // Guardar token
  private setSession(authResult: any) {
    localStorage.setItem('token', authResult.token);
    if (authResult.refreshToken) {
      localStorage.setItem('refreshToken', authResult.refreshToken);
    }
  }

  // Verificar si estoy logueado
  isLoggedIn(): boolean {
    return !!localStorage.getItem('token');
  }
}