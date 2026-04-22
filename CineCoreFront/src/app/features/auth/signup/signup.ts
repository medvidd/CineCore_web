import { Component } from '@angular/core';
import { Header } from '../../../core/components/header/header';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [Header, RouterLink],
  templateUrl: './signup.html',
  styleUrl: './signup.scss',
})
export class Signup {
  days: number[] = Array.from({ length: 31 }, (_, i) => i + 1);

  // Масив місяців
  months = [
    { value: '1', label: 'January' }, { value: '2', label: 'February' },
    { value: '3', label: 'March' }, { value: '4', label: 'April' },
    { value: '5', label: 'May' }, { value: '6', label: 'June' },
    { value: '7', label: 'July' }, { value: '8', label: 'August' },
    { value: '9', label: 'September' }, { value: '10', label: 'October' },
    { value: '11', label: 'November' }, { value: '12', label: 'December' },
  ];

  // Генерація масиву років (останні 100 років від поточного)
  currentYear = new Date().getFullYear();
  years: number[] = Array.from({ length: 100 }, (_, i) => this.currentYear - i);

  // Стан для відображення пароля
  showPassword = false;

  togglePasswordVisibility() {
    this.showPassword = !this.showPassword;
  }
}
