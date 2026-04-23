import { Component, inject } from '@angular/core';
import { Header } from '../../core/components/header/header';
import { CommonModule } from '@angular/common';

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
  imports: [Header, CommonModule],
  templateUrl: './projects.html',
  styleUrl: './projects.scss'
})
export class Projects {
  // Фільтрація
  filters = ['All', 'Project owner', 'Manager', 'Actor'];
  activeFilter = 'All';

  // Мокові дані для перевірки (потім заміните на дані з API)
  allProjects: Project[] = [
    {
      id: 1, title: 'Midnight in Paris', genre: 'Drama', role: 'Manager',
      status: 'Pre-production', statusColor: '#E9A60F',
      director: 'Woody Allen', startDate: 'Sep 12, 2026', teamSize: 45, duration: '8 weeks',
      synopsis: 'A screenwriter travels back in time...',
      crew: [{ role: 'Producer', name: 'Letty Aronson' }]
    },
    {
      id: 2, title: 'Neon Dreams', genre: 'Sci-Fi', role: 'Actor',
      status: 'Casting', statusColor: '#51A2FF',
      director: 'Ridley Scott', startDate: 'Nov 1, 2026', teamSize: 120, duration: '16 weeks',
      synopsis: 'In a cyberpunk future, a rogue detective...',
      crew: [{ role: 'Casting Director', name: 'Sarah Finn' }]
    },
    {
      id: 3, title: 'Silent Echo', genre: 'Thriller', role: 'Project owner',
      status: 'Script Review', statusColor: '#C27AFF',
      director: 'David Fincher', startDate: 'Jan 15, 2027', teamSize: 80, duration: '12 weeks',
      synopsis: 'A deaf investigator uncovers a conspiracy...',
      crew: [{ role: 'Writer', name: 'Gillian Flynn' }]
    },
    {
      id: 4, title: 'Desert Winds', genre: 'Western', role: 'Project owner',
      status: 'Location Scouting', statusColor: '#FF8904',
      director: 'John Miller', startDate: 'October 5, 2026', teamSize: 10, duration: '14 weeks',
      synopsis: 'A modern western following a drifter seeking redemption in the harsh landscapes of the American Southwest.',
      crew: [
        { role: 'Stunt Coordinator', name: 'Rick Stone' },
        { role: 'Location Manager', name: 'Paula Martinez' },
        { role: 'Gaffer', name: 'Chris Adams' }
      ]
    },
    {
      id: 5, title: 'Ocean\'s Call', genre: 'Adventure', role: 'Manager',
      status: 'Filming', statusColor: '#3AB9A0',
      director: 'James Cameron', startDate: 'Aug 20, 2026', teamSize: 200, duration: '24 weeks',
      synopsis: 'Deep sea explorers find a lost civilization...',
      crew: [{ role: 'VFX Supervisor', name: 'Joe Letteri' }]
    }
  ];

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
