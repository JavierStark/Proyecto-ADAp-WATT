import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Location } from '@angular/common';
// 1. Importamos el servicio (ahora sí existe la ruta)
import { AuthService } from '../services/auth.service'; 

@Component({
  selector: 'app-sign-up',
  imports: [FormsModule],
  templateUrl: './sign-up.component.html',
  styleUrl: './sign-up.component.css'
})
export class SignUpComponent {
  
  username: string = '';
  email: string = '';
  password: string = '';

  // 2. Inyectamos el AuthService
  constructor(
    private router: Router, 
    private location: Location,
    private authService: AuthService 
  ) {}

  onSubmit() {
    console.log('Enviando registro...');

    // NOTA IMPORTANTE:
    // backend (Auth.cs) espera un objeto "RegisterDto" que SOLO tiene Email y Password.
    // Por eso, aunque el usuario escriba su nombre (username), no lo enviamos aquí
    // para evitar errores 400 Bad Request.
    const credentials = {
      email: this.email,
      password: this.password
    };

    // 3. Llamamos al método register del servicio
    this.authService.register(credentials).subscribe({
      next: (response) => {
        console.log('Registro exitoso:', response);
        alert('Cuenta creada con éxito. Por favor inicia sesión.');
        
        // Redirigir al Login para que entre
        this.router.navigate(['/log-in']); 
      },
      error: (error) => {
        console.error('Error al registrar:', error);
        // Intentamos mostrar el mensaje de error del backend si existe
        const mensaje = error.error?.message || error.error?.error || 'Hubo un error al crear la cuenta.';
        alert(mensaje);
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