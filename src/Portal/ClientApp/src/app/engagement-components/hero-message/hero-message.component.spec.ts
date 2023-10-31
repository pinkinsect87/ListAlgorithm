import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { HeroMessageComponent } from './hero-message.component';

describe('HeroMessageComponent', () => {
  let component: HeroMessageComponent;
  let fixture: ComponentFixture<HeroMessageComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [HeroMessageComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(HeroMessageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
