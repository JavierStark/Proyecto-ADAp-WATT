import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-login',
  imports: [FormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  
  email: string = '';
  password: string = '';

  constructor(
    private router: Router,
    private authService: AuthService
  ) {}

  onSubmit() {
    console.log('Intentando iniciar sesión...', this.email);

    const credentials = {
      email: this.email,
      password: this.password
    };

    this.authService.login(credentials).subscribe({
      next: (response) => {
        console.log('Login correcto:', response);
        this.router.navigate(['/']); 
      },
      error: (err) => {
        console.error('Error login:', err);

        const backendMessage = err.error?.error;

        if (backendMessage) {
          alert(backendMessage);
        } else {
          alert('Usuario o contraseña incorrectos.');
        }
      }
    });
  }

  goToSignUp() {
    this.router.navigate(['/sign-up']);
  }

  goToHome() {
    this.router.navigate(['/']);
  }
}