import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';
import { CompraService } from '../services/compra.service';

interface Evento {
  id: string;
  titulo: string;
  descripcion: string;
  fecha: Date;
  imagen: string;
  imageUrl?: string;
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
  generalTicketEventId: string | null = null;
  vipTicketEventId: string | null = null;
  
  // Datos personales
  nombre: string = '';
  apellidos: string = '';
  telefono: string = '';
  dni: string = '';
  calle: string = '';
  numero: string = '';
  pisoPuerta: string = '';
  codigoPostal: string = '';
  ciudad: string = '';
  provincia: string = '';
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
    private authService: AuthService,
    private compraService: CompraService
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    console.log(`🎫 Compra Entradas - Evento ID: ${id}`);
    if (id) {
      this.cargarEvento(id);
      // Cargar datos del perfil si está logueado
      if (this.authService.isLoggedIn()) {
        this.cargarDatosUsuario();
      }
    }
  }

  private cargarDatosUsuario(): void {
    this.authService.getProfile().subscribe({
      next: (perfil: any) => {
        console.log('👤 Datos del perfil cargados:', perfil);
        this.nombre = perfil.nombre || '';
        this.apellidos = perfil.apellidos || '';
        this.telefono = perfil.telefono || '';
        this.dni = perfil.dni || '';
        this.calle = perfil.calle || '';
        this.numero = perfil.numero || '';
        this.pisoPuerta = perfil.piso || '';
        this.codigoPostal = perfil.cp || '';
        this.ciudad = perfil.ciudad || '';
        this.provincia = perfil.provincia || '';
        this.pais = perfil.pais || '';
      },
      error: (err) => {
        console.warn('⚠️ Error cargando datos del perfil:', err);
        // No es crítico, continuamos sin prellenar
      }
    });
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
    console.log(`🌐 Cargando evento PÚBLICO: ${id}`);
    this.http.get<any>(`${this.apiUrl}/events/${id}`).subscribe({
      next: (item) => {
        console.log(`📊 Respuesta del backend (público):`, item);
        console.log(`   - imageUrl: ${item.imageUrl || 'undefined'}`);
        this.evento = this.mapearEvento(item);
        // Sin datos de admin, usamos valores por defecto
        this.precioGeneral = item.precioGeneral || 25;
        this.precioVip = item.precioVip || 45;
        this.cantidadGeneralDisponible = item.cantidadGeneral || (item.aforo || 50);
        this.cantidadVipDisponible = item.cantidadVip || 0;
        this.tieneEntradasVip = this.cantidadVipDisponible > 0;
        this.cargarTiposEntrada(id);
        console.log(`✅ Evento público cargado: ${this.evento?.titulo}`);
        this.isLoading = false;
      },
      error: (err) => {
        console.error(`❌ Error cargando evento público:`, err);
        this.isLoading = false;
        this.errorMessage = 'Error cargando el evento';
      }
    });
  }

  private cargarTiposEntrada(eventId: string): void {
    this.http.get<any>(`${this.apiUrl}/tickets/type/event/${eventId}`).subscribe({
      next: (res) => {
        const data = Array.isArray(res?.data) ? res.data : Array.isArray(res) ? res : [];
        if (!data.length) {
          console.warn(`⚠️ No se encontraron tipos de entrada para el evento ${eventId}`);
          return;
        }

        const normalizados = data.map((t: any) => ({
          id: t.TicketEventId || t.ticketEventId || t.id || null,
          nombre: (t.Nombre || t.nombre || t.Tipo || '').toString().toLowerCase(),
          precio: t.Precio ?? t.precio ?? t.price ?? null,
          stock: t.Stock ?? t.stock ?? t.cantidad ?? t.Cantidad ?? null
        }));

        const general = normalizados.find((t: any) => t.nombre.includes('general'));
        const vip = normalizados.find((t: any) => t.nombre.includes('vip'));

        if (general) {
          this.generalTicketEventId = general.id ? String(general.id) : this.generalTicketEventId;
          this.precioGeneral = general.precio ?? this.precioGeneral;
          this.cantidadGeneralDisponible = general.stock ?? this.cantidadGeneralDisponible;
        }

        if (vip) {
          this.vipTicketEventId = vip.id ? String(vip.id) : this.vipTicketEventId;
          this.precioVip = vip.precio ?? this.precioVip;
          this.cantidadVipDisponible = vip.stock ?? this.cantidadVipDisponible;
        }

        this.tieneEntradasVip = !!this.vipTicketEventId && (this.cantidadVipDisponible ?? 0) > 0;
      },
      error: (err) => {
        console.error(`❌ Error cargando tipos de entrada para el evento ${eventId}:`, err);
      }
    });
  }

  private cargarEventoDesdeAdmin(id: string): void {
    console.log(`🔐 Cargando evento ADMIN: ${id}`);
    this.http.get<any[]>(`${this.apiUrl}/admin/events`, {
      headers: { Authorization: `Bearer ${localStorage.getItem('token')}` }
    }).subscribe({
      next: (items) => {
        console.log(`📊 Respuesta del backend (admin) - Total eventos:`, items.length);
        const item = items.find((e) => `${e.id}` === `${id}`);

        if (!item) {
          console.warn(`⚠️ Evento ${id} no encontrado en admin, intentando endpoint público`);
          // Si no se encuentra en admin, usamos el público
          this.cargarEventoPublico(id);
          return;
        }

        console.log(`📸 Evento encontrado en admin:`, item);
        console.log(`   - imageUrl: ${item.imageUrl || 'undefined'}`);
        this.evento = this.mapearEventoAdmin(item);
        this.precioGeneral = item.precioGeneral ?? item.PrecioGeneral ?? 25;
        this.precioVip = item.precioVip ?? item.PrecioVip ?? 0;
        this.cantidadGeneralDisponible = item.cantidadGeneral ?? item.CantidadGeneral ?? item.aforo ?? 50;
        this.cantidadVipDisponible = item.cantidadVip ?? item.CantidadVip ?? 0;
        this.tieneEntradasVip = (this.precioVip ?? 0) > 0 && this.cantidadVipDisponible > 0;
        this.cargarTiposEntrada(id);
        console.log(`✅ Evento admin cargado: ${this.evento?.titulo}`);
        this.isLoading = false;
      },
      error: (err) => {
        console.error(`❌ Error cargando eventos admin, intentando endpoint público:`, err);
        // Si falla admin, intentamos público
        this.cargarEventoPublico(id);
      }
    });
  }

  private mapearEvento(item: any): Evento {
    const imagenUrl = item.imageUrl || item.imagenUrl || item.ImagenUrl || item.imagenURL || item.imagen || 'assets/images/fondoCudeca.png';
    console.log(`🎫 Mapeando evento (público): ${item.nombre || 'Sin título'}`);
    console.log(`   - Raw item keys:`, Object.keys(item));
    console.log(`   - item.imageUrl: ${item.imageUrl || 'undefined'}`);
    console.log(`   - item.imagenUrl: ${item.imagenUrl || 'undefined'}`);
    console.log(`   - item.ImagenUrl: ${item.ImagenUrl || 'undefined'}`);
    console.log(`   - item.imagenURL: ${item.imagenURL || 'undefined'}`);
    console.log(`   - Imagen final: ${imagenUrl}`);
    return {
      id: item.id,
      titulo: item.nombre || item.titulo || 'Evento sin título',
      descripcion: item.description || item.descripcion || 'Sin descripción',
      fecha: new Date(item.date || item.fecha || Date.now()),
      imagen: imagenUrl,
      imageUrl: imagenUrl,
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
    const imagenUrl = item.imageUrl || item.imagenUrl || item.ImagenUrl || item.imagenURL || item.imagen || 'assets/images/fondoCudeca.png';
    console.log(`🎫 Mapeando evento (admin): ${item.nombre || 'Sin título'}`);
    console.log(`   - Raw item keys:`, Object.keys(item));
    console.log(`   - item.imageUrl: ${item.imageUrl || 'undefined'}`);
    console.log(`   - item.imagenUrl: ${item.imagenUrl || 'undefined'}`);
    console.log(`   - item.ImagenUrl: ${item.ImagenUrl || 'undefined'}`);
    console.log(`   - item.imagenURL: ${item.imagenURL || 'undefined'}`);
    console.log(`   - Imagen final: ${imagenUrl}`);
    return {
      id: item.id,
      titulo: item.nombre || item.titulo || 'Evento sin título',
      descripcion: item.descripcion || item.description || 'Sin descripción',
      fecha: new Date(item.fechaEvento || item.fecha || Date.now()),
      imagen: imagenUrl,
      imageUrl: imagenUrl,
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

    if (!this.calle.trim() || !this.numero.trim() || !this.codigoPostal.trim() || !this.ciudad.trim()) {
      this.errorMessage = 'La dirección completa es obligatoria (calle, número, código postal y ciudad).';
      return;
    }

    if (this.numeroEntradasGeneral > 0 && !this.generalTicketEventId) {
      this.errorMessage = 'No se pudieron cargar las entradas General. Recarga la página e inténtalo de nuevo.';
      return;
    }

    if (this.numeroEntradasVip > 0 && this.tieneEntradasVip && !this.vipTicketEventId) {
      this.errorMessage = 'No se pudieron cargar las entradas VIP. Recarga la página e inténtalo de nuevo.';
      return;
    }

    this.isProcessing = true;

    // Guardar los datos de la compra en el servicio
    if (this.evento) {
      this.compraService.guardarEventoCompra({
        id: this.evento.id,
        titulo: this.evento.titulo,
        numeroEntradasGeneral: this.numeroEntradasGeneral,
        numeroEntradasVip: this.numeroEntradasVip,
        precioGeneral: this.precioGeneral,
        precioVip: this.precioVip,
        totalPrecio: this.totalPrecio,
        nombreCliente: this.nombre,
        apellidosCliente: this.apellidos,
        telefonoCliente: this.telefono,
        dniCliente: this.dni,
        ubicacion: this.evento.ubicacion || (this.evento as any).ubicacion || '',
        fecha: this.evento.fecha ? (this.evento.fecha instanceof Date ? this.evento.fecha.toLocaleDateString() : String(this.evento.fecha)) : '',
        imagen: this.evento.imagen || 'assets/images/fondoCudeca.png',
        generalTicketEventId: this.generalTicketEventId || undefined,
        vipTicketEventId: this.vipTicketEventId || undefined,
        calle: this.calle,
        numero: this.numero,
        pisoPuerta: this.pisoPuerta,
        codigoPostal: this.codigoPostal,
        ciudad: this.ciudad,
        provincia: this.provincia,
        pais: this.pais,
        codigoDescuento: this.codigoDescuento || undefined
      });

      // Navegar a la página de pagos con el ID del evento
      this.router.navigate(['/pagos', this.evento.id]);
    }

    this.isProcessing = false;
  }

  goBack(): void {
    this.router.navigate(['/eventos']);
  }
}
