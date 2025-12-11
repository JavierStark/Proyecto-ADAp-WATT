import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CompraService, EventoCompra } from '../services/compra.service';
import { Router, ActivatedRoute } from '@angular/router';

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

  // Métodos de pago disponibles
  paymentMethods: PaymentMethod[] = [
    { id: 'bizum', name: 'Bizum' },
    { id: 'bank_app', name: 'App del banco' },
    { id: 'debit_card', name: 'Tarjeta de débito/crédito' },
    { id: 'apple_pay', name: 'Apple Pay' },
    { id: 'google_pay', name: 'Google Pay' }
  ];

  constructor(
    private compraService: CompraService, 
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

  processPayment(): void {
    if (!this.selectedPaymentMethod) {
      alert('Por favor selecciona un método de pago');
      return;
    }
    
    console.log('Procesando pago con:', this.selectedPaymentMethod);
    console.log('Evento compra:', this.eventoCompra);
    // Aquí se integraría la lógica de pago real
    // Simulamos éxito y redirigimos a la página de compra finalizada
    
    // Limpiar los datos de compra del sessionStorage después del pago exitoso
    this.compraService.limpiarEventoCompra();
    
    this.router.navigate(['/compra-finalizada']);
  }
}
