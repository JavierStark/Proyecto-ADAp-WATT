import { Component, OnInit  } from '@angular/core';
import { PartnerService } from '../services/partner.service';
import { PartnerApiService } from '../services/partner-api.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-hazte-socio',
  imports: [],
  templateUrl: './hazte-socio.component.html',
  styleUrl: './hazte-socio.component.css'
})
export class HazteSocioComponent implements OnInit{



  planSeleccionado: 'mensual' | 'trimestral' | 'anual' | null = null;
  precioSeleccionado: number = 0;
  isProcessing = false;
  errorMessage = '';

  constructor(private router: Router) {}

    ngOnInit(): void {
    // aquí podrías recuperar datos si quieres más adelante
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


