import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class PartnerApiService {

  private readonly API =
    'https://cudecabackend-c7hhc5ejeygfb4ah.spaincentral-01.azurewebsites.net';

  constructor(private http: HttpClient) {}

  // GET /partners/data
  getPartnerData() {
    return this.http.get<any>(`${this.API}/partners/data`);
  }

  // POST /partners/subscribe
  subscribePartner(body: {
    plan: string;
    importe: number;
    paymentToken: string;
    metodoPago: string | null;
  }) {
    return this.http.post<any>(
      `${this.API}/partners/subscribe`,
      body
    );
  }
}
