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

  locations: any[] = [];
  props: any[] = [];

  currentUserRole: string = 'none';
  canEdit: boolean = false;

  searchQuery: string = '';
  filteredLocations: any[] = [];
  filteredProps: any[] = [];

  locationFilters = ['All', 'Interior', 'Exterior', 'Studio'];
  activeLocationFilter = 'All';

  propFilters = ['All', 'Action', 'Scenography', 'Functional'];
  activePropFilter = 'All';

  locationTypes = ['interior', 'exterior', 'studio'];
  propTypes = ['action', 'scenography', 'functional'];
  propStatuses = ['available', 'leased', 'unavailable'];
  acquisitionTypes = ['buy', 'rent'];

  // ==========================================
  // СТАН ДЛЯ LOCATIONS
  // ==========================================
  isLocationModalOpen = false;
  isLocationLoading = false;
  locationServerError: string | null = null;

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
  // СТАН ДЛЯ PROPS
  // ==========================================
  editingPropId: number | null = null;
  isPropLoading = false;
  propServerError: string | null = null;

  propForm = {
    id: null as number | null,
    propName: '',
    description: '',
    propType: 'action',
    acquisitionType: 'buy',
    propStatus: 'available'
  };

  ngOnInit() {
    this.api.currentRole$.subscribe(role => {
      this.currentUserRole = role;
      this.canEdit = (role === 'owner' || role === 'manager');
      this.cdr.detectChanges();
    });

    this.route.parent?.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.projectId = Number(id);
        this.loadData();
      }
    });
  }

  loadData() {
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
  // HELPERS
  // ==========================================
  private getErrorMessage(err: any): string {
    if (err.error && err.error.message) return err.error.message;
    if (err.error && err.error.errors) {
      const firstKey = Object.keys(err.error.errors)[0];
      return err.error.errors[firstKey][0];
    }
    return typeof err.error === 'string' ? err.error : "An unknown error occurred.";
  }

  filterData() {
    const q = this.searchQuery.toLowerCase().trim();

    this.filteredLocations = this.locations.filter(loc => {
      const matchesSearch = !q || loc.name?.toLowerCase().includes(q) || loc.desc?.toLowerCase().includes(q);
      const matchesFilter = this.activeLocationFilter === 'All' || loc.type?.toLowerCase() === this.activeLocationFilter.toLowerCase();
      return matchesSearch && matchesFilter;
    });

    this.filteredProps = this.props.filter(prop => {
      const isNewRow = prop.id === 0;
      const matchesSearch = !q || prop.name?.toLowerCase().includes(q) || prop.desc?.toLowerCase().includes(q);
      const matchesFilter = this.activePropFilter === 'All' || prop.category?.toLowerCase() === this.activePropFilter.toLowerCase();
      return isNewRow || (matchesSearch && matchesFilter);
    });

    this.cdr.detectChanges();
  }

  onLocationFilterChange() { this.filterData(); }
  onPropFilterChange() { this.filterData(); }
  onSearch() { this.loadData(); }
  onFilterChange() { this.loadData(); }

  setTab(tab: 'locations' | 'props') {
    this.activeTab = tab;
    this.propServerError = null; // Очищуємо помилки при перемиканні
    this.loadData();
  }

  openAddResourceModal() {
    if (this.activeTab === 'locations') {
      this.locationForm = { id: null, locationName: '', city: '', street: '', locationType: 'interior', contactName: '', contactPhone: '' };
      this.locationServerError = null;
      this.isLocationLoading = false;
      this.isLocationModalOpen = true;
    } else {
      this.propForm = { id: null, propName: '', description: '', propType: 'action', acquisitionType: 'buy', propStatus: 'available' };
      this.propServerError = null;
      this.isPropLoading = false;
      this.editingPropId = 0;
      if (!this.props.find(p => p.id === 0)) {
        this.props.unshift({ id: 0 });
        this.filterData();
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
    this.locationServerError = null;
    this.isLocationLoading = false;
    this.isLocationModalOpen = true;
  }

  closeLocationModal() {
    this.isLocationModalOpen = false;
  }

  saveLocation(isValid: boolean | null) {
    if (!isValid) return;

    this.isLocationLoading = true;
    this.locationServerError = null;

    const payload = { projectId: this.projectId, ...this.locationForm };

    if (this.locationForm.id) {
      this.api.updateLocation(this.locationForm.id, payload).subscribe({
        next: () => { this.loadData(); this.closeLocationModal(); },
        error: (err) => {
          this.isLocationLoading = false;
          this.locationServerError = this.getErrorMessage(err);
          this.cdr.detectChanges();
        }
      });
    } else {
      this.api.createLocation(payload).subscribe({
        next: () => { this.loadData(); this.closeLocationModal(); },
        error: (err) => {
          this.isLocationLoading = false;
          this.locationServerError = this.getErrorMessage(err);
          this.cdr.detectChanges();
        }
      });
    }
  }

  deleteLocation(id: number) {
    if(confirm('Are you sure you want to delete this location?')) {
      this.locations = this.locations.filter(loc => loc.id !== id);
      this.filterData();

      this.api.deleteLocation(id).subscribe({
        next: () => {},
        error: (err) => {
          console.error(err);
          this.loadData();
        }
      });
    }
  }

  // ==========================================
  // PROPS CRUD
  // ==========================================
  editProp(prop: any) {
    this.props = this.props.filter(p => p.id !== 0);
    this.filterData();

    this.propServerError = null;
    this.isPropLoading = false;
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
    this.propServerError = null;
    this.props = this.props.filter(p => p.id !== 0);
    this.filterData();
  }

  saveProp(isValid: boolean | null) {
    if (!isValid) return;

    this.isPropLoading = true;
    this.propServerError = null;

    const payload = { projectId: this.projectId, ...this.propForm };

    if (this.propForm.id) {
      this.api.updateProp(this.propForm.id, payload).subscribe({
        next: () => { this.editingPropId = null; this.loadData(); },
        error: (err) => {
          this.isPropLoading = false;
          this.propServerError = this.getErrorMessage(err);
          this.cdr.detectChanges();
        }
      });
    } else {
      this.api.createProp(payload).subscribe({
        next: () => { this.editingPropId = null; this.loadData(); },
        error: (err) => {
          this.isPropLoading = false;
          this.propServerError = this.getErrorMessage(err);
          this.cdr.detectChanges();
        }
      });
    }
  }

  deleteProp(id: number) {
    if(confirm('Are you sure you want to delete this prop?')) {
      this.props = this.props.filter(prop => prop.id !== id);
      this.filterData();

      this.api.deleteProp(id).subscribe({
        next: () => {},
        error: (err) => {
          console.error(err);
          this.loadData();
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
