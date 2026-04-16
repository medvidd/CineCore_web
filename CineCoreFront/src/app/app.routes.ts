import { Routes } from '@angular/router';
import { Landing } from './features/landing/landing';
import { HowToUse } from './features/how-to-use/how-to-use';

export const routes: Routes = [
  {
    path: '',
    component: Landing,
    title: 'CineCore | Pre-Production Management'
  },
  {
    path: 'how-to-use', // Адреса нової сторінки
    component: HowToUse,
    title: 'CineCore | How to Use'
  },
  {
    path: '**',
    redirectTo: ''
  }
];
