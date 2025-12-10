import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';

interface Evento {
  id: string;
  titulo: string;
  descripcion: string;
  fecha: Date;
  imagen: string;
  ubicacion: string;
  capacidad: number;
  inscritos: number;
  objetoRecaudacion?: string;
  precioGeneral?: number | null;
  cantidadGeneral?: number | null;
  precioVip?: number | null;
  cantidadVip?: number | null;
}

@Component({
  selector: 'app-compra-entradas',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './compra-entradas.component.html',
  styleUrls: ['./compra-entradas.component.css']
})
export class CompraEntradasComponent implements OnInit {
  
  evento: Evento | null = null;
  isLoading: boolean = true;
  
  // Datos del formulario
  numeroEntradasGeneral: number = 0;
  numeroEntradasVip: number = 0;
  
  // Precios y cantidades disponibles (del backend)
  precioGeneral: number = 0;
  cantidadGeneralDisponible: number = 0;
  precioVip: number = 0;
  cantidadVipDisponible: number = 0;
  tieneEntradasVip: boolean = false;
  
  // Datos personales
  nombre: string = '';
  apellidos: string = '';
  telefono: string = '';
  dni: string = '';
  direccion: string = '';
  codigoPostal: string = '';
  ciudad: string = '';
  pais: string = '';
  
  esEmpresa: boolean = false;
  codigoDescuento: string = '';
  
  isProcessing: boolean = false;
  errorMessage: string = '';
  
  private apiUrl = 'https://cudecabackend-c7hhc5ejeygfb4ah.spaincentral-01.azurewebsites.net';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private http: HttpClient,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.cargarEvento(id);
    }
  }

  cargarEvento(id: string): void {
    this.isLoading = true;
    
    // Primero intentamos cargar desde admin (si estamos logueados)
    if (this.authService.isLoggedIn()) {
      this.cargarEventoDesdeAdmin(id);
    } else {
      this.cargarEventoPublico(id);
    }
  }

  private cargarEventoPublico(id: string): void {
    this.http.get<any>(`${this.apiUrl}/events/${id}`).subscribe({
      next: (item) => {
        this.evento = this.mapearEvento(item);
        // Sin datos de admin, usamos valores por defecto
        this.precioGeneral = item.precioGeneral || 25;
        this.precioVip = item.precioVip || 45;
        this.cantidadGeneralDisponible = item.cantidadGeneral || (item.aforo || 50);
        this.cantidadVipDisponible = item.cantidadVip || 0;
        this.tieneEntradasVip = this.cantidadVipDisponible > 0;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Error cargando el evento';
      }
    });
  }

  private cargarEventoDesdeAdmin(id: string): void {
    this.http.get<any>(`${this.apiUrl}/admin/events/${id}`, {
      headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
    }).subscribe({
      next: (item) => {
        this.evento = this.mapearEventoAdmin(item);
        this.precioGeneral = item.precioGeneral || 25;
        this.precioVip = item.precioVip || 0;
        this.cantidadGeneralDisponible = item.cantidadGeneral || (item.aforo || 50);
        this.cantidadVipDisponible = item.cantidadVip || 0;
        this.tieneEntradasVip = this.cantidadVipDisponible > 0;
        this.isLoading = false;
      },
      error: () => {
        // Si falla admin, intentamos público
        this.cargarEventoPublico(id);
      }
    });
  }

  private mapearEvento(item: any): Evento {
    return {
      id: item.id,
      titulo: item.nombre || item.titulo || 'Evento sin título',
      descripcion: item.description || item.descripcion || 'Sin descripción',
      fecha: new Date(item.date || item.fecha || Date.now()),
      imagen: item.imageUrl || item.imagen || 'assets/images/fondoCudeca.png',
      ubicacion: item.location || item.ubicacion || 'Ubicación pendiente',
      capacidad: item.capacity || item.capacidad || item.aforo || 50,
      inscritos: item.enrolled || item.inscritos || item.entradasVendidas || 0,
      objetoRecaudacion: item.goalDescription || item.objetoRecaudacion,
      precioGeneral: item.precioGeneral,
      cantidadGeneral: item.cantidadGeneral,
      precioVip: item.precioVip,
      cantidadVip: item.cantidadVip
    };
  }

  private mapearEventoAdmin(item: any): Evento {
    return {
      id: item.id,
      titulo: item.nombre || item.titulo || 'Evento sin título',
      descripcion: item.descripcion || item.description || 'Sin descripción',
      fecha: new Date(item.fechaEvento || item.fecha || Date.now()),
      imagen: item.imageUrl || item.imagen || 'assets/images/fondoCudeca.png',
      ubicacion: item.ubicacion || 'Ubicación pendiente',
      capacidad: item.aforo || item.capacity || 50,
      inscritos: item.entradasVendidas || item.inscritos || 0,
      objetoRecaudacion: item.objetoRecaudacion,
      precioGeneral: item.precioGeneral ?? item.PrecioGeneral,
      cantidadGeneral: item.cantidadGeneral ?? item.CantidadGeneral,
      precioVip: item.precioVip ?? item.PrecioVip,
      cantidadVip: item.cantidadVip ?? item.CantidadVip
    };
  }

  get totalGeneral(): number {
    return this.numeroEntradasGeneral * this.precioGeneral;
  }

  get totalVip(): number {
    return this.numeroEntradasVip * this.precioVip;
  }

  get totalEntradas(): number {
    return this.numeroEntradasGeneral + this.numeroEntradasVip;
  }

  get totalPrecio(): number {
    return this.totalGeneral + this.totalVip;
  }

  procesarCompra(): void {
    this.errorMessage = '';

    // Validaciones básicas
    if (this.totalEntradas <= 0) {
      this.errorMessage = 'Debes seleccionar al menos una entrada.';
      return;
    }

    if (this.numeroEntradasGeneral > this.cantidadGeneralDisponible) {
      this.errorMessage = `Solo hay ${this.cantidadGeneralDisponible} entradas General disponibles.`;
      return;
    }

    if (this.numeroEntradasVip > this.cantidadVipDisponible) {
      this.errorMessage = `Solo hay ${this.cantidadVipDisponible} entradas VIP disponibles.`;
      return;
    }

    if (!this.nombre.trim() || !this.apellidos.trim()) {
      this.errorMessage = 'El nombre y apellidos son obligatorios.';
      return;
    }

    if (!this.telefono.trim() || !this.dni.trim()) {
      this.errorMessage = 'El teléfono y DNI son obligatorios.';
      return;
    }

    this.isProcessing = true;

    // Aquí iría la llamada al backend para procesar la compra
    setTimeout(() => {
      this.isProcessing = false;
      alert('¡Compra procesada correctamente! (Demo)');
      this.router.navigate(['/eventos']);
    }, 1000);
  }

  goBack(): void {
    this.router.navigate(['/eventos']);
  }
}
