import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { HelpdeskssoComponent } from './helpdesksso.component';

describe('HelpdeskssoComponent', () => {
  let component: HelpdeskssoComponent;
  let fixture: ComponentFixture<HelpdeskssoComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ HelpdeskssoComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(HelpdeskssoComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
