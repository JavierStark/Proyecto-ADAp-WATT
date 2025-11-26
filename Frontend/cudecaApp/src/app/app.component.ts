import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, Router } from '@angular/router';
import {HostListener } from '@angular/core';
import { NgClass } from '@angular/common';
@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  imports: [RouterOutlet, NgClass],
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  constructor(private router: Router) {}

  showHeader = true;  // Por defecto el header es visible
    private lastScrollTop = 0;  // Última posición de scroll

    @HostListener('window:scroll', ['$event'])
    onWindowScroll() {
      const currentScroll = window.pageYOffset || document.documentElement.scrollTop;

      // Si el scroll está bajando, ocultamos el header
      if (currentScroll > this.lastScrollTop && currentScroll > 0) {
        this.showHeader = false;
      } else {
        this.showHeader = true;  // Si el scroll va hacia arriba, mostramos el header
      }

      this.lastScrollTop = currentScroll <= 0 ? 0 : currentScroll; // Evitar desplazamiento negativo
    }
  

  goToLogin() {
    this.router.navigate(['/auth/log-in']);  // Navega a la página de login
  }

  goToSignUp() {
    this.router.navigate(['/auth/sign-up']); // Navega a la página de registro
  }

  
}
