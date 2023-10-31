import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { DataExtractRequestCompanyComponent } from './dataextractrequest-company.component';

describe('DataExtractRequestCompanyComponent', () => {
  let component: DataExtractRequestCompanyComponent;
  let fixture: ComponentFixture<DataExtractRequestCompanyComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [DataExtractRequestCompanyComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DataExtractRequestCompanyComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
