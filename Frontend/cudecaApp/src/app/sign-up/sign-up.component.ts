import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Location } from '@angular/common';
import { CommonModule } from '@angular/common';
import { AuthService } from '../services/auth.service'; 

@Component({
  selector: 'app-sign-up',
  imports: [FormsModule, CommonModule],
  templateUrl: './sign-up.component.html',
  styleUrl: './sign-up.component.css'
})
export class SignUpComponent {
  
  email: string = '';
  password: string = '';
  isLoading: boolean = false;

  // 2. Inyectamos el AuthService
  constructor(
    private router: Router, 
    private location: Location,
    private authService: AuthService 
  ) {}

  onSubmit() {
    if (this.isLoading) return; // Evita doble clic accidental

    console.log('Enviando registro...');

    this.isLoading = true;

    const credentials = {
      email: this.email,
      password: this.password
    };

    // 3. Llamamos al método register del servicio
    this.authService.register(credentials).subscribe({
      next: (response) => {
        console.log('Registro exitoso:', response);
        this.isLoading = false;
        alert('Cuenta creada con éxito. HEMOS ENVIADO UN CORREO DE CONFIRMACIÓN. Por favor, revísalo y haz clic en el enlace para activar tu cuenta antes de iniciar sesión.');
        
        // Redirigir al Login para que entre
        this.router.navigate(['/log-in']); 
      },
      error: (error) => {
        console.error('Error al registrar:', error);
        // Intentamos mostrar el mensaje de error del backend si existe
        const mensaje = error.error?.message || error.error?.error || 'Hubo un error al crear la cuenta.';
        alert(mensaje);
        this.isLoading = false
      }
    });
  }

  goToLogin() {
    this.router.navigate(['/log-in']);
  }

  goBack() {
    this.location.back();
  }
}