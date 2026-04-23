import { Component, inject, OnInit } from '@angular/core';
import { Header } from './../../core/components/header/header';
import { Router, RouterLink } from '@angular/router'; // Додаємо Router
import { CommonModule } from '@angular/common';
import {FormsModule} from '@angular/forms';
import { ProjectCard } from '../../shared/components/project-card/project-card';

@Component({
  selector: 'app-account',
  standalone: true,
  imports: [Header, CommonModule, RouterLink, ProjectCard],
  templateUrl: './account.html',
  styleUrl: './account.scss'
})
export class Account {
  private router = inject(Router);
  userData: any = null;

  myProjects: any[] = [];

  ngOnInit() {
    // Підтягуємо дані при ініціалізації компонента
    const savedUser = localStorage.getItem('cinecore_user');
    if (savedUser) {
      this.userData = JSON.parse(savedUser);
    } else {
      // Якщо користувач не залогінений, а намагається зайти на /account — повертаємо на вхід
      this.router.navigate(['/login']);
    }
  }

  get userInitials(): string {
    if (!this.userData) return 'JD';
    return `${this.userData.firstName?.charAt(0) || ''}${this.userData.lastName?.charAt(0) || ''}`.toUpperCase();
  }

  logout() {
    // Очищаємо localStorage і переходимо на сторінку входу
    localStorage.removeItem('cinecore_user');
    this.router.navigate(['/login']);
  }
}
