import { Routes } from '@angular/router';
import { Landing } from './features/landing/landing';
import { HowToUse } from './features/how-to-use/how-to-use';
import { Login } from './features/auth/login/login';
import { Signup } from './features/auth/signup/signup';
import { Account } from './features/account/account';
import { Projects } from './features/projects/projects';
import { WorkspaceLayout } from './features/workspace/workspace-layout/workspace-layout';
import { Dashboard } from './features/workspace/dashboard/dashboard';
import { Crew } from './features/workspace/crew/crew';
import { Resources } from './features/workspace/resources/resources';
import { Casting } from './features/workspace/casting/casting';

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
    path: 'workspace/:id',
    component: WorkspaceLayout,
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', component: Dashboard, title: 'CineCore | Dashboard' },
      { path: 'crew', component: Crew, title: 'CineCore | Crew' },
      { path: 'resources', component: Resources, title: 'CineCore | Resources' },
      { path: 'casting', component: Casting, title: 'CineCore | Casting' },
    ]
  },
  {
    path: '**',
    redirectTo: ''
  }
];
