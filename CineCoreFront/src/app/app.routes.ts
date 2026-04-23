import { Routes } from '@angular/router';
import { Landing } from './features/landing/landing';
import { HowToUse } from './features/how-to-use/how-to-use';
import { Login } from './features/auth/login/login';
import { Signup } from './features/auth/signup/signup';
import { Account } from './features/account/account';
import { Projects } from './features/projects/projects';

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
    path: 'login',
    component: Login,
    title: 'CineCore | Log In'
  },
  {
    path: 'signup',
    component: Signup,
    title: 'CineCore | Sign Up'
  },
  {
    path: 'account',
    component: Account,
    title: 'CineCore | My Account'
  },
  {
    path: 'projects',
    component: Projects,
    title: 'CineCore | My Projects'
  },
  {
    path: '**',
    redirectTo: ''
  }
];
