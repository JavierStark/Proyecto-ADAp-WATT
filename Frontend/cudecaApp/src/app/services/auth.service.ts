import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, from, map, tap } from 'rxjs';
import { createClient, SupabaseClient } from '@supabase/supabase-js';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  private supabaseUrl = 'https://wntetrtsydueijushgoc.supabase.co/';
  private supabaseKey = 'sb_publishable_JJbBjtwM5rYMl150Vorfww_G3jJrFok';
  private supabase: SupabaseClient;


  // URL de azure
  private apiUrl = 'https://cudecabackend-c7hhc5ejeygfb4ah.spaincentral-01.azurewebsites.net/'; 

  constructor(private http: HttpClient) { 
    this.supabase = createClient(this.supabaseUrl, this.supabaseKey);
  }

  register(credentials: { email: string; password: string }): Observable<any> {
    return from(this.supabase.auth.signUp({
      email: credentials.email,
      password: credentials.password,
    })).pipe(
      map(response => {
        if (response.error) throw response.error;
        return response.data;
      })
    );
  }

  
  login(credentials: { email: string; password: string }): Observable<any> {
    return from(this.supabase.auth.signInWithPassword({
      email: credentials.email,
      password: credentials.password,
    })).pipe(
      map(response => {
        if (response.error) throw response.error;
        return response.data; // Devuelve objeto con { user, session }
      }),
      tap((data) => {
        // IMPORTANTE: Guardamos el token manualmente para poder usarlo 
        // en las peticiones al backend (getProfile, etc.)
        if (data.session) {
          localStorage.setItem('token', data.session.access_token);
        }
      })
    );
  }

  logout(): void {
    this.supabase.auth.signOut();
    localStorage.removeItem('token');
  }


  // Verificar si estoy logueado
  isLoggedIn(): boolean {
    return !!localStorage.getItem('token');
  }

  // Verificar si el token está expirado
  isTokenExpired(): boolean {
    const token = localStorage.getItem('token');
    if (!token) return true;

    try {
      // Decodificar el JWT sin verificación (solo para leer el payload)
      const parts = token.split('.');
      if (parts.length !== 3) return true;

      const decoded = JSON.parse(atob(parts[1]));
      const expirationTime = decoded.exp * 1000; // Convertir de segundos a milisegundos
      const currentTime = new Date().getTime();

      return currentTime > expirationTime;
    } catch (error) {
      return true;
    }
  }

  // Método para hacer logout desde cualquier lugar
  forceLogout(message: string = 'Tu sesión ha expirado. Por favor, inicia sesión nuevamente.'): void {
    alert(message);
    this.logout();
  }







  // METODOS PARA CUENTA

  private getHeaders() {
    const token = localStorage.getItem('token');
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`
    });
  }

  // Headers para FormData (sin Content-Type para que se envíe automáticamente)
  private getHeadersWithoutContentType() {
    const token = localStorage.getItem('token');
    return new HttpHeaders({
      'Authorization': `Bearer ${token}`
      // NO agregar Content-Type, el navegador lo establece automáticamente para FormData
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

  // 5. Validar ticket vía QR (GET /tickets/validate?qrCode=...)
  validateTicketQr(qrCode: string): Observable<any> {
    const url = `${this.apiUrl}/tickets/validate?qrCode=${encodeURIComponent(qrCode)}`;
    return this.http.get(url, { headers: this.getHeaders() });
  }

  // 6. Comprobar si el usuario es admin (GET /users/me/is-admin)
  isAdmin(): Observable<boolean> {
    return this.http.get<{ isAdmin: boolean }>(`${this.apiUrl}/users/me/is-admin`, { headers: this.getHeaders() })
      .pipe(map(res => !!res.isAdmin));
  }

  // 7. Listado de eventos para admins (GET /admin/events)
  getAdminEvents(): Observable<any> {
    return this.http.get(`${this.apiUrl}/admin/events`, { headers: this.getHeaders() });
  }

  // 8. Crear evento (POST /admin/events)
  createAdminEvent(body: any): Observable<any> {
    // Si es FormData, usar headers sin Content-Type
    if (body instanceof FormData) {
      return this.http.post(`${this.apiUrl}/admin/events`, body, { headers: this.getHeadersWithoutContentType() });
    }
    // Si es JSON, usar headers normales
    return this.http.post(`${this.apiUrl}/admin/events`, body, { headers: this.getHeaders() });
  }

  // 9. Actualizar evento (PUT /admin/events/{eventId})
  updateAdminEvent(eventId: string, body: any): Observable<any> {
    // Si es FormData, usar headers sin Content-Type
    if (body instanceof FormData) {
      return this.http.put(`${this.apiUrl}/admin/events/${eventId}`, body, { headers: this.getHeadersWithoutContentType() });
    }
    // Si es JSON, usar headers normales
    return this.http.put(`${this.apiUrl}/admin/events/${eventId}`, body, { headers: this.getHeaders() });
  }

  // 10. Eliminar evento (DELETE /admin/events/{eventId})
  deleteAdminEvent(eventId: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/admin/events/${eventId}`, { headers: this.getHeaders() });
  }

  // 11. Descargar certificado de donaciones (POST /donations/certificate/annual)
  downloadDonationCertificate(year: number, userData: any = {}): Observable<Blob> {
    const body = {
      Year: year,
      Dni: userData.dni,
      Calle: userData.calle,
      Numero: userData.numero,
      PisoPuerta: userData.pisoPuerta,
      CodigoPostal: userData.codigoPostal,
      Ciudad: userData.ciudad,
      Provincia: userData.provincia,
      Pais: userData.pais
    };

    return this.http.post(
      `${this.apiUrl}/donations/certificate/annual`,
      body,
      {
        headers: this.getHeaders(),
        responseType: 'blob'
      }
    );
  }

  // 12. Crear donación (POST /donations)
  createDonation(amount: number, paymentMethod: string): Observable<any> {
    const body = {
      Amount: amount,
      PaymentMethod: paymentMethod
    };

    return this.http.post(`${this.apiUrl}/donations`, body, { headers: this.getHeaders() });
  }

}