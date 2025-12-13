import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { CompraService } from '../services/compra.service'; // Asegúrate de tener este servicio
import { FormsModule } from '@angular/forms';

// --- INTERFACES BASADAS EN TU SWAGGER ---
interface PartnerData {
  plan: string | null;
  cuota: number;
  fechaInicio: string;
  fechaFin: string;
  isActivo: boolean;
  diasRestantes: number;
}

type Vista = 'CARGANDO' | 'YA_SOCIO' | 'SELECCION' | 'RESUMEN';
type PlanType = 'mensual' | 'trimestral' | 'anual';

@Component({
  selector: 'app-hazte-socio',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './hazte-socio.component.html',
  styleUrls: ['./hazte-socio.component.css']
})
export class HazteSocioComponent implements OnInit {

  // Url base
  private apiUrl = 'https://cudecabackend-c7hhc5ejeygfb4ah.spaincentral-01.azurewebsites.net';

  // Estado de la vista
  vista: Vista = 'CARGANDO';

  // Datos traídos del backend (GET /partners/data)
  datosSocio: PartnerData | null = null;

  // Datos seleccionados por el usuario para NUEVA suscripción
  planSeleccionado: PlanType | null = null;
  precioSeleccionado: number = 0;
  
  // UI Helpers
  isProcessing: boolean = false;
  errorMessage: string = '';

  constructor(
    private router: Router,
    private compraService: CompraService,
    private http: HttpClient
  ) {}

  cancelarSeleccion() {
    if (this.datosSocio && this.datosSocio.isActivo) {
      // Si ya era socio y le dio a "Renovar", volvemos a la vista de su perfil
      this.vista = 'YA_SOCIO';
    } else {
      // Si es un usuario nuevo, volvemos al inicio
      this.router.navigate(['/']);
    }
    // Limpiamos la selección por si acaso
    this.planSeleccionado = null;
    this.errorMessage = '';
  }

  ngOnInit(): void {
    this.checkEstadoSocio();
  }

  // 1. CONSULTAR ESTADO ACTUAL (GET /partners/data)
  checkEstadoSocio() {
    const token = localStorage.getItem('token');
    
    // Si no hay token, asumimos que no es socio y vamos directo a elegir plan
    if (!token) {
      this.vista = 'SELECCION';
      return;
    }

    const headers = new HttpHeaders({ Authorization: `Bearer ${token}` });

    this.http.get<PartnerData>(`${this.apiUrl}/partners/data`, { headers }).subscribe({
      next: (data) => {
        // Asignamos la respuesta exacta de tu imagen
        this.datosSocio = data;

        if (data && data.isActivo) {
          this.vista = 'YA_SOCIO';
        } else {
          this.vista = 'SELECCION';
        }
      },
      error: (err) => {
        console.error('Error al obtener datos de socio', err);
        // Si da 404 o error, asumimos que no es socio
        this.vista = 'SELECCION';
      }
    });
  }

  // --- ACCIONES DE VISTA ---

  renovar() {
    // Permitimos renovar aunque sea socio
    this.vista = 'SELECCION';
  }

  volverHome() {
    this.router.navigate(['/']);
  }

  seleccionarPlan(plan: PlanType, precio: number) {
    this.planSeleccionado = plan;
    this.precioSeleccionado = precio;
  }

  irAResumen() {
    if (!this.planSeleccionado) {
      this.errorMessage = 'Por favor, selecciona un plan para continuar.';
      return;
    }
    this.errorMessage = '';
    this.vista = 'RESUMEN';
  }

  volverASeleccion() {
    this.vista = 'SELECCION';
  }

  // 2. PREPARAR DATOS PARA EL PAGO
  confirmarYPagar() {
    if (!this.planSeleccionado) return;

    this.isProcessing = true;

    const datosParaPago = {
      tipo: 'socio',
      plan: this.planSeleccionado,
      importe: this.precioSeleccionado,
    };

    // Guardamos los datos en el servicio
    this.compraService.guardarSocioCompra(datosParaPago);

    // CORRECCIÓN AQUÍ: Añadimos 'socio' a la ruta
    // Antes: this.router.navigate(['/pagos']);
    this.router.navigate(['/pagos', 'socio']); 
  }

}