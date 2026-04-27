import { Component, OnInit, inject, ChangeDetectorRef, HostListener } from '@angular/core';
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

  viewMode: 'general' | 'personal' = 'general';
  myApprovedRoleIds: number[] = [];

  // Модалка СТВОРЕННЯ
  isModalOpen = false;
  newDayForm = {
    unit: 'MAIN UNIT', shootDate: '',
    shiftStart: '09:00', shiftEnd: '19:00',
    baseLocationId: null as number | null, notes: ''
  };

  // Модалка РЕДАГУВАННЯ
  isEditModalOpen = false;
  editingDay: any = null;
  editDayForm = {
    unit: '', shootDate: '',
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

  // AUTO-SCHEDULE MODAL
  isAutoModalOpen = false;
  isAutoLoading = false;
  autoScheduleResult: any = null;

  autoForm = {
    mode: 'fill' as 'fill' | 'generate',
    startDate: '',
    maxShiftHours: 10,
    skipWeekends: true,
    bufferMinutes: 15,
    groupBy: 'location' as 'location' | 'sequence',
    defaultShiftStart: '09:00',
    defaultShiftEnd: '19:00',
    // Нові поля для покращеного алгоритму
    setupMinutes: 30,           // час на setup на початку дня
    locationSwitchMinutes: 20,  // overhead при зміні локації всередині дня
  };

  // Auto-calendar (для вибору startDate у режимі generate)
  autoDisplayMonth = 0;
  autoDisplayYear = 0;
  autoSelectedDateObj: Date | null = null;
  autoCalendarWeeks: (number | null)[][] = [];


  ngOnInit() {
    this.route.parent?.params.subscribe(params => {
      this.projectId = +params['id'];
      if (this.projectId) {
        this.loadBoard();
        this.loadLocations();
        this.checkActorRoles();
      }
    });
    this.api.currentRole$.subscribe(role => {
      this.currentUserRole = role;
      this.canEdit = (role === 'owner' || role === 'manager');

      this.checkActorRoles();
      this.cdr.detectChanges();
    });
    const today = new Date();
    this.displayMonth = today.getMonth(); this.displayYear = today.getFullYear();
    this.editDisplayMonth = today.getMonth(); this.editDisplayYear = today.getFullYear();
    this.generateCalendar(); this.generateEditCalendar();
  }

  checkActorRoles() {
    if (!this.canEdit && this.projectId) {
      const user = JSON.parse(localStorage.getItem('cinecore_user') || '{}');

      if (user && user.id) {
        this.api.getActorCastingsInProject(this.projectId, user.id).subscribe({
          next: (castings: any[]) => {
            this.myApprovedRoleIds = castings
              .filter(c => c.status?.toLowerCase() === 'approved')
              .map(c => c.roleId);

            this.cdr.detectChanges();
          },
          error: (err) => console.error('Error loading approved roles:', err)
        });
      }
    }
  }

  loadLocations() {
    this.api.getLocationsByProject(this.projectId).subscribe({ next: (data) => this.projectLocations = data });
  }

  loadBoard() {
    this.api.getPlannerBoard(this.projectId).subscribe({
      next: (data) => {
        this.board = data;
        this.applyFilters();
        this.cdr.detectChanges();
      }
    });
  }

  // ВИПРАВЛЕННЯ: фільтр локацій тепер показує лише:
  //   - "All Locations" (завжди перший)
  //   - реальні назви локацій тільки з scenes що мають hasLocationResource === true
  //   - "No Location" (для сцен без прив'язаного location-ресурсу)
  get uniqueLocations(): string[] {
    const locs = this.board.scenePool
      .filter((s: any) => s.hasLocationResource === true)
      .map((s: any) => s.location as string)
      .filter((l: string) => !!l && l !== 'TBD');

    const unique = [...new Set(locs)].sort() as string[];
    return ['All Locations', ...unique, 'No Location'];
  }

  get uniqueRoles(): string[] {
    const roles: string[] = [];
    this.board.scenePool.forEach((s: any) => { if (s.cast) roles.push(...s.cast); });
    return ['All Roles', ...new Set(roles)] as string[];
  }

  applyFilters() {
    this.filteredScenePool = this.board.scenePool.filter((scene: any) => {
      const q = this.searchQuery.toLowerCase();
      const matchesSearch = !this.searchQuery
        || scene.title?.toLowerCase().includes(q)
        || scene.displayId?.toLowerCase().includes(q);

      // ВИПРАВЛЕННЯ: фільтр за локацією з підтримкою "No Location"
      let matchesLoc: boolean;
      if (this.selectedLocation === 'All Locations') {
        matchesLoc = true;
      } else if (this.selectedLocation === 'No Location') {
        matchesLoc = !scene.hasLocationResource;
      } else {
        matchesLoc = scene.hasLocationResource && scene.location === this.selectedLocation;
      }

      const matchesRole = this.selectedRole === 'All Roles'
        || (scene.cast && scene.cast.includes(this.selectedRole));

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
      next: () => this.loadBoard(),
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

    const payload: any = { unit: this.editDayForm.unit, generalNotes: this.editDayForm.notes };

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
    day.status = newStatus;
    this.cdr.detectChanges();

    this.api.updateShootDay(this.projectId, day.id, { status: newStatus }).subscribe({
      error: () => {
        day.status = previousStatus;
        this.cdr.detectChanges();
      }
    });
  }

  get visibleShootDays() {
    if (!this.board.shootDays) return [];
    if (this.canEdit) return this.board.shootDays;

    // Якщо це актор - фільтруємо
    return this.board.shootDays.filter((day: any) => {
      // Актори не бачать чорнетки (draft) і generated
      if (day.status === 'draft' || day.status === 'generated') return false;

      // Якщо увімкнено "Мій розклад" (personal)
      if (this.viewMode === 'personal') {
        const hasMyScene = day.scenes.some((scene: any) =>
          scene.roleIds?.some((id: number) => this.myApprovedRoleIds.includes(id))
        );
        if (!hasMyScene) return false;
      }

      return true;
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

  openAutoModal() {
    const today = new Date();
    this.autoDisplayMonth = today.getMonth();
    this.autoDisplayYear = today.getFullYear();
    this.generateAutoCalendar();
    this.autoScheduleResult = null;
    this.isAutoModalOpen = true;
  }

  closeAutoModal() {
    this.isAutoModalOpen = false;
    this.autoScheduleResult = null;
  }

  generateAutoCalendar() {
    this.autoCalendarWeeks = this.buildCalendarWeeks(
      this.autoDisplayYear, this.autoDisplayMonth
    );
  }

  prevAutoMonth() {
    if (this.autoDisplayMonth === 0) {
      this.autoDisplayMonth = 11; this.autoDisplayYear--;
    } else { this.autoDisplayMonth--; }
    this.generateAutoCalendar();
  }

  nextAutoMonth() {
    if (this.autoDisplayMonth === 11) {
      this.autoDisplayMonth = 0; this.autoDisplayYear++;
    } else { this.autoDisplayMonth++; }
    this.generateAutoCalendar();
  }

  get autoCurrentMonthName() { return this.monthNames[this.autoDisplayMonth]; }

  selectAutoDate(day: number | null) {
    if (!day) return;
    this.autoSelectedDateObj = new Date(this.autoDisplayYear, this.autoDisplayMonth, day);
    this.autoForm.startDate = this.formatDate(this.autoSelectedDateObj);
  }

  isAutoSelected(day: number): boolean {
    if (!this.autoSelectedDateObj) return false;
    return day === this.autoSelectedDateObj.getDate()
      && this.autoDisplayMonth === this.autoSelectedDateObj.getMonth()
      && this.autoDisplayYear === this.autoSelectedDateObj.getFullYear();
  }

  isAutoToday(day: number): boolean {
    const t = new Date();
    return day === t.getDate()
      && this.autoDisplayMonth === t.getMonth()
      && this.autoDisplayYear === t.getFullYear();
  }

  submitAutoSchedule() {
    if (this.autoForm.mode === 'generate' && !this.autoForm.startDate) return;
    this.isAutoLoading = true;

    const payload = {
      mode: this.autoForm.mode,
      startDate: this.autoForm.startDate || null,
      maxShiftMinutes: this.autoForm.maxShiftHours * 60,
      skipWeekends: this.autoForm.skipWeekends,
      bufferMinutes: this.autoForm.bufferMinutes,
      groupBy: this.autoForm.groupBy,
      defaultShiftStart: this.autoForm.defaultShiftStart,
      defaultShiftEnd: this.autoForm.defaultShiftEnd,
      // Нові параметри алгоритму
      setupMinutes: this.autoForm.setupMinutes,
      locationSwitchMinutes: this.autoForm.locationSwitchMinutes,
    };

    this.api.autoSchedule(this.projectId, payload).subscribe({
      next: (result) => {
        this.isAutoLoading = false;
        this.autoScheduleResult = result;
        this.loadBoard();
        this.cdr.detectChanges();
      },
      error: () => {
        this.isAutoLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  // Підтвердження/відхилення конкретного дня
  confirmGeneratedDay(dayId: number) {
    this.api.confirmDay(this.projectId, dayId, true).subscribe({
      next: () => this.loadBoard()
    });
  }

  rejectGeneratedDay(dayId: number) {
    this.api.confirmDay(this.projectId, dayId, false).subscribe({
      next: () => this.loadBoard()
    });
  }

  // Масове підтвердження
  confirmAllGeneratedDays() {
    this.api.confirmAllGenerated(this.projectId).subscribe({
      next: () => { this.closeAutoModal(); this.loadBoard(); }
    });
  }

  // Масове відхилення всіх generated днів
  rejectAllGeneratedDays() {
    const generatedIds = this.board.shootDays
      ?.filter((d: any) => d.status === 'generated')
      .map((d: any) => d.id) ?? [];

    if (!generatedIds.length) return;

    const requests = generatedIds.map((id: number) =>
      this.api.confirmDay(this.projectId, id, false)
    );

    requests.reduce(
      (chain: Promise<any>, req: any) =>
        chain.then(() => req.toPromise()),
      Promise.resolve()
    ).then(() => {
      this.closeAutoModal();
      this.loadBoard();
    });
  }

  get hasGeneratedDays(): boolean {
    return this.board.shootDays?.some((d: any) => d.status === 'generated') ?? false;
  }

  get generatedDaysCount(): number {
    return this.board.shootDays?.filter((d: any) => d.status === 'generated').length ?? 0;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent) {
    const target = event.target as HTMLElement;
    if (!target.closest('.status-dropdown-wrapper')) {
      this.board.shootDays?.forEach((d: any) => d._statusOpen = false);
      this.cdr.detectChanges();
    }
  }
}
