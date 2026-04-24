import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Api } from '../../../core/services/api';

@Component({
  selector: 'app-resources',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './resources.html',
  styleUrl: './resources.scss'
})
export class Resources implements OnInit {
  private api = inject(Api);
  private route = inject(ActivatedRoute);
  private cdr = inject(ChangeDetectorRef);

  activeTab: 'locations' | 'props' = 'locations';
  projectId: number = 0;

  // Основні масиви з БД
  locations: any[] = [];
  props: any[] = [];

  // ==========================================
  // ПОШУК ТА ФІЛЬТРАЦІЯ
  // ==========================================
  searchQuery: string = '';
  filteredLocations: any[] = [];
  filteredProps: any[] = [];

  locationFilters = ['All', 'Interior', 'Exterior', 'Studio'];
  activeLocationFilter = 'All';

  propFilters = ['All', 'Action', 'Scenography', 'Functional'];
  activePropFilter = 'All';

  // ENUMS З БЕКЕНДУ ДЛЯ ФОРМ
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
  editingPropId: number | null = null;
  propForm = {
    id: null as number | null,
    propName: '',
    description: '',
    propType: 'action',
    acquisitionType: 'buy',
    propStatus: 'available'
  };

  ngOnInit() {
    this.route.parent?.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.projectId = Number(id);
        this.loadData();
      }
    });
  }

  loadData() {
    // Завантажуємо локації (з урахуванням фільтрів та пошуку саме для локацій)
    this.api.getLocationsByProject(this.projectId, this.activeLocationFilter, this.searchQuery)
      .subscribe({
        next: (data) => {
          this.locations = data.map(loc => ({
            ...loc,
            typeColor: this.getColorForLocationType(loc.type)
          }));
          this.cdr.detectChanges();
        },
        error: (err) => console.error('Failed to load locations', err)
      });

    // Завантажуємо реквізит (з урахуванням фільтрів та пошуку саме для реквізиту)
    this.api.getPropsByProject(this.projectId, this.activePropFilter, this.searchQuery)
      .subscribe({
        next: (data) => {
          this.props = data.map(prop => ({
            ...prop,
            categoryColor: this.getColorForCategory(prop.category),
            statusColor: this.getColorForStatus(prop.status)
          }));
          this.cdr.detectChanges();
        },
        error: (err) => console.error('Failed to load props', err)
      });
  }

  // ==========================================
  // КОМПЛЕКСНА ФІЛЬТРАЦІЯ (ПОШУК + DROPDOWN)
  // ==========================================
  filterData() {
    const q = this.searchQuery.toLowerCase().trim();

    // Фільтруємо ЛОКАЦІЇ
    this.filteredLocations = this.locations.filter(loc => {
      // 1. Умова пошуку (співпадає ім'я АБО опис)
      const matchesSearch = !q ||
        loc.name?.toLowerCase().includes(q) ||
        loc.desc?.toLowerCase().includes(q);

      // 2. Умова випадаючого списку (тип приміщення)
      const matchesFilter = this.activeLocationFilter === 'All' ||
        loc.type?.toLowerCase() === this.activeLocationFilter.toLowerCase();

      // Локація залишається, тільки якщо виконуються ОБИДВІ умови
      return matchesSearch && matchesFilter;
    });

    // Фільтруємо РЕКВІЗИТ (PROPS)
    this.filteredProps = this.props.filter(prop => {
      const isNewRow = prop.id === 0; // Завжди показуємо рядок створення, якщо він відкритий

      const matchesSearch = !q ||
        prop.name?.toLowerCase().includes(q) ||
        prop.desc?.toLowerCase().includes(q);

      const matchesFilter = this.activePropFilter === 'All' ||
        prop.category?.toLowerCase() === this.activePropFilter.toLowerCase();

      return isNewRow || (matchesSearch && matchesFilter);
    });

    this.cdr.detectChanges(); // Примусово оновлюємо UI після фільтрації
  }

  onLocationFilterChange() {
    this.filterData();
  }

  onPropFilterChange() {
    this.filterData();
  }

  onSearch() {
    this.loadData();
  }

  onFilterChange() {
    this.loadData();
  }

  // При зміні вкладок теж оновлюємо дані
  setTab(tab: 'locations' | 'props') {
    this.activeTab = tab;
    this.loadData();
  }

  // ==========================================
  // ГОЛОВНА КНОПКА "ADD RESOURCE"
  // ==========================================
  openAddResourceModal() {
    if (this.activeTab === 'locations') {
      this.locationForm = { id: null, locationName: '', city: '', street: '', locationType: 'interior', contactName: '', contactPhone: '' };
      this.isLocationModalOpen = true;
    } else {
      this.propForm = { id: null, propName: '', description: '', propType: 'action', acquisitionType: 'buy', propStatus: 'available' };
      this.editingPropId = 0;
      if (!this.props.find(p => p.id === 0)) {
        this.props.unshift({ id: 0 });
        this.filterData(); // Оновлюємо таблицю, щоб показати новий порожній рядок
      }
    }
  }

  // ==========================================
  // LOCATIONS CRUD
  // ==========================================
  editLocation(loc: any) {
    this.locationForm = {
      id: loc.id,
      locationName: loc.name,
      city: loc.city || '',
      street: loc.street || '',
      locationType: loc.type ? loc.type.toLowerCase() : 'interior',
      contactName: loc.manager || '',
      contactPhone: loc.phone || ''
    };
    this.isLocationModalOpen = true;
  }

  closeLocationModal() {
    this.isLocationModalOpen = false;
  }

  saveLocation() {
    const payload = { projectId: this.projectId, ...this.locationForm };

    if (this.locationForm.id) {
      this.api.updateLocation(this.locationForm.id, payload).subscribe({
        next: () => { this.loadData(); this.closeLocationModal(); },
        error: (err) => alert(err.error?.message || 'Error updating location')
      });
    } else {
      this.api.createLocation(payload).subscribe({
        next: () => { this.loadData(); this.closeLocationModal(); },
        error: (err) => alert(err.error?.message || 'Error creating location. Name might exist.')
      });
    }
  }

  deleteLocation(id: number) {
    if(confirm('Are you sure you want to delete this location?')) {
      this.locations = this.locations.filter(loc => loc.id !== id);
      this.filterData(); // Миттєве оновлення UI

      this.api.deleteLocation(id).subscribe({
        next: () => {},
        error: (err) => {
          console.error(err);
          this.loadData(); // Повертаємо у разі помилки
        }
      });
    }
  }

  // ==========================================
  // PROPS CRUD (Inline)
  // ==========================================
  editProp(prop: any) {
    this.props = this.props.filter(p => p.id !== 0);
    this.filterData();

    this.editingPropId = prop.id;
    this.propForm = {
      id: prop.id,
      propName: prop.name,
      description: prop.desc,
      propType: prop.category ? prop.category.toLowerCase() : 'action',
      acquisitionType: prop.acquisition ? prop.acquisition.toLowerCase() : 'buy',
      propStatus: prop.status ? prop.status.toLowerCase() : 'available'
    };
  }

  cancelPropEdit() {
    this.editingPropId = null;
    this.props = this.props.filter(p => p.id !== 0);
    this.filterData();
  }

  saveProp() {
    const payload = { projectId: this.projectId, ...this.propForm };

    if (this.propForm.id) {
      this.api.updateProp(this.propForm.id, payload).subscribe({
        next: () => { this.editingPropId = null; this.loadData(); },
        error: (err) => alert(err.error?.message || 'Error updating prop')
      });
    } else {
      this.api.createProp(payload).subscribe({
        next: () => { this.editingPropId = null; this.loadData(); },
        error: (err) => alert(err.error?.message || 'Error creating prop. Name might exist.')
      });
    }
  }

  deleteProp(id: number) {
    if(confirm('Are you sure you want to delete this prop?')) {
      this.props = this.props.filter(prop => prop.id !== id);
      this.filterData(); // Миттєве оновлення UI

      this.api.deleteProp(id).subscribe({
        next: () => {},
        error: (err) => {
          console.error(err);
          this.loadData(); // Повертаємо у разі помилки
        }
      });
    }
  }

  // ==========================================
  // HELPERS ДЛЯ КОЛЬОРІВ
  // ==========================================
  getColorForLocationType(type: string): string {
    const t = (type || '').toLowerCase();
    if (t === 'interior') return 'blue';
    if (t === 'exterior') return 'green';
    return 'purple';
  }

  getColorForStatus(status: string): string {
    const s = (status || '').toLowerCase();
    if (s === 'available') return 'green';
    if (s === 'leased') return 'yellow';
    return 'red';
  }

  getColorForCategory(category: string): string {
    const c = (category || '').toLowerCase();
    if (c === 'action') return 'red';
    if (c === 'scenography') return 'blue';
    return 'purple';
  }
}
