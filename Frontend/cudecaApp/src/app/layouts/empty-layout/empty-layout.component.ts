import { Component } from '@angular/core';
import { RouterOutlet} from '@angular/router';
@Component({
  selector: 'app-empty-layout',
  imports: [RouterOutlet],
  template: '<router-outlet></router-outlet>', // Solo contiene el router-outlet
  styleUrls: ['./empty-layout.component.css']
})
export class EmptyLayoutComponent {

}


