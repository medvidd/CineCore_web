import { Component, EventEmitter, Input, Output, inject, OnInit, OnChanges, SimpleChanges } from '@angular/core';
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

  ngOnInit() {
    this.api.getGenres().subscribe(res => this.dbGenres = res);
  }

  // Відстежуємо зміни, щоб підставити дані при відкритті вікна
  ngOnChanges(changes: SimpleChanges) {
    if (changes['isOpen'] && this.isOpen) {
      if (this.mode === 'edit' && this.projectData) {
        this.formData.title = this.projectData.title;
        this.formData.synopsis = this.projectData.synopsis || '';
        this.formData.startDate = this.projectData.startDate || '';

        // Підстановка жанру
        if (this.projectData.genreIds && this.projectData.genreIds.length > 0) {
          this.formData.genreId = this.projectData.genreIds[0].toString();
          this.isCustomGenre = false;
        } else if (this.projectData.genre && this.projectData.genre !== 'No genre') {
          this.isCustomGenre = true;
          this.customGenreValue = this.projectData.genre;
        }
      } else {
        // Очищаємо для режиму створення
        this.formData = { title: '', synopsis: '', startDate: '', genreId: '' };
        this.isCustomGenre = false;
        this.customGenreValue = '';
      }
    }
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
    const user = JSON.parse(localStorage.getItem('cinecore_user') || '{}');
    if (!user.id) {
      alert("Error: User is not authorized");
      return;
    }

    const payload = {
      title: this.formData.title,
      synopsis: this.formData.synopsis || null,
      startDate: this.formData.startDate || null,
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
          console.error(err);
          alert("Failed to create the project.");
        }
      });
    } else {
      this.api.updateProject(this.projectData.id, payload).subscribe({
        next: (res) => {
          this.projectUpdated.emit(res);
          this.closeModal();
        },
        error: (err) => {
          console.error(err);
          alert("Failed to update the project.");
        }
      });
    }
  }

  closeModal() {
    this.close.emit();
  }
}
