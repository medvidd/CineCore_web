import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-project-details',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './project-details.html',
  styleUrl: './project-details.scss'
})
export class ProjectDetails {
  // Приймаємо об'єкт проекту з батьківського компонента
  @Input() project: any;

  // Логіка для кольору ролі (така ж, як у списку)
  get roleColor(): string {
    switch (this.project?.role) {
      case 'Project owner': return '#FF8904'; // Помаранчевий
      case 'Manager': return '#3AB9A0';       // Teal
      case 'Actor': return '#51A2FF';         // Синій
      default: return '#C27AFF';              // Фіолетовий
    }
  }
}
