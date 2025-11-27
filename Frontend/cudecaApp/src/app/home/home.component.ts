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
  frase = 'Con tu donación, transformas el dolor en dignidad y acompañamiento para cientos de pacientes y sus familias. Por favor, colabora hoy con Cudeca y haz que cada momento cuente.';
  i = 0;

  ngOnInit() {
    this.escribirTexto();
  }

  escribirTexto() {
    if (this.i < this.frase.length) {
      this.texto += this.frase[this.i];
      this.i++;
      setTimeout(() => this.escribirTexto(), 50); // velocidad por letra
    }
  }
  
  
}
