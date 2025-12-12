import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CompraService } from '../services/compra.service';
import { HttpClient, HttpHeaders } from '@angular/common/http';

type Plan = 'mensual' | 'trimestral' | 'anual';

@Component({
  selector: 'app-hacerte-socio',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './hacerte-socio.component.html'
})
export class HacerteSocioComponent implements OnInit {
  forzarVistaNoSuscrito = false;

  plan: Plan | null = null;
  isCheckingSuscripcion = true;
  modoRenovacion = false;
  view: 'suscrito' | 'planes' = 'planes';

  isSuscrito = false;

  suscripcion: {
    tipo: Plan | string;
    vence: string;
    pagoRef: string;
  } | null = null;

  esEmpresa = false;
  precioSocio = 50;

  private apiUrl =
    'https://cudecabackend-c7hhc5ejeygfb4ah.spaincentral-01.azurewebsites.net';

  constructor(
    private router: Router,
    private compraService: CompraService,
    private http: HttpClient
  ) {}

 ngOnInit(): void {

  const planGuardado = sessionStorage.getItem('socioPlan');
  const precioGuardado = sessionStorage.getItem('socioPrecio');

  if (planGuardado && precioGuardado) {
    this.plan = planGuardado as Plan;
    this.precioSocio = Number(precioGuardado);
  }

}





  setTipo(empresa: boolean): void {
    this.esEmpresa = empresa;
    this.precioSocio = empresa ? 150 : this.precioSocio;
  }

  continuarPago(): void {
    if (!this.plan) {
      alert('Selecciona un plan');
      return;
    }

    this.compraService.guardarSocioCompra({
      plan: this.plan,
      importe: this.precioSocio
    });

    this.router.navigate(['/pagos', 'socio']);
  }

 renovar(): void {
  this.view = 'planes';   // 👈 solo UI
}


  goBack(): void {
    this.router.navigate(['/']);
  }
}
