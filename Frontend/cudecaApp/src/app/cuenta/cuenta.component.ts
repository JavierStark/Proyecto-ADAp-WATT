import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-cuenta',
  standalone: true,
  imports: [],
  templateUrl: './cuenta.component.html',
  styles: `` // No necesitamos CSS por ahora, usaremos Tailwind directo en el HTML
})
export class CuentaComponent {

  constructor(private authService: AuthService, private router: Router) {}

  onLogout() {
    // 1. Borramos el token
    this.authService.logout();
    
    // 2. Redirigimos al home
    this.router.navigate(['/']);
  }
}