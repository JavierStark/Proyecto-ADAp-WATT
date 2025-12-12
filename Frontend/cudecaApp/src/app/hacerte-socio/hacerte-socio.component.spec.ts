import { ComponentFixture, TestBed } from '@angular/core/testing';

import { HacerteSocioComponent } from './hacerte-socio.component';

describe('HacerteSocioComponent', () => {
  let component: HacerteSocioComponent;
  let fixture: ComponentFixture<HacerteSocioComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HacerteSocioComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(HacerteSocioComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
