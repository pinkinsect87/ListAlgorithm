import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { SurveyCountryComponent } from './survey-country.component';

describe('SurveyCountryComponent', () => {
  let component: SurveyCountryComponent;
  let fixture: ComponentFixture<SurveyCountryComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [SurveyCountryComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SurveyCountryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
