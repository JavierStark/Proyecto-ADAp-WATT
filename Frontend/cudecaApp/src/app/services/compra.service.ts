import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface EventoCompra {
  id: string;
  titulo: string;
  numeroEntradasGeneral: number;
  numeroEntradasVip: number;
  precioGeneral: number;
  precioVip: number;
  totalPrecio: number;
  nombreCliente: string;
  apellidosCliente: string;
  telefonoCliente: string;
  dniCliente: string;
  ubicacion?: string;
  fecha?: string;
  imagen?: string;
}

@Injectable({
  providedIn: 'root'
})
export class CompraService {
  private eventoCompraSubject = new BehaviorSubject<EventoCompra | null>(null);
  public eventoCompra$ = this.eventoCompraSubject.asObservable();

  constructor() {}

  guardarEventoCompra(evento: EventoCompra): void {
    this.eventoCompraSubject.next(evento);
  }

  obtenerEventoCompra(): EventoCompra | null {
    return this.eventoCompraSubject.value;
  }

  limpiarEventoCompra(): void {
    this.eventoCompraSubject.next(null);
  }
}
