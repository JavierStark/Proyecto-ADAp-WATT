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
        this.totalDonado = data.reduce((acc: number, curr: any) => acc + (curr.amount || curr.cantidad || 0), 0);
        this.isLoading = false;
      },
      error: () => this.isLoading = false
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