import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
@Component({
  selector: 'app-login',
  imports: [FormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css'
})
export class LoginComponent {
 email: string = '';  // Propiedad para ngModel
 password: string = '';  // Propiedad para ngModel

  onSubmit() {
    console.log('Formulario enviado', this.email, this.password);
    // Lógica para manejar el login
  }
}
