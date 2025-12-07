import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { AuthService } from '../services/auth.service';

type EstadoQr = 'loading' | 'success' | 'error' | 'missing';

@Component({
  selector: 'app-qr-validate',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './qr-validate.component.html',
})
export class QrValidateComponent implements OnInit {
  estado: EstadoQr = 'loading';
  mensaje = 'Validando código QR...';
  ticket: {
    id?: string;
    evento?: string;
    precio?: number;
    fechaCompra?: string;
    estado?: string;
  } | null = null;

  constructor(
    private route: ActivatedRoute,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    const code = this.route.snapshot.queryParamMap.get('qr')
      ?? this.route.snapshot.queryParamMap.get('code')
      ?? this.route.snapshot.queryParamMap.get('qrCode');

    if (!code) {
      this.estado = 'missing';
      this.mensaje = 'No se encontró el código QR en la URL. Añade ?qr=...';
      return;
    }

    this.authService.validateTicketQr(code).subscribe({
      next: (resp) => {
        const raw = resp?.ticket ?? {};
        this.ticket = {
          id: raw.Id ?? raw.id ?? raw.ticketId ?? '',
          evento: raw.EventoNombre ?? raw.eventoNombre ?? raw.evento ?? '',
          precio: raw.Precio ?? raw.precio ?? null,
          fechaCompra: raw.FechaCompra ?? raw.fechaCompra ?? null,
          estado: raw.Estado ?? raw.estado ?? '',
        };
        this.mensaje = resp?.message ?? 'Código QR válido.';
        this.estado = 'success';
      },
      error: (err) => {
        if (err.status === 404) {
          this.mensaje = 'Este código QR no es válido.';
        } else if (err.status === 401 || err.status === 403) {
          this.mensaje = 'Necesitas iniciar sesión para validar tu entrada.';
        } else {
          this.mensaje = 'Error al validar el código QR. Intenta de nuevo.';
        }
        this.estado = 'error';
      }
    });
  }
}
