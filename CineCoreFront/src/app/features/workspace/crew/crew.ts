import {Component, OnInit, inject, ChangeDetectorRef, OnDestroy} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Subject, Subscription, interval } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { Api } from '../../../core/services/api';
import { Clipboard } from '@angular/cdk/clipboard';

@Component({
  selector: 'app-crew',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './crew.html',
  styleUrl: './crew.scss'
})
export class Crew implements OnInit, OnDestroy {
  private api = inject(Api);
  private route = inject(ActivatedRoute);
  private cdr = inject(ChangeDetectorRef);
  private clipboard = inject(Clipboard);
  private pollSubscription?: Subscription; // ДОДАНО: підписка на таймер

  projectId: number = 0;
  currentUserId: number = 0;

  activeMembers: any[] = [];
  pendingInvites: any[] = [];

  currentUserRole: string = 'none';
  canEdit: boolean = false;

  searchQuery: string = '';
  roleFilter: string = 'All';
  allRoles = ['All', 'Owner', 'Manager', 'Actor'];

  activeTab: 'members' | 'pending' = 'members';

  switchTab(tab: 'members' | 'pending') {
    this.activeTab = tab;
  }

  isEditModalOpen = false;
  editingMember: any = null;

  // Стани модалки запрошення
  isInviteModalOpen = false;
  isUserFoundInDb = false;
  isDuplicateEmail = false; // Чи вже є в проекті?
  searchInfo = ''; // Текст інфо під полем
  serverError: string | null = null;
  isLoading = false;

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

    this.route.parent?.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.projectId = Number(id);
        this.loadCrewData();

