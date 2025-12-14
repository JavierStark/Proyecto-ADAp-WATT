import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../services/auth.service';
// 1. IMPORTAR HTTPCLIENT Y HEADERS
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { CompanyService } from '../services/company.service';

@Component({
  selector: 'app-cuenta',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './cuenta.component.html',
  styleUrls: ['./cuenta.component.css']
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

  isSocio: boolean = false;
  private apiUrl = 'https://cudecabackend-c7hhc5ejeygfb4ah.spaincentral-01.azurewebsites.net';

  empresa: string = ''; // El nombre de la empresa
  esEmpresa: boolean = false; // Checkbox visual para el formulario

  constructor(
    private authService: AuthService, 
    private router: Router,
    private http: HttpClient,
    private companyService: CompanyService
  ) {}

  ngOnInit() {
    this.cargarDatosUsuario();
    this.verificarEstadoSocio();
    this.cargarDatosEmpresa();
    this.generarAniosDisponibles();
  }

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

  cargarDatosEmpresa() {
    this.companyService.getCompanyProfile().subscribe({
      next: (data) => {
        // Si devuelve datos (200 OK), guardamos el nombre
        if (data && data.nombreEmpresa) {
          this.empresa = data.nombreEmpresa;
          this.esEmpresa = true; // Activamos el check visualmente
        }
      },
      error: (err) => {
        // Si da 404 (Not Found) es que NO es empresa aún, no pasa nada
        if (err.status !== 404) {
          console.error('Error cargando datos empresa', err);
        }
        this.esEmpresa = false;
        this.empresa = '';
      }
    });
  }

  guardarCambios() {
    this.isLoading = true;

    // 1. Datos de usuario normal
    const datosUsuario = {
      ...this.usuario,
      piso: this.usuario.pisoPuerta,
      cp: this.usuario.codigoPostal
    };

    // 2. Actualizar perfil de usuario
    this.authService.updateProfile(datosUsuario).subscribe({
      next: () => {
        
        // --- LÓGICA EMPRESA ---
        
        // CASO A: Es empresa y hay texto -> Guardamos
        if (this.esEmpresa && this.empresa.trim() !== '') {
          this.companyService.saveCompanyProfile(this.empresa).subscribe({
            next: () => this.finalizarGuardado(true), // true = recargar datos para confirmar
            error: () => {
              alert('Datos de usuario guardados, pero error al guardar empresa.');
              this.isLoading = false;
            }
          });
        } 
        
        // CASO B: Check desmarcado o texto vacío -> Borrar
        else {
          // Intentamos enviar cadena vacía
          this.companyService.saveCompanyProfile('').subscribe({
            next: () => {
              // Éxito al borrar: Limpiamos localmente y NO recargamos del server
              this.empresa = '';
              this.esEmpresa = false;
              this.finalizarGuardado(false); 
            },
            error: (err) => {
              // Si el backend da error (ej: no permite vacíos), forzamos el borrado visual
              console.warn('Backend rechazó nombre vacío, forzando limpieza local', err);
              this.empresa = '';
              this.esEmpresa = false;
              this.finalizarGuardado(false);
            }
          });
        }

      },
      error: () => {
        alert('❌ Error al actualizar perfil');
        this.isLoading = false;
      }
    });
  }

  // Modificamos esta función para aceptar el parámetro
  finalizarGuardado(recargarEmpresa: boolean = true) {
    alert('✅ Datos actualizados correctamente');
    this.isLoading = false;
    this.modoEdicion = false;
    this.usuarioOriginal = JSON.parse(JSON.stringify(this.usuario));
    
    // Solo recargamos si no acabamos de borrarla
    if (recargarEmpresa) {
      this.cargarDatosEmpresa();
    }
  }

  onLogout() {
    this.authService.logout();
    this.router.navigate(['/']);
  }
}