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

  // 5. Validar ticket vía QR (GET /tickets/validate?qrCode=...)
  validateTicketQr(qrCode: string): Observable<any> {
    const url = `${this.apiUrl}/tickets/validate?qrCode=${encodeURIComponent(qrCode)}`;
    return this.http.get(url, { headers: this.getHeaders() });
  }

}