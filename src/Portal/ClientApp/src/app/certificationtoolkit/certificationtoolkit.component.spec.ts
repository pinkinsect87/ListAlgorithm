import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { CertificationToolkitComponent } from './certificationtoolkit.component';

describe('CertificationToolkitComponent', () => {
    let component: CertificationToolkitComponent;
    let fixture: ComponentFixture<CertificationToolkitComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
        declarations: [CertificationToolkitComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
      fixture = TestBed.createComponent(CertificationToolkitComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
