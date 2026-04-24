import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Api} from '../../../core/services/api';

@Component({
  selector: 'app-casting',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './casting.html',
  styleUrl: './casting.scss'
})
export class Casting {
  private api = inject(Api);
  private cdr = inject(ChangeDetectorRef);

  // Статистика
  totalRoles = 6;
  castRoles = 1;

  currentUserRole: string = 'none';
  canEdit: boolean = false;

  ngOnInit() {
    // 1. Підписуємося на роль (БЕЗ ДОДАТКОВИХ HTTP ЗАПИТІВ!)
    this.api.currentRole$.subscribe(role => {
      this.currentUserRole = role;
      this.canEdit = (role === 'owner' || role === 'manager');
      this.cdr.detectChanges();
    });

    // ... ваша існуюча підписка на paramMap для отримання projectId
  }

  // Мокові дані для ролей (Ліва панель)
  roles = [
    {
      id: 1, name: 'Captain Eva Rostova', type: 'Lead', isCast: true,
      desc: 'A brilliant but troubled police captain leading the investigation into a series of mysterious murders.',
      gender: 'Female', age: '28-35',
      castActor: { initials: 'EJ', name: 'Emily Jones', age: 28, email: 'emily.jones@example.com', phone: '+1 555 234-5678', bio: 'Versatile actress with 8 years of theater and film experience. Known for dramatic roles.' },
      candidates: [{ initials: 'SW', name: 'Sarah William', age: 30 }]
    },
    {
      id: 2, name: 'Detective Miles Cross', type: 'Lead', isCast: false,
      desc: 'Eva\'s partner, skeptical of her unorthodox methods but fiercely loyal.',
      gender: 'Male', age: '35-45',
      castActor: null,
      candidates: [
        { initials: 'JB', name: 'James Brown', age: 38 },
        { initials: 'MD', name: 'Michael Davis', age: 42 }
      ]
    },
    { id: 3, name: 'Dr. Helena Vance', type: 'Supporting', isCast: false, candidates: [] },
    { id: 4, name: 'Officer James Reed', type: 'Supporting', isCast: false, candidates: [] },
    { id: 5, name: 'Mayor Victoria Stone', type: 'Supporting', isCast: false, candidates: [] },
    { id: 6, name: 'Witness #1', type: 'Extra', isCast: false, candidates: [] }
  ];

  // Вибрана роль (за замовчуванням перша)
  selectedRole: any = this.roles[0];

  selectRole(role: any) {
    this.selectedRole = role;
  }

  // Допоміжні класи для кольорів бейджів
  getRoleTypeClass(type: string): string {
    switch (type) {
      case 'Lead': return 'badge-lead';
      case 'Supporting': return 'badge-supporting';
      case 'Extra': return 'badge-extra';
      default: return '';
    }
  }
}
