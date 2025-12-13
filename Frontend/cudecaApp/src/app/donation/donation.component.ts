import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';

interface PaymentMethod {
  id: string;
  name: string;
}

type Vista = 'MONTO' | 'METODO' | 'COMPLETADO';

@Component({
  selector: 'app-donation',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './donation.component.html',
  styleUrls: ['./donation.component.css']
})
export class DonationComponent {
  // Cantidades predefinidas
  presetAmounts: number[] = [5, 10, 20, 50];
  
  // Estado de la selección
  selectedAmount: number | null = null;
  isCustomAmountMode: boolean = false;
  customAmountValue: number | null = null;

  // Métodos de pago
  selectedPaymentMethod: string | null = null;
  paymentMethods: PaymentMethod[] = [
    { id: 'bizum', name: 'Bizum' },
    { id: 'debit_card', name: 'Tarjeta de débito/crédito' },
    { id: 'apple_pay', name: 'Apple Pay' },
    { id: 'google_pay', name: 'Google Pay' }
  ];

  // Estado de procesamiento
  isProcessing: boolean = false;
  vista: Vista = 'MONTO';
  errorMessage: string = '';

  // Donación completada
  montoCompletado: number | null = null;
  metodoPago: string | null = null;

  constructor(private authService: AuthService, private router: Router) {}

  // Función para seleccionar una cantidad predefinida
  selectPreset(amount: number) {
    this.selectedAmount = amount;
    this.isCustomAmountMode = false;
    this.customAmountValue = null;
    this.errorMessage = '';
  }

  // Función para activar el modo "Otro"
  enableCustomAmount() {
    this.selectedAmount = null;
    this.isCustomAmountMode = true;
    this.errorMessage = '';
  }

  // Validar monto y mostrar métodos de pago
  confirmarMonto() {
    const finalAmount = this.isCustomAmountMode ? this.customAmountValue : this.selectedAmount;

    if (!finalAmount || finalAmount <= 0) {
      this.errorMessage = 'Por favor selecciona o introduce una cantidad válida.';
      return;
    }

    if (finalAmount < 1) {
      this.errorMessage = 'La cantidad mínima es 1€';
      return;
    }

    this.errorMessage = '';
    this.vista = 'METODO';
    this.selectedPaymentMethod = null;
  }

  // Seleccionar método de pago
  selectPaymentMethod(methodId: string) {
    this.selectedPaymentMethod = methodId;
  }

  // Procesar el pago
  processPayment() {
    const finalAmount = this.isCustomAmountMode ? this.customAmountValue : this.selectedAmount;

    if (!finalAmount || finalAmount <= 0) {
      this.errorMessage = 'Por favor selecciona una cantidad válida.';
      return;
    }

    if (!this.selectedPaymentMethod) {
      this.errorMessage = 'Por favor selecciona un método de pago.';
      return;
    }

    this.isProcessing = true;
    this.errorMessage = '';

    // Llamar al servicio para crear la donación
    this.authService.createDonation(finalAmount, this.selectedPaymentMethod).subscribe({
      next: (response) => {
        this.isProcessing = false;
        
        // Guardar los datos de la donación completada
        this.montoCompletado = finalAmount;
        this.metodoPago = this.paymentMethods.find(m => m.id === this.selectedPaymentMethod)?.name || '';
        
        // Mostrar pantalla de confirmación
        this.vista = 'COMPLETADO';
      },
      error: (error) => {
        this.isProcessing = false;
        console.error('Error al procesar la donación:', error);
        
        if (error.error?.error) {
          this.errorMessage = `❌ ${error.error.error}`;
        } else {
          this.errorMessage = '❌ Error al procesar la donación. Por favor, intenta de nuevo.';
        }
      }
    });
  }

  // Volver al inicio desde la confirmación
  volverAlInicio() {
    this.router.navigate(['/']);
  }

  // Volver a seleccionar método de pago
  volverAMetodos() {
    this.vista = 'METODO';
    this.errorMessage = '';
  }

  // Volver a seleccionar monto
  volverAMonto() {
    this.vista = 'MONTO';
    this.selectedPaymentMethod = null;
    this.errorMessage = '';
  }
}