import { Component, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Header } from '../../core/components/header/header';
import { Api } from '../../core/services/api';

@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [RouterLink, Header],
  templateUrl: './landing.html',
  styleUrl: './landing.scss',
})
export class Landing implements OnInit {
  // Змінна для перевірки стану
  isLoggedIn = false;

  ngOnInit() {
    this.checkUserStatus();
  }

  checkUserStatus() {
    // Якщо в сховищі є дані користувача, значить він залогінений
    const savedUser = localStorage.getItem('cinecore_user');
    this.isLoggedIn = !!savedUser;
  }
}
