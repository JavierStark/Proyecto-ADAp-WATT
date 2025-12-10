import { Component } from '@angular/core';
import { RouterOutlet, NavigationEnd, Router } from '@angular/router';
import { HostListener } from '@angular/core'; // Se queda por si usas el de 'storage'
import { NgClass, CommonModule } from '@angular/common'; 
import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  imports: [RouterOutlet, NgClass, CommonModule],
  styleUrls: ['./app.component.css']
})
export class AppComponent {

  isAuthRoute: boolean = false;
  isLoggedIn: boolean = false;
  menuOpen = false; // Para el menú móvil

  constructor(private router: Router, private authService: AuthService) {
     // Detectar cambios en la ruta (login/signup)
    this.router.events.subscribe((event) => {
      if (event instanceof NavigationEnd) {
        this.isAuthRoute = event.urlAfterRedirects.includes('/log-in') || event.urlAfterRedirects.includes('/sign-up');
        this.updateAuthStatus();
      }
    });
    this.updateAuthStatus();
  }

  // Escucha cambios en el localStorage (por si hacen login en otra pestaña)
  @HostListener('window:storage', ['$event'])
  onStorageChange() {
    this.updateAuthStatus();
  }

  // --- NAVEGACIÓN ---
  goToLogin() { this.router.navigate(['/log-in']); }
  goToSignUp() { this.router.navigate(['/sign-up']); }
  goToDonation() { this.router.navigate(['/donation']); }
  goToCuenta() { this.router.navigate(['/cuenta']); }
  goToHome() { this.router.navigate(['/']); }
  goToEventos() { this.router.navigate(['/eventos']); }

  private updateAuthStatus() {
    this.isLoggedIn = this.authService.isLoggedIn();
  }
}