import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-project-list-item',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './project-list-item.html',
  styleUrl: './project-list-item.scss'
})
export class ProjectListItem {
  @Input() project: any;
  @Input() isSelected = false;

  // Динамічний колір для ролі
  get roleColor(): string {
    switch (this.project?.role) {
      case 'Project owner': return '#FF8904'; // Помаранчевий
      case 'Manager': return '#3AB9A0';       // Teal
      case 'Actor': return '#51A2FF';         // Синій
      default: return '#C27AFF';              // Фіолетовий (на всяк випадок)
    }
  }
}
