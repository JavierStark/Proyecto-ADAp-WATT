import { Component, OnInit } from '@angular/core';
import { RouterOutlet, NavigationEnd, Router } from '@angular/router';
import { HostListener } from '@angular/core'; // Se queda por si usas el de 'storage'
import { NgClass, CommonModule } from '@angular/common'; 
import { AuthService } from './services/auth.service';
import { HelpModalComponent } from './help-modal/help-modal.component';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  imports: [RouterOutlet, CommonModule, HelpModalComponent],
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

  goToSocio() {
  this.router.navigate(['/hazte-socio']);
  }

  loaded = false;
  fadeOut = false;

   promoEventId: string | null = null;
    promoEventTitle: string | null = null;
    promoEventImage: string | null = null;
    showEventPopup: boolean = false;

  ngOnInit() {

    // Primero hacemos fade-out del logo
    setTimeout(() => {
        this.fadeOut = true;
    }, 900);

    // Luego mostramos el contenido real
    setTimeout(() => {
        this.loaded = true;
    }, 1600);

    

    // Verificar cada 30 segundos si el token ha expirado
    this.tokenCheckInterval = setInterval(() => {
      if (this.authService.isLoggedIn() && this.authService.isTokenExpired()) {
        this.authService.forceLogout('Tu sesión ha expirado. Por favor, inicia sesión nuevamente.');
        this.router.navigate(['/log-in']);
        this.updateAuthStatus();
      }
    }, 30000); // Verificar cada 30 segundos

    this.cargarEventoDestacado();
  }

  // En app.component.ts

  private cargarEventoDestacado() {
      const url = 'https://cudecabackend-c7hhc5ejeygfb4ah.spaincentral-01.azurewebsites.net/events';

      fetch(url)
          .then(r => r.json())
          .then((items: any[]) => {
              // Verificar si hay elementos en la lista
              if (!items || items.length === 0) {
                  return;
              }

              // --- CAMBIO: SELECCIÓN ALEATORIA ---
              // Generamos un índice aleatorio entre 0 y el total de eventos
              const randomIndex = Math.floor(Math.random() * items.length);
              const evento = items[randomIndex];

              // Asignamos las variables del evento seleccionado
              this.promoEventId = evento.id || evento.Id || null;
              this.promoEventTitle =
                  evento.nombre ||
                  evento.titulo ||
                  evento.name ||
                  'Nuevo evento solidario';
              this.promoEventImage =
                  evento.imageUrl ||
                  evento.imagenUrl ||
                  evento.ImagenUrl ||
                  evento.imagenURL ||
                  evento.imagen ||
                  'assets/images/fondoCudeca.png';

              // --- CAMBIO: TEMPORIZADORES ---
              // 1. Mostrar el popup a los 5 segundos de cargar la app
              setTimeout(() => {
                  this.showEventPopup = true;

                  // 2. Ocultar el popup automáticamente 10 segundos después de mostrarse
                  setTimeout(() => {
                      this.showEventPopup = false;
                  }, 15000); 

              }, 5000); 
          })
          .catch(err => {
              console.error('Error cargando evento destacado', err);
          });
  } 

  cerrarPopupEvento() {
        this.showEventPopup = false;
    }

  irACompraEntradasDestacada() {
        if (this.promoEventId) {
            this.router.navigate(['/compra-entradas', this.promoEventId]);
        }
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