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
  if (!this.nombre || !this.apellidos || !this.telefono) {
    alert('Completa los datos obligatorios');
    return;
  }

  this.compraService.guardarSocioCompra({
    tipo: this.plan as any,
    precio: this.precioSocio,
    nombre: this.nombre,
    apellidos: this.apellidos,
    telefono: this.telefono,
    dni: this.dni
  });

  // 👉 reutilizamos el mismo componente de pagos
  this.router.navigate(['/pagos', 'socio']);
}

}
