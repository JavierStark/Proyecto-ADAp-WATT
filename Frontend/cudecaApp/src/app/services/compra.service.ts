import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

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
  
  // ✅ NUEVO CAMPO: Email (para invitados)
  email?: string;

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

  // --- LÓGICA DE SOCIOS Y DONACIONES (Se mantiene igual) ---
  private readonly SOCIO_KEY = 'socioCompra';
  private socioCompraSubject = new BehaviorSubject<SocioCompra | null>(null);

  constructor() {
    this.recuperarDatosAlmacenados();
  }

  private recuperarDatosAlmacenados(): void {
    try {
      const datosGuardados = sessionStorage.getItem(this.STORAGE_KEY);
      if (datosGuardados) {
        this.eventoCompraSubject.next(JSON.parse(datosGuardados));
      }
      
      // Recuperar socio también
      const socioGuardado = sessionStorage.getItem(this.SOCIO_KEY);
      if (socioGuardado) {
        this.socioCompraSubject.next(JSON.parse(socioGuardado));
      }
    } catch (error) {
      console.error('Error al recuperar datos:', error);
    }
  }

  guardarEventoCompra(evento: EventoCompra): void {
    this.eventoCompraSubject.next(evento);
    try {
      sessionStorage.setItem(this.STORAGE_KEY, JSON.stringify(evento));
    } catch (error) {
      console.error('Error al guardar evento:', error);
    }
  }

  obtenerEventoCompra(): EventoCompra | null {
    return this.eventoCompraSubject.value;
  }

  limpiarEventoCompra(): void {
    this.eventoCompraSubject.next(null);
    try {
      sessionStorage.removeItem(this.STORAGE_KEY);
    } catch (error) {
      console.error('Error al limpiar evento:', error);
    }
  }

  // --- MÉTODOS DE SOCIO ---
  guardarSocioCompra(data: SocioCompra): void {
    this.socioCompraSubject.next(data);
    sessionStorage.setItem(this.SOCIO_KEY, JSON.stringify(data));
  }

  obtenerSocioCompra(): SocioCompra | null {
    // Intentar leer de memoria o storage si está vacío
    if (!this.socioCompraSubject.value) {
       const raw = sessionStorage.getItem(this.SOCIO_KEY);
       if (raw) this.socioCompraSubject.next(JSON.parse(raw));
    }
    return this.socioCompraSubject.value;
  }

  limpiarSocioCompra(): void {
    this.socioCompraSubject.next(null);
    sessionStorage.removeItem(this.SOCIO_KEY);
  }
}

export interface SocioCompra {
  plan: string;
  importe: number;
}

export interface DonacionCompra {
  importe: number;
}

export class DonacionState {
  private static readonly DONACION_KEY = 'donacionCompra';
  static guardar(d: DonacionCompra) {
    sessionStorage.setItem(DonacionState.DONACION_KEY, JSON.stringify(d));
  }
  static obtener(): DonacionCompra | null {
    const raw = sessionStorage.getItem(DonacionState.DONACION_KEY);
    return raw ? JSON.parse(raw) : null;
  }
  static limpiar() {
    sessionStorage.removeItem(DonacionState.DONACION_KEY);
  }
}