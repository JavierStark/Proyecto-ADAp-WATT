import { Component, OnInit} from '@angular/core';
import { CommonModule } from '@angular/common';
import { PartnerService } from '../services/partner.service';
import { PartnerApiService } from '../services/partner-api.service';
import { Router } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { CompraService } from '../services/compra.service';

@Component({
  selector: 'app-hazte-socio',
  imports: [CommonModule],
  templateUrl: './hazte-socio.component.html',
  styleUrl: './hazte-socio.component.css'
})
export class HazteSocioComponent implements OnInit{


    private apiUrl = 'https://cudecabackend-c7hhc5ejeygfb4ah.spaincentral-01.azurewebsites.net';
  planSeleccionado: 'mensual' | 'trimestral' | 'anual' | null = null;
  precioSeleccionado: number = 0;
  isProcessing = false;
  errorMessage = '';
  isCheckingSuscripcion = true;

    constructor(
    private router: Router,
    private compraService: CompraService,
    private http: HttpClient
    ) {}


    ngOnInit(): void {

  const renovar = sessionStorage.getItem('renovarSuscripcion') === 'true';

  const token = localStorage.getItem('token');
  const headers = token
    ? new HttpHeaders({ Authorization: `Bearer ${token}` })
    : undefined;

  this.http.get<any>(`${this.apiUrl}/partners/data`, { headers }).subscribe({
    next: (data) => {
      if (data?.isActivo && !renovar) {
        // 🚫 Ya es socio y NO está renovando → fuera
        this.router.navigate(['/ya-eres-socio']);
        return;
      }

      // 🟢 Puede continuar (no es socio o está renovando)
      this.isCheckingSuscripcion = false;

      // 🔥 flag usada → se elimina
      sessionStorage.removeItem('renovarSuscripcion');
    },
    error: () => {
      // No es socio → puede continuar
      this.isCheckingSuscripcion = false;
      sessionStorage.removeItem('renovarSuscripcion');
    }
  });
}

  seleccionarPlan(plan: 'mensual' | 'trimestral' | 'anual', precio: number) {
    this.planSeleccionado = plan;
    this.precioSeleccionado = precio;
  }

  hacerseSocio() {
    if (!this.planSeleccionado) {
      this.errorMessage = 'Selecciona un plan';
      return;
    }

    this.isProcessing = true;

    // guardamos la info si quieres usarla luego
    sessionStorage.setItem('socioPlan', this.planSeleccionado);
    sessionStorage.setItem('socioPrecio', this.precioSeleccionado.toString());

    // navegación al nuevo componente
    this.goto('/hacerte-socio');
  }

  goto(path: string) {
    this.router.navigate([path]);
  }
}


