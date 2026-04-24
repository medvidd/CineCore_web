import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-crew',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './crew.html',
  styleUrl: './crew.scss'
})
export class Crew {
  // Керування вкладками
  activeTab: 'active' | 'pending' = 'active';

  // Мокові дані
  activeMembers = [
    { initials: 'AT', name: 'Alex Thompson', email: 'alex.thompson@example.com', sysRole: 'Owner', jobTitle: 'Director', dept: 'Direction', joined: 'Jan 15, 2026' },
    { initials: 'SM', name: 'Sarah Miller', email: 'sarah.miller@example.com', sysRole: 'Manager', jobTitle: 'Producer', dept: 'Production', joined: 'Feb 2, 2026' },
    { initials: 'MA', name: 'Michael Actor', email: 'michael.actor@example.com', sysRole: 'Actor', jobTitle: 'Lead Actor', dept: 'Cast', joined: 'Feb 11, 2026' },
    { initials: 'JC', name: 'John Camera', email: 'john.camera@example.com', sysRole: 'Crew', jobTitle: 'Director of Photography', dept: 'Camera', joined: 'Feb 16, 2026' }
  ];

  pendingInvites = [
    { email: 'david.grip@example.com', sysRole: 'Crew', jobTitle: 'Key Grip', dept: 'Camera', invitedBy: 'Alex Thompson', dateSent: 'Mar 15, 2026' },
    { email: 'laura.costume@example.com', sysRole: 'Crew', jobTitle: 'Costume Designer', dept: 'Wardrobe', invitedBy: 'Sarah Miller', dateSent: 'Mar 18, 2026' },
    { email: 'robert.editor@example.com', sysRole: 'Crew', jobTitle: 'Film Editor', dept: 'Post-Production', invitedBy: 'Alex Thompson', dateSent: 'Mar 20, 2026' }
  ];

  // Метод для визначення класу кольору ролі
  getRoleClass(role: string): string {
    switch (role) {
      case 'Owner': return 'role-owner';
      case 'Manager': return 'role-manager';
      case 'Actor': return 'role-actor';
      case 'Crew': return 'role-crew';
      default: return 'role-crew';
    }
  }
}
