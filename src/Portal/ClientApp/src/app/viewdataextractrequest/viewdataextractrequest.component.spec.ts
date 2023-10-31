import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { ViewDataExtractRequestComponent } from './viewdataextractrequest.component';

describe('DataExtractRequestComponent', () => {
  let component: ViewDataExtractRequestComponent;
  let fixture: ComponentFixture<ViewDataExtractRequestComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ViewDataExtractRequestComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ViewDataExtractRequestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
