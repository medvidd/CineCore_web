import { Component, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { Header } from '../../core/components/header/header';
import { CommonModule } from '@angular/common';
import { ProjectDetails} from '../../shared/components/project-details/project-details';
import { ProjectListItem } from '../../shared/components/project-list-item/project-list-item';
import { ProjectModal } from '../../shared/components/project-modal/project-modal';
import { Api } from '../../core/services/api';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [Header, CommonModule, ProjectDetails, ProjectListItem, ProjectModal],
  templateUrl: './projects.html',
  styleUrl: './projects.scss'
})
export class Projects implements OnInit {
  private api = inject(Api);
  private cdr = inject(ChangeDetectorRef);
  private route = inject(ActivatedRoute); // Інжектуємо роут

  filters = ['All', 'Project owner', 'Manager', 'Actor'];
  activeFilter = 'All';

  allProjects: any[] = [];
  selectedProject: any = null; // Виправлено тип

  isModalOpen = false;
  isLoading = false;

  openModal() { this.isModalOpen = true; }
  closeModal() { this.isModalOpen = false; }

  ngOnInit() {
    // При старті компонента спочатку завантажуємо дані
    this.loadProjects();
  }

  onProjectCreated(newProject: any) {
    this.loadProjects();
    this.closeModal();
  }

  // loadProjects() {
  //   const user = JSON.parse(localStorage.getItem('cinecore_user') || '{}');
  //   if (user.id) {
  //     this.isLoading = true;
  //     // ПЕРЕДАЄМО activeFilter НА БЕКЕНД
  //     this.api.getUserProjects(user.id, this.activeFilter).subscribe({
  //       next: (res) => {
  //         this.allProjects = res; // Це вже ВІДФІЛЬТРОВАНІ дані з БД
  //
  //         if (this.allProjects.length > 0) {
  //           this.selectProject(this.allProjects[0]);
  //         } else {
  //           this.selectedProject = null;
  //         }
  //
  //         this.isLoading = false;
  //         this.cdr.detectChanges();
  //       },
  //       error: (err) => {
  //         console.error(err);
  //         this.isLoading = false;
  //         this.cdr.detectChanges();
  //       }
  //     });
  //   }
  // }


  applyFilter(filter: string) {
    this.activeFilter = filter;
    this.loadProjects(); // ЗАМІСТЬ локального сортування, робимо запит до БД!
  }

  selectProject(project: any) {
    this.selectedProject = project;
  }

  // В методі ngOnInit або класі Projects додайте:
  handleDeleteProject(id: number) {
    this.api.deleteProject(id).subscribe({
      next: () => {
        // Після видалення просто перезавантажуємо список
        this.loadProjects();
      },
      error: (err) => console.error('Error deleting project:', err)
    });
  }

  loadProjects() {
    const user = JSON.parse(localStorage.getItem('cinecore_user') || '{}');
    if (user.id) {
      this.isLoading = true;
      this.api.getUserProjects(user.id, this.activeFilter).subscribe({
        next: (res) => {
          this.allProjects = res;

          // ПЕРЕВІРКА ПАРАМЕТРА В URL
          const idFromUrl = this.route.snapshot.queryParams['selectedId'];

          if (idFromUrl) {
            // Шукаємо проект з цим ID
            const targetProject = this.allProjects.find(p => p.id == idFromUrl);
            if (targetProject) {
              this.selectProject(targetProject);
            } else {
              this.applyFilter(this.activeFilter);
            }
          } else {
            this.applyFilter(this.activeFilter);
          }

          this.isLoading = false;
          this.cdr.detectChanges();
        }
      });
    }
  }
}
