import { Component, inject } from '@angular/core';
import { Header } from '../../../core/components/header/header';
import {RouterLink, Router, RouterModule} from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Api } from '../../../core/services/api';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [Header, RouterLink, FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  private api = inject(Api);
  private router = inject(Router);

  // Об'єкт для збереження даних з форми (email та пароль)
  credentials = {
    email: '',
    password: ''
  };

  showPassword = false;

  togglePasswordVisibility() {
    this.showPassword = !this.showPassword;
  }

  onSubmit() {
    console.log('Login attempt for:', this.credentials.email);

    // Викликаємо метод login з вашого сервісу
    this.api.login(this.credentials).subscribe({
      next: (response: any) => {
        console.log('Login successful!', response);

        // Зберігаємо дані користувача в localStorage, щоб хедер та сторінка акаунта їх бачили
        localStorage.setItem('cinecore_user', JSON.stringify(response));

        // Переходимо на сторінку акаунта
        this.router.navigate(['/account']);
      },
      error: (err: any) => {
        console.error('Login error:', err);
        // Тут можна додати більш детальну обробку помилок
        alert('Incorrect email or password');
      }
    });
  }
}
