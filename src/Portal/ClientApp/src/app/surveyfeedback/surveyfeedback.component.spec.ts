import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { SurveyfeedbackComponent } from './surveyfeedback.component';

describe('SurveyfeedbackComponent', () => {
  let component: SurveyfeedbackComponent;
  let fixture: ComponentFixture<SurveyfeedbackComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ SurveyfeedbackComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SurveyfeedbackComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
