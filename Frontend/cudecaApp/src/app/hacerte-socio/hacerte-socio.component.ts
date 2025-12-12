import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CompraService } from '../services/compra.service';

@Component({
  selector: 'app-hacerte-socio',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './hacerte-socio.component.html'
})
export class HacerteSocioComponent implements OnInit {

  // Plan seleccionado
  plan: 'mensual' | 'trimestral' | 'anual' | null = null;

  // Tipo de socio
  esEmpresa = false;
  precioSocio = 50;

  // Datos personales
  nombre = '';
  apellidos = '';
  telefono = '';
  dni = '';

  constructor(
    private router: Router,
    private compraService: CompraService
  ) {}

  ngOnInit() {
    const plan = sessionStorage.getItem('socioPlan');
    const precio = sessionStorage.getItem('socioPrecio');

    if (plan && precio) {
      this.plan = plan as any;
      this.precioSocio = +precio;
    }
  }

  setTipo(empresa: boolean) {
    this.esEmpresa = empresa;
    this.precioSocio = empresa ? 150 : this.precioSocio;
  }


  goBack() {
    this.router.navigate(['/']);
  }

  continuarPago() {
  if (!this.plan) {
    alert('Selecciona un plan');
    return;
  }

  // ⚠️ NO validamos nombre/apellidos/teléfono
  // porque NO son necesarios para la suscripción

  this.compraService.guardarSocioCompra({
    plan: this.plan,
    importe: this.precioSocio
  });

  // 👉 reutilizamos el mismo componente de pagos
  this.router.navigate(['/pagos', 'socio']);
}

}

