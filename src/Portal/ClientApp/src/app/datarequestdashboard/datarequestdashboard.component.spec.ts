import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DatarequestdashboardComponent } from './datarequestdashboard.component';

describe('DatarequestdashboardComponent', () => {
  let component: DatarequestdashboardComponent;
  let fixture: ComponentFixture<DatarequestdashboardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ DatarequestdashboardComponent ]
    })
    .compileComponents();
  });

  beforeEach(() => {
    fixture = TestBed.createComponent(DatarequestdashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
