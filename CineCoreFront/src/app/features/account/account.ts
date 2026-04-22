import { Component, inject, OnInit } from '@angular/core';
import { Header } from './../../core/components/header/header';
import { Router } from '@angular/router'; // Додаємо Router
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-account',
  standalone: true,
  imports: [Header, CommonModule],
  templateUrl: './account.html',
  styleUrl: './account.scss'
})
export class Account {
  private router = inject(Router);
  userData: any = null;

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

  logout() {
    // Очищаємо localStorage і переходимо на сторінку входу
    localStorage.removeItem('cinecore_user');
    this.router.navigate(['/login']);
  }
}
