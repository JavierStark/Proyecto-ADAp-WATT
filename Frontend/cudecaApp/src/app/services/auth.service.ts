import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {

  // URL del Backend
  private apiUrl = 'http://localhost:5000'; 

  constructor(private http: HttpClient) { }

  // 1. Registro
  register(credentials: { email: string; password: string }): Observable<any> {
    return this.http.post(`${this.apiUrl}/register`, credentials);
  }

  // 2. Login
  login(credentials: { email: string; password: string }): Observable<any> {
    return this.http.post(`${this.apiUrl}/login`, credentials);
  }

  // 3. Guardar sesión (Token)
  // Esto guarda el "pasaporte" digital en el navegador
  setSession(authResult: any) {
    localStorage.setItem('token', authResult.token);
    if (authResult.refreshToken) {
      localStorage.setItem('refreshToken', authResult.refreshToken);
    }
  }
  
  // 4. Cerrar sesión
  logout() {
    localStorage.removeItem('token');
    localStorage.removeItem('refreshToken');
  }
}