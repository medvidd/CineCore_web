import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DragDropModule, CdkDragDrop, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { Api} from '../../../core/services/api';

@Component({
  selector: 'app-planner',
  standalone: true,
  imports: [CommonModule, FormsModule, DragDropModule], // Додаємо DragDropModule
  templateUrl: './planner.html',
  styleUrl: './planner.scss'
})
export class Planner implements OnInit {
  private api = inject(Api);
  private cdr = inject(ChangeDetectorRef);

  currentUserRole: string = 'none';
  canEdit: boolean = false;

  isModalOpen = false;

  newDayForm = {
    unit: '', shootDate: '', callTime: '', shiftStart: '', shiftEnd: '', baseLocation: '', notes: ''
  };

  displayMonth = 0;
  displayYear = 0;
  selectedDateObj: Date | null = null;
  calendarWeeks: (number | null)[][] = [];
  monthNames = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];

  ngOnInit() {
    // Ініціалізуємо поточний місяць
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

  get currentMonthName() {
    return this.monthNames[this.displayMonth];
  }

  generateCalendar() {
    // Шукаємо перший день місяця (0 - Неділя, 1 - Понеділок...)
    const firstDay = new Date(this.displayYear, this.displayMonth, 1).getDay();
    // Кількість днів у місяці
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
    // Якщо тиждень не закінчився, але дні місяця закінчилися, додаємо хвіст
    if (currentDayOfWeek > 0) {
      this.calendarWeeks.push(currentWeek);
    }
  }

  prevMonth() {
    if (this.displayMonth === 0) {
      this.displayMonth = 11;
      this.displayYear--;
    } else {
      this.displayMonth--;
    }
    this.generateCalendar();
  }

  nextMonth() {
    if (this.displayMonth === 11) {
      this.displayMonth = 0;
      this.displayYear++;
    } else {
      this.displayMonth++;
    }
    this.generateCalendar();
  }

  selectDate(day: number | null) {
    if (day !== null) {
      this.selectedDateObj = new Date(this.displayYear, this.displayMonth, day);

      // Форматуємо для бекенду YYYY-MM-DD
      const y = this.selectedDateObj.getFullYear();
      const m = String(this.selectedDateObj.getMonth() + 1).padStart(2, '0');
      const d = String(this.selectedDateObj.getDate()).padStart(2, '0');
      this.newDayForm.shootDate = `${y}-${m}-${d}`;
    }
  }

  // Перевірка чи день є сьогоднішнім (щоб підсвітити)
  isToday(day: number): boolean {
    const today = new Date();
    return day === today.getDate() &&
      this.displayMonth === today.getMonth() &&
      this.displayYear === today.getFullYear();
  }

  // Перевірка чи день вибраний користувачем
  isSelected(day: number): boolean {
    if (!this.selectedDateObj) return false;
    return day === this.selectedDateObj.getDate() &&
      this.displayMonth === this.selectedDateObj.getMonth() &&
      this.displayYear === this.selectedDateObj.getFullYear();
  }

  formatTimeInput(event: any) {
    let val = event.target.value.replace(/\D/g, ''); // Видаляємо все, крім цифр
    if (val.length > 2) {
      val = val.substring(0, 2) + ':' + val.substring(2, 4);
    }
    event.target.value = val;
  }

  openModal() { this.isModalOpen = true; }
  closeModal() { this.isModalOpen = false; }

  // Мокові дані: Пул сцен (ті, що ще не в розкладі)
  scenePool = [
    { id: 'SC-005', title: 'Chase through downtown square', duration: '1h 30m', location: 'Historic Downtown Square', timeOfDay: 'EXT/DAY', cast: ['MC', 'JIR'] },
    { id: 'SC-006', title: 'Interrogation room confrontation', duration: '45m', location: 'Modern Office Building', timeOfDay: 'INT/DAY', cast: ['ER', 'MC'] },
    { id: 'SC-007', title: 'Midnight stakeout', duration: '50m', location: 'Historic Downtown Square', timeOfDay: 'EXT/NIGHT', cast: ['ER', 'MC', 'JIR'] },
    { id: 'SC-008', title: 'Apartment search', duration: '35m', location: 'Vintage Apartment', timeOfDay: 'INT/DAY', cast: [] }
  ];

  // Мокові дані: Знімальні дні (Shoot Days) та їхні сцени (SceneSchedules)
  shootDays = [
    {
      id: 1, date: 'Mar 24', unit: 'MAIN UNIT', status: 'Completed', callTime: '08:00',
      capacityStr: '1h 45m / 10h 0m', capacityPct: 15,
      scenes: [
        { id: 'SC-001', title: 'Captain Rostova discovers the first letter', duration: '1h 0m', location: 'Old Abandoned Factory', timeOfDay: 'INT/NIGHT', cast: ['ER', 'MC'] }
      ]
    },
    {
      id: 2, date: 'Mar 25', unit: 'MAIN UNIT', status: 'Draft', callTime: '09:00',
      capacityStr: '55m / 10h 0m', capacityPct: 10,
      scenes: [
        { id: 'SC-003', title: 'Forensic analysis with Dr. Vance', duration: '30m', location: 'Modern Office Building', timeOfDay: 'INT/DAY', cast: ['ER', 'HV'] },
        { id: 'SC-004', title: 'Meeting with Mayor Stone', duration: '25m', location: 'Modern Office Building', timeOfDay: 'INT/DAY', cast: ['ER', 'VS'] }
      ]
    }
  ];

  // Магія Drag & Drop
  drop(event: CdkDragDrop<any[]>) {
    if (event.previousContainer === event.container) {
      // Перетягування всередині одного списку (просто зміна порядку)
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
      // Тут буде виклик API для оновлення SceneOrder
    } else {
      // Перенесення сцени в інший день або з пулу
      transferArrayItem(
        event.previousContainer.data,
        event.container.data,
        event.previousIndex,
        event.currentIndex,
      );
      // Тут буде виклик API: Створення або оновлення SceneSchedule (встановлення нового ShootDayId)
    }
  }
}
