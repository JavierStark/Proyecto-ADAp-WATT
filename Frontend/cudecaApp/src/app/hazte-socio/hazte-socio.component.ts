import { Component } from '@angular/core';

@Component({
  selector: 'app-hazte-socio',
  imports: [],
  templateUrl: './hazte-socio.component.html',
  styleUrl: './hazte-socio.component.css'
})
export class HazteSocioComponent {



  planSeleccionado: string | null = null;
  importeSeleccionado = 0;

  seleccionarPlan(plan: string, importe: number) {
    this.planSeleccionado = plan;
    this.importeSeleccionado = importe;
  }

}


