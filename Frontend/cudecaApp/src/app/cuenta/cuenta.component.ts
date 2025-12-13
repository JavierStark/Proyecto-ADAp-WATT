import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common'; // Importante para *ngIf y *ngFor
import { FormsModule } from '@angular/forms';   // Importante para editar datos
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-cuenta',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './cuenta.component.html',
  styles: `` // No necesitamos CSS por ahora, usaremos Tailwind directo en el HTML
})
export class CuentaComponent implements OnInit{

  // Control de Vistas
  vistaActual: 'menu' | 'perfil' | 'tickets' | 'donaciones' = 'menu';
  isLoading: boolean = false;
  modoEdicion: boolean = false;

  // Datos del Usuario
  usuario: any = {};
  usuarioOriginal: any = {}; // Para poder cancelar edición
  
  // Listas de datos
  tickets: any[] = [];
  donaciones: any[] = [];
  totalDonado: number = 0;
  loadingPerfil: boolean = true;

  // Certificado de donaciones
  yearSeleccionado: number = new Date().getFullYear();
  descargandoCertificado: boolean = false;
  aniosDisponibles: number[] = [];

  constructor(private authService: AuthService, private router: Router) {}

  ngOnInit() {
    this.cargarDatosUsuario();
    this.generarAniosDisponibles();
  }

  // NAVEGACIÓN INTERNA
  cambiarVista(vista: 'menu' | 'perfil' | 'tickets' | 'donaciones') {
    this.vistaActual = vista;
    this.modoEdicion = false; // Salir de modo edición
    if (vista === 'tickets') this.cargarTickets();
    if (vista === 'donaciones') this.cargarDonaciones();
  }

  // MODO EDICIÓN
  entrarModoEdicion() {
    this.modoEdicion = true;
    this.usuarioOriginal = JSON.parse(JSON.stringify(this.usuario)); // Backup
  }

  cancelarEdicion() {
    this.usuario = JSON.parse(JSON.stringify(this.usuarioOriginal)); // Restaurar
    this.modoEdicion = false;
  }

  // CARGA DE DATOS
  cargarDatosUsuario() {
    this.loadingPerfil = true;
    this.authService.getProfile().subscribe({
      next: (data) => {
        // Mapear los campos del backend al frontend
        // Backend envía: piso (no pisoPuerta) y cp (no codigoPostal)
        this.usuario = {
          ...data,
          pisoPuerta: data.piso,
          codigoPostal: data.cp
        };
        this.usuarioOriginal = JSON.parse(JSON.stringify(this.usuario));
        this.loadingPerfil = false;
      },
      error: (e) => {
        console.error('Error cargando perfil', e)
        this.loadingPerfil = false;
      }
    });
  }

  cargarTickets() {
    this.isLoading = true;
    this.authService.getMyTickets().subscribe({
      next: (data) => {
        this.tickets = data;
        this.isLoading = false;
      },
      error: () => this.isLoading = false
    });
  }

  cargarDonaciones() {
    this.isLoading = true;
    this.authService.getMyDonations().subscribe({
      next: (data) => {
        this.donaciones = data;
        // Calculamos el total sumando el importe de todas las donaciones
        this.totalDonado = data.reduce((acc: number, curr: any) => acc + (curr.amount || curr.cantidad || 0), 0);
        this.isLoading = false;
      },
      error: () => this.isLoading = false
    });
  }

  // Generar años disponibles (últimos 10 años)
  generarAniosDisponibles() {
    const anioActual = new Date().getFullYear();
    this.aniosDisponibles = [];
    for (let i = 0; i < 10; i++) {
      this.aniosDisponibles.push(anioActual - i);
    }
  }

  // Descargar certificado de donaciones
  descargarCertificado() {
    if (this.descargandoCertificado) return;

    this.descargandoCertificado = true;

    // Preparar los datos fiscales del usuario
    const userData = {
      dni: this.usuario.dni || '',
      calle: this.usuario.calle || '',
      numero: this.usuario.numero || '',
      pisoPuerta: this.usuario.pisoPuerta || '',
      codigoPostal: this.usuario.codigoPostal || '',
      ciudad: this.usuario.ciudad || '',
      provincia: this.usuario.provincia || '',
      pais: this.usuario.pais || ''
    };

    this.authService.downloadDonationCertificate(this.yearSeleccionado, userData).subscribe({
      next: (blob) => {
        // Crear un URL temporal para el blob
        const url = window.URL.createObjectURL(blob);
        
        // Crear un elemento <a> temporal y simular el click para descargar
        const link = document.createElement('a');
        link.href = url;
        link.download = `Certificado_Donaciones_${this.yearSeleccionado}.pdf`;
        document.body.appendChild(link);
        link.click();
        
        // Limpiar
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
        
        this.descargandoCertificado = false;
      },
      error: (error) => {
        this.descargandoCertificado = false;
        console.error('Error descargando certificado:', error);
        
        // Mostrar mensaje de error más específico
        if (error.error?.error) {
          alert(`❌ ${error.error.error}\n\n${error.error.message || ''}`);
        } else {
          alert(`❌ Error al descargar el certificado. Por favor, verifica tus datos fiscales estén completos.`);
        }
      }
    });
  }

  // --- ACCIONES ---
  guardarCambios() {
    this.isLoading = true;
    // Mapear los campos al formato esperado por el backend
    const datosAEnviar = {
      ...this.usuario,
      piso: this.usuario.pisoPuerta,
      cp: this.usuario.codigoPostal
    };
    this.authService.updateProfile(datosAEnviar).subscribe({
      next: () => {
        alert('✅ Datos actualizados correctamente');
        this.isLoading = false;
        this.modoEdicion = false;
        this.usuarioOriginal = JSON.parse(JSON.stringify(this.usuario));
      },
      error: () => {
        alert('❌ Error al actualizar');
        this.isLoading = false;
      }
    });
  }

  onLogout() {
    // 1. Borramos el token
    this.authService.logout();
    
    // 2. Redirigimos al home
    this.router.navigate(['/']);
  }
}