import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { DataExtractRequestComponent } from './dataextractrequest.component';

describe('DataExtractRequestComponent', () => {
  let component: DataExtractRequestComponent;
  let fixture: ComponentFixture<DataExtractRequestComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [DataExtractRequestComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DataExtractRequestComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
