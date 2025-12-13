import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CompraService, EventoCompra, SocioCompra } from '../services/compra.service';
import { DonacionState, DonacionCompra } from '../services/compra.service';
import { AuthService } from '../services/auth.service';
import { Router, ActivatedRoute } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';

interface PaymentMethod {
  id: string;
  name: string;
}

@Component({
  selector: 'app-pagos',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './pagos.component.html',
  styleUrls: ['./pagos.component.css']
})
export class PagosComponent implements OnInit {
  selectedPaymentMethod: string | null = null;
  eventoCompra: EventoCompra | null = null;
  eventoId: string | null = null;
  isProcessing: boolean = false;
  errorMessage: string = '';

  // Métodos de pago disponibles
  paymentMethods: PaymentMethod[] = [
    { id: 'bizum', name: 'Bizum' },
    { id: 'debit_card', name: 'Tarjeta de débito/crédito' },
    { id: 'apple_pay', name: 'Apple Pay' },
    { id: 'google_pay', name: 'Google Pay' }
  ];

  private apiUrl = 'https://cudecabackend-c7hhc5ejeygfb4ah.spaincentral-01.azurewebsites.net';

  constructor(
    private compraService: CompraService, 
    private authService: AuthService,
    private http: HttpClient,
    private router: Router,
    private route: ActivatedRoute
  ) {}

  socioCompra: SocioCompra | null = null;
  donacionCompra: DonacionCompra | null = null;

  ngOnInit(): void {
  this.eventoId = this.route.snapshot.paramMap.get('id');

  if (this.eventoId === 'socio') {
    this.socioCompra = this.compraService.obtenerSocioCompra();
  } else if (this.eventoId === 'donacion') {
    this.donacionCompra = DonacionState.obtener();
  } else {
    this.eventoCompra = this.compraService.obtenerEventoCompra();
  }
  }

  

  selectPaymentMethod(methodId: string): void {
    this.selectedPaymentMethod = methodId;
  }

processPayment(): void {
    if (!this.selectedPaymentMethod) {
      this.errorMessage = 'Selecciona un método de pago';
      return;
    }

    if (this.socioCompra) {
      this.procesarPagoSocio();
    } else if (this.donacionCompra) {
      this.procesarPagoDonacion();
    } else {
      this.procesarPagoEvento();
    }
  }

private procesarPagoEvento(): void {
  if (!this.eventoCompra) {
    this.errorMessage = 'No se encontraron datos de compra';
    return;
  }

  if (!this.selectedPaymentMethod) {
    this.errorMessage = 'Selecciona un método de pago';
    return;
  }

  this.isProcessing = true;
  this.errorMessage = '';

  const items: any[] = [];

  if (this.eventoCompra.numeroEntradasGeneral > 0 && this.eventoCompra.generalTicketEventId) {
    items.push({
      ticketEventId: this.eventoCompra.generalTicketEventId,
      quantity: this.eventoCompra.numeroEntradasGeneral
    });
  }

  if (this.eventoCompra.numeroEntradasVip > 0 && this.eventoCompra.vipTicketEventId) {
    items.push({
      ticketEventId: this.eventoCompra.vipTicketEventId,
      quantity: this.eventoCompra.numeroEntradasVip
    });
  }

  const payload = {
    eventId: this.eventoCompra.id,
    items,
    paymentToken: 'sim_ok',
    paymentMethod: this.selectedPaymentMethod,
    discountCode: this.eventoCompra.codigoDescuento || null,
    dni: this.eventoCompra.dniCliente,
    nombre: this.eventoCompra.nombreCliente,
    apellidos: this.eventoCompra.apellidosCliente,
    telefono: this.eventoCompra.telefonoCliente,
    calle: this.eventoCompra.calle,
    numero: this.eventoCompra.numero,
    pisoPuerta: this.eventoCompra.pisoPuerta,
    ciudad: this.eventoCompra.ciudad,
    codigoPostal: this.eventoCompra.codigoPostal,
    provincia: this.eventoCompra.provincia,
    pais: this.eventoCompra.pais
  };


  const token = localStorage.getItem('token');
  const headers = new HttpHeaders({
    Authorization: `Bearer ${token}`
  });

  this.http.post(`${this.apiUrl}/tickets/purchase`, payload, { headers }).subscribe({
    next: () => {
      this.compraService.limpiarEventoCompra();
      this.router.navigate(['/compra-finalizada']);
    },
    error: (err) => {
      console.error('❌ Error compra evento:', err);
      this.errorMessage =
        err.error?.message || 'Error al procesar el pago';
      this.isProcessing = false;
    }
  });
}


private procesarPagoSocio(): void {
  if (!this.socioCompra) {
    this.errorMessage = 'No se encontraron datos de la suscripción';
    return;
  }

  if (!this.selectedPaymentMethod) {
    this.errorMessage = 'Selecciona un método de pago';
    return;
  }

  const token = localStorage.getItem('token');

  this.isProcessing = true;
  this.errorMessage = '';

  const payload = {
    plan: this.socioCompra.plan,
    importe: this.socioCompra.importe,
    paymentToken: 'sim_ok',              // obligatorio y NO vacío
    metodoPago: this.selectedPaymentMethod
  };

  const headers = new HttpHeaders({
    Authorization: `Bearer ${token}`
  });

  this.http
    .post(`${this.apiUrl}/partners/subscribe`, payload, {headers})
    .subscribe({
      next: (res: any) => {

        // 1️⃣ Guardar resumen para la pantalla final
        sessionStorage.setItem(
          'socioResumen',
          JSON.stringify({
            mensaje: res.mensaje,
            vence: res.vence,
            pagoRef: res.pagoRef
          })
        );

        // 2️⃣ Limpiar estado
        this.compraService.limpiarSocioCompra();

        // 3️⃣ Navegar a compra finalizada
        this.router.navigate(['/compra-finalizada']);
      },

      error: (err) => {
        console.error('❌ Error backend socio:', err);
        this.errorMessage = 'Error al procesar la suscripción';
        this.isProcessing = false;
      }
    });
}

private procesarPagoDonacion(): void {
  if (!this.donacionCompra) {
    this.errorMessage = 'No se encontraron datos de donación';
    return;
  }

  if (!this.selectedPaymentMethod) {
    this.errorMessage = 'Selecciona un método de pago';
    return;
  }

  const token = localStorage.getItem('token');
  this.isProcessing = true;
  this.errorMessage = '';

  const headers = new HttpHeaders({
    Authorization: `Bearer ${token}`
  });

  const payload = {
    Amount: this.donacionCompra.importe,
    PaymentMethod: this.selectedPaymentMethod,
    paymentToken: 'sim_ok'
  };

  this.http.post(`${this.apiUrl}/donations`, payload, { headers }).subscribe({
    next: (res: any) => {
      // Guardar resumen y limpiar estado de donación
      sessionStorage.setItem(
        'donacionResumen',
        JSON.stringify({
          mensaje: res?.message || `Donación de ${this.donacionCompra!.importe}€ realizada correctamente`,
          pagoRef: res?.paymentId || null,
          importe: this.donacionCompra!.importe
        })
      );

      DonacionState.limpiar();
      this.router.navigate(['/compra-finalizada']);
    },
    error: (err) => {
      console.error('❌ Error backend donación:', err);
      this.errorMessage = err.error?.error || err.error?.message || 'Error al procesar la donación';
      this.isProcessing = false;
    }
  });
}

}


