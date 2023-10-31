import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { ListcalendarComponent } from './listcalendar.component';

describe('ListcalendarComponent', () => {
  let component: ListcalendarComponent;
  let fixture: ComponentFixture<ListcalendarComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ ListcalendarComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ListcalendarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
