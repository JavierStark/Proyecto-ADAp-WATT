import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

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

  constructor(private router: Router) {}

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

  continuarPago() {
    if (!this.nombre || !this.apellidos || !this.telefono) {
      alert('Completa los datos obligatorios');
      return;
    }

    // Aquí luego podrás guardar los datos en un service
    this.router.navigate(['/pagos-socio']);
  }

  goBack() {
    this.router.navigate(['/']);
  }
}
