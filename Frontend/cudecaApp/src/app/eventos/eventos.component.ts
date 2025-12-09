import { Component, OnInit, LOCALE_ID } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { map } from 'rxjs/operators';
import { registerLocaleData } from '@angular/common';
import localeEs from '@angular/common/locales/es';
import { AuthService } from '../services/auth.service';

registerLocaleData(localeEs);

export interface Evento {
  id: string;
  titulo: string;
  descripcion: string;
  fecha: Date;
  imagen: string;
  ubicacion?: string;
  capacidad?: number;
  inscritos?: number;
  objetoRecaudacion?: string;
  visible?: boolean;
}

@Component({
  selector: 'app-eventos',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './eventos.component.html',
  styleUrls: ['./eventos.component.css'],
  providers: [{ provide: LOCALE_ID, useValue: 'es-ES' }]
})
export class EventosComponent implements OnInit {
  eventos: Evento[] = [];
  // 1. VARIABLE DE ESTADO DE CARGA
  isLoading: boolean = true; 
  isAdmin: boolean = false;
  adminMode: boolean = false;
  // Estado de formularios admin
  showForm: boolean = false;
  editingId: string | null = null;
  saving: boolean = false;
  formError: string = '';
  formData: any = this.getEmptyForm();

  constructor(
    private router: Router,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    // Cargar eventos públicos por defecto; detectar si es admin para habilitar el modo
    this.cargarEventosPublicos();
    this.comprobarAdmin();
  }

  comprobarAdmin(): void {
    this.authService.isAdmin().subscribe({
      next: (esAdmin) => {
        this.isAdmin = esAdmin;
      },
      error: () => {
        this.isAdmin = false;
      }
    });
  }

  cargarEventosPublicos(): void {
    this.isLoading = true; // Iniciamos carga
    const url = 'https://cudecabackend-c7hhc5ejeygfb4ah.spaincentral-01.azurewebsites.net/events'; 

    console.log('🔄 Intentando conectar con:', url);

    fetch(url)
      .then(r => r.text())
      .then(texto => this.mapearEventos(texto))
      .then(eventosTraducidos => {
        this.isLoading = false;
        if (eventosTraducidos.length > 0) {
          this.eventos = eventosTraducidos;
        } else {
          this.cargarEventosMock();
        }
      })
      .catch(err => {
        console.error('❌ Error Backend:', err);
        this.isLoading = false;
        this.cargarEventosMock();
      });
  }

  cargarEventosAdmin(): void {
    this.isLoading = true;
    this.authService.getAdminEvents()
      .pipe(map(datos => this.mapearEventosDesdeAdmin(datos)))
      .subscribe({
        next: (eventosTraducidos) => {
          this.isLoading = false;
          this.eventos = eventosTraducidos;
        },
        error: (error) => {
          console.error('❌ Error cargando eventos admin, usando públicos:', error);
          this.isLoading = false;
          this.cargarEventosPublicos();
        }
      });
  }

  toggleAdminMode(): void {
    if (!this.isAdmin) return;
    this.adminMode = !this.adminMode;
    if (this.adminMode) {
      this.cargarEventosAdmin();
    } else {
      this.cargarEventosPublicos();
    }
  }

  // --- FORM ADMIN ---
  getEmptyForm() {
    return {
      nombre: '',
      descripcion: '',
      fecha: '',
      ubicacion: '',
      eventoVisible: true,
      objetoRecaudacion: '',
      precioGeneral: 0,
      cantidadGeneral: 0,
      precioVip: null,
      cantidadVip: null
    };
  }

  abrirCrear() {
    this.editingId = null;
    this.formData = this.getEmptyForm();
    this.showForm = true;
  }

  abrirEditar(evento: any) {
    this.editingId = evento.id;
    this.formData = {
      nombre: evento.titulo,
      descripcion: evento.descripcion,
      fecha: evento.fecha ? new Date(evento.fecha).toISOString().slice(0,16) : '',
      ubicacion: evento.ubicacion,
      eventoVisible: evento.visible ?? true,
      objetoRecaudacion: evento.objetoRecaudacion || '',
      precioGeneral: evento.precioGeneral ?? 0,
      cantidadGeneral: evento.cantidadGeneral ?? 0,
      precioVip: evento.precioVip ?? null,
      cantidadVip: evento.cantidadVip ?? null
    };
    this.showForm = true;
  }

  cerrarForm() {
    if (this.saving) return;
    this.showForm = false;
    this.formError = '';
  }

  guardarEvento() {
    if (this.saving) return;
    
    // Validación frontend
    if (!this.formData.nombre?.trim()) {
      this.formError = 'El nombre del evento es obligatorio.';
      return;
    }
    if (!this.formData.fecha) {
      this.formError = 'La fecha es obligatoria.';
      return;
    }
    if (new Date(this.formData.fecha) < new Date()) {
      this.formError = 'La fecha no puede ser en el pasado.';
      return;
    }
    if (Number(this.formData.cantidadGeneral) <= 0) {
      this.formError = 'Debe haber al menos 1 entrada General.';
      return;
    }
    if (Number(this.formData.precioGeneral) < 0) {
      this.formError = 'El precio General no puede ser negativo.';
      return;
    }

    this.formError = '';
    this.saving = true;

    const payload: any = {
      nombre: this.formData.nombre,
      descripcion: this.formData.descripcion,
      fecha: this.formData.fecha ? new Date(this.formData.fecha).toISOString() : null,
      ubicacion: this.formData.ubicacion,
      eventoVisible: !!this.formData.eventoVisible,
      objetoRecaudacion: this.formData.objetoRecaudacion || null,
      precioGeneral: Number(this.formData.precioGeneral) || 0,
      cantidadGeneral: Number(this.formData.cantidadGeneral) || 0,
      precioVip: this.formData.precioVip !== null && this.formData.precioVip !== '' ? Number(this.formData.precioVip) : null,
      cantidadVip: this.formData.cantidadVip !== null && this.formData.cantidadVip !== '' ? Number(this.formData.cantidadVip) : null
    };

    const obs = this.editingId
      ? this.authService.updateAdminEvent(this.editingId, payload)
      : this.authService.createAdminEvent(payload);

    obs.subscribe({
      next: () => {
        this.saving = false;
        this.showForm = false;
        this.formError = '';
        this.cargarEventosAdmin();
      },
      error: (err) => {
        console.error('Error guardando evento', err);
        this.formError = err?.error?.error || err?.error?.message || 'Error al guardar. Intenta de nuevo.';
        this.saving = false;
      }
    });
  }

