import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs/operators';

// Definimos la interfaz aquí también para que no de error
interface Evento {
  id: string;
  titulo: string;
  descripcion: string;
  fecha: Date;
  imagen: string;
  ubicacion: string;
  capacidad: number;
  inscritos: number;
  objetoRecaudacion?: string;
  precioGeneral?: number | null;
  precioVip?: number | null;
}

@Component({
  selector: 'app-evento-detalles',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './evento-detalles.component.html',
})
export class EventoDetalleComponent implements OnInit {
  
  evento: Evento | null = null;
  isLoading: boolean = true;
  private apiUrl = 'https://cudecabackend-c7hhc5ejeygfb4ah.spaincentral-01.azurewebsites.net/events';

  constructor(
    private route: ActivatedRoute, // Para leer la URL
    private router: Router,        // Para navegar
    private http: HttpClient       // Para pedir datos
  ) {}

  ngOnInit(): void {
    // 1. Obtenemos el ID de la URL (ej: /eventos/5 -> id = 5)
    const id = this.route.snapshot.paramMap.get('id');
    
    if (id) {
      this.cargarDetalle(id);
    }
  }

  cargarDetalle(id: string): void {
    this.isLoading = true;
    
    // 2. Pedimos al Backend: GET /events/{id}
    //
    this.http.get<any>(`${this.apiUrl}/${id}`).pipe(
      map(item => ({
        id: item.id,
        titulo: item.nombre || item.titulo || item.name || 'Evento sin título',
        descripcion: item.description || item.descripcion || 'Sin descripción detallada.',
        fecha: new Date(item.date || item.fecha || Date.now()),
        imagen: item.imageUrl || item.imagen || 'assets/images/fondoCudeca.png',
        ubicacion: item.location || item.ubicacion || 'Ubicación pendiente',
        capacidad: item.capacity || item.capacidad || item.aforo || 50,
        inscritos: item.enrolled || item.inscritos || item.entradasVendidas || 0,
        objetoRecaudacion: item.goalDescription || item.objetoRecaudacion,
        precioGeneral: item.precioGeneral ?? item.priceGeneral ?? item.precio ?? null,
        precioVip: item.precioVip ?? item.priceVip ?? null
      }))
    ).subscribe({
      next: (data) => {
        this.evento = data;
        this.isLoading = false;
      },
      error: (err) => {
        console.warn('Backend falló, usando mock local...', err);
        // Si falla, buscamos en un mock local para que no se vea vacío
        this.usarMock(id);
        this.isLoading = false;
      }
    });
  }

  usarMock(id: string) {
    // Lista falsa de respaldo
    const mocks: Evento[] = [
      { id: '1', titulo: 'Carrera Solidaria Cudeca', descripcion: 'Descripción larga de la carrera...', fecha: new Date('2025-12-15'), imagen: 'assets/images/fondoCudeca.png', ubicacion: 'Parque Central', capacidad: 200, inscritos: 145, objetoRecaudacion: 'Equipamiento médico' },
      { id: '2', titulo: 'Concierto Benéfico', descripcion: 'Descripción del concierto...', fecha: new Date('2025-12-20'), imagen: 'assets/images/fondoCudeca.png', ubicacion: 'Teatro Municipal', capacidad: 150, inscritos: 150 },
    ];
    this.evento = mocks.find(e => e.id === id) || mocks[0]; // Si no encuentra el ID, muestra el primero
  }

  goBack() {
    this.router.navigate(['/eventos']);
  }

  inscribirse() {
    console.log('Inscribirse logic here');
    alert('¡Gracias por tu interés! Próximamente podrás inscribirte.');
  }
 goToDonation() {
    this.router.navigate(['/donation']);
  }

}
