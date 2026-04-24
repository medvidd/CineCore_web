import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-resources',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './resources.html',
  styleUrl: './resources.scss'
})
export class Resources {
  // Керування вкладками
  activeTab: 'locations' | 'props' = 'locations';

  // Мокові дані для локацій
  locations = [
    { name: 'Old Police Station', desc: '125 Main Street, Downtown', type: 'Approved', typeColor: 'green', manager: 'John Peterson', phone: '+1 555 123-4567', usage: '12 scenes' },
    { name: 'Riverside Park', desc: 'River Road, North District', type: 'Approved', typeColor: 'green', manager: 'Sarah Mitchell', phone: '+1 555 234-5678', usage: '5 scenes' },
    { name: 'Downtown Alley', desc: 'Between 5th & 6th Street', type: 'Approved', typeColor: 'green', manager: 'Lisa Anderson', phone: '+1 555 456-7890', usage: '3 scenes' },
    { name: 'Historic Manor House', desc: '45 Oak Avenue, Suburbs', type: 'Scouting', typeColor: 'yellow', manager: 'Michael Brown', phone: '+1 555 345-6789', usage: '8 scenes' },
    { name: 'City Hall', desc: '1 Government Plaza', type: 'Unavailable', typeColor: 'red', manager: 'Robert Chen', phone: '+1 555 567-8901', usage: '0 scenes' }
  ];

  // Мокові дані для реквізиту (Props)
  props = [
    { name: 'Vintage Pistol (Replica)', desc: '1940s-style police revolver, fully functional replica with blanks capability', category: 'Action', categoryColor: 'red', acquisition: 'Rent', price: '$250', status: 'Available', statusColor: 'green', scenes: 8 },
    { name: 'Detective\'s Desk', desc: 'Authentic wooden desk from the 1950s with drawers and vintage details', category: 'Scenography', categoryColor: 'blue', acquisition: 'Buy', price: '$1200', status: 'Leased', statusColor: 'yellow', scenes: 15 },
    { name: 'Crime Scene Tape', desc: 'Authentic police barrier tape, 500 feet', category: 'Scenography', categoryColor: 'blue', acquisition: 'Buy', price: '$45', status: 'Available', statusColor: 'green', scenes: 4 },
    { name: 'Police Car Light Bar', desc: 'Functional red and blue emergency lights for vehicle mounting', category: 'Functional', categoryColor: 'purple', acquisition: 'Rent', price: '$380', status: 'Available', statusColor: 'green', scenes: 5 },
    { name: 'Case Files Stack', desc: 'Set of 50 vintage-looking police case files and folders', category: 'Scenography', categoryColor: 'blue', acquisition: 'Buy', price: '$95', status: 'Available', statusColor: 'green', scenes: 12 }
  ];
}
