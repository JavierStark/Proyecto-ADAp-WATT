import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  // Tu URL de Azure (sin barra al final)
  private apiUrl = 'https://cudecabackend-c7hhc5ejeygfb4ah.spaincentral-01.azurewebsites.net/'; 

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







  // METODOS PARA CUENTA

  private getHeaders() {
    const token = localStorage.getItem('token');
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`
    });
  }

  // 1. Obtener Perfil (GET /users/me)
  getProfile(): Observable<any> {
    return this.http.get(`${this.apiUrl}/users/me`, { headers: this.getHeaders() });
  }

  // 2. Editar Perfil (PUT /users/me)
  updateProfile(data: any): Observable<any> {
    return this.http.put(`${this.apiUrl}/users/me`, data, { headers: this.getHeaders() });
  }

  // 3. Historial de Tickets (GET /users/me/tickets)
  getMyTickets(): Observable<any> {
    return this.http.get(`${this.apiUrl}/users/me/tickets`, { headers: this.getHeaders() });
  }

  // 4. Historial de Donaciones (GET /users/me/donations)
  getMyDonations(): Observable<any> {
    return this.http.get(`${this.apiUrl}/users/me/donations`, { headers: this.getHeaders() });
  }
  
}