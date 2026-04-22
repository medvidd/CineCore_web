import { Component, inject } from '@angular/core';
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

  onSubmit() {
    // Формуємо дату народження
    let birthdayISO = null;
    if (this.selectedBirthDate.day && this.selectedBirthDate.month && this.selectedBirthDate.year) {
      const date = new Date(
        Number(this.selectedBirthDate.year),
        Number(this.selectedBirthDate.month) - 1,
        Number(this.selectedBirthDate.day),
        12, 0, 0
      );
      birthdayISO = date.toISOString();
    }

    // Створюємо фінальний об'єкт для бекенду (мапимо на UserRegisterDto)
    const finalData = {
      ...this.formData,
      birthday: birthdayISO
    };

    console.log('Sending data for registration:', finalData);

    this.api.register(finalData).subscribe({
      next: (response) => {
        // Якщо сервер повернув 200 OK і дані користувача
        console.log('Successfully registered!', response);

        // Зберігаємо дані (Id, Email, FirstName, LastName) у пам'ять браузера,
        // щоб сторінка Акаунта знала, хто ми
        localStorage.setItem('cinecore_user', JSON.stringify(response));

        // Переходимо на сторінку акаунта
        this.router.navigate(['/account']);
      },
      error: (err) => {
        // Якщо сервер повернув помилку (наприклад, 400 або 500)
        console.error('Registration error:', err);
        alert('Error during registration. Check the entered data.');
      }
    });
  }
}
