import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './home/home.component'; // Asegúrate de crear el HomeComponent
import { LoginComponent } from './login/login.component';  // Importa tu LoginComponent
import { SignUpComponent } from './sign-up/sign-up.component';  // Importa tu SignUpComponent
import { EmptyLayoutComponent } from './layouts/empty-layout/empty-layout.component';
import { CuentaComponent } from './cuenta/cuenta.component';
import { DonationComponent } from './donation/donation.component';  // Importa tu DonationComponent
import { EventosComponent } from './eventos/eventos.component';  // Importa el EventosComponent
import { EventoDetalleComponent } from './evento-detalles/evento-detalles.component';
import { PagosComponent } from './pagos/pagos.component';  // Importa el PagosComponent
import { publicGuard } from './guards/public.guard';
import { authGuard } from './guards/auth.guard';
import { QrValidateComponent } from './qr-validate/qr-validate.component';

export const routes: Routes = [
  { path: '', component: HomeComponent }, // Ruta por defecto que carga el HomeComponent
  { path: 'log-in', component: LoginComponent, canActivate: [publicGuard]}, // Ruta para login
  { path: 'sign-up', component: SignUpComponent, canActivate: [publicGuard]},  // Ruta para registrarse
  { path: 'cuenta', component: CuentaComponent,canActivate: [authGuard] },
  { path: 'donation', component: DonationComponent }, // Ruta para donaciones
  { path: 'eventos', component: EventosComponent }, // Ruta para eventos
  { path: 'eventos/:id', component: EventoDetalleComponent },
  { path: 'pagos', component: PagosComponent }, // Ruta para pagos
  { path: 'validar-qr', component: QrValidateComponent },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
