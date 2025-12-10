import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

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

  // Métodos de pago disponibles
  paymentMethods: PaymentMethod[] = [
    { id: 'bizum', name: 'Bizum' },
    { id: 'bank_app', name: 'App del banco' },
    { id: 'debit_card', name: 'Tarjeta de débito/crédito' },
    { id: 'apple_pay', name: 'Apple Pay' },
    { id: 'google_pay', name: 'Google Pay' }
  ];

  constructor() {}

  ngOnInit(): void {
    // Aquí se cargaría el evento seleccionado
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
    // Aquí se integraría la lógica de pago real
  }
}