  eliminarEvento(id: string) {
    if (!confirm('¿Eliminar este evento?')) return;
    this.authService.deleteAdminEvent(id).subscribe({
      next: () => this.cargarEventosAdmin(),
      error: (err) => console.error('Error eliminando evento', err)
    });
  }

  toggleVisible(evento: any) {
    const nuevoVisible = !(evento.visible ?? true);
    const payload = { eventoVisible: nuevoVisible };
    this.authService.updateAdminEvent(evento.id, payload).subscribe({
      next: () => this.cargarEventosAdmin(),
      error: (err) => console.error('Error cambiando visibilidad', err)
    });
  }

  private mapearEventos(textoRespuesta: string): Evento[] {
    try {
      const datosJson = JSON.parse(textoRespuesta);
      return datosJson.map((item: any) => ({
        id: item.id || item.Id || '',
        titulo: item.nombre || item.titulo || item.name || 'Evento sin título',
        descripcion: item.description || item.descripcion || 'Sin descripción',
        fecha: new Date(item.date || item.fecha || item.fechaEvento || Date.now()),
        imagen: item.imageUrl || item.imagen || 'assets/images/fondoCudeca.png',
        ubicacion: item.location || item.ubicacion || 'Ubicación pendiente',
        capacidad: item.capacity || item.capacidad || item.aforo || 50,
        inscritos: item.enrolled || item.inscritos || item.entradasVendidas || 0,
        objetoRecaudacion: item.goalDescription || item.objetoRecaudacion || item.objetivo || null,
        visible: item.eventoVisible ?? true
      }));
    } catch (e) {
      console.warn('⚠️ Error al leer los datos:', e);
      return [];
    }
  }

  private mapearEventosDesdeAdmin(datos: any): Evento[] {
    try {
      return (datos || []).map((item: any) => ({
        id: item.id || item.Id || '',
        titulo: item.nombre || item.titulo || item.name || 'Evento sin título',
        descripcion: item.descripcion || item.description || 'Sin descripción',
        fecha: new Date(item.fechaEvento || item.fecha || Date.now()),
        imagen: 'assets/images/fondoCudeca.png',
        ubicacion: item.ubicacion || 'Ubicación pendiente',
        capacidad: item.aforo || item.capacity || 50,
        inscritos: item.entradasVendidas || item.inscritos || 0,
        objetoRecaudacion: item.objetoRecaudacion || null,
        visible: item.eventoVisible ?? true
      }));
    } catch (e) {
      console.warn('⚠️ Error al leer eventos admin:', e);
      return [];
    }
  }

  cargarEventosMock(): void {
    this.eventos = [
      {
        id: '1',
        titulo: 'Carrera Solidaria Cudeca',
        descripcion: 'Únete a nuestra carrera anual para recaudar fondos. Disfruta de un día de deporte y solidaridad.',
        fecha: new Date('2025-12-15'),
        imagen: 'assets/images/fondoCudeca.png',
        ubicacion: 'Parque Central',
        capacidad: 200,
        inscritos: 145,
        objetoRecaudacion: 'Recaudar fondos para equipamiento médico'
      },
      {
        id: '2',
        titulo: 'Concierto Benéfico',
        descripcion: 'Una noche de música en vivo con artistas locales. Todas las ganancias van a Cudeca.',
        fecha: new Date('2025-12-20'),
        imagen: 'assets/images/fondoCudeca.png',
        ubicacion: 'Teatro Municipal',
        capacidad: 150,
        inscritos: 98
      },
      {
        id: '3',
        titulo: 'Taller de Artesanía',
        descripcion: 'Aprende técnicas de artesanía mientras apoyas una gran causa. Incluye materiales.',
        fecha: new Date('2026-01-10'),
        imagen: 'assets/images/fondoCudeca.png',
        ubicacion: 'Centro Cudeca',
        capacidad: 30,
        inscritos: 22,
        objetoRecaudacion: 'Apoyo a programas de atención domiciliaria'
      },
      {
        id: '4',
        titulo: 'Mercadillo Solidario',
        descripcion: 'Venta de productos artesanales y segunda mano. Todos los beneficios para Cudeca.',
        fecha: new Date('2026-01-25'),
        imagen: 'assets/images/fondoCudeca.png',
        ubicacion: 'Plaza Mayor',
        capacidad: 500,
        inscritos: 0
      }
    ];
    // Aseguramos que isLoading se apague si entramos directo al mock
    this.isLoading = false; 
  }

  inscribirseEvento(eventoId: string): void {
    console.log('Inscribiéndose al evento:', eventoId);
  }

  irAdminEventos(): void {
    alert('Gestión de eventos (crear/editar/eliminar) pendiente de UI.');
  }

  verDetalles(eventoId: string): void {
    console.log('Ver detalles del evento:', eventoId);
    this.router.navigate(['/eventos', eventoId]);
  }

  goToHome(): void {
    this.router.navigate(['/']);
  }
}