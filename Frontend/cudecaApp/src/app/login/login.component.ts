import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-login',
  imports: [FormsModule, CommonModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
  
  email: string = '';
  password: string = '';
  isLoading: boolean = false;
  showPassword: boolean = false;

  constructor(
    private router: Router,
    private authService: AuthService
  ) {}

  onSubmit() {
    if (this.isLoading) return; // Evita doble clic accidental

    console.log('Intentando iniciar sesión...', this.email);

    this.isLoading = true;

    const credentials = {
      email: this.email,
      password: this.password
    };

    this.authService.login(credentials).subscribe({
      next: (response) => {
        console.log('Login Supabase OK:', response);
        this.isLoading = false;
        this.router.navigate(['/']); 
      },
      error: (err) => {
        this.isLoading = false;
        console.error(err);
        alert(err.message || 'Error al iniciar sesión');
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