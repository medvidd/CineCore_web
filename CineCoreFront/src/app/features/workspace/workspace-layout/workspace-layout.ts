import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet, ActivatedRoute } from '@angular/router';
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

  projectId: string = '';
  projectName: string = 'The Crimson Letter';

  ngOnInit() {
    this.route.params.subscribe(params => {
      this.projectId = params['id'];
    });
  }
}
