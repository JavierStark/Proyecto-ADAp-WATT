import { Component } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-home',
  imports: [],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css'] 
})
export class HomeComponent {
  constructor(private router: Router) {}
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
      setTimeout(() => this.escribirTexto(), 30); // velocidad por letra
    }
  }
  
  goToLogin() {
    this.router.navigate(['/log-in']);
  }

  goToSignUp() {
    this.router.navigate(['/sign-up']);
  }
  goToDonation() {
    this.router.navigate(['/donation']);
  }
  
}
