import { Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-help-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './help-modal.component.html',
  styleUrls: ['./help-modal.component.css']
})
export class HelpModalComponent {
  @Output() close = new EventEmitter<void>();

  closeModal() {
    this.close.emit();
  }
}
