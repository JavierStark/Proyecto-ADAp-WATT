import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs/operators';
import { registerLocaleData } from '@angular/common';
import localeEs from '@angular/common/locales/es';

registerLocaleData(localeEs);

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
  eventos: Evento[] = [];
  // 1. VARIABLE DE ESTADO DE CARGA
  isLoading: boolean = true; 

  constructor(
    private router: Router,
    private http: HttpClient 
  ) {}

  ngOnInit(): void {
    this.cargarEventosDesdeBackend();
  }

  cargarEventosDesdeBackend(): void {
    this.isLoading = true; // Iniciamos carga
    const url = 'https://cudecabackend-c7hhc5ejeygfb4ah.spaincentral-01.azurewebsites.net/eventos'; 

    console.log('🔄 Intentando conectar con:', url);

    this.http.get(url, { responseType: 'text' })
      .pipe(
        map(respuestaTexto => {
          try {
            const datosJson = JSON.parse(respuestaTexto);
            console.log('✅ Datos crudos recibidos:', datosJson);
            
            return datosJson.map((item: any) => ({
              id: item.id || Math.floor(Math.random() * 10000),
              titulo: item.title || item.titulo || item.name || 'Evento sin título',
              descripcion: item.description || item.descripcion || 'Sin descripción',
              fecha: new Date(item.date || item.fecha || Date.now()),
              imagen: item.imageUrl || item.imagen || 'assets/images/fondoCudeca.png',
              ubicacion: item.location || item.ubicacion || 'Ubicación pendiente',
              capacidad: item.capacity || item.capacidad || 50,
              inscritos: item.enrolled || item.inscritos || 0
            }));

          } catch (e) {
            console.warn('⚠️ Error al leer los datos:', e);
            return [];
          }
        })
      )
      .subscribe({
        next: (eventosTraducidos) => {
          this.isLoading = false; // 2. TERMINA LA CARGA
          if (eventosTraducidos.length > 0) {
            this.eventos = eventosTraducidos;
            console.log('🎉 Eventos cargados exitosamente');
          } else {
            console.log('⚠️ Lista vacía, cargando mocks...');
            this.cargarEventosMock();
          }
        },
        error: (error) => {
          this.isLoading = false; // 2. TERMINA LA CARGA (incluso con error)
          console.error('❌ Error Backend:', error);
          this.cargarEventosMock();
        }
      });
  }

  cargarEventosMock(): void {
    this.eventos = [
      {
        id: 1,
        titulo: 'Carrera Solidaria Cudeca',
        descripcion: 'Únete a nuestra carrera anual para recaudar fondos. Disfruta de un día de deporte y solidaridad.',
        fecha: new Date('2025-12-15'),
        imagen: 'assets/images/fondoCudeca.png',
        ubicacion: 'Parque Central',
        capacidad: 200,
        inscritos: 145
      },
      {
        id: 2,
        titulo: 'Concierto Benéfico',
        descripcion: 'Una noche de música en vivo con artistas locales. Todas las ganancias van a Cudeca.',
        fecha: new Date('2025-12-20'),
        imagen: 'assets/images/fondoCudeca.png',
        ubicacion: 'Teatro Municipal',
        capacidad: 150,
        inscritos: 98
      },
      {
        id: 3,
        titulo: 'Taller de Artesanía',
        descripcion: 'Aprende técnicas de artesanía mientras apoyas una gran causa. Incluye materiales.',
        fecha: new Date('2026-01-10'),
        imagen: 'assets/images/fondoCudeca.png',
        ubicacion: 'Centro Cudeca',
        capacidad: 30,
        inscritos: 22
      },
      {
        id: 4,
        titulo: 'Mercadillo Solidario',
        descripcion: 'Venta de productos artesanales y segunda mano. Todos los beneficios para Cudeca.',
        fecha: new Date('2026-01-25'),
        imagen: 'assets/images/fondoCudeca.png',
        ubicacion: 'Plaza Mayor',
        capacidad: 500,
        inscritos: 0
      }
    ];
    // Aseguramos que isLoading se apague si entramos directo al mock
    this.isLoading = false; 
  }

  inscribirseEvento(eventoId: number): void {
    console.log('Inscribiéndose al evento:', eventoId);
  }

  verDetalles(eventoId: number): void {
    console.log('Ver detalles del evento:', eventoId);
  }

  goToHome(): void {
    this.router.navigate(['/']);
  }
}