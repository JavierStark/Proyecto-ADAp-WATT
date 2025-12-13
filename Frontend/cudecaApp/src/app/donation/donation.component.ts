import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms'; // Necesario para el input con ngModel
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Router } from '@angular/router';
import { DonacionState } from '../services/compra.service';

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

    constructor(private router: Router) {}

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

  get hasValidAmount(): boolean {
    const amount = this.isCustomAmountMode ? Number(this.customAmountValue) : this.selectedAmount;
    return !!amount && amount > 0;
  }

  // Función del botón Pagar
  processPayment() {
    const finalAmount = this.isCustomAmountMode ? Number(this.customAmountValue) : this.selectedAmount;

    if (!finalAmount || finalAmount <= 0) {
      alert('Por favor selecciona o introduce una cantidad válida.');
      return;
    }


    // Guardar estado y redirigir a la pantalla de pagos unificada
    DonacionState.guardar({ importe: finalAmount });
    this.router.navigate(['/pagos', 'donacion']);
  }
}