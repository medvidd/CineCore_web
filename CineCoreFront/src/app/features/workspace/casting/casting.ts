import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Api } from '../../../core/services/api';

@Component({
  selector: 'app-casting',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './casting.html',
  styleUrl: './casting.scss'
})
export class Casting implements OnInit {
  private api = inject(Api);
  private route = inject(ActivatedRoute);
  private cdr = inject(ChangeDetectorRef);

  projectId: number = 0;
  currentUserRole: string = 'none';
  canEdit: boolean = false;

  // Дані з API
  roles: any[] = [];
  selectedRole: any = null;
  candidates: any[] = [];
  projectActors: any[] = [];

  // ==========================================
  // СТАН МОДАЛЬНОГО ВІКНА ДЛЯ РОЛІ
  // ==========================================
  isRoleModalOpen = false;
  isRoleLoading = false;
  roleServerError: string | null = null;

  roleForm = {
    id: null as number | null,
    roleName: '',
    roleType: 'supporting',
    description: '',
    age: null as number | null,
    colorHex: '#51A2FF'
  };
  charFields: { key: string, value: string }[] = [];
  roleTypes = ['lead', 'supporting', 'extra'];

  // ==========================================
  // СТАН МОДАЛЬНОГО ВІКНА ДЛЯ КАНДИДАТА
  // ==========================================
  isCandidateModalOpen = false;
  isCandidateLoading = false;
  candidateServerError: string | null = null;

  candidateForm = {
    actorId: null as number | null,
    notes: ''
  };

  // ==========================================
  // МОДАЛЬНЕ ВІКНО ДЕТАЛЕЙ КАНДИДАТА
  // ==========================================
  selectedCandidateDetails: any = null;
  isCandidateDetailsModalOpen = false;

  viewCandidateDetails(candidate: any) {
    this.selectedCandidateDetails = candidate;
    this.isCandidateDetailsModalOpen = true;
  }

  closeCandidateDetailsModal() {
    this.isCandidateDetailsModalOpen = false;
    this.selectedCandidateDetails = null;
  }

  // Змінні для пошуку
  searchQuery: string = '';
  statusFilter: string = 'all';

  get filteredRoles() {
    return this.roles.filter(role => {
      const matchesSearch = !this.searchQuery ||
        role.roleName.toLowerCase().includes(this.searchQuery.toLowerCase());

      let matchesStatus = true;
      if (this.statusFilter === 'casted') matchesStatus = role.isCast;
      if (this.statusFilter === 'uncasted') matchesStatus = !role.isCast;

      return matchesSearch && matchesStatus;
    });
  }

  activeActorTab: 'profile' | 'castings' = 'profile';
  actorProfile: any = null;
  myCastings: any[] = [];
  actorCharFields: { key: string, value: string }[] = [];
  isActorSaving = false;

