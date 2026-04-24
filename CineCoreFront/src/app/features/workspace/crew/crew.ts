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

  // Стан активної вкладки
  activeTab: 'members' | 'pending' = 'members';

  // Метод для перемикання вкладок
  switchTab(tab: 'members' | 'pending') {
    this.activeTab = tab;
  }

  isEditModalOpen = false;
  editingMember: any = null;
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

  editForm = {
    sysRole: 'actor',
    jobTitle: '',
    department: ''
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
    // Формуємо payload. Назви полів мають точно збігатися з CreateInvitationDto
    const payload = {
      projectId: this.projectId,
      invitedById: this.currentUserId,
      email: this.inviteForm.email,
      firstName: this.inviteForm.firstName,
      lastName: this.inviteForm.lastName,
      sysRole: this.inviteForm.sysRole,
      jobTitle: this.inviteForm.jobTitle,
      department: this.inviteForm.department,
      message: this.inviteForm.message
    };

    this.api.inviteProjectMember(payload).subscribe({
      next: (res) => {
        this.loadCrewData(); // Оновлюємо таблиці (користувач одразу з'явиться в Active або Pending)
        this.closeInviteModal();
      },
      error: (err) => {
        alert(err.error?.message || 'Error sending invitation');
      }
    });
  }

  deleteInvite(inviteId: number) {
    if (confirm('Are you sure you want to cancel this invitation?')) {
      this.api.deleteProjectInvite(inviteId).subscribe({
        next: () => {
          this.pendingInvites = this.pendingInvites.filter(i => i.id !== inviteId);
          this.cdr.detectChanges();
        },
        error: (err) => alert('Failed to delete invitation')
      });
    }
  }

  openEditModal(member: any) {
    this.editingMember = member;
    // Заповнюємо форму поточними даними учасника
    this.editForm = {
      sysRole: member.sysRole || 'actor',
      jobTitle: member.jobTitle || '',
      department: member.department || ''
    };
    this.isEditModalOpen = true;
  }

  closeEditModal() {
    this.isEditModalOpen = false;
    this.editingMember = null;
  }

  saveMemberChanges() {
    if (!this.editingMember) return;

    const payload = {
      sysRole: this.editForm.sysRole,
      jobTitle: this.editForm.jobTitle,
      department: this.editForm.department
    };

    // Викликаємо наш новий метод з api.ts
    this.api.updateProjectMember(this.projectId, this.editingMember.userId, this.currentUserId, payload).subscribe({
      next: () => {
        this.loadCrewData(); // Оновлюємо таблицю, щоб побачити зміни
        this.closeEditModal();
      },
      error: (err) => alert(err.error?.message || 'Failed to update member')
    });
  }

  removeMember(member: any) {
    // Власника видаляти не можна (бекенд це теж перевіряє, але краще зупинити відразу на фронті)
    if (member.sysRole === 'owner') {
      alert('Cannot remove the project owner.');
      return;
    }

    if (confirm(`Are you sure you want to remove ${member.fullName} from this project?`)) {
      this.api.removeProjectMember(this.projectId, member.userId, this.currentUserId).subscribe({
        next: () => {
          // Оновлюємо дані таблиці після успішного видалення
          this.loadCrewData();
        },
        error: (err) => alert(err.error?.message || 'Failed to remove member')
      });
    }
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
