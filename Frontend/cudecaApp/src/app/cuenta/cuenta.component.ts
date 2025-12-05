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

  // Datos del Usuario
  usuario: any = {};
  
  // Listas de datos
  tickets: any[] = [];
  donaciones: any[] = [];
  totalDonado: number = 0;
  loadingPerfil: boolean = true;

  constructor(private authService: AuthService, private router: Router) {}

  ngOnInit() {
    this.cargarDatosUsuario();
  }

  // NAVEGACIÓN INTERNA
  cambiarVista(vista: 'menu' | 'perfil' | 'tickets' | 'donaciones') {
    this.vistaActual = vista;
    if (vista === 'tickets') this.cargarTickets();
    if (vista === 'donaciones') this.cargarDonaciones();
  }

  // CARGA DE DATOS
  cargarDatosUsuario() {
    this.loadingPerfil = true;
    this.authService.getProfile().subscribe({
      next: (data) => {
        this.usuario = data,
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

  // --- ACCIONES ---
  guardarCambios() {
    this.isLoading = true;
    this.authService.updateProfile(this.usuario).subscribe({
      next: () => {
        alert('✅ Datos actualizados correctamente');
        this.isLoading = false;
        this.cambiarVista('menu');
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