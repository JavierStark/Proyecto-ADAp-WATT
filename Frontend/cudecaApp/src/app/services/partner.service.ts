import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PartnerService {

  private api = environment.api;

  constructor(private http: HttpClient) {}

  getPartnerData() {
    return this.http.get<any>(`${this.api}/partners/data`);
  }

  subscribe(body: any) {
    return this.http.post(`${this.api}/partners/subscribe`, body);
  }
}
