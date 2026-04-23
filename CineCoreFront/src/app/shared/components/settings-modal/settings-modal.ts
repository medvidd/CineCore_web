import { Component, EventEmitter, Input, Output, inject, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Api } from '../../../core/services/api';

@Component({
  selector: 'app-settings-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './settings-modal.html',
  styleUrl: './settings-modal.scss'
})
export class SettingsModal implements OnChanges {
  private api = inject(Api);

  @Input() isOpen = false;
  @Input() userData: any = null;
  @Output() close = new EventEmitter<void>();
  @Output() profileUpdated = new EventEmitter<any>();
  @Output() accountDeleted = new EventEmitter<void>(); // Сигнал для виходу з акаунта

  // Основні дані форми
  formData = {
    firstName: '',
    lastName: '',
    email: '',
    phoneNum: '',
    avatarTheme: 'theme-teal' // Тема за замовчуванням
  };

  // Доступні теми для аватарки
  // Доступні теми для аватарки
  avatarThemes = [
    { id: 'theme-teal', style: 'linear-gradient(135deg, #3AB9A0 0%, #E9A60F 100%)' },
    { id: 'theme-cyber', style: 'linear-gradient(135deg, #51A2FF 0%, #C27AFF 100%)' },
    { id: 'theme-sunset', style: 'linear-gradient(135deg, #FF8904 0%, #FF3B30 100%)' },
    { id: 'theme-mono', style: 'linear-gradient(135deg, #8E8E93 0%, #3A3A3C 100%)' },
    { id: 'theme-ocean', style: 'linear-gradient(135deg, #0A84FF 0%, #30D158 100%)' },
    { id: 'theme-lavender', style: 'linear-gradient(135deg, #FF9A9E 0%, #C27AFF 100%)' },
    { id: 'theme-ruby', style: 'linear-gradient(135deg, #D9138A 0%, #E2D111 100%)' },
    { id: 'theme-midnight', style: 'linear-gradient(135deg, #1A2980 0%, #26D0CE 100%)' }
  ];

  // Стан для зміни пароля
  isChangingPassword = false;
  passwords = { current: '', new: '', confirm: '' };

  // Стан для видалення акаунта
  isDeletingAccount = false;
  deleteConfirmation = '';

  // Оновлюємо форму, коли передаються дані користувача
  ngOnChanges(changes: SimpleChanges) {
    if (changes['userData'] && this.userData) {
      this.formData = {
        firstName: this.userData.firstName || '',
        lastName: this.userData.lastName || '',
        email: this.userData.email || '',
        phoneNum: this.userData.phoneNum || '',
        avatarTheme: this.userData.avatarTheme || 'theme-teal'
      };
      this.resetStates();
    }
  }

  selectTheme(themeId: string) {
    this.formData.avatarTheme = themeId;
  }

  togglePasswordSection() {
    this.isChangingPassword = !this.isChangingPassword;
    this.passwords = { current: '', new: '', confirm: '' }; // Очищаємо при закритті
  }

  toggleDeleteSection() {
    this.isDeletingAccount = !this.isDeletingAccount;
    this.deleteConfirmation = '';
  }

  resetStates() {
    this.isChangingPassword = false;
    this.isDeletingAccount = false;
  }

  saveProfile() {
    // ТУТ БУДЕ ВАШ API ЗАПИТ (напр. this.api.updateUser(this.userData.id, this.formData).subscribe(...))
    console.log('Збереження профілю:', this.formData);

    // ТИМЧАСОВА ЛОГІКА ДЛЯ UI: Оновлюємо localStorage
    const updatedUser = { ...this.userData, ...this.formData };
    localStorage.setItem('cinecore_user', JSON.stringify(updatedUser));

    this.profileUpdated.emit(updatedUser); // Повідомляємо Account компонент
    this.closeModal();
  }

  changePassword() {
    if (this.passwords.new !== this.passwords.confirm) {
      alert('New passwords do not match!');
      return;
    }
    // ТУТ БУДЕ ВАШ API ЗАПИТ ДЛЯ ПАРОЛЯ
    console.log('Пароль змінено!');
    alert('Password successfully changed! (Mock)');
    this.togglePasswordSection();
  }

  deleteAccount() {
    if (this.deleteConfirmation !== 'DELETE') {
      alert('Please type DELETE to confirm.');
      return;
    }
    // ТУТ БУДЕ ВАШ API ЗАПИТ ДЛЯ ВИДАЛЕННЯ (DELETE /api/users/{id})
    console.log('Акаунт видалено!');
    this.accountDeleted.emit(); // Повідомляємо Account компонент, щоб розлогінити
  }

  closeModal() {
    this.resetStates();
    this.close.emit();
  }
}
