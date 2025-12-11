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
  confirmPassword: string = '';
  isLoading: boolean = false;
  showPassword: boolean = false;
  showConfirmPassword: boolean = false;

  // 2. Inyectamos el AuthService
  constructor(
    private router: Router, 
    private location: Location,
    private authService: AuthService 
  ) {}

  onSubmit() {
    if (this.isLoading) return; // Evita doble clic accidental

    // Validar que las contraseñas coincidan
    if (this.password !== this.confirmPassword) {
      alert('Las contraseñas no coinciden. Por favor, verifica que ambas sean iguales.');
      return;
    }

    // Validar que la contraseña no esté vacía
    if (!this.password || this.password.trim() === '') {
      alert('La contraseña no puede estar vacía.');
      return;
    }

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
        this.isLoading = false
        console.error(error);
        // Intentamos mostrar el mensaje de error del backend si existe
        alert(error.message || 'Error al crear la cuenta');
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