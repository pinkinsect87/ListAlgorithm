import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { MulticheckFilterComponent } from './multicheck-filter.component';

describe('MulticheckFilterComponent', () => {
  let component: MulticheckFilterComponent;
  let fixture: ComponentFixture<MulticheckFilterComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ MulticheckFilterComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(MulticheckFilterComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
