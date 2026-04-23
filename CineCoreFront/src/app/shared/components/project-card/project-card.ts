import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import {FormsModule} from '@angular/forms';

@Component({
  selector: 'app-project-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './project-card.html',
  styleUrl: './project-card.scss',
})
export class ProjectCard {
  @Input() project: any;

  get roleColor(): string{
    switch (this.project?.role) {
      case 'Project owner': return '#FF8904';
      case 'Manager': return '#3AB9A0';
      case 'Actor': return '#51A2FF';
      default: return '#C27AFF';
    }
  }
}
