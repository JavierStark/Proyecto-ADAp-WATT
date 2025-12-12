import { ComponentFixture, TestBed } from '@angular/core/testing';

import { YaEresSocioComponentComponent } from './ya-eres-socio-component.component';

describe('YaEresSocioComponentComponent', () => {
  let component: YaEresSocioComponentComponent;
  let fixture: ComponentFixture<YaEresSocioComponentComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [YaEresSocioComponentComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(YaEresSocioComponentComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
