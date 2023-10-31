import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { NewEcrComponent2 } from './newecr2.component';

describe('NewEcrComponent2', () => {
  let component: NewEcrComponent2;
  let fixture: ComponentFixture<NewEcrComponent2>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [NewEcrComponent2 ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NewEcrComponent2);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
