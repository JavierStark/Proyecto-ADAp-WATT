import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export interface PartnerData {
  mensaje?: string;
  vence?: string;
}

@Injectable({
  providedIn: 'root'
})
export class PartnerService {

  private readonly STORAGE_KEY = 'partnerData';
  private partnerSubject = new BehaviorSubject<PartnerData | null>(null);

  partner$ = this.partnerSubject.asObservable();

  constructor() {
    this.recuperar();
  }

  private recuperar() {
    const data = sessionStorage.getItem(this.STORAGE_KEY);
    if (data) {
      this.partnerSubject.next(JSON.parse(data));
    }
  }

  guardar(data: PartnerData) {
    this.partnerSubject.next(data);
    sessionStorage.setItem(this.STORAGE_KEY, JSON.stringify(data));
  }

  obtener(): PartnerData | null {
    return this.partnerSubject.value;
  }

  limpiar() {
    this.partnerSubject.next(null);
    sessionStorage.removeItem(this.STORAGE_KEY);
  }
}