        this.pollSubscription = interval(5000).subscribe(() => {
          this.loadCrewData();
        });
      }
    });

    this.emailSearchSubject.pipe(
      debounceTime(500),
      distinctUntilChanged()
    ).subscribe(email => {
      this.performEmailSearch(email);
    });
  }

  ngOnDestroy() {
    if (this.pollSubscription) {
      this.pollSubscription.unsubscribe();
    }
  }

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

  onEmailInput(email: string) {
    this.searchInfo = '';
    this.serverError = null;

    if (!email || !email.includes('@')) {
      this.isUserFoundInDb = false;
      this.isDuplicateEmail = false;
      return;
    }

    // ЛОКАЛЬНА ПЕРЕВІРКА: чи є вже такий email в таблицях?
    const emailLower = email.toLowerCase().trim();
    const inActive = this.activeMembers.some(m => m.email.toLowerCase() === emailLower);
    const inPending = this.pendingInvites.some(i => i.email.toLowerCase() === emailLower);

    if (inActive || inPending) {
      this.isDuplicateEmail = true;
      this.isUserFoundInDb = false; // Вимикаємо пошук в базі
      return;
    } else {
      this.isDuplicateEmail = false;
    }

    // Якщо все ок - шукаємо в базі даних CineCore
    this.emailSearchSubject.next(emailLower);
  }

  performEmailSearch(email: string) {
    if (this.isDuplicateEmail) return;

    this.api.searchUserByEmail(email).subscribe({
      next: (user) => {
        this.isUserFoundInDb = true;
        this.searchInfo = 'User found in system!';
        this.inviteForm.firstName = user.firstName || '';
        this.inviteForm.lastName = user.lastName || '';
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isUserFoundInDb = false;
        this.searchInfo = 'User not found. They will receive an email to register.';
        this.inviteForm.firstName = '';
        this.inviteForm.lastName = '';
        this.cdr.detectChanges();
      }
    });
  }

  openInviteModal() {
    this.inviteForm = { email: '', firstName: '', lastName: '', sysRole: 'actor', jobTitle: '', department: '', message: '' };
    this.isUserFoundInDb = false;
    this.isDuplicateEmail = false;
    this.searchInfo = '';
    this.serverError = null;
    this.isLoading = false;
    this.isInviteModalOpen = true;
  }

  closeInviteModal() {
    this.isInviteModalOpen = false;
  }

  sendInvite(formValid: boolean | null) {
    if (!formValid || this.isDuplicateEmail) return;

    this.isLoading = true;
    this.serverError = null;

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
        this.loadCrewData();
        this.closeInviteModal();
      },
      error: (err) => {
        this.isLoading = false;
        this.serverError = err.error?.message || err.error || 'Error sending invitation';
        this.cdr.detectChanges();
      }
    });
  }

  deleteInvite(invite: any) {
      if (invite.inviteId) {
        // Видалення зовнішнього запрошення (через email)
        this.api.deleteProjectInvite(invite.inviteId).subscribe({
          next: () => {
            this.pendingInvites = this.pendingInvites.filter(i => i.inviteId !== invite.inviteId);
            this.cdr.detectChanges();
          },
          error: () => alert('Failed to delete invitation')
        });
      } else if (invite.userId) {
        // Видалення внутрішнього (відхиленого або очікуючого) запрошення
        this.api.removeProjectMember(this.projectId, invite.userId, this.currentUserId).subscribe({
          next: () => this.loadCrewData(), // Перезавантажуємо, щоб оновити списки
          error: () => alert('Failed to remove pending member')
        });
      }

  }

  resendDeclinedInvite(invite: any) {
    const payload = {
      projectId: this.projectId,
      invitedById: this.currentUserId,
      email: invite.email,
      firstName: '-', // ВИПРАВЛЕННЯ: заглушка для валідації DTO
      lastName: '-',  // ВИПРАВЛЕННЯ: заглушка для валідації DTO
      sysRole: invite.sysRole,
      jobTitle: invite.jobTitle || '',
      department: invite.department || ''
    };

    this.api.inviteProjectMember(payload).subscribe({
      next: () => {
        // Забираємо alert, щоб не дратував, просто миттєво оновлюємо дані
        this.loadCrewData();
      },
      error: (err) => alert(err.error?.message || 'Error resending invite')
    });
  }

  openEditModal(member: any) {
    this.editingMember = member;
    this.serverError = null;
    this.isLoading = false;
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

  saveMemberChanges(formValid: boolean | null) {
    if (!this.editingMember || !formValid) return;

    this.isLoading = true;
    this.serverError = null;

    const payload = {
      sysRole: this.editForm.sysRole,
      jobTitle: this.editForm.jobTitle,
      department: this.editForm.department
    };

    this.api.updateProjectMember(this.projectId, this.editingMember.userId, this.currentUserId, payload).subscribe({
      next: () => {
        this.loadCrewData();
        this.closeEditModal();
      },
      error: (err) => {
        this.isLoading = false;
        this.serverError = err.error?.message || 'Failed to update member';
        this.cdr.detectChanges();
      }
    });
  }

  removeMember(member: any) {
    if (member.sysRole === 'owner') {
      alert('Cannot remove the project owner.');
      return;
    }

    if (confirm(`Are you sure you want to remove ${member.fullName} from this project?`)) {
      this.api.removeProjectMember(this.projectId, member.userId, this.currentUserId).subscribe({
        next: () => this.loadCrewData(),
        error: (err) => alert(err.error?.message || 'Failed to remove member')
      });
    }
  }

  get filteredMembers() {
    const q = this.searchQuery.toLowerCase().trim();
    return this.activeMembers.filter(m => {
      const matchesSearch = !q || m.fullName.toLowerCase().includes(q) || m.email.toLowerCase().includes(q) || (m.jobTitle && m.jobTitle.toLowerCase().includes(q));
      const matchesRole = this.roleFilter === 'All' || m.sysRole.toLowerCase() === this.roleFilter.toLowerCase();
      return matchesSearch && matchesRole;
    });
  }

  get filteredInvites() {
    const q = this.searchQuery.toLowerCase().trim();
    return this.pendingInvites.filter(i => {
      const matchesSearch = !q || i.email.toLowerCase().includes(q);
      const matchesRole = this.roleFilter === 'All' || i.sysRole.toLowerCase() === this.roleFilter.toLowerCase();
      return matchesSearch && matchesRole;
    });
  }

  copyToClipboard(text: string, label: string) {
    if (!text) return;
    this.clipboard.copy(text);
    alert(`${label} copied to clipboard: ${text}`);
  }

  getRoleClass(role: string): string {
    const r = (role || '').toLowerCase();
    if (r === 'owner') return 'role-owner';
    if (r === 'manager') return 'role-manager';
    if (r === 'actor') return 'role-actor';
    return 'role-crew';
  }
}
