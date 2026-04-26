import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DragDropModule, CdkDragDrop, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { Api } from '../../../core/services/api';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-planner',
  standalone: true,
  imports: [CommonModule, FormsModule, DragDropModule],
  templateUrl: './planner.html',
  styleUrl: './planner.scss'
})
export class Planner implements OnInit {
  private api = inject(Api);
  private cdr = inject(ChangeDetectorRef);
  private route = inject(ActivatedRoute);

  projectId: number = 0;
  board: any = { scenePool: [], shootDays: [] };

  filteredScenePool: any[] = [];
  searchQuery: string = '';
  selectedLocation: string = 'All Locations';
  selectedRole: string = 'All Roles';

  projectLocations: any[] = [];
  currentUserRole: string = 'none';
  canEdit: boolean = false;

  // Модалка СТВОРЕННЯ
  isModalOpen = false;
  newDayForm = {
    unit: 'MAIN UNIT', shootDate: '', callTime: '08:00',
    shiftStart: '09:00', shiftEnd: '19:00',
    baseLocationId: null as number | null, notes: ''
  };

  // Модалка РЕДАГУВАННЯ
  isEditModalOpen = false;
  editingDay: any = null;
  editDayForm = {
    unit: '', shootDate: '', callTime: '',
    shiftStart: '', shiftEnd: '',
    baseLocationId: null as number | null, notes: '', status: ''
  };

  displayMonth = 0; displayYear = 0;
  editDisplayMonth = 0; editDisplayYear = 0;
  selectedDateObj: Date | null = null;
  editSelectedDateObj: Date | null = null;
  calendarWeeks: (number | null)[][] = [];
  editCalendarWeeks: (number | null)[][] = [];
  monthNames = ['January','February','March','April','May','June','July','August','September','October','November','December'];

  ngOnInit() {
    this.route.parent?.params.subscribe(params => {
      this.projectId = +params['id'];
      if (this.projectId) { this.loadBoard(); this.loadLocations(); }
    });
    this.api.currentRole$.subscribe(role => {
      this.currentUserRole = role;
      this.canEdit = (role === 'owner' || role === 'manager');
      this.cdr.detectChanges();
    });
    const today = new Date();
    this.displayMonth = today.getMonth(); this.displayYear = today.getFullYear();
    this.editDisplayMonth = today.getMonth(); this.editDisplayYear = today.getFullYear();
    this.generateCalendar(); this.generateEditCalendar();
  }

  loadLocations() {
    this.api.getLocationsByProject(this.projectId).subscribe({ next: (data) => this.projectLocations = data });
  }

  loadBoard() {
    if (!this.projectId) return;
    this.api.getPlannerBoard(this.projectId).subscribe({
      next: (data) => { this.board = data; this.applyFilters(); this.cdr.detectChanges(); }
    });
  }

  get uniqueLocations(): string[] {
    const locs = this.board.scenePool.map((s: any) => s.location).filter((l: string) => l && l !== 'TBD');
    return ['All Locations', ...new Set(locs)] as string[];
  }
  get uniqueRoles(): string[] {
    const roles: string[] = [];
    this.board.scenePool.forEach((s: any) => { if (s.cast) roles.push(...s.cast); });
    return ['All Roles', ...new Set(roles)] as string[];
  }
  applyFilters() {
    this.filteredScenePool = this.board.scenePool.filter((scene: any) => {
      const q = this.searchQuery.toLowerCase();
      const matchesSearch = !this.searchQuery || scene.title?.toLowerCase().includes(q) || scene.displayId?.toLowerCase().includes(q);
      const matchesLoc = this.selectedLocation === 'All Locations' || scene.location === this.selectedLocation;
      const matchesRole = this.selectedRole === 'All Roles' || (scene.cast && scene.cast.includes(this.selectedRole));
      return matchesSearch && matchesLoc && matchesRole;
    });
  }

