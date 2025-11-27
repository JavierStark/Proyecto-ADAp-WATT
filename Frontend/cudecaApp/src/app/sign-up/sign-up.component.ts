import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
@Component({
  selector: 'app-sign-up',
  imports: [FormsModule],
  templateUrl: './sign-up.component.html',
  styleUrl: './sign-up.component.css'
})
export class SignUpComponent {
 username: string = '';  // Propiedad para ngModel
  email: string = '';  // Propiedad para ngModel
  password: string = '';  // Propiedad para ngModel

  onSubmit() {
    console.log('Formulario enviado', this.username, this.email, this.password);
    // Lógica para manejar el registro
  }

}
