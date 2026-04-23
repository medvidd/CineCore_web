import { Component, inject } from '@angular/core';
import { Header } from '../../core/components/header/header';
import { CommonModule } from '@angular/common';
import { ProjectDetails} from '../../shared/components/project-details/project-details';
import { ProjectListItem } from '../../shared/components/project-list-item/project-list-item';

interface Project {
  id: number;
  title: string;
  genre: string;
  role: 'Project owner' | 'Manager' | 'Actor';
  status: string;
  statusColor: string; // hex color для бейджа
  director: string;
  startDate: string;
  teamSize: number;
  duration: string;
  synopsis: string;
  crew: { role: string; name: string }[];
}

@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [Header, CommonModule, ProjectDetails, ProjectListItem],
  templateUrl: './projects.html',
  styleUrl: './projects.scss'
})
export class Projects {
  // Фільтрація
  filters = ['All', 'Project owner', 'Manager', 'Actor'];
  activeFilter = 'All';

  // Мокові дані для перевірки (потім заміните на дані з API)
  allProjects: Project[] = [  ];

  // Вибраний проект (за замовчуванням - перший у відфільтрованому списку)
  selectedProject: Project | null = null;

  ngOnInit() {
    this.applyFilter('All');
  }

  // Гетер для відфільтрованого списку
  get filteredProjects(): Project[] {
    if (this.activeFilter === 'All') return this.allProjects;
    return this.allProjects.filter(p => p.role === this.activeFilter);
  }

  applyFilter(filter: string) {
    this.activeFilter = filter;
    // При зміні фільтра автоматично вибираємо перший проект з нового списку
    if (this.filteredProjects.length > 0) {
      this.selectProject(this.filteredProjects[0]);
    } else {
      this.selectedProject = null;
    }
  }

  selectProject(project: Project) {
    this.selectedProject = project;
  }
}
