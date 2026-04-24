import { Component, inject, OnInit } from '@angular/core';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ProjectModal } from '../../../shared/components/project-modal/project-modal';
import {Api} from '../../services/api';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, CommonModule, ProjectModal], // Обов'язково додаємо це сюди!
  templateUrl: './header.html',
  styleUrl: './header.scss'
})
export class Header implements OnInit {
  private router = inject(Router);
  private api = inject(Api); // Інжектуємо сервіс

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
    // Підписуємося на оновлення користувача
    this.api.user$.subscribe(user => {
      this.userData = user;
      this.isLoggedIn = !!user;
      if (user) {
        this.userInitials = (user.firstName?.[0] || '') + (user.lastName?.[0] || '');
      }
    });
    this.checkUserStatus()
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
    const path = this.router.url.split('?')[0]; // Беремо лише шлях без параметрів
    return path === '/login' || path === '/signup';
  }

  get isDashboardPage(): boolean {
    const path = this.router.url.split('?')[0];
    return path === '/account' || path === '/projects';
  }

  get idProjectsPage(): boolean {
    const path = this.router.url.split('?')[0];
    return path === '/projects';
  }

  get avatarStyle(): string {
    const theme = this.userData?.avatarTheme;
    switch (theme) {
      case 'theme-cyber': return 'linear-gradient(135deg, #51A2FF 0%, #C27AFF 100%)';
      case 'theme-sunset': return 'linear-gradient(135deg, #FF8904 0%, #FF3B30 100%)';
      case 'theme-mono': return 'linear-gradient(135deg, #8E8E93 0%, #3A3A3C 100%)';
      case 'theme-ocean': return 'linear-gradient(135deg, #0A84FF 0%, #30D158 100%)';
      case 'theme-lavender': return 'linear-gradient(135deg, #FF9A9E 0%, #C27AFF 100%)';
      case 'theme-ruby': return 'linear-gradient(135deg, #D9138A 0%, #E2D111 100%)';
      case 'theme-midnight': return 'linear-gradient(135deg, #1A2980 0%, #26D0CE 100%)';
      default: return 'linear-gradient(135deg, #3AB9A0 0%, #E9A60F 100%)';
    }
  }
}
