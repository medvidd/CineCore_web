import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { Api } from '../../../core/services/api';

@Component({
  selector: 'app-crew',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './crew.html',
  styleUrl: './crew.scss'
})
export class Crew implements OnInit {
  private api = inject(Api);
  private route = inject(ActivatedRoute);
  private cdr = inject(ChangeDetectorRef);

  projectId: number = 0;
  currentUserId: number = 0; // Нам потрібен ID того, хто запрошує

  // Дані для таблиць
  activeMembers: any[] = [];
  pendingInvites: any[] = [];

  currentUserRole: string = 'none';
  canEdit: boolean = false;

  // ==========================================
  // СТАН МОДАЛЬНОГО ВІКНА ЗАПРОШЕННЯ
  // ==========================================
  isInviteModalOpen = false;
  isUserFoundInDb = false; // Чи знайшли ми пошту в базі?
  searchError = ''; // Повідомлення, якщо пошту не знайдено

  // Форма запрошення
  inviteForm = {
    email: '',
    firstName: '',
    lastName: '',
    sysRole: 'actor',
    jobTitle: '',
    department: '',
    message: ''
  };

  // Subject для затримки пошуку при введенні email
  private emailSearchSubject = new Subject<string>();

  ngOnInit() {
    this.api.currentRole$.subscribe(role => {
      this.currentUserRole = role;
      this.canEdit = (role === 'owner' || role === 'manager');
      this.cdr.detectChanges();
    });

    this.api.user$.subscribe(user => {
      if (user) this.currentUserId = user.id;
    });

    // Отримуємо ID проекту з URL
    this.route.parent?.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.projectId = Number(id);
        this.loadCrewData();
      }
    });

    // Налаштовуємо "розумний" пошук (чекає 500мс після останнього натискання клавіші)
    this.emailSearchSubject.pipe(
      debounceTime(500),
      distinctUntilChanged()
    ).subscribe(email => {
      this.performEmailSearch(email);
    });
  }

  // ==========================================
  // ЗАВАНТАЖЕННЯ ДАНИХ
  // ==========================================
  loadCrewData() {
    this.api.getProjectCrew(this.projectId).subscribe({
      next: (data) => {
        this.activeMembers = data.activeMembers || [];
        this.pendingInvites = data.pendingInvites || [];
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Failed to load crew data', err)
    });
  }

  // ==========================================
  // ЛОГІКА ПОШУКУ ПОШТИ
  // ==========================================
  onEmailInput(email: string) {
    if (!email || !email.includes('@')) {
      this.isUserFoundInDb = false;
      this.searchError = '';
      return;
    }
    this.emailSearchSubject.next(email);
  }

  performEmailSearch(email: string) {
    this.api.searchUserByEmail(email).subscribe({
      next: (user) => {
        // Користувача знайдено в БД!
        this.isUserFoundInDb = true;
        this.searchError = '';
        this.inviteForm.firstName = user.firstName || '';
        this.inviteForm.lastName = user.lastName || '';
        this.cdr.detectChanges();
      },
      error: (err) => {
        // Користувача немає в БД — дозволяємо вводити ім'я вручну
        this.isUserFoundInDb = false;
        this.searchError = 'User not found. They will receive an email to register.';
        this.inviteForm.firstName = '';
        this.inviteForm.lastName = '';
        this.cdr.detectChanges();
      }
    });
  }

  // ==========================================
  // МОДАЛЬНЕ ВІКНО ТА ВІДПРАВКА
  // ==========================================
  openInviteModal() {
    this.inviteForm = { email: '', firstName: '', lastName: '', sysRole: 'actor', jobTitle: '', department: '', message: '' };
    this.isUserFoundInDb = false;
    this.searchError = '';
    this.isInviteModalOpen = true;
  }

  closeInviteModal() {
    this.isInviteModalOpen = false;
  }

  sendInvite() {
    const payload = {
      projectId: this.projectId,
      invitedById: this.currentUserId,
      ...this.inviteForm
    };

    this.api.inviteProjectMember(payload).subscribe({
      next: (res) => {
        console.log('Invite sent!', res);
        this.loadCrewData(); // Оновлюємо таблиці
        this.closeInviteModal();
      },
      error: (err) => {
        alert(err.error?.message || 'Error sending invitation');
      }
    });
  }

  // ==========================================
  // HELPERS (Класи для бейджів)
  // ==========================================
  getRoleClass(role: string): string {
    const r = (role || '').toLowerCase();
    if (r === 'owner') return 'role-owner';
    if (r === 'manager') return 'role-manager';
    if (r === 'actor') return 'role-actor';
    return 'role-crew';
  }
}
