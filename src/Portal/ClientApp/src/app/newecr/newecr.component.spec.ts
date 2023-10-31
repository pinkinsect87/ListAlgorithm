import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { NewecrComponent } from './newecr.component';

describe('NewecrComponent', () => {
  let component: NewecrComponent;
  let fixture: ComponentFixture<NewecrComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ NewecrComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NewecrComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
