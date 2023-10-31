import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { NewecrdoneComponent } from './newecrdone.component';

describe('NewecrdoneComponent', () => {
  let component: NewecrdoneComponent;
  let fixture: ComponentFixture<NewecrdoneComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ NewecrdoneComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NewecrdoneComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