  drop(event: CdkDragDrop<any[]>, targetDayId: number | null) {
    const scene = event.previousContainer.data[event.previousIndex];
    if (event.previousContainer === event.container) {
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
    } else {
      transferArrayItem(event.previousContainer.data, event.container.data, event.previousIndex, event.currentIndex);
      if (event.previousContainer.id === 'poolList') {
        this.board.scenePool = this.board.scenePool.filter((s: any) => s.id !== scene.id);
      }
      if (targetDayId === null) {
        if (!this.board.scenePool.find((s: any) => s.id === scene.id)) this.board.scenePool.push(scene);
        this.applyFilters();
      }
    }
    this.api.moveScene(this.projectId, scene.id, targetDayId, event.currentIndex).subscribe({
      next: () => this.loadBoard(),   // reload so capacityStr/capacityPct update
      error: () => this.loadBoard()
    });
  }

  openModal() { this.isModalOpen = true; }
  closeModal() { this.isModalOpen = false; }
  submitNewDay() {
    if (!this.newDayForm.shootDate) return;
    this.api.createShootDay(this.projectId, this.newDayForm).subscribe({
      next: () => { this.closeModal(); this.loadBoard(); }
    });
  }

  openEditModal(day: any) {
    this.editingDay = day;
    const shootDateStr: string = day.shootDateIso ?? '';
    if (shootDateStr) {
      const [year, month, dayNum] = shootDateStr.split('-').map(Number);
      this.editSelectedDateObj = new Date(year, month - 1, dayNum);
      this.editDisplayMonth = month - 1;
      this.editDisplayYear = year;
    } else {
      this.editSelectedDateObj = null;
      const today = new Date();
      this.editDisplayMonth = today.getMonth();
      this.editDisplayYear = today.getFullYear();
    }
    this.generateEditCalendar();
    this.editDayForm = {
      unit: day.unit || 'MAIN UNIT',
      shootDate: shootDateStr,
      callTime: day.callTime || '08:00',
      shiftStart: day.shiftStartTime || day.callTime || '09:00',
      shiftEnd: day.shiftEndTime || '19:00',
      baseLocationId: day.baseLocationId ?? null,
      notes: day.notes || '',
      status: day.status || 'draft'
    };
    this.isEditModalOpen = true;
  }
  closeEditModal() { this.isEditModalOpen = false; this.editingDay = null; }
  submitEditDay() {
    if (!this.editingDay) return;
    const payload: any = { unit: this.editDayForm.unit, status: this.editDayForm.status, generalNotes: this.editDayForm.notes };
    if (this.editDayForm.shootDate) payload.shootDate = this.editDayForm.shootDate;
    if (this.editDayForm.shiftStart) payload.shiftStart = this.editDayForm.shiftStart;
    if (this.editDayForm.shiftEnd) payload.shiftEnd = this.editDayForm.shiftEnd;
    if (this.editDayForm.baseLocationId) payload.baseLocationId = this.editDayForm.baseLocationId;
    this.api.updateShootDay(this.projectId, this.editingDay.id, payload).subscribe({
      next: () => { this.closeEditModal(); this.loadBoard(); }
    });
  }

  deleteDay(dayId: number) {
    if (confirm('Видалити цей знімальний день? Усі сцени повернуться в пул.')) {
      this.api.deleteShootDay(this.projectId, dayId).subscribe(() => this.loadBoard());
    }
  }

  // -------------------------------------------------------
  // INLINE STATUS CHANGE
  // -------------------------------------------------------
  toggleStatusDropdown(day: any) {
    const wasOpen = day._statusOpen;
    // close all other open dropdowns first
    this.board.shootDays?.forEach((d: any) => d._statusOpen = false);
    day._statusOpen = !wasOpen;
    this.cdr.detectChanges();
  }

  closeStatusDropdown(day: any) {
    day._statusOpen = false;
    this.cdr.detectChanges();
  }

