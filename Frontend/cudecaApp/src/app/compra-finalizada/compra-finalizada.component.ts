import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpHeaders, HttpClientModule } from '@angular/common/http';

@Component({
  selector: 'app-compra-finalizada',
  standalone: true,
  imports: [CommonModule, HttpClientModule],
  templateUrl: './compra-finalizada.component.html',
  styleUrls: ['./compra-finalizada.component.css']
})
export class CompraFinalizadaComponent {
  // Debe coincidir con `AuthService.apiUrl`
  private apiUrl = 'https://cudecabackend-c7hhc5ejeygfb4ah.spaincentral-01.azurewebsites.net';

  constructor(private http: HttpClient) {}

  /**
   * Llama al endpoint backend que genera el certificado y descarga el PDF.
   * Envía un body vacío (el backend usará el año por defecto si no se especifica).
   */
  downloadCertificate() {
    const token = localStorage.getItem('token');
    const headers = new HttpHeaders({
      'Authorization': `Bearer ${token}`,
      'Content-Type': 'application/json'
    });

    const url = `${this.apiUrl}/donations/certificate/annual`;

    // Pedimos la respuesta completa para leer headers y el blob
    this.http.post(url, {}, { headers, responseType: 'blob', observe: 'response' as const })
      .subscribe({
        next: resp => {
          const blob = resp.body as Blob;
          // Intentar extraer filename de Content-Disposition
          let filename = 'certificado.pdf';
          const contentDisposition = resp.headers.get('content-disposition') || resp.headers.get('Content-Disposition');
          if (contentDisposition) {
            const match = /filename\*=UTF-8''([^;]+)|filename="?([^";]+)"?/.exec(contentDisposition);
            if (match) filename = decodeURIComponent(match[1] || match[2]);
          }

          const link = document.createElement('a');
          const urlBlob = window.URL.createObjectURL(blob);
          link.href = urlBlob;
          link.download = filename;
          document.body.appendChild(link);
          link.click();
          link.remove();
          window.URL.revokeObjectURL(urlBlob);
        },
        error: err => {
          console.error('Error descargando certificado:', err);
          if (err?.error instanceof Blob) {
            // El backend puede devolver JSON de error aunque responseType sea blob
            const reader = new FileReader();
            reader.onload = () => {
              try {
                const text = reader.result as string;
                const json = JSON.parse(text);
                alert(json?.message || json?.error || 'Error generando certificado');
              } catch {
                alert('Error generando certificado');
              }
            };
            reader.readAsText(err.error);
          } else {
            alert('Error generando certificado');
          }
        }
      });
  }
}
