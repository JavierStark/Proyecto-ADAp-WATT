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
  generalTicketEventId?: string;
  vipTicketEventId?: string;
  calle?: string;
  numero?: string;
  pisoPuerta?: string;
  codigoPostal?: string;
  ciudad?: string;
  provincia?: string;
  pais?: string;
  codigoDescuento?: string;
}



@Injectable({
  providedIn: 'root'
})
export class CompraService {
  private eventoCompraSubject = new BehaviorSubject<EventoCompra | null>(null);
  public eventoCompra$ = this.eventoCompraSubject.asObservable();
  private readonly STORAGE_KEY = 'eventoCompra';

  //Socio

  private readonly SOCIO_KEY = 'socioCompra';
  private socioCompra: SocioCompra | null = null;

  constructor() {
    // Recuperar datos del sessionStorage al inicializar el servicio
    this.recuperarDatosAlmacenados();
  }

  private recuperarDatosAlmacenados(): void {
    try {
      const datosGuardados = sessionStorage.getItem(this.STORAGE_KEY);
      if (datosGuardados) {
        const evento = JSON.parse(datosGuardados);
        this.eventoCompraSubject.next(evento);
      }
    } catch (error) {
      console.error('Error al recuperar datos de compra:', error);
    }
  }

  guardarEventoCompra(evento: EventoCompra): void {
    this.eventoCompraSubject.next(evento);
    // Guardar en sessionStorage para persistencia
    try {
      sessionStorage.setItem(this.STORAGE_KEY, JSON.stringify(evento));
    } catch (error) {
      console.error('Error al guardar datos de compra:', error);
    }
  }

  obtenerEventoCompra(): EventoCompra | null {
    return this.eventoCompraSubject.value;
  }

  limpiarEventoCompra(): void {
    this.eventoCompraSubject.next(null);
    // Limpiar del sessionStorage
    try {
      sessionStorage.removeItem(this.STORAGE_KEY);
    } catch (error) {
      console.error('Error al limpiar datos de compra:', error);
    }
  }

  private recuperarSocio(): void {
    try {
      const raw = sessionStorage.getItem(this.SOCIO_KEY);
      if (raw) {
        this.socioCompra = JSON.parse(raw);
      }
    } catch (e) {
      console.error('Error recuperando socio:', e);
    }
  }

  guardarSocioCompra(data: SocioCompra): void {
    this.socioCompra = data;
    sessionStorage.setItem(this.SOCIO_KEY, JSON.stringify(data));
  }

  obtenerSocioCompra(): SocioCompra | null {
    return this.socioCompra;
  }

  limpiarSocioCompra(): void {
    this.socioCompra = null;
    sessionStorage.removeItem(this.SOCIO_KEY);
  }

}

  
export interface SocioCompra {
  tipo: 'mensual' | 'trimestral' | 'anual';
  precio: number;
  nombre: string;
  apellidos: string;
  telefono: string;
  dni: string;
}

