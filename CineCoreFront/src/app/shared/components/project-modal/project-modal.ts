import { Component, EventEmitter, Input, Output, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Api } from '../../../core/services/api';

@Component({
  selector: 'app-project-modal',
  standalone: true,
  imports: [CommonModule, FormsModule], // Обов'язково додаємо FormsModule
  templateUrl: './project-modal.html',
  styleUrl: './project-modal.scss'
})
export class ProjectModal implements OnInit {
  private api = inject(Api);

  @Input() isOpen = false; // Керує тим, чи показувати вікно
  @Output() close = new EventEmitter<void>(); // Сигнал для закриття вікна
  @Output() projectCreated = new EventEmitter<any>(); // Сигнал про успішне створення

  dbGenres: any[] = []; // Сюди завантажаться жанри з БД

  formData = {
    title: '',
    synopsis: '',
    startDate: '',
    genreId: ''
  };

  isCustomGenre = false;
  customGenreValue = '';

  ngOnInit() {
    // Завантажуємо жанри при ініціалізації компоненти
    this.api.getGenres().subscribe(res => this.dbGenres = res);
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

  onSubmit() {
    // Дістаємо ID поточного користувача
    const user = JSON.parse(localStorage.getItem('cinecore_user') || '{}');
    if (!user.id) {
      alert("Error: User is not authorized");
      return;
    }

    // Формуємо об'єкт ТОЧНО ТАК, ЯК ОЧІКУЄ C# DTO
    const payload = {
      title: this.formData.title,
      synopsis: this.formData.synopsis || null,
      startDate: this.formData.startDate || null,
      ownerId: user.id,
      // Якщо вибрали з випадного списку - масив з одним числом, інакше порожній
      genreIds: this.formData.genreId ? [Number(this.formData.genreId)] : [],
      // Якщо ввели вручну - розділяємо по комі, інакше порожній
      customGenres: this.isCustomGenre && this.customGenreValue
        ? this.customGenreValue.split(',').map(g => g.trim())
        : []
    };

    console.log("Sending to the backend:", payload);

    this.api.createProject(payload).subscribe({
      next: (res) => {
        console.log("The project has been created!", res);
        this.projectCreated.emit(res); // Повідомляємо батьківську компоненту
        this.closeModal(); // Закриваємо вікно
      },
      error: (err) => {
        console.error("Creation error:", err);
        alert("Failed to create the project.");
      }
    });
  }

  closeModal() {
    // Очищаємо форму
    this.formData = { title: '', synopsis: '', startDate: '', genreId: '' };
    this.isCustomGenre = false;
    this.customGenreValue = '';
    this.close.emit(); // Сигналізуємо батькові сховати вікно
  }
}
