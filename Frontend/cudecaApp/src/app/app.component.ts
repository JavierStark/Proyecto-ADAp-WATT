import { Component, OnInit } from '@angular/core';
import { RouterOutlet, NavigationEnd, Router } from '@angular/router';
import { HostListener } from '@angular/core'; // Se queda por si usas el de 'storage'
import { NgClass, CommonModule } from '@angular/common'; 
import { AuthService } from './services/auth.service';
import { HelpModalComponent } from './help-modal/help-modal.component';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  imports: [RouterOutlet, NgClass, CommonModule, HelpModalComponent],
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit {

  isAuthRoute: boolean = false;
  isLoggedIn: boolean = false;
  menuOpen = false; // Para el menú móvil
  showHelpModal = false; // Para el modal de ayuda
  private tokenCheckInterval: any;

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

  ngOnInit() {
    // Verificar cada 30 segundos si el token ha expirado
    this.tokenCheckInterval = setInterval(() => {
      if (this.authService.isLoggedIn() && this.authService.isTokenExpired()) {
        this.authService.forceLogout('Tu sesión ha expirado. Por favor, inicia sesión nuevamente.');
        this.router.navigate(['/log-in']);
        this.updateAuthStatus();
      }
    }, 30000); // Verificar cada 30 segundos
  }

  ngOnDestroy() {
    // Limpiar el intervalo cuando se destruya el componente
    if (this.tokenCheckInterval) {
      clearInterval(this.tokenCheckInterval);
    }
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

  goToPagos() {
    this.router.navigate(['/pagos']); // Navega a la página de pagos
  }

  openHelpModal() {
    this.showHelpModal = true;
  }

  closeHelpModal() {
    this.showHelpModal = false;
  }

  private updateAuthStatus() {
    this.isLoggedIn = this.authService.isLoggedIn();
  }
}