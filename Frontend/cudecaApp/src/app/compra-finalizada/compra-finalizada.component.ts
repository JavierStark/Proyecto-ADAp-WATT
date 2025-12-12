import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-compra-finalizada',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './compra-finalizada.component.html',
  styleUrls: ['./compra-finalizada.component.css']
})
export class CompraFinalizadaComponent {
  constructor() {}


   socioResumen: {
    mensaje: string;
    vence: string;
    pagoRef: string;
  } | null = null;

  ngOnInit(): void {
    const raw = sessionStorage.getItem('socioResumen');
    if (raw) {
      this.socioResumen = JSON.parse(raw);
      sessionStorage.removeItem('socioResumen');
    }
  }

  
}
