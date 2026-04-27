import { Component, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Api } from '../../../core/services/api';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { ProjectModal } from '../../../shared/components/project-modal/project-modal';

// Інтерфейси згідно з DashboardDto на бекенді
export interface DashboardData {
  projectSummary: {
    title: string;
    synopsis: string;
    teamMembersCount: number;
  };
  scriptProgress: {
    completedScenes: number;
    draftScenes: number;
    totalScenes: number;
    progressPercentage: number;
  };
  castingProgress: {
    castRoles: number;
    pendingRoles: number;
    totalRoles: number;
    progressPercentage: number;
  };
  upcomingShoot: {
    hasUpcoming: boolean;
    date: string | null;
    callTime: string | null;
    locationName: string | null;
    scenesCount: number;
  };
  quickStats: {
    totalScenes: number;
    totalRoles: number;
    totalLocations: number;
    totalProps: number;
    unscheduledScenes: number;
    pendingInvites: number;
  };
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, ProjectModal],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard implements OnInit {
  private api = inject(Api);
  private cdr = inject(ChangeDetectorRef);
  private route = inject(ActivatedRoute);

  currentUserRole: string = 'none';
  canEdit: boolean = false;

  data?: DashboardData;
  isLoading: boolean = true;

  isProjectModalOpen = false;
  editProjectData: any = null;
  projectId!: number;

  ngOnInit() {
    // 1. Отримуємо роль
    this.api.currentRole$.subscribe(role => {
      this.currentUserRole = role;
      this.canEdit = (role === 'owner' || role === 'manager');
      this.cdr.detectChanges();
    });

    // 2. Отримуємо ID проекту з URL
    // Читаємо параметр 'id'. Залежно від налаштувань роутингу, він може бути
    // на поточному рівні або на рівні батька (parent).
    const idParam = this.route.snapshot.params['id'] || this.route.parent?.snapshot.params['id'];

    if (idParam) {
      this.projectId = Number(idParam);
      this.loadDashboardData(this.projectId);
    } else {
      this.isLoading = false; // Вимикаємо загрузку, щоб не висіла вічно
      this.cdr.detectChanges();
    }
  }

  loadDashboardData(projectId: number) {
    this.isLoading = true;

    this.api.getDashboardStats(projectId).subscribe({
      next: (res: DashboardData) => {
        this.data = res;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('❌ Помилка при завантаженні статистики дашборду:', err);
        this.isLoading = false;
        this.cdr.detectChanges(); // ВАЖЛИВО: Оновлюємо UI навіть при помилці, щоб прибрати лоадер
      }
    });
  }

  // Допоміжний метод для форматування дати
  formatDate(dateStr: string | null): { day: string, year: string } {
    if (!dateStr) return { day: 'TBD', year: '' };
    const date = new Date(dateStr);
    return {
      day: date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', weekday: 'long' }),
      year: date.getFullYear().toString()
    };
  }

  openEditModal() {
    // Робимо свіжий запит, щоб отримати повні дані проекту (з жанрами, StartDate тощо)
    this.api.getProjectById(this.projectId).subscribe({
      next: (data) => {
        this.editProjectData = data;
        this.isProjectModalOpen = true;
        this.cdr.detectChanges();
      },
      error: (err) => console.error("Failed to fetch project details", err)
    });
  }

  onProjectUpdated() {
    // Після успішного збереження просто перезавантажуємо дашборд
    this.loadDashboardData(this.projectId);
  }
}
