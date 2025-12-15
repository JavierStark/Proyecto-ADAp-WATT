import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class CompanyService {

  private apiUrl = 'https://cudecabackend-c7hhc5ejeygfb4ah.spaincentral-01.azurewebsites.net/company';

  constructor(private http: HttpClient) { }

  // Obtener perfil de empresa
  getCompanyProfile(): Observable<any> {
    const token = localStorage.getItem('token');
    const headers = new HttpHeaders({ Authorization: `Bearer ${token}` });
    return this.http.get<any>(this.apiUrl, { headers });
  }

  // Crear o actualizar perfil de empresa
  saveCompanyProfile(nombreEmpresa: string): Observable<any> {
    const token = localStorage.getItem('token');
    const headers = new HttpHeaders({ Authorization: `Bearer ${token}` });
    
    // El body según tu imagen es { "nombreEmpresa": "string" }
    const body = { nombreEmpresa: nombreEmpresa };

    return this.http.post<any>(this.apiUrl, body, { headers });
  }
}