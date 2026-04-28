import { Component, inject, ChangeDetectorRef } from '@angular/core';
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
  private cdr = inject(ChangeDetectorRef);

  // Об'єкт для збереження даних з форми (email та пароль)
  credentials = {
    email: '',
    password: ''
  };

  showPassword = false;

  isLoading = false;
  serverError: string | null = null;

  togglePasswordVisibility() {
    this.showPassword = !this.showPassword;
  }

  onSubmit(formValid: boolean | null) {
    if (!formValid) return; // Захист

    this.isLoading = true;
    this.serverError = null;

    this.api.login(this.credentials).subscribe({
      next: (response: any) => {
        localStorage.setItem('cinecore_user', JSON.stringify(response));
        this.router.navigate(['/account']);
      },
      error: (err: any) => {
        this.isLoading = false;

        if (typeof err.error === 'string') {
          this.serverError = err.error; // Для Unauthorized("Wrong email or password")
        } else {
          this.serverError = err.error?.message || 'Incorrect email or password';
        }

        this.cdr.detectChanges(); // <--- 2. ПРИМУСОВО ОНОВЛЮЄМО ІНТЕРФЕЙС
      }
    });
  }
}
