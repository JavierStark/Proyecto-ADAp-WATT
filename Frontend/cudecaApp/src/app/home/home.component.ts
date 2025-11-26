import { Component } from '@angular/core';

@Component({
  selector: 'app-home',
  imports: [],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css'] 
})
export class HomeComponent {
title = 'oratoristWeb';

  texto = '';
  frase = 'Compra tus entradas aquí';
  i = 0;

  ngOnInit() {
    this.escribirTexto();
  }

  escribirTexto() {
    if (this.i < this.frase.length) {
      this.texto += this.frase[this.i];
      this.i++;
      setTimeout(() => this.escribirTexto(), 80); // velocidad por letra
    }
  }
  
  
}
