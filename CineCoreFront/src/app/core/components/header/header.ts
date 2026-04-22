import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [RouterLink, RouterLinkActive], // Обов'язково додаємо це сюди!
  templateUrl: './header.html',
  styleUrl: './header.scss'
})
export class Header {
  private router = inject(Router);

  get isAuthPage(): boolean {
    return this.router.url === '/login' || this.router.url === '/signup';
  }

  get isDashboardPage(): boolean {
    return this.router.url === '/account';
  }
}
