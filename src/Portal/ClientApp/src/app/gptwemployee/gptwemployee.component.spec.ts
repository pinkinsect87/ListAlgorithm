import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { GptwemployeeComponent } from './gptwemployee.component';

describe('GptwemployeeComponent', () => {
  let component: GptwemployeeComponent;
  let fixture: ComponentFixture<GptwemployeeComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ GptwemployeeComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(GptwemployeeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
