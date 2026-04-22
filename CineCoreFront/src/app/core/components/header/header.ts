import { Component, inject, OnInit } from '@angular/core';
import { RouterLink, RouterLinkActive, Router } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-header',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, CommonModule], // Обов'язково додаємо це сюди!
  templateUrl: './header.html',
  styleUrl: './header.scss'
})
export class Header {
  private router = inject(Router);

  isLoggedIn = false;
  userData: any = null;
  userInitials = '';

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
    return this.router.url === '/account';
  }
}
