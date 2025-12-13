import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms'; // Necesario para el input con ngModel

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

  constructor() {}

  // Función para seleccionar una cantidad predefinida
  selectPreset(amount: number) {
    this.selectedAmount = amount;
    this.isCustomAmountMode = false;
    this.customAmountValue = null;
  }

  // Función para activar el modo "Otro"
  enableCustomAmount() {
    this.selectedAmount = null;
    this.isCustomAmountMode = true;
  }

  // Función del botón Pagar
  processPayment() {
    const finalAmount = this.isCustomAmountMode ? this.customAmountValue : this.selectedAmount;

    if (!finalAmount || finalAmount <= 0) {
      alert('Por favor selecciona o introduce una cantidad válida.');
      return;
    }

    // Aquí iría la lógica de conexión con la pasarela de pago (Stripe, PayPal, etc.)
    console.log('Procesando pago por valor de:', finalAmount + '€');
    alert(`Iniciando pago de ${finalAmount}€. (Simulación)`);
  }
}