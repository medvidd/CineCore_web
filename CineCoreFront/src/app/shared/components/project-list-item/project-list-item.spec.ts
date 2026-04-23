import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProjectListItem } from './project-list-item';

describe('ProjectListItem', () => {
  let component: ProjectListItem;
  let fixture: ComponentFixture<ProjectListItem>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProjectListItem],
    }).compileComponents();

    fixture = TestBed.createComponent(ProjectListItem);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
