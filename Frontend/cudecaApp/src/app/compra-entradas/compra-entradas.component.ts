import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../services/auth.service';
import { CompraService } from '../services/compra.service';
import { CompanyService } from '../services/company.service'; // Asegúrate de importar esto

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

  precioGeneral: number = 0;
  cantidadGeneralDisponible: number = 0;
  precioVip: number = 0;
  cantidadVipDisponible: number = 0;
  tieneEntradasVip: boolean = false;
  generalTicketEventId: string | null = null;
  vipTicketEventId: string | null = null;

  // Datos personales
  email: string = ''; // Nuevo campo para invitados
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

  // Empresa
  esEmpresa: boolean = false;
  nombreEmpresa: string = ''; // Nuevo campo
  esEmpresaRegistrada: boolean = false;

  // Descuentos
  codigoDescuento: string = '';
  codigoValido: boolean = false;
  validandoCodigo: boolean = false;
  mensajeDescuento: string = '';
  tipoDescuento: 'porcentaje' | 'fijo' | null = null;
  valorDescuento: number = 0;

  isProcessing: boolean = false;
  errorMessage: string = '';

  private apiUrl = 'https://cudecabackend-c7hhc5ejeygfb4ah.spaincentral-01.azurewebsites.net';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private http: HttpClient,
    private authService: AuthService,
    private compraService: CompraService,
    private companyService: CompanyService // Inyectamos servicio de empresa
  ) {}

  // Getter auxiliar para el HTML
  get isLoggedIn(): boolean {
    return this.authService.isLoggedIn();
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.cargarEvento(id);
      if (this.isLoggedIn) {
        this.cargarDatosUsuario();
      }
    }
  }

  private cargarDatosUsuario(): void {
    // 1. Cargar perfil personal
    this.authService.getProfile().subscribe({
      next: (perfil: any) => {
        this.nombre = perfil.nombre || '';
        this.apellidos = perfil.apellidos || '';
        this.telefono = perfil.telefono || '';
        this.dni = perfil.dni || '';
        // El email del usuario logueado lo maneja el backend por el token, 
        // pero podemos guardarlo si queremos mostrarlo (opcional)
        
        // Direcciones (opcionales ahora)
        this.calle = perfil.calle || '';
        this.numero = perfil.numero || '';
        this.pisoPuerta = perfil.piso || '';
        this.codigoPostal = perfil.cp || '';
        this.ciudad = perfil.ciudad || '';
        this.provincia = perfil.provincia || '';
        this.pais = perfil.pais || '';

        // 2. Intentar cargar datos de empresa si existen
        this.companyService.getCompanyProfile().subscribe({
          next: (comp) => {
            if (comp && comp.nombreEmpresa) {
              this.nombreEmpresa = comp.nombreEmpresa;
              
              // 1. Forzamos la selección a Empresa
              this.esEmpresa = true;
              
              // 2. Activamos el bloqueo para que no pueda cambiar a Persona
              this.esEmpresaRegistrada = true;
            }
          },
          error: () => { /* No es empresa o error, ignoramos */ }
        });
      },
      error: (err) => console.warn('Error cargando perfil:', err)
    });
  }

  // ... (cargarEvento, cargarEventoPublico, cargarTiposEntrada, cargarEventoDesdeAdmin, mapearEvento... SIN CAMBIOS) ...
  // [Mantén todo el código de carga de eventos igual que antes]
  
  cargarEvento(id: string): void {
    this.isLoading = true;
    if (this.authService.isLoggedIn()) {
      this.cargarEventoDesdeAdmin(id);
    } else {
      this.cargarEventoPublico(id);
    }
  }

  private cargarEventoPublico(id: string): void {
    this.http.get<any>(`${this.apiUrl}/events/${id}`).subscribe({
      next: (item) => {
        this.evento = this.mapearEvento(item);
        this.precioGeneral = item.precioGeneral || 25;
        this.precioVip = item.precioVip || 45;
        this.cantidadGeneralDisponible = item.cantidadGeneral || (item.aforo || 50);
        this.cantidadVipDisponible = item.cantidadVip || 0;
        this.tieneEntradasVip = this.cantidadVipDisponible > 0;
        this.cargarTiposEntrada(id);
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.errorMessage = 'Error cargando el evento';
      }
    });
  }

  private cargarTiposEntrada(eventId: string): void {
     // ... (mismo código que tenías)
     this.http.get<any>(`${this.apiUrl}/tickets/type/event/${eventId}`).subscribe({
      next: (res) => {
        const data = Array.isArray(res?.data) ? res.data : Array.isArray(res) ? res : [];
        if (!data.length) return;

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
      }
    });
  }

  private cargarEventoDesdeAdmin(id: string): void {
    // ... (mismo código que tenías)
    this.http.get<any[]>(`${this.apiUrl}/admin/events`, {
      headers: { Authorization: `Bearer ${localStorage.getItem('token')}` }
    }).subscribe({
      next: (items) => {
        const item = items.find((e) => `${e.id}` === `${id}`);
        if (!item) { this.cargarEventoPublico(id); return; }
        this.evento = this.mapearEventoAdmin(item);
        this.precioGeneral = item.precioGeneral ?? item.PrecioGeneral ?? 25;
        this.precioVip = item.precioVip ?? item.PrecioVip ?? 0;
        this.cantidadGeneralDisponible = item.cantidadGeneral ?? item.CantidadGeneral ?? item.aforo ?? 50;
        this.cantidadVipDisponible = item.cantidadVip ?? item.CantidadVip ?? 0;
        this.tieneEntradasVip = (this.precioVip ?? 0) > 0 && this.cantidadVipDisponible > 0;
        this.cargarTiposEntrada(id);
        this.isLoading = false;
      },
      error: () => this.cargarEventoPublico(id)
    });
  }

  private mapearEvento(item: any): Evento {
    // ... (mismo código)
    const imagenUrl = item.imageUrl || item.imagenUrl || item.ImagenUrl || item.imagenURL || item.imagen || 'assets/images/fondoCudeca.png';
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
    // ... (mismo código)
    const imagenUrl = item.imageUrl || item.imagenUrl || item.ImagenUrl || item.imagenURL || item.imagen || 'assets/images/fondoCudeca.png';
    return {
      id: item.id,
      titulo: item.nombre || item.titulo || 'Evento sin título',
      descripcion: item.description || item.descripcion || 'Sin descripción',
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
  // ... Fin mapeadores

  get totalGeneral(): number { return this.numeroEntradasGeneral * this.precioGeneral; }
  get totalVip(): number { return this.numeroEntradasVip * this.precioVip; }
  get totalEntradas(): number { return this.numeroEntradasGeneral + this.numeroEntradasVip; }
  get subtotalPrecio(): number { return this.totalGeneral + this.totalVip; }
  get importeDescuento(): number {
    if (!this.codigoValido) return 0;
    return (this.subtotalPrecio * this.valorDescuento) / 100;
  }
  get totalPrecio(): number { return Math.max(0, this.subtotalPrecio - this.importeDescuento); }

  // Lógica para cambiar entre persona/empresa
  seleccionarTipoComprador(empresa: boolean) {
    if (!empresa && this.esEmpresaRegistrada) {
      return; 
    }

    if (empresa && !this.isLoggedIn) {
      alert('Debes iniciar sesión para comprar como empresa.');
      // this.router.navigate(['/login']); // Descomentar si quieres redirigir
      return;
    }
    this.esEmpresa = empresa;
  }

  private validarEmail(email: string): boolean {
    // Esta expresión regular comprueba: texto + @ + texto + . + texto
    const regex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
    return regex.test(email);
  }

  procesarCompra(): void {
    this.errorMessage = '';

    // Validaciones de entradas
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

    if (!this.isLoggedIn) {
        if (!this.email.trim()) {
            this.errorMessage = 'El correo electrónico es obligatorio.';
            window.scrollTo({ top: 0, behavior: 'smooth' });
            return;
        }
        
        // Comprobamos si tiene formato real
        if (!this.validarEmail(this.email)) {
            this.errorMessage = 'Por favor, introduce un correo electrónico válido (ej: nombre@gmail.com).';
            window.scrollTo({ top: 0, behavior: 'smooth' });
            return;
        }
    }

    if (!this.nombre.trim() || !this.apellidos.trim()) {
      this.errorMessage = 'El nombre y apellidos son obligatorios.';
      return;
    }
    if (!this.telefono.trim() || !this.dni.trim()) {
      this.errorMessage = 'El teléfono y DNI son obligatorios.';
      return;
    }

    if (this.esEmpresa && !this.nombreEmpresa.trim()) {
      this.errorMessage = 'El nombre de la empresa es obligatorio.';
      return;
    }


    this.isProcessing = true;

    // LÓGICA DE EMPRESA: Si es empresa, actualizamos primero el perfil
    if (this.esEmpresa && this.isLoggedIn) {
      this.companyService.saveCompanyProfile(this.nombreEmpresa).subscribe({
        next: () => {
          // Perfil actualizado, procedemos a guardar compra y navegar
          this.finalizarProcesamientoCompra();
        },
        error: (err) => {
          console.error('Error guardando empresa:', err);
          this.errorMessage = 'Error al actualizar los datos de empresa.';
          this.isProcessing = false;
        }
      });
    } else {
      // Flujo normal (Persona o Invitado)
      this.finalizarProcesamientoCompra();
    }
  }

  private finalizarProcesamientoCompra() {
    if (this.evento) {
      this.compraService.guardarEventoCompra({
        id: this.evento.id,
        titulo: this.evento.titulo,
        numeroEntradasGeneral: this.numeroEntradasGeneral,
        numeroEntradasVip: this.numeroEntradasVip,
        precioGeneral: this.precioGeneral,
        precioVip: this.precioVip,
        totalPrecio: this.totalPrecio,
        
        // Datos cliente
        nombreCliente: this.nombre,
        apellidosCliente: this.apellidos,
        telefonoCliente: this.telefono,
        dniCliente: this.dni,

        // ✅ AÑADIR ESTA LÍNEA (Si no está logueado, mandamos el email)
        email: !this.isLoggedIn ? this.email : undefined,

        // ... resto de campos (calle, ciudad, etc.) ...
        calle: this.calle,
        numero: this.numero,
        pisoPuerta: this.pisoPuerta,
        codigoPostal: this.codigoPostal,
        ciudad: this.ciudad,
        provincia: this.provincia,
        pais: this.pais,
        ubicacion: this.evento.ubicacion,
        fecha: this.evento.fecha ? (this.evento.fecha instanceof Date ? this.evento.fecha.toLocaleDateString() : String(this.evento.fecha)) : '',
        imagen: this.evento.imagen,
        generalTicketEventId: this.generalTicketEventId || undefined,
        vipTicketEventId: this.vipTicketEventId || undefined,
        codigoDescuento: this.codigoDescuento || undefined
      });

      this.router.navigate(['/pagos', this.evento.id]);
    }
    this.isProcessing = false;
  }

  // Descuentos y GoBack igual...
  quitarDescuento(): void {
    this.codigoValido = false;
    this.mensajeDescuento = '';
    this.codigoDescuento = '';
    this.tipoDescuento = null;
    this.valorDescuento = 0;
  }

  validarCodigo() {
    const codigo = this.codigoDescuento.trim().toUpperCase();
    if (!codigo) { this.quitarDescuento(); return; }
    this.validandoCodigo = true;
    this.mensajeDescuento = '';
    this.http.post<any>(`${this.apiUrl}/discounts/validate`, { code: codigo }).subscribe({
      next: (response) => {
        const data = response.discount;
        if (data && data.valido) {
          this.codigoValido = true;
          this.tipoDescuento = 'porcentaje';
          this.valorDescuento = data.porcentaje || data.descuento * 100;
          this.mensajeDescuento = `¡Éxito! Descuento del ${this.valorDescuento}% aplicado.`;
        }
        this.validandoCodigo = false;
      },
      error: (error) => {
        this.codigoValido = false;
        this.mensajeDescuento = error.error?.error || 'El código no existe o ha expirado.';
        this.tipoDescuento = null;
        this.valorDescuento = 0;
        this.validandoCodigo = false;
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/eventos']);
  }
}