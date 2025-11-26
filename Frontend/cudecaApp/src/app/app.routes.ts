import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { HomeComponent } from './home/home.component'; // Asegúrate de crear el HomeComponent

export const routes: Routes = [
  { path: '', component: HomeComponent }, // Ruta por defecto que carga el HomeComponent
  // Puedes agregar más rutas aquí, como:
  // { path: 'about', component: AboutComponent }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
