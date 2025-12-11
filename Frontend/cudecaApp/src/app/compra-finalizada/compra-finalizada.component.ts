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
}
