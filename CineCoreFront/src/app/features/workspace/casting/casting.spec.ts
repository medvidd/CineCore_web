import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Casting } from './casting';

describe('Casting', () => {
  let component: Casting;
  let fixture: ComponentFixture<Casting>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Casting],
    }).compileComponents();

    fixture = TestBed.createComponent(Casting);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
