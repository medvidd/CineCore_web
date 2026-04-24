import { Component, inject, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import {Api} from '../../../core/services/api';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class Dashboard {
    private api = inject(Api);
    private cdr = inject(ChangeDetectorRef);

  currentUserRole: string = 'none';
  canEdit: boolean = false;

  ngOnInit() {
    // 1. Підписуємося на роль (БЕЗ ДОДАТКОВИХ HTTP ЗАПИТІВ!)
    this.api.currentRole$.subscribe(role => {
      this.currentUserRole = role;
      this.canEdit = (role === 'owner' || role === 'manager');
      this.cdr.detectChanges();
    });
  }

}
