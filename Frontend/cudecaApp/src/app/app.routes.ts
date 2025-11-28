import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './home/home.component'; // Asegúrate de crear el HomeComponent
import { LoginComponent } from './login/login.component';  // Importa tu LoginComponent
import { SignUpComponent } from './sign-up/sign-up.component';  // Importa tu SignUpComponent
import { EmptyLayoutComponent } from './layouts/empty-layout/empty-layout.component';
import { DonationComponent } from './donation/donation.component';  // Importa tu DonationComponent

export const routes: Routes = [
  { path: '', component: HomeComponent }, // Ruta por defecto que carga el HomeComponent
  { path: 'log-in', component: LoginComponent },  // Ruta para login
  { path: 'sign-up', component: SignUpComponent },  // Ruta para registrarse
  { path: 'donation', component: DonationComponent }, // Ruta para donaciones
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
