import { Component, EventEmitter, Input, Output, inject, OnInit, OnChanges, SimpleChanges, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Api } from '../../../core/services/api';

@Component({
  selector: 'app-project-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './project-modal.html',
  styleUrl: './project-modal.scss'
})
export class ProjectModal implements OnInit, OnChanges {
  private api = inject(Api);
  private cdr = inject(ChangeDetectorRef);

  @Input() isOpen = false;
  @Input() mode: 'create' | 'edit' = 'create'; // РЕЖИМ РОБОТИ
  @Input() projectData: any = null; // ДАНІ ПРОЕКТУ ДЛЯ РЕДАГУВАННЯ

  @Output() close = new EventEmitter<void>();
  @Output() projectCreated = new EventEmitter<any>();
  @Output() projectUpdated = new EventEmitter<any>(); // СИГНАЛ ПРО ОНОВЛЕННЯ

  dbGenres: any[] = [];

  formData = {
    title: '',
    synopsis: '',
    startDate: '',
    genreId: ''
  };

  isCustomGenre = false;
  customGenreValue = '';

  // Стани форми
  isLoading = false;
  serverError: string | null = null;

  ngOnInit() {
    this.api.getGenres().subscribe(res => this.dbGenres = res);
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['isOpen'] && this.isOpen) {
      this.resetStates(); // Очищаємо помилки при кожному відкритті

      if (this.mode === 'edit' && this.projectData) {
        this.formData.title = this.projectData.title;
        this.formData.synopsis = this.projectData.synopsis || '';

        // Відрізаємо час від дати, щоб підставити у <input type="date">
        this.formData.startDate = this.projectData.startDate
          ? this.projectData.startDate.split('T')[0]
          : '';

        if (this.projectData.genreIds && this.projectData.genreIds.length > 0) {
          this.formData.genreId = this.projectData.genreIds[0].toString();
          this.isCustomGenre = false;
        } else if (this.projectData.genre && this.projectData.genre !== 'No genre') {
          this.isCustomGenre = true;
          this.customGenreValue = this.projectData.genre;
        }
      } else {
        this.formData = { title: '', synopsis: '', startDate: '', genreId: '' };
        this.isCustomGenre = false;
        this.customGenreValue = '';
      }
    }
  }

  // Обчислення мінімальної дати для HTML <input min="...">
  get minAllowedDate(): string {
    const today = new Date().toISOString().split('T')[0];

    // Якщо це режим редагування і проект ВЖЕ має дату в минулому,
    // дозволяємо зберегти цю ж саму стару дату (щоб форма не блокувалась).
    if (this.mode === 'edit' && this.projectData?.startDate) {
      const existingDate = this.projectData.startDate.split('T')[0];
      return existingDate < today ? existingDate : today;
    }

    // Для нових проектів мінімум — це сьогодні
    return today;
  }

  onGenreChange(event: any) {
    if (event.target.value === 'custom') {
      this.isCustomGenre = true;
      this.formData.genreId = '';
    }
  }

  cancelCustomGenre() {
    this.isCustomGenre = false;
    this.customGenreValue = '';
    this.formData.genreId = '';
  }

  resetStates() {
    this.isLoading = false;
    this.serverError = null;
  }

  // Допоміжна функція для безпечного отримання тексту помилки
  private getErrorMessage(err: any): string {
    if (err.error && err.error.message) {
      return err.error.message;
    }
    if (err.error && err.error.errors) {
      const firstKey = Object.keys(err.error.errors)[0];
      return err.error.errors[firstKey][0];
    }
    if (typeof err.error === 'string') {
      return err.error;
    }
    return "An unknown error occurred. Please check your data.";
  }

  onSubmit(formValid: boolean | null) {
    if (!formValid) return;

    // Додаткова ручна перевірка дати на фронтенді
    if (this.formData.startDate) {
      const selected = new Date(this.formData.startDate);
      const today = new Date();
      today.setHours(0, 0, 0, 0);

      if (selected < today && this.mode === 'create') {
        this.serverError = "Start date cannot be in the past.";
        return;
      }
    }

    const user = JSON.parse(localStorage.getItem('cinecore_user') || '{}');
    if (!user.id) {
      this.serverError = "Error: User is not authorized";
      return;
    }

    this.isLoading = true;
    this.serverError = null;

    // ВАЖЛИВО: Не робимо .toISOString(), відправляємо як є (YYYY-MM-DD),
    // щоб бекенд (DateOnly) зміг це прочитати.
    let finalDate = this.formData.startDate ? this.formData.startDate : null;

    const payload = {
      title: this.formData.title,
      synopsis: this.formData.synopsis || null,
      startDate: finalDate,
      ownerId: user.id,
      genreIds: this.formData.genreId ? [Number(this.formData.genreId)] : [],
      customGenres: this.isCustomGenre && this.customGenreValue
        ? this.customGenreValue.split(',').map(g => g.trim())
        : []
    };

    if (this.mode === 'create') {
      this.api.createProject(payload).subscribe({
        next: (res) => {
          this.projectCreated.emit(res);
          this.closeModal();
        },
        error: (err) => {
          this.isLoading = false;
          this.serverError = this.getErrorMessage(err);
          this.cdr.detectChanges();
        }
      });
    } else {
      this.api.updateProject(this.projectData.id, payload).subscribe({
        next: (res) => {
          this.projectUpdated.emit(res);
          this.closeModal();
        },
        error: (err) => {
          this.isLoading = false;
          this.serverError = this.getErrorMessage(err);
          this.cdr.detectChanges();
        }
      });
    }
  }

  closeModal() {
    this.close.emit();
  }
}
