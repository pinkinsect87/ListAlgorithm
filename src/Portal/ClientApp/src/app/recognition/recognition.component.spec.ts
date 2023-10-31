import { ComponentFixture, TestBed, waitForAsync } from '@angular/core/testing';

import { RecognitionComponent } from './recognition.component';

describe('RecognitionComponent', () => {
  let component: RecognitionComponent;
  let fixture: ComponentFixture<RecognitionComponent>;

  beforeEach(waitForAsync(() => {
    TestBed.configureTestingModule({
      declarations: [ RecognitionComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(RecognitionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
