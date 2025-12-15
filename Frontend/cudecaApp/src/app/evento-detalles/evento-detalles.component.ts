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
  imageUrl?: string;
  ubicacion: string;
  capacidad: number;
  inscritos: number;
  objetivoRecaudacion?: number;
  totalRecaudado?: number;
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
    console.log(`🔍 Cargando detalles de evento con ID: ${id}`);
    
    // 2. Pedimos al Backend: GET /events/{id}
    //
    this.http.get<any>(`${this.apiUrl}/${id}`).pipe(
      map(item => {
        const imagenUrl = item.imageUrl || item.imagenUrl || item.ImagenUrl || item.imagenURL || item.imagen || 'assets/images/fondoCudeca.png';
        console.log(`📸 Evento Detalle: ${item.nombre || 'Sin título'}`);
        console.log(`   - Raw item keys:`, Object.keys(item));
        console.log(`   - item.imageUrl: ${item.imageUrl || 'undefined'}`);
        console.log(`   - item.imagenUrl: ${item.imagenUrl || 'undefined'}`);
        console.log(`   - item.ImagenUrl: ${item.ImagenUrl || 'undefined'}`);
        console.log(`   - item.imagenURL: ${item.imagenURL || 'undefined'}`);
        console.log(`   - item.imagen: ${item.imagen || 'undefined'}`);
        console.log(`   - Imagen final: ${imagenUrl}`);
        
        return {
          id: item.id,
          titulo: item.nombre || item.titulo || item.name || 'Evento sin título',
          descripcion: item.description || item.descripcion || 'Sin descripción detallada.',
          fecha: new Date(item.date || item.fecha || Date.now()),
          imagen: imagenUrl,
          imageUrl: imagenUrl,
          ubicacion: item.location || item.ubicacion || 'Ubicación pendiente',
          capacidad: item.capacity || item.capacidad || item.aforo || 50,
          inscritos: item.enrolled || item.inscritos || item.entradasVendidas || 0,
          objetivoRecaudacion: item.ObjetivoRecaudacion || item.objetivoRecaudacion || 0,
          totalRecaudado: item.TotalRecaudado || item.totalRecaudado || 0
        };
      })
    ).subscribe({
      next: (data) => {
        console.log(`✅ Evento cargado exitosamente: ${data.titulo}`);
        this.evento = data;
        this.isLoading = false;
      },
      error: (err) => {
        console.error('❌ Backend falló, usando mock local...', err);
        // Si falla, buscamos en un mock local para que no se vea vacío
        this.usarMock(id);
        this.isLoading = false;
      }
    });
  }

  usarMock(id: string) {
    // Lista falsa de respaldo
    const mocks: Evento[] = [
      { id: '1', titulo: 'Carrera Solidaria Cudeca', descripcion: 'Descripción larga de la carrera...', fecha: new Date('2025-12-15'), imagen: 'assets/images/fondoCudeca.png', imageUrl: undefined, ubicacion: 'Parque Central', capacidad: 200, inscritos: 145, objetivoRecaudacion: 5000, totalRecaudado: 1250 },
      { id: '2', titulo: 'Concierto Benéfico', descripcion: 'Descripción del concierto...', fecha: new Date('2025-12-20'), imagen: 'assets/images/fondoCudeca.png', imageUrl: undefined, ubicacion: 'Teatro Municipal', capacidad: 150, inscritos: 150 },
    ];
    this.evento = mocks.find(e => e.id === id) || mocks[0]; // Si no encuentra el ID, muestra el primero
  }

  goBack() {
    this.router.navigate(['/eventos']);
  }

  inscribirse() {
    // Navegar a página de compra de entradas
    this.router.navigate(['/compra-entradas', this.evento?.id]);
  }

  esEventoPasado(fecha: Date | string): boolean {
  if (!fecha) return false;
  const fechaEvento = new Date(fecha);
  const ahora = new Date();
  return fechaEvento < ahora;
  }

  goToDonation() {
    this.router.navigate(['/donation']);
  }

}
