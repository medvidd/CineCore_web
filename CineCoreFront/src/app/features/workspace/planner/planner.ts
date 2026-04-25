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

  // Об'єкт дошки, що відповідає PlannerBoardDto з бекенду
  board: any = { scenePool: [], shootDays: [] };

  currentUserRole: string = 'none';
  canEdit: boolean = false;
  isModalOpen = false;

  newDayForm = {
    unit: 'MAIN UNIT', shootDate: '', callTime: '08:00', shiftStart: '09:00', shiftEnd: '19:00', baseLocation: null as number | null, notes: ''
  };

  displayMonth = 0;
  displayYear = 0;
  selectedDateObj: Date | null = null;
  calendarWeeks: (number | null)[][] = [];
  monthNames = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];

  ngOnInit() {
    // Отримуємо projectId з URL (наприклад: /workspace/1/planner)
    this.route.parent?.params.subscribe(params => {
      this.projectId = +params['id'];
      if (this.projectId) {
        this.loadBoard();
      }
    });

    this.api.currentRole$.subscribe(role => {
      this.currentUserRole = role;
      this.canEdit = (role === 'owner' || role === 'manager');
      this.cdr.detectChanges();
    });

    const today = new Date();
    this.displayMonth = today.getMonth();
    this.displayYear = today.getFullYear();
    this.generateCalendar();
  }

  loadBoard() {
    if (!this.projectId) return;
    this.api.getPlannerBoard(this.projectId).subscribe({
      next: (data) => {
        this.board = data;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Error loading planner board', err)
    });
  }

  // Обробка Drag & Drop
  drop(event: CdkDragDrop<any[]>, targetDayId: number | null) {
    const scene = event.previousContainer.data[event.previousIndex];

    if (event.previousContainer === event.container) {
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
    } else {
      transferArrayItem(
        event.previousContainer.data,
        event.container.data,
        event.previousIndex,
        event.currentIndex
      );
    }

    // Відправляємо на бекенд
    this.api.moveScene(this.projectId, scene.id, targetDayId, event.currentIndex).subscribe({
      error: (err) => {
        console.error('Failed to move scene', err);
        this.loadBoard(); // Відкочуємо зміни в UI, якщо запит впав
      }
    });
  }

  // --- CRUD Знімальних днів ---
  submitNewDay() {
    this.api.createShootDay(this.projectId, this.newDayForm).subscribe({
      next: () => {
        this.closeModal();
        this.loadBoard(); // Перезавантажуємо дошку з новим днем
      },
      error: (err) => console.error('Error creating shoot day', err)
    });
  }

  deleteDay(dayId: number) {
    if (confirm('Ви впевнені, що хочете видалити цей знімальний день? Усі сцени повернуться в пул.')) {
      this.api.deleteShootDay(this.projectId, dayId).subscribe(() => {
        this.loadBoard();
      });
    }
  }

  openModal() { this.isModalOpen = true; }
  closeModal() { this.isModalOpen = false; }

  // --- Календар ---
  get currentMonthName() { return this.monthNames[this.displayMonth]; }

  generateCalendar() {
    const firstDay = new Date(this.displayYear, this.displayMonth, 1).getDay();
    const daysInMonth = new Date(this.displayYear, this.displayMonth + 1, 0).getDate();

    this.calendarWeeks = [];
    let currentWeek: (number | null)[] = Array(7).fill(null);
    let currentDayOfWeek = firstDay;

    for (let day = 1; day <= daysInMonth; day++) {
      currentWeek[currentDayOfWeek] = day;
      currentDayOfWeek++;

      if (currentDayOfWeek === 7) {
        this.calendarWeeks.push(currentWeek);
        currentWeek = Array(7).fill(null);
        currentDayOfWeek = 0;
      }
    }
    if (currentDayOfWeek > 0) {
      this.calendarWeeks.push(currentWeek);
    }
  }

  prevMonth() {
    if (this.displayMonth === 0) { this.displayMonth = 11; this.displayYear--; } else { this.displayMonth--; }
    this.generateCalendar();
  }

  nextMonth() {
    if (this.displayMonth === 11) { this.displayMonth = 0; this.displayYear++; } else { this.displayMonth++; }
    this.generateCalendar();
  }

  selectDate(day: number | null) {
    if (day !== null) {
      this.selectedDateObj = new Date(this.displayYear, this.displayMonth, day);
      const y = this.selectedDateObj.getFullYear();
      const m = String(this.selectedDateObj.getMonth() + 1).padStart(2, '0');
      const d = String(this.selectedDateObj.getDate()).padStart(2, '0');
      this.newDayForm.shootDate = `${y}-${m}-${d}`;
    }
  }

  isToday(day: number): boolean {
    const today = new Date();
    return day === today.getDate() && this.displayMonth === today.getMonth() && this.displayYear === today.getFullYear();
  }

  isSelected(day: number): boolean {
    if (!this.selectedDateObj) return false;
    return day === this.selectedDateObj.getDate() && this.displayMonth === this.selectedDateObj.getMonth() && this.displayYear === this.selectedDateObj.getFullYear();
  }

  formatTimeInput(event: any) {
    let val = event.target.value.replace(/\D/g, '');
    if (val.length > 2) { val = val.substring(0, 2) + ':' + val.substring(2, 4); }
    event.target.value = val;
  }
}
