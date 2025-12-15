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
  imageUrl?: string;
  ubicacion?: string;
  capacidad?: number;
  inscritos?: number;

  // Recaudación
  objetivoRecaudacion?: number;
  recaudacionExtra?: number;
  totalRecaudado?: number;

  visible?: boolean;
  precioGeneral?: number;
  cantidadGeneral?: number;
  precioVip?: number | null;
  cantidadVip?: number | null;
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
  selectedFile: File | null = null;
  previewUrl: string | null = null;

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
      .then(r => {
        console.log(`📡 Respuesta del servidor (status: ${r.status})`);
        return r.text();
      })
      .then(texto => {
        console.log(`📦 Datos recibidos (${texto.length} caracteres)`);
        return this.mapearEventos(texto);
      })
      .then(eventosTraducidos => {
        console.log(`✅ Eventos parseados correctamente: ${eventosTraducidos.length} eventos`);
        this.isLoading = false;
        if (eventosTraducidos.length > 0) {
          this.eventos = eventosTraducidos;
        } else {
          console.warn('⚠️ Sin eventos, cargando mock');
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
    console.log('🔐 Cargando eventos ADMIN');
    this.authService.getAdminEvents()
      .pipe(map(datos => {
        console.log(`📦 Datos admin recibidos: ${datos.length} eventos`);
        return this.mapearEventosDesdeAdmin(datos);
      }))
      .subscribe({
        next: (eventosTraducidos) => {
          console.log(`✅ Eventos admin parseados: ${eventosTraducidos.length} eventos`);
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
      objetivoRecaudacion: 0,
      recaudacionExtra: 0,
      precioGeneral: 0,
      cantidadGeneral: 0,
      precioVip: null,
      cantidadVip: null,
      imagen: null as File | null
    };
  }

  abrirCrear() {
    this.editingId = null;
    this.formData = this.getEmptyForm();
    this.selectedFile = null;
    this.previewUrl = null;
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
      objetivoRecaudacion: evento.objetivoRecaudacion || 0,
      recaudacionExtra: evento.recaudacionExtra || 0,
      precioGeneral: evento.precioGeneral ?? 0,
      cantidadGeneral: evento.cantidadGeneral ?? 0,
      precioVip: evento.precioVip ?? null,
      cantidadVip: evento.cantidadVip ?? null,
      imagen: null
    };
    this.selectedFile = null;
    this.previewUrl = evento.imagen || null;
    this.showForm = true;
  }

  cerrarForm() {
    if (this.saving) return;
    this.showForm = false;
    this.formError = '';
    this.selectedFile = null;
    this.previewUrl = null;
  }

  onFileSelected(event: any): void {
    const file: File = event.target.files[0];
    if (file) {
      // Validar que sea imagen
      if (!file.type.startsWith('image/')) {
        this.formError = 'Por favor selecciona una imagen válida.';
        return;
      }
      
      // Validar tamaño (máximo 5MB)
      if (file.size > 5 * 1024 * 1024) {
        this.formError = 'La imagen no debe superar 5MB.';
        return;
      }

      this.selectedFile = file;
      this.formError = '';

      // Mostrar preview
      const reader = new FileReader();
      reader.onload = (e: any) => {
        this.previewUrl = e.target.result;
      };
      reader.readAsDataURL(file);
    }
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
    if (Number(this.formData.precioGeneral) < 0) {
      this.formError = 'El precio General no puede ser negativo.';
      return;
    }

    this.formError = '';
    this.saving = true;

    const formData = new FormData();
    formData.append('Nombre', this.formData.nombre);
    formData.append('Descripcion', this.formData.descripcion);
    formData.append('Fecha', this.formData.fecha ? new Date(this.formData.fecha).toISOString() : '');
    formData.append('Ubicacion', this.formData.ubicacion);
    formData.append('EventoVisible', String(!!this.formData.eventoVisible));
    formData.append('ObjetivoRecaudacion', String(Number(this.formData.objetivoRecaudacion) || 0));
    formData.append('RecaudacionExtra', String(Number(this.formData.recaudacionExtra) || 0));
    formData.append('PrecioGeneral', String(Number(this.formData.precioGeneral) || 0));
    formData.append('CantidadGeneral', String(Number(this.formData.cantidadGeneral) || 0));
    if (this.formData.precioVip !== null && this.formData.precioVip !== '') {
      formData.append('PrecioVip', String(Number(this.formData.precioVip)));
    }
    if (this.formData.cantidadVip !== null && this.formData.cantidadVip !== '') {
      formData.append('CantidadVip', String(Number(this.formData.cantidadVip)));
    }

    // Agregar imagen si existe
    if (this.selectedFile) {
      formData.append('Imagen', this.selectedFile, this.selectedFile.name);
      console.log(`📸 Subiendo imagen: ${this.selectedFile.name}`);
    }

    const obs = this.editingId
      ? this.authService.updateAdminEvent(this.editingId, formData)
      : this.authService.createAdminEvent(formData);

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
    const formData = new FormData();
    
    console.log('🔄 Toggle visible - Estado del evento:', {
      id: evento.id,
      titulo: evento.titulo,
      fecha: evento.fecha,
      visibleActual: evento.visible,
      nuevoVisible: nuevoVisible
    });
    
    // Enviar campos obligatorios para que el backend acepte la actualización
    formData.append('Nombre', evento.titulo);
    formData.append('Fecha', new Date(evento.fecha).toISOString());
    formData.append('EventoVisible', nuevoVisible ? 'true' : 'false');
    
    console.log('📤 FormData a enviar:', {
      Nombre: evento.titulo,
      Fecha: new Date(evento.fecha).toISOString(),
      EventoVisible: nuevoVisible ? 'true' : 'false'
    });
    
    this.authService.updateAdminEvent(evento.id, formData).subscribe({
      next: (response) => {
        console.log(`✅ Visibilidad cambiada exitosamente:`, response);
        this.cargarEventosAdmin();
      },
      error: (err) => {
        console.error('❌ Error cambiando visibilidad:', err);
        console.error('Error details:', err.error);
      }
    });
  }

  private mapearEventos(textoRespuesta: string): Evento[] {
    try {
      const datosJson = JSON.parse(textoRespuesta);
      return datosJson.map((item: any) => {
        // Buscar imageUrl en diferentes formatos
        const imagenUrl = item.imageUrl || item.imagenUrl || item.ImagenUrl || item.imagenURL || item.imagen || 'assets/images/fondoCudeca.png';
        console.log(`📸 Evento: ${item.nombre || 'Sin título'}`);
        console.log(`   - Raw item keys:`, Object.keys(item));
        console.log(`   - item.imageUrl: ${item.imageUrl || 'undefined'}`);
        console.log(`   - item.imagenUrl: ${item.imagenUrl || 'undefined'}`);
        console.log(`   - item.ImagenUrl: ${item.ImagenUrl || 'undefined'}`);
        console.log(`   - item.imagenURL: ${item.imagenURL || 'undefined'}`);
        console.log(`   - item.imagen: ${item.imagen || 'undefined'}`);
        console.log(`   - Imagen final: ${imagenUrl}`);
        
        return {
          id: item.id || item.Id || '',
          titulo: item.nombre || item.titulo || item.name || 'Evento sin título',
          descripcion: item.description || item.descripcion || 'Sin descripción',
          fecha: new Date(item.date || item.fecha || item.fechaEvento || Date.now()),
          imagen: imagenUrl,
          imageUrl: imagenUrl,
          ubicacion: item.location || item.ubicacion || 'Ubicación pendiente',
          capacidad: item.capacity || item.capacidad || item.aforo || 50,
          inscritos: item.enrolled || item.inscritos || item.entradasVendidas || 0,
          objetivoRecaudacion: item.ObjetivoRecaudacion || item.objetivoRecaudacion || 0,
          totalRecaudado: item.TotalRecaudado || item.totalRecaudado || 0,
          recaudacionExtra: 0,
          visible: item.eventoVisible ?? true
        };
      });
    } catch (e) {
      console.error('❌ Error al leer los datos:', e);
      return [];
    }
  }

  private mapearEventosDesdeAdmin(datos: any): Evento[] {
    try {
      return (datos || []).map((item: any) => {
        // Buscar imageUrl en diferentes formatos (camelCase, PascalCase, snake_case)
        const imagenUrl = item.imageUrl || item.imagenUrl || item.ImagenUrl || item.imagenURL || item.imagen || 'assets/images/fondoCudeca.png';
        console.log(`📸 Evento (ADMIN): ${item.nombre || 'Sin título'}`);
        console.log(`   - Raw item keys:`, Object.keys(item));
        console.log(`   - item.imageUrl: ${item.imageUrl || 'undefined'}`);
        console.log(`   - item.imagenUrl: ${item.imagenUrl || 'undefined'}`);
        console.log(`   - item.ImagenUrl: ${item.ImagenUrl || 'undefined'}`);
        console.log(`   - item.imagenURL: ${item.imagenURL || 'undefined'}`);
        console.log(`   - item.imagen: ${item.imagen || 'undefined'}`);
        console.log(`   - Imagen final: ${imagenUrl}`);
        
        return {
          id: item.id || item.Id || '',
          titulo: item.nombre || item.titulo || item.name || 'Evento sin título',
          descripcion: item.descripcion || item.description || 'Sin descripción',
          fecha: new Date(item.fechaEvento || item.fecha || Date.now()),
          imagen: imagenUrl,
          imageUrl: imagenUrl,
          ubicacion: item.ubicacion || 'Ubicación pendiente',
          capacidad: item.aforo || item.capacity || 50,
          inscritos: item.entradasVendidas || item.inscritos || 0,
          objetivoRecaudacion: item.ObjetivoRecaudacion || item.objetivoRecaudacion || 0,
          recaudacionExtra: item.RecaudacionExtra || item.recaudacionExtra || 0,
          totalRecaudado: item.TotalRecaudado || item.totalRecaudado || 0,
          visible: item.eventoVisible ?? true,
          precioGeneral: item.precioGeneral ?? item.PrecioGeneral ?? null,
          cantidadGeneral: item.cantidadGeneral ?? item.CantidadGeneral ?? null,
          precioVip: item.precioVip ?? item.PrecioVip ?? null,
          cantidadVip: item.cantidadVip ?? item.CantidadVip ?? null
        };
      });
    } catch (e) {
      console.error('❌ Error al leer eventos admin:', e);
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
        objetivoRecaudacion: 10000, 
        totalRecaudado: 7250
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
        objetivoRecaudacion: 5000, 
        totalRecaudado: 1250
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
    // Navegar a página de compra de entradas
    this.router.navigate(['/compra-entradas', eventoId]);
  }

  esEventoPasado(fecha: Date | string): boolean {
  const fechaEvento = new Date(fecha);
  const ahora = new Date();
  return fechaEvento < ahora;
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

