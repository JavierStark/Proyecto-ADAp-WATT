import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

@Component({
  selector: 'app-ya-eres-socio',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './ya-eres-socio-component.component.html'
})
export class YaEresSocioComponent implements OnInit {

  suscripcion: {
    tipo: string;
    vence: string;
    pagoRef?: string;
  } | null = null;

  constructor(private router: Router) {}

  ngOnInit(): void {
    const raw = sessionStorage.getItem('socioResumen');

    if (raw) {
      this.suscripcion = JSON.parse(raw);
    }
  }

  renovar(): void {
    // 🟢 Permitimos entrar a hacerte-socio en modo renovación
    sessionStorage.setItem('renovarSuscripcion', 'true');

    this.router.navigate(['/hazte-socio']);
  }

  volver(): void {
    this.router.navigate(['/']);
  }
}
