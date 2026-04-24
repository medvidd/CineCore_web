import { Component, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { Api } from '../../../core/services/api';

@Component({
  selector: 'app-workspace-layout',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './workspace-layout.html',
  styleUrl: './workspace-layout.scss'
})
export class WorkspaceLayout implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private api = inject(Api);
  private cdr = inject(ChangeDetectorRef); // 1. Інжектуємо ваш звичний ChangeDetectorRef

  projectId: string = '';
  projectName: string = 'Loading...';

  ngOnInit() {
    // Спочатку беремо ID синхронно
    this.projectId = this.route.snapshot.paramMap.get('id') || '';

    // Підписка для оновлення при зміні роута
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.projectId = id;
        this.fetchProjectDetails(Number(id)); // 2. Викликаємо метод завантаження
      }
    });
  }

  fetchProjectDetails(id: number) {
    this.api.getProjectById(id).subscribe({
      next: (res) => {
        this.projectName = res.title;
        this.cdr.detectChanges(); // 3. Оновлюємо UI, так само як у projects.ts!
      },
      error: (err) => {
        console.error('Failed to load project details', err);
        this.projectName = 'Project not found';
        this.cdr.detectChanges(); // Оновлюємо UI навіть при помилці
      }
    });
  }

  get isDashboardPage(): boolean {
    return this.router.url.includes('/dashboard');
  }
}
