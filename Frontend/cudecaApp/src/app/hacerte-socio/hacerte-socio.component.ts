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

    this.http.get<any>(`${this.apiUrl}/partners/data`).subscribe({
    next: (data) => {
      if (data?.isActivo) {
        this.isSuscrito = true;
        this.suscripcion = {
          tipo: data.plan,
          vence: data.fechaFin,
          pagoRef: data.pagoRef ?? ''
        };
      } else {
        this.isSuscrito = false;
      }

      this.isCheckingSuscripcion = false;
    },
    error: () => {
      // ❗ Cualquier error → no es socio
      this.isSuscrito = false;
      this.isCheckingSuscripcion = false;
    }
  });

    if (this.forzarVistaNoSuscrito) {
        this.isSuscrito = false;
        return;
      }
    const token = localStorage.getItem('token');

    // 🔒 Si no está logueado, no puede ser socio
    if (!token) {
      this.isSuscrito = false;
      return;
    }

    const headers = new HttpHeaders({
      Authorization: `Bearer ${token}`
    });

    this.http.get<any>(`${this.apiUrl}/partners/data`, { headers }).subscribe({
      next: (data) => {
        if (data?.isActivo) {
          this.isSuscrito = true;
          this.suscripcion = {
            tipo: data.plan,
            vence: data.fechaFin,
            pagoRef: data.pagoRef ?? ''
          };
        } else {
          this.isSuscrito = false;
        }
      },
      error: () => {
        // 401 / 404 → no es socio
        this.isSuscrito = false;
      }
    });
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
  this.forzarVistaNoSuscrito = true;
  this.isSuscrito = false;
  this.suscripcion = null;
}


  goBack(): void {
    this.router.navigate(['/']);
  }
}
