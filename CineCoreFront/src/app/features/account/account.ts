import {Component, inject, OnInit, ChangeDetectorRef, OnDestroy} from '@angular/core';
import { Header } from './../../core/components/header/header';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ProjectCard } from '../../shared/components/project-card/project-card';
import { ProjectModal } from '../../shared/components/project-modal/project-modal';
import { Api } from '../../core/services/api'; // 1. Імпортуємо сервіс
import { SettingsModal } from '../../shared/components/settings-modal/settings-modal';
import { Subscription, interval } from 'rxjs'; // ДОДАНО

@Component({
  selector: 'app-account',
  standalone: true,
  imports: [Header, CommonModule, RouterLink, ProjectCard, ProjectModal, SettingsModal],
  templateUrl: './account.html',
  styleUrl: './account.scss'
})
export class Account implements OnInit, OnDestroy {
  private router = inject(Router);
  private api = inject(Api); // 2. Інжектуємо сервіс
  private cdr = inject(ChangeDetectorRef);
  private pollSubscription?: Subscription; // ДОДАНО

  userData: any = null;
  myProjects: any[] = [];
  invitations: any[] = []; // ДОДАНО: масив для вхідних запрошень

  isModalOpen = false;
  isLoading = false;

  isInviteModalOpen = false;
  selectedInvite: any = null;

  isSettingsOpen = false; // Змінна для відкриття вікна

  openSettings() { this.isSettingsOpen = true; }
  closeSettings() { this.isSettingsOpen = false; }

  ngOnInit() {
    const savedUser = localStorage.getItem('cinecore_user');
    if (savedUser) {
      this.userData = JSON.parse(savedUser);
      this.loadMyProjects();
      this.loadInvitations();
      this.pollSubscription = interval(5000).subscribe(() => {
        this.loadInvitations();
        this.loadMyProjects();
      });
    } else {
      this.router.navigate(['/login']);
    }
  }

  ngOnDestroy() {
    if (this.pollSubscription) {
      this.pollSubscription.unsubscribe();
    }
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

  loadInvitations() {
    if (this.userData?.id) {
      this.api.getUserInvitations(this.userData.id).subscribe({
        next: (res) => {
          this.invitations = res;
          this.cdr.detectChanges();
        }
      });
    }
  }

  openInviteDetails(invite: any) {
    this.selectedInvite = invite;
    this.isInviteModalOpen = true;
  }

  closeInviteModal() {
    this.isInviteModalOpen = false;
    this.selectedInvite = null;
  }

  respondToInvite(accept: boolean) {
    if (!this.userData?.id || !this.selectedInvite) return;

    const request = accept
      ? this.api.acceptProjectInvite(this.selectedInvite.projectId, this.userData.id)
      : this.api.rejectProjectInvite(this.selectedInvite.projectId, this.userData.id);

    request.subscribe({
      next: () => {
        this.closeInviteModal();
        this.loadInvitations(); // Оновлюємо бейдж із кількістю запрошень
        if (accept) this.loadMyProjects(); // Якщо прийняли - оновлюємо проекти
      },
      error: (err) => console.error('Error responding to invite', err)
    });
  }

  openModal() { this.isModalOpen = true; }
  closeModal() { this.isModalOpen = false; }

  // 4. Оновлюємо список, коли модалка повідомляє про успішне створення
  onProjectCreated(projectData: any) {
    this.loadMyProjects(); // Перезавантажуємо список з бази
    this.closeModal();
  }

  get userInitials(): string {
    if (!this.userData) return 'JD';
    return `${this.userData.firstName?.charAt(0) || ''}${this.userData.lastName?.charAt(0) || ''}`.toUpperCase();
  }

  logout() {
    localStorage.removeItem('cinecore_user');
    this.api.updateUserStream(null); // Очищаємо потік при виході
    this.router.navigate(['/login']);
  }

  get recentProjects(): any[] {
    return this.myProjects.slice(0, 3);
  }

  get projectsCount(): number {
    return this.myProjects.filter(p => p.isJoined).length;
  }

  get invitationsCount(): number {
    // ОНОВЛЕНО: беремо реальну кількість запрошень з нового запиту
    return this.invitations.length;
  }

  navigateToProject(projectId: number) {
    this.router.navigate(['/projects'], { queryParams: { selectedId: projectId } });
  }

  onProfileUpdated(updatedUser: any) {
    this.userData = updatedUser;
    this.api.updateUserStream(updatedUser);
  }

  // Динамічний градієнт для аватара
  // Динамічний градієнт для аватара
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

      default: return 'linear-gradient(135deg, #3AB9A0 0%, #E9A60F 100%)'; // theme-teal
    }
  }
}
