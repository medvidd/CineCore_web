import { Component, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { Header } from './../../core/components/header/header';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ProjectCard } from '../../shared/components/project-card/project-card';
import { ProjectModal } from '../../shared/components/project-modal/project-modal';
import { Api } from '../../core/services/api'; // 1. Імпортуємо сервіс

@Component({
  selector: 'app-account',
  standalone: true,
  imports: [Header, CommonModule, RouterLink, ProjectCard, ProjectModal],
  templateUrl: './account.html',
  styleUrl: './account.scss'
})
export class Account implements OnInit {
  private router = inject(Router);
  private api = inject(Api); // 2. Інжектуємо сервіс
  private cdr = inject(ChangeDetectorRef);

  userData: any = null;
  myProjects: any[] = [];
  isModalOpen = false;
  isLoading = false;

  ngOnInit() {
    const savedUser = localStorage.getItem('cinecore_user');
    if (savedUser) {
      this.userData = JSON.parse(savedUser);
      // 3. Завантажуємо проекти відразу після отримання даних користувача
      this.loadMyProjects();
    } else {
      this.router.navigate(['/login']);
    }

    this.loadMyProjects();
  }

  // Метод для завантаження проектів користувача
  loadMyProjects() {
    if (this.userData?.id) {
      this.isLoading = true;
      this.api.getUserProjects(this.userData.id).subscribe({
        next: (res) => {
          this.myProjects = res;
          this.isLoading = false;
          this.cdr.detectChanges(); // Оновлюємо UI
        },
        error: (err) => {
          this.isLoading = false;
          this.cdr.detectChanges();
        }
      });
    }
  }

  openModal() { this.isModalOpen = true; }
  closeModal() { this.isModalOpen = false; }

  // 4. Оновлюємо список, коли модалка повідомляє про успішне створення
  onProjectCreated(projectData: any) {
    console.log('Сигнал про створення проекту отримано:', projectData);
    this.loadMyProjects(); // Перезавантажуємо список з бази
    this.closeModal();
  }

  get userInitials(): string {
    if (!this.userData) return 'JD';
    return `${this.userData.firstName?.charAt(0) || ''}${this.userData.lastName?.charAt(0) || ''}`.toUpperCase();
  }

  logout() {
    localStorage.removeItem('cinecore_user');
    this.router.navigate(['/login']);
  }

  get projectsCount(): number {
    // Рахуємо лише ті, де ми власники або вже приєдналися
    return this.myProjects.filter(p => p.isJoined).length;
  }

  get invitationsCount(): number {
    // Рахуємо лише ті, куди нас запросили, але ми ще не приєдналися
    return this.myProjects.filter(p => !p.isJoined).length;
  }

  navigateToProject(projectId: number) {
    this.router.navigate(['/projects'], { queryParams: { selectedId: projectId } });
  }
}
