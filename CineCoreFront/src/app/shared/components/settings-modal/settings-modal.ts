import { Component, EventEmitter, Input, Output, inject, OnChanges, SimpleChanges, ChangeDetectorRef } from '@angular/core';
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
  private cdr = inject(ChangeDetectorRef);

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

  // Стани валідації та повідомлень від сервера
  updateError: string | null = null;
  updateSuccess = false;
  passError: string | null = null;

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
    this.passError = null; // Очищаємо помилки
  }

  toggleDeleteSection() {
    this.isDeletingAccount = !this.isDeletingAccount;
    this.deleteConfirmation = '';
  }

  resetStates() {
    this.isChangingPassword = false;
    this.isDeletingAccount = false;
    this.updateError = null;
    this.updateSuccess = false;
    this.passError = null;
    this.passwords = { current: '', new: '', confirm: '' };
    this.deleteConfirmation = '';
  }

  saveProfile() {
    this.updateError = null;
    this.updateSuccess = false;

    this.api.updateUserProfile(this.userData.id, this.formData).subscribe({
      next: (updatedUserFromDb) => {
        // Оновлюємо локальні дані
        const updatedUser = { ...this.userData, ...this.formData };
        localStorage.setItem('cinecore_user', JSON.stringify(updatedUser));

        // Сигналізуємо батьківському компоненту
        this.profileUpdated.emit(updatedUser);

        // Показуємо зелене повідомлення про успіх
        this.updateSuccess = true;
        this.cdr.detectChanges();
        // Автоматично закриваємо модалку через 1.5 секунди
        setTimeout(() => this.closeModal(), 1500);
      },
      error: (err) => {
        // Якщо email зайнятий або інші проблеми
        this.updateError = err.error?.message || err.error || 'Error updating profile. Please check your data.';
        this.cdr.detectChanges();
      }
    });
  }

  changePassword() {
    this.passError = null; // Очищуємо попередні помилки

    if (this.passwords.new !== this.passwords.confirm) {
      this.passError = 'New passwords do not match!';
      return;
    }

    const payload = {
      currentPassword: this.passwords.current,
      newPassword: this.passwords.new
    };

    this.api.updateUserPassword(this.userData.id, payload).subscribe({
      next: () => {
        // Успіх! Закриваємо форму зміни пароля і очищаємо поля
        this.togglePasswordSection();
      },
      error: (err) => {
        // Показуємо червоне повідомлення (наприклад: "Invalid current password")
        this.passError = err.error?.message || err.error || 'Wrong current password.';
        this.cdr.detectChanges();
      }
    });
  }

  deleteAccount() {
    // Додатковий захист, хоча кнопка заблокована в HTML, якщо не 'DELETE'
    if (this.deleteConfirmation !== 'DELETE') {
      return;
    }

    this.api.deleteUserAccount(this.userData.id).subscribe({
      next: () => {
        this.accountDeleted.emit(); // Сигналізуємо, що юзера видалено (щоб розлогінити)
      },
      error: (err) => console.error('Помилка видалення', err)
    });
  }

  closeModal() {
    this.resetStates();
    this.close.emit();
  }
}
