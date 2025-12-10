import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs/operators';

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
  
  // Precios (se cargarán del endpoint)
  precioGeneral: number = 0;
  precioVip: number = 0;
  
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
  
  private apiUrl = 'https://cudecabackend-c7hhc5ejeygfb4ah.spaincentral-01.azurewebsites.net/events';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private http: HttpClient
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.cargarEvento(id);
    }
  }

  cargarEvento(id: string): void {
    this.isLoading = true;
    this.http.get<any>(`${this.apiUrl}/${id}`).pipe(
      map(item => ({
        id: item.id,
        titulo: item.nombre || item.titulo || item.name || 'Evento sin título',
        descripcion: item.description || item.descripcion || 'Sin descripción',
        fecha: new Date(item.date || item.fecha || Date.now()),
        imagen: item.imageUrl || item.imagen || 'assets/images/fondoCudeca.png',
        ubicacion: item.location || item.ubicacion || 'Ubicación pendiente',
        capacidad: item.capacity || item.capacidad || item.aforo || 50,
        inscritos: item.enrolled || item.inscritos || item.entradasVendidas || 0,
        objetoRecaudacion: item.goalDescription || item.objetoRecaudacion
      }))
    ).subscribe({
      next: (data) => {
        this.evento = data;
        // Precios dummy por ahora (backend no los devuelve en público)
        this.precioGeneral = 25;
        this.precioVip = 45;
        this.isLoading = false;
      },
      error: () => {
        console.warn('Error cargando evento');
        this.isLoading = false;
      }
    });
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
