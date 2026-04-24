import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms'; // Обов'язково для [(ngModel)]
import { Api } from '../../../core/services/api';

@Component({
  selector: 'app-resources',
  standalone: true,
  imports: [CommonModule, FormsModule], // Додано FormsModule
  templateUrl: './resources.html',
  styleUrl: './resources.scss'
})
export class Resources implements OnInit {
  private api = inject(Api);

  activeTab: 'locations' | 'props' = 'locations';
  projectId: number = 1; // Замініть на реальне отримання ID проекту

  locations: any[] = [];
  props: any[] = [];

  // ==========================================
  // ENUMS З БЕКЕНДУ
  // ==========================================
  locationTypes = ['interior', 'exterior', 'studio'];
  propTypes = ['action', 'scenography', 'functional'];
  propStatuses = ['available', 'leased', 'unavailable'];
  acquisitionTypes = ['buy', 'rent'];

  // ==========================================
  // СТАН ДЛЯ LOCATIONS (Модалка)
  // ==========================================
  isLocationModalOpen = false;
  locationForm = {
    id: null as number | null,
    locationName: '',
    city: '',
    street: '',
    locationType: 'interior',
    contactName: '',
    contactPhone: ''
  };

  // ==========================================
  // СТАН ДЛЯ PROPS (Inline Table Editing)
  // ==========================================
  editingPropId: number | null = null; // null - нічого не редагується, 0 - новий рядок
  propForm = {
    id: null as number | null,
    propName: '',
    description: '',
    propType: 'action',
    acquisitionType: 'buy',
    propStatus: 'available'
  };

  ngOnInit() {
    this.loadData();
  }

  loadData() {
    this.api.getLocationsByProject(this.projectId).subscribe({
      next: (data) => {
        this.locations = data.map(loc => ({
          ...loc,
          typeColor: this.getColorForLocationType(loc.type)
        }));
      },
      error: (err) => console.error('Failed to load locations', err)
    });

    this.api.getPropsByProject(this.projectId).subscribe({
      next: (data) => {
        this.props = data.map(prop => ({
          ...prop,
          categoryColor: this.getColorForCategory(prop.category),
          statusColor: this.getColorForStatus(prop.status)
        }));
      },
      error: (err) => console.error('Failed to load props', err)
    });
  }

  // ГОЛОВНА КНОПКА "ADD RESOURCE"
  openAddResourceModal() {
    if (this.activeTab === 'locations') {
      this.locationForm = { id: null, locationName: '', city: '', street: '', locationType: 'interior', contactName: '', contactPhone: '' };
      this.isLocationModalOpen = true;
    } else {
      // Додаємо тимчасовий порожній об'єкт в масив для відображення рядка введення
      this.propForm = { id: null, propName: '', description: '', propType: 'action', acquisitionType: 'buy', propStatus: 'available' };
      this.editingPropId = 0; // 0 означає "Новий"

      // Перевіряємо, чи вже немає відкритого рядка створення
      if (!this.props.find(p => p.id === 0)) {
        this.props.unshift({ id: 0 }); // Додаємо на початок таблиці
      }
    }
  }

  // ==========================================
  // LOCATIONS CRUD
  // ==========================================
  closeLocationModal() {
    this.isLocationModalOpen = false;
  }

  saveLocation() {
    const payload = {
      projectId: this.projectId,
      ...this.locationForm
    };

    if (this.locationForm.id) {
      // Оновлення (якщо захочете додати кнопку Edit для локації)
      this.api.updateLocation(this.locationForm.id, payload).subscribe(() => {
        this.loadData();
        this.closeLocationModal();
      });
    } else {
      // Створення
      this.api.createLocation(payload).subscribe(() => {
        this.loadData();
        this.closeLocationModal();
      });
    }
  }

  deleteLocation(id: number) {
    if(confirm('Are you sure you want to delete this location?')) {
      this.api.deleteLocation(id).subscribe(() => this.loadData());
    }
  }

  // ==========================================
  // PROPS CRUD (Inline)
  // ==========================================
  editProp(prop: any) {
    // Якщо був інший незбережений новий рядок, видаляємо його
    this.props = this.props.filter(p => p.id !== 0);

    this.editingPropId = prop.id;
    this.propForm = {
      id: prop.id,
      propName: prop.name,
      description: prop.desc,
      propType: prop.category || 'action',
      acquisitionType: prop.acquisition || 'buy',
      propStatus: prop.status || 'available'
    };
  }

  cancelPropEdit() {
    this.editingPropId = null;
    this.props = this.props.filter(p => p.id !== 0); // Видаляємо рядок "нового", якщо скасували створення
  }

  saveProp() {
    const payload = {
      projectId: this.projectId,
      ...this.propForm
    };

    if (this.propForm.id) {
      // Оновлення
      this.api.updateProp(this.propForm.id, payload).subscribe(() => {
        this.editingPropId = null;
        this.loadData();
      });
    } else {
      // Створення
      this.api.createProp(payload).subscribe(() => {
        this.editingPropId = null;
        this.loadData();
      });
    }
  }

  deleteProp(id: number) {
    if(confirm('Are you sure you want to delete this prop?')) {
      this.api.deleteProp(id).subscribe(() => this.loadData());
    }
  }

  // ==========================================
  // HELPERS ДЛЯ КОЛЬОРІВ
  // ==========================================
  getColorForLocationType(type: string): string {
    const t = (type || '').toLowerCase();
    if (t === 'interior') return 'blue';
    if (t === 'exterior') return 'green';
    return 'purple'; // studio
  }

  getColorForStatus(status: string): string {
    const s = (status || '').toLowerCase();
    if (s === 'available') return 'green';
    if (s === 'leased') return 'yellow';
    return 'red'; // unavailable
  }

  getColorForCategory(category: string): string {
    const c = (category || '').toLowerCase();
    if (c === 'action') return 'red';
    if (c === 'scenography') return 'blue';
    return 'purple'; // functional
  }
}
