import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, Router, NavigationEnd } from '@angular/router';
import {HostListener } from '@angular/core';
import { NgClass } from '@angular/common';
import { CommonModule } from '@angular/common'; 

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  imports: [RouterOutlet, NgClass, CommonModule],
  styleUrls: ['./app.component.css']
})
export class AppComponent {

   isAuthRoute: boolean = false;
  constructor(private router: Router) {

     // Detectar cambios en la ruta
    this.router.events.subscribe((event) => {
      if (event instanceof NavigationEnd) {
        this.isAuthRoute = event.urlAfterRedirects.includes('/log-in') || event.urlAfterRedirects.includes('/sign-up');
        console.log('isAuthRoute:', this.isAuthRoute);
      }
    });

  }

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
    this.router.navigate(['/log-in']);  // Navega a la página de login
  }

  goToSignUp() {
    this.router.navigate(['/sign-up']); // Navega a la página de registro
  }
   goToDonation() {
    this.router.navigate(['/donation']);
  }
  goToHome() {
    this.router.navigate(['/']); // Navega a la raíz (Home)
  }

  goToEventos() {
    this.router.navigate(['/eventos']); // Navega a la página de eventos
  }
}
