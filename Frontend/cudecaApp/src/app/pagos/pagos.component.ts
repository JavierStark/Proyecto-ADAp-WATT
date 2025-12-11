import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CompraService, EventoCompra } from '../services/compra.service';
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
    { id: 'bank_app', name: 'App del banco' },
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

  ngOnInit(): void {
    // Capturar el ID del evento desde la ruta
    this.eventoId = this.route.snapshot.paramMap.get('id');
    // Obtener los datos de la compra del servicio
    this.eventoCompra = this.compraService.obtenerEventoCompra();
  }

  selectPaymentMethod(methodId: string): void {
    this.selectedPaymentMethod = methodId;
  }

  private construirDireccion(): string {
    if (!this.eventoCompra) return '';
    const partes = [
      this.eventoCompra.calle || '',
      this.eventoCompra.numero || '',
      this.eventoCompra.pisoPuerta ? `${this.eventoCompra.pisoPuerta}` : ''
    ];
    return partes.filter(p => p).join(' ');
  }

  processPayment(): void {
    if (!this.selectedPaymentMethod) {
      this.errorMessage = 'Por favor selecciona un método de pago';
      return;
    }

    if (!this.eventoCompra) {
      this.errorMessage = 'No se encontraron datos de compra. Por favor, intenta de nuevo desde el principio.';
      return;
    }

    if (!this.eventoCompra.generalTicketEventId && this.eventoCompra.numeroEntradasGeneral > 0) {
      this.errorMessage = 'No se encontraron datos de tipos de entrada. Por favor, intenta de nuevo.';
      return;
    }

    this.isProcessing = true;
    this.errorMessage = '';

    // Construir el payload de la compra
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

    const purchasePayload = {
      eventId: this.eventoCompra.id,
      items: items,
      paymentToken: 'sim_ok', // En un caso real, aquí iría el token generado por el gateway de pago
      paymentMethod: this.selectedPaymentMethod,
      discountCode: this.eventoCompra.codigoDescuento || null,
      dni: this.eventoCompra.dniCliente,
      nombre: this.eventoCompra.nombreCliente,
      apellidos: this.eventoCompra.apellidosCliente,
      direccion: this.construirDireccion(),
      ciudad: this.eventoCompra.ciudad,
      codigoPostal: this.eventoCompra.codigoPostal,
      provincia: this.eventoCompra.provincia
    };

    console.log('📤 Enviando compra:', purchasePayload);

    const token = localStorage.getItem('token');
    const headers = new HttpHeaders({
      'Authorization': `Bearer ${token}`
    });

    this.http.post(`${this.apiUrl}/tickets/purchase`, purchasePayload, { headers }).subscribe({
      next: (response: any) => {
        console.log('✅ Compra exitosa:', response);
        // Limpiar los datos de compra del sessionStorage después del pago exitoso
        this.compraService.limpiarEventoCompra();
        this.router.navigate(['/compra-finalizada']);
      },
      error: (err) => {
        console.error('❌ Error procesando la compra:', err);
        const errorMsg = err.error?.error || err.error?.message || 'Error al procesar el pago. Por favor, intenta de nuevo.';
        this.errorMessage = errorMsg;
        this.isProcessing = false;
      }
    });
  }
}
