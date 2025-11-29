import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

// Interfaz para definir la estructura de un Evento
export interface Evento {
  id: number;
  titulo: string;
  descripcion: string;
  fecha: Date;
  imagen: string;
  ubicacion?: string;
  capacidad?: number;
  inscritos?: number;
}

@Component({
  selector: 'app-eventos',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './eventos.component.html',
  styleUrls: ['./eventos.component.css']
})
export class EventosComponent implements OnInit {
  // Array de eventos - Aquí irán los datos del backend
  eventos: Evento[] = [];

  constructor(private router: Router) {}

  ngOnInit(): void {
    // Datos de ejemplo (mock data) - Después se reemplazará con llamada al backend
    this.cargarEventosMock();
  }

  // Método temporal con datos de ejemplo
  cargarEventosMock(): void {
    this.eventos = [
      {
        id: 1,
        titulo: 'Carrera Solidaria Cudeca',
        descripcion: 'Únete a nuestra carrera anual para recaudar fondos. Disfruta de un día de deporte y solidaridad.',
        fecha: new Date('2025-12-15'),
        imagen: 'assets/images/evento1.jpg',
        ubicacion: 'Parque Central',
        capacidad: 200,
        inscritos: 145
      },
      {
        id: 2,
        titulo: 'Concierto Benéfico',
        descripcion: 'Una noche de música en vivo con artistas locales. Todas las ganancias van a Cudeca.',
        fecha: new Date('2025-12-20'),
        imagen: 'assets/images/evento2.jpg',
        ubicacion: 'Teatro Municipal',
        capacidad: 150,
        inscritos: 98
      },
      {
        id: 3,
        titulo: 'Taller de Artesanía',
        descripcion: 'Aprende técnicas de artesanía mientras apoyas una gran causa. Incluye materiales.',
        fecha: new Date('2026-01-10'),
        imagen: 'assets/images/evento3.jpg',
        ubicacion: 'Centro Cudeca',
        capacidad: 30,
        inscritos: 22
      },
      {
        id: 4,
        titulo: 'Mercadillo Solidario',
        descripcion: 'Venta de productos artesanales y segunda mano. Todos los beneficios para Cudeca.',
        fecha: new Date('2026-01-25'),
        imagen: 'assets/images/evento4.jpg',
        ubicacion: 'Plaza Mayor',
        capacidad: 500,
        inscritos: 0
      }
    ];
  }

  // Método para cuando se conecte al backend
  // cargarEventosDesdeBackend(): void {
  //   this.http.get<Evento[]>('http://localhost:5000/api/eventos')
  //     .subscribe({
  //       next: (data) => this.eventos = data,
  //       error: (error) => console.error('Error al cargar eventos:', error)
  //     });
  // }

  inscribirseEvento(eventoId: number): void {
    console.log('Inscribiéndose al evento:', eventoId);
    // Aquí irá la lógica para inscribirse (llamada al backend)
  }

  verDetalles(eventoId: number): void {
    console.log('Ver detalles del evento:', eventoId);
    // Navegar a página de detalles del evento
    // this.router.navigate(['/eventos', eventoId]);
  }

  goToHome(): void {
    this.router.navigate(['/']);
  }
}
