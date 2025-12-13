import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../services/auth.service';
// 1. IMPORTAR HTTPCLIENT Y HEADERS
import { HttpClient, HttpHeaders } from '@angular/common/http';

@Component({
  selector: 'app-cuenta',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './cuenta.component.html',
  styles: ``
})
export class CuentaComponent implements OnInit {

  vistaActual: 'menu' | 'perfil' | 'tickets' | 'donaciones' = 'menu';
  isLoading: boolean = false;
  modoEdicion: boolean = false;

  usuario: any = {};
  usuarioOriginal: any = {};
  
  tickets: any[] = [];
  donaciones: any[] = [];
  totalDonado: number = 0;
  loadingPerfil: boolean = true;

  // Certificado de donaciones
  yearSeleccionado: number = new Date().getFullYear();
  descargandoCertificado: boolean = false;
  aniosDisponibles: number[] = [];
  certificadoWarning: string = '';

  // 2. NUEVA VARIABLE PARA EL ESTADO DE SOCIO
  isSocio: boolean = false;
  private apiUrl = 'https://cudecabackend-c7hhc5ejeygfb4ah.spaincentral-01.azurewebsites.net';

  // 3. INYECTAR HTTPCLIENT
  constructor(
    private authService: AuthService, 
    private router: Router,
    private http: HttpClient 
  ) {}

  ngOnInit() {
    this.cargarDatosUsuario();
    this.verificarEstadoSocio(); // <--- LLAMADA NUEVA
    this.generarAniosDisponibles();
  }

  // 4. NUEVA FUNCIÓN PARA COMPROBAR SI ES SOCIO
  verificarEstadoSocio() {
    const token = localStorage.getItem('token');
    if (!token) return;

    const headers = new HttpHeaders({ Authorization: `Bearer ${token}` });

    this.http.get<any>(`${this.apiUrl}/partners/data`, { headers }).subscribe({
      next: (data) => {
        // Si devuelve datos y isActivo es true, mostramos el cartelito
        this.isSocio = data && data.isActivo;
      },
      error: (err) => {
        console.error('No se pudo verificar estado de socio', err);
        this.isSocio = false;
      }
    });
  }

  // ... Resto de funciones (cambiarVista, entrarModoEdicion, etc.) siguen igual ...
  cambiarVista(vista: 'menu' | 'perfil' | 'tickets' | 'donaciones') {
    this.vistaActual = vista;
    this.modoEdicion = false;
    if (vista === 'tickets') this.cargarTickets();
    if (vista === 'donaciones') this.cargarDonaciones();
  }

  entrarModoEdicion() {
    this.modoEdicion = true;
    this.usuarioOriginal = JSON.parse(JSON.stringify(this.usuario));
  }

  cancelarEdicion() {
    this.usuario = JSON.parse(JSON.stringify(this.usuarioOriginal));
    this.modoEdicion = false;
  }

  cargarDatosUsuario() {
    this.loadingPerfil = true;
    this.authService.getProfile().subscribe({
      next: (data) => {
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
        this.totalDonado = data.reduce((acc: number, curr: any) => acc + (curr.monto || 0), 0);
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
    this.certificadoWarning = '';

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

        // Advertencia amigable en la página en vez de alerta
        if (error.status === 404) {
          // Sin donaciones para el año seleccionado
          this.certificadoWarning = `No se encontraron donaciones en el ejercicio ${this.yearSeleccionado}.`;
          return;
        }

        // Datos fiscales incompletos u otros errores del backend
        if (error.error?.error) {
          this.certificadoWarning = `${error.error.error}${error.error.message ? ' — ' + error.error.message : ''}`;
        } else {
          this.certificadoWarning = 'No se pudo generar el certificado. Verifica que tus datos fiscales estén completos.';
        }
      }
    });
  }

  guardarCambios() {
    this.isLoading = true;
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
    this.authService.logout();
    this.router.navigate(['/']);
  }
}