  changeStatus(day: any, newStatus: string) {
    day._statusOpen = false;
    const previousStatus = day.status;
    day.status = newStatus; // optimistic update
    this.cdr.detectChanges();

    this.api.updateShootDay(this.projectId, day.id, { status: newStatus }).subscribe({
      error: () => {
        day.status = previousStatus; // revert on error
        this.cdr.detectChanges();
      }
    });
  }

  // -------------------------------------------------------
  // CALENDARS
  // -------------------------------------------------------
  get currentMonthName() { return this.monthNames[this.displayMonth]; }
  get editCurrentMonthName() { return this.monthNames[this.editDisplayMonth]; }
  private buildCalendarWeeks(year: number, month: number): (number | null)[][] {
    const firstDay = new Date(year, month, 1).getDay();
    const daysInMonth = new Date(year, month + 1, 0).getDate();
    const weeks: (number | null)[][] = [];
    let week: (number | null)[] = Array(7).fill(null);
    let dow = firstDay;
    for (let d = 1; d <= daysInMonth; d++) {
      week[dow] = d; dow++;
      if (dow === 7) { weeks.push(week); week = Array(7).fill(null); dow = 0; }
    }
    if (dow > 0) weeks.push(week);
    return weeks;
  }
  generateCalendar() { this.calendarWeeks = this.buildCalendarWeeks(this.displayYear, this.displayMonth); }
  generateEditCalendar() { this.editCalendarWeeks = this.buildCalendarWeeks(this.editDisplayYear, this.editDisplayMonth); }
  prevMonth() { if (this.displayMonth === 0) { this.displayMonth = 11; this.displayYear--; } else { this.displayMonth--; } this.generateCalendar(); }
  nextMonth() { if (this.displayMonth === 11) { this.displayMonth = 0; this.displayYear++; } else { this.displayMonth++; } this.generateCalendar(); }
  prevEditMonth() { if (this.editDisplayMonth === 0) { this.editDisplayMonth = 11; this.editDisplayYear--; } else { this.editDisplayMonth--; } this.generateEditCalendar(); }
  nextEditMonth() { if (this.editDisplayMonth === 11) { this.editDisplayMonth = 0; this.editDisplayYear++; } else { this.editDisplayMonth++; } this.generateEditCalendar(); }
  private formatDate(date: Date): string {
    return `${date.getFullYear()}-${String(date.getMonth()+1).padStart(2,'0')}-${String(date.getDate()).padStart(2,'0')}`;
  }
  selectDate(day: number | null) { if (!day) return; this.selectedDateObj = new Date(this.displayYear, this.displayMonth, day); this.newDayForm.shootDate = this.formatDate(this.selectedDateObj); }
  selectEditDate(day: number | null) { if (!day) return; this.editSelectedDateObj = new Date(this.editDisplayYear, this.editDisplayMonth, day); this.editDayForm.shootDate = this.formatDate(this.editSelectedDateObj); }
  isToday(day: number): boolean { const t = new Date(); return day === t.getDate() && this.displayMonth === t.getMonth() && this.displayYear === t.getFullYear(); }
  isEditToday(day: number): boolean { const t = new Date(); return day === t.getDate() && this.editDisplayMonth === t.getMonth() && this.editDisplayYear === t.getFullYear(); }
  isSelected(day: number): boolean { if (!this.selectedDateObj) return false; return day === this.selectedDateObj.getDate() && this.displayMonth === this.selectedDateObj.getMonth() && this.displayYear === this.selectedDateObj.getFullYear(); }
  isEditSelected(day: number): boolean { if (!this.editSelectedDateObj) return false; return day === this.editSelectedDateObj.getDate() && this.editDisplayMonth === this.editSelectedDateObj.getMonth() && this.editDisplayYear === this.editSelectedDateObj.getFullYear(); }
  formatTimeInput(event: any) { let val = event.target.value.replace(/\D/g, ''); if (val.length > 2) val = val.substring(0,2) + ':' + val.substring(2,4); event.target.value = val; }
}
