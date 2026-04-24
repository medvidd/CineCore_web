import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive, RouterOutlet, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-workspace-layout',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive, RouterOutlet],
  templateUrl: './workspace-layout.html',
  styleUrl: './workspace-layout.scss'
})
export class WorkspaceLayout {
  private route = inject(ActivatedRoute);
  private router = inject(Router); // Інжектуємо Router

  projectId: string = '';
  projectName: string = 'The Crimson Letter';

  ngOnInit() {
    // 1. Отримуємо ID миттєво (синхронно), щоб посилання в HTML одразу були правильними!
    this.projectId = this.route.snapshot.paramMap.get('id') || '';

    // 2. Залишаємо підписку на випадок, якщо ми перейдемо з одного проекту в інший
    // без перезавантаження самого компонента Layout
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.projectId = id;
      }
    });
  }

  get isDashboardPage(): boolean {
    return this.router.url.includes('/dashboard');
  }
}
