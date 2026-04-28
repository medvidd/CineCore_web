import { Component, inject, ChangeDetectorRef } from '@angular/core';
import { Header } from '../../../core/components/header/header';
import { RouterLink, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Api } from '../../../core/services/api';

@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [Header, RouterLink, FormsModule],
  templateUrl: './signup.html',
  styleUrl: './signup.scss',
})
export class Signup {
  private api = inject(Api);
  private router = inject(Router);
  private cdr = inject(ChangeDetectorRef);

  days: number[] = Array.from({ length: 31 }, (_, i) => i + 1);
  months = [
    { value: '1', label: 'January' }, { value: '2', label: 'February' },
    { value: '3', label: 'March' }, { value: '4', label: 'April' },
    { value: '5', label: 'May' }, { value: '6', label: 'June' },
    { value: '7', label: 'July' }, { value: '8', label: 'August' },
    { value: '9', label: 'September' }, { value: '10', label: 'October' },
    { value: '11', label: 'November' }, { value: '12', label: 'December' },
  ];
  currentYear = new Date().getFullYear();
  years: number[] = Array.from({ length: 100 }, (_, i) => this.currentYear - i);

  showPassword = false;
  isLoading = false;
  serverError: string | null = null;
  agreedToTerms = false;

  togglePasswordVisibility() {
    this.showPassword = !this.showPassword;
  }

  selectedBirthDate = {
    day: '',
    month: '',
    year: ''
  };

  formData = {
    firstName: '',
    lastName: '',
    email: '',
    password: '',
    phoneNum: ''
  };

  onSubmit(formValid: boolean | null) {
    if (!formValid || !this.agreedToTerms) return;

    this.isLoading = true;
    this.serverError = null;

    let birthdayISO = null;
    if (this.selectedBirthDate.day && this.selectedBirthDate.month && this.selectedBirthDate.year) {
      const date = new Date(Number(this.selectedBirthDate.year), Number(this.selectedBirthDate.month) - 1, Number(this.selectedBirthDate.day), 12, 0, 0);
      birthdayISO = date.toISOString();
    }

    const finalData = { ...this.formData, birthday: birthdayISO };

    this.api.register(finalData).subscribe({
      next: (response) => {
        localStorage.setItem('cinecore_user', JSON.stringify(response));
        this.router.navigate(['/account']);
      },
      error: (err) => {
        this.isLoading = false;
        if (err.error?.errors) {
          const firstErrorKey = Object.keys(err.error.errors)[0];
          this.serverError = err.error.errors[firstErrorKey][0];
        } else {
          this.serverError = err.error?.message || err.error || 'Registration failed.';
        }
        this.cdr.detectChanges(); // <--- ПРИМУСОВО ОНОВЛЮЄМО
      }
    });
  }
}