  ngOnInit() {
    this.api.currentRole$.subscribe(role => {
      this.currentUserRole = role;
      this.canEdit = (role === 'owner' || role === 'manager');
      this.safeLoadActorData();
      this.cdr.detectChanges();
    });

    this.route.parent?.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.projectId = Number(id);

        this.loadRoles();
        this.loadProjectActors();
        this.safeLoadActorData();

        this.route.queryParams.subscribe(queryParams => {
          const targetRoleId = queryParams['roleId'] ? Number(queryParams['roleId']) : null;
          if (targetRoleId && this.roles.length > 0) {
            const role = this.roles.find(r => r.id === targetRoleId);
            if (role) this.selectRole(role);
          }
        });
      }
    });
  }

  safeLoadActorData() {
    if (this.currentUserRole === 'actor' && this.projectId > 0) {
      const user = JSON.parse(localStorage.getItem('cinecore_user') || '{}');
      if (user?.id) {
        if (!this.actorProfile) {
          this.loadActorProfile(user.id);
        }
        this.loadMyCastingsForActor(user.id);
      }
    }
  }

  switchActorTab(tab: 'profile' | 'castings') {
    this.activeActorTab = tab;
    if (tab === 'castings') {
      const user = JSON.parse(localStorage.getItem('cinecore_user') || '{}');
      if (user?.id && this.projectId > 0) {
        this.loadMyCastingsForActor(user.id);
      }
    }
  }

  // ==========================================
  // HELPERS
  // ==========================================
  private getErrorMessage(err: any): string {
    if (err.error && err.error.message) return err.error.message;
    if (err.error && err.error.errors) {
      const firstKey = Object.keys(err.error.errors)[0];
      return err.error.errors[firstKey][0];
    }
    return typeof err.error === 'string' ? err.error : "An unknown error occurred.";
  }

  // ==========================================
  // ЗАВАНТАЖЕННЯ ДАНИХ
  // ==========================================
  loadRoles() {
    this.api.getProjectRoles(this.projectId).subscribe({
      next: (data) => {
        this.roles = data;
        if (this.selectedRole) {
          this.selectedRole = this.roles.find(r => r.id === this.selectedRole.id) || this.roles[0];
        } else if (this.roles.length > 0) {
          this.selectRole(this.roles[0]);
        }
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error loading roles:', err)
    });
  }

  loadCandidates(roleId: number) {
    this.api.getRoleCandidates(this.projectId, roleId).subscribe({
      next: (data) => {
        this.candidates = data;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error loading candidates:', err)
    });
  }

  loadProjectActors() {
    this.api.getProjectCrew(this.projectId).subscribe({
      next: (data) => {
        this.projectActors = (data.activeMembers || []).filter((m: any) => m.sysRole === 'actor');
        this.cdr.detectChanges();
      }
    });
  }

  selectRole(role: any) {
    this.selectedRole = role;
    this.loadCandidates(role.id);
  }

  // ==========================================
  // МОДАЛЬНЕ ВІКНО РОЛІ ТА ХАРАКТЕРИСТИКИ
  // ==========================================
  openAddRoleModal() {
    this.roleForm = { id: null, roleName: '', roleType: 'supporting', description: '', age: null, colorHex: '#51A2FF' };
    this.charFields = [{ key: '', value: '' }];
    this.roleServerError = null;
    this.isRoleLoading = false;
    this.isRoleModalOpen = true;
  }

  openEditRoleModal() {
    if (!this.selectedRole) return;
    this.roleForm = {
      id: this.selectedRole.id,
      roleName: this.selectedRole.roleName,
      roleType: this.selectedRole.roleType,
      description: this.selectedRole.description,
      age: this.selectedRole.age,
      colorHex: this.selectedRole.colorHex
    };

    try {
      const parsed = JSON.parse(this.selectedRole.characteristics || '{}');
      this.charFields = Object.keys(parsed).map(k => ({ key: k, value: parsed[k] }));
      if (this.charFields.length === 0) this.charFields.push({ key: '', value: '' });
    } catch {
      this.charFields = [{ key: '', value: '' }];
    }

    this.roleServerError = null;
    this.isRoleLoading = false;
    this.isRoleModalOpen = true;
  }

  closeRoleModal() {
    this.isRoleModalOpen = false;
  }

  addCharField() { this.charFields.push({ key: '', value: '' }); }
  removeCharField(index: number) { this.charFields.splice(index, 1); }

  saveRole(formValid: boolean | null) {
    if (!formValid) return;

    this.isRoleLoading = true;
    this.roleServerError = null;

    const charObj: any = {};
    this.charFields.forEach(f => {
      if (f.key && f.key.trim() !== '') {
        charObj[f.key.trim()] = f.value;
      }
    });

    const payload = {
      ...this.roleForm,
      characteristics: JSON.stringify(charObj)
    };

    if (this.roleForm.id) {
      this.api.updateRole(this.projectId, this.roleForm.id, payload).subscribe({
        next: () => { this.loadRoles(); this.closeRoleModal(); },
        error: (err) => {
          this.isRoleLoading = false;
          this.roleServerError = this.getErrorMessage(err);
          this.cdr.detectChanges();
        }
      });
    } else {
      this.api.createRole(this.projectId, payload).subscribe({
        next: () => { this.loadRoles(); this.closeRoleModal(); },
        error: (err) => {
          this.isRoleLoading = false;
          this.roleServerError = this.getErrorMessage(err);
          this.cdr.detectChanges();
        }
      });
    }
  }

  deleteRole() {
    if (!this.selectedRole) return;
    if (confirm(`Are you sure you want to delete ${this.selectedRole.roleName}?`)) {
      this.api.deleteRole(this.projectId, this.selectedRole.id).subscribe({
        next: () => {
          this.selectedRole = null;
          this.loadRoles();
        }
      });
    }
  }

  // ==========================================
  // МОДАЛЬНЕ ВІКНО КАНДИДАТА
  // ==========================================
  openAddCandidateModal() {
    this.candidateForm = { actorId: null, notes: '' };
    this.candidateServerError = null;
    this.isCandidateLoading = false;
    this.isCandidateModalOpen = true;
  }

  closeCandidateModal() {
    this.isCandidateModalOpen = false;
  }

  saveCandidate(formValid: boolean | null) {
    if (!formValid || !this.candidateForm.actorId || !this.selectedRole) return;

    this.isCandidateLoading = true;
    this.candidateServerError = null;

    this.api.addCandidate(this.projectId, this.selectedRole.id, {
      actorId: Number(this.candidateForm.actorId),
      notes: this.candidateForm.notes
    }).subscribe({
      next: () => {
        this.loadCandidates(this.selectedRole.id);
        this.loadRoles();
        this.closeCandidateModal();
      },
      error: (err) => {
        this.isCandidateLoading = false;
        this.candidateServerError = this.getErrorMessage(err);
        this.cdr.detectChanges();
      }
    });
  }

  // ==========================================
  // KANBAN ДОШКА
  // ==========================================
  get pendingCandidates() { return this.candidates.filter(c => c.castStatus === 'pending'); }
  get holdCandidates() { return this.candidates.filter(c => c.castStatus === 'hold'); }
  get approvedCandidates() { return this.candidates.filter(c => c.castStatus === 'approved'); }
  get declinedCandidates() { return this.candidates.filter(c => c.castStatus === 'declined'); }

  draggedCandidate: any = null;

  onDragStart(event: DragEvent, candidate: any) {
    this.draggedCandidate = candidate;
    event.dataTransfer?.setData('text/plain', candidate.actorId.toString());
    setTimeout(() => { if (event.target instanceof HTMLElement) event.target.style.opacity = '0.5'; }, 0);
  }

  onDragEnd(event: DragEvent) {
    if (event.target instanceof HTMLElement) event.target.style.opacity = '1';
    this.draggedCandidate = null;
  }

  onDragOver(event: DragEvent) {
    event.preventDefault();
  }

  onDrop(event: DragEvent, newStatus: string) {
    event.preventDefault();
    if (this.draggedCandidate && this.draggedCandidate.castStatus !== newStatus) {
      const actorId = this.draggedCandidate.actorId;
      const oldStatus = this.draggedCandidate.castStatus;

      this.draggedCandidate.castStatus = newStatus;

      this.api.updateCandidateStatus(this.projectId, this.selectedRole.id, actorId, newStatus)
        .subscribe({
          next: () => {
            if (newStatus === 'approved' || oldStatus === 'approved') {
              this.loadRoles();
            }
          },
          error: () => {
            this.loadCandidates(this.selectedRole.id);
            alert('Failed to update status');
          }
        });
    }
  }

  deleteCandidate(candidate: any) {
    if (confirm(`Remove ${candidate.firstName} from this role?`)) {
      this.api.removeCandidate(this.projectId, this.selectedRole.id, candidate.actorId).subscribe({
        next: () => this.loadCandidates(this.selectedRole.id)
      });
    }
  }

  clearDeclinedCandidates() {
    const declined = this.declinedCandidates;
    if (declined.length === 0) return;

    if (confirm(`Remove all ${declined.length} declined candidates?`)) {
      declined.forEach(c => {
        this.api.removeCandidate(this.projectId, this.selectedRole.id, c.actorId).subscribe();
      });
      this.candidates = this.candidates.filter(c => c.castStatus !== 'declined');
      this.cdr.detectChanges();
    }
  }

  getAgeInfo(birthday: string | null): string {
    if (!birthday) return 'Age unknown';
    const birthDate = new Date(birthday);
    const today = new Date();
    let age = today.getFullYear() - birthDate.getFullYear();
    const m = today.getMonth() - birthDate.getMonth();
    if (m < 0 || (m === 0 && today.getDate() < birthDate.getDate())) {
      age--;
    }
    return `${birthDate.getFullYear()}   (${age} years old)`;
  }

  parseCharacteristics(jsonString: string): string[] {
    try {
      const parsed = JSON.parse(jsonString || '{}');
      return Object.entries(parsed).map(([k, v]) => `${k}: ${v}`);
    } catch {
      return [];
    }
  }

  // ==========================================
  // ACTORs
  // ==========================================
  loadActorProfile(userId: number) {
    this.api.getActorProfile(userId).subscribe(profile => {
      this.actorProfile = profile;
      try {
        const parsed = JSON.parse(profile.characteristics || '{}');
        this.actorCharFields = Object.keys(parsed).map(k => ({ key: k, value: parsed[k] }));
        if (this.actorCharFields.length === 0) this.actorCharFields.push({ key: '', value: '' });
      } catch {
        this.actorCharFields = [{ key: '', value: '' }];
      }
      this.cdr.detectChanges();
    });
  }

  loadMyCastingsForActor(userId: number) {
    this.api.getActorCastingsInProject(this.projectId, userId).subscribe({
      next: (data) => {
        this.myCastings = data;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error loading my castings:', err)
    });
  }

  // --- ВІДНОВЛЕНІ МЕТОДИ ---
  addActorCharField() {
    this.actorCharFields.push({ key: '', value: '' });
  }

  removeActorCharField(index: number) {
    this.actorCharFields.splice(index, 1);
  }

  saveActorCharacteristics() {
    this.isActorSaving = true;
    const charObj: any = {};
    this.actorCharFields.forEach(f => {
      if (f.key?.trim()) charObj[f.key.trim()] = f.value;
    });

    this.api.updateActorCharacteristics(this.actorProfile.id, JSON.stringify(charObj)).subscribe({
      next: () => {
        this.isActorSaving = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        this.isActorSaving = false;
        alert(this.getErrorMessage(err));
        this.cdr.detectChanges();
      }
    });
  }
}
