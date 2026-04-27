import { Component, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Api } from '../../../core/services/api';
import { ActivatedRoute, RouterModule } from '@angular/router';

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
  imports: [CommonModule, RouterModule],
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

  ngOnInit() {
    // 1. Отримуємо роль
    this.api.currentRole$.subscribe(role => {
      this.currentUserRole = role;
      this.canEdit = (role === 'owner' || role === 'manager');
      this.cdr.detectChanges();
    });

    // 2. Завантажуємо дані дашборду
    // Отримуємо ID проекту з URL (наприклад, з /project/5/dashboard)
    const projectId = this.route.parent?.snapshot.params['id'];
    if (projectId) {
      this.loadDashboardData(Number(projectId));
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
        console.error('Error loading dashboard stats', err);
        this.isLoading = false;
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
}
