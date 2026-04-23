import { Component, inject, OnInit } from '@angular/core';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ProjectModal } from '../../../shared/components/project-modal/project-modal';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, CommonModule, ProjectModal], // Обов'язково додаємо це сюди!
  templateUrl: './header.html',
  styleUrl: './header.scss'
})
export class Header {
  private router = inject(Router);

  isLoggedIn = false;
  userData: any = null;
  userInitials = '';
  isModalOpen = false;

  openModal() { this.isModalOpen = true; }
  closeModal() { this.isModalOpen = false; }

  onProjectCreated(projectData: any) {
    console.log('New project:', projectData);
  }

  ngOnInit() {
    this.checkUserStatus();
  }

  checkUserStatus() {
    const savedUser = localStorage.getItem('cinecore_user');
    if (savedUser) {
      this.isLoggedIn = true;
      this.userData = JSON.parse(savedUser);
      // Формуємо ініціали (Тетяна Медвідь -> ТМ)
      this.userInitials = (this.userData.firstName?.[0] || '') + (this.userData.lastName?.[0] || '');
    }
  }

  get isAuthPage(): boolean {
    return this.router.url === '/login' || this.router.url === '/signup';
  }

  get isDashboardPage(): boolean {
    return this.router.url === '/account' || this.router.url === '/projects';
  }

  get idProjectsPage(): boolean {
    return this.router.url === '/projects';
  }
}
