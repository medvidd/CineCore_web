import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class Api {
  private http = inject(HttpClient);
  private readonly USERS_API_URL = 'http://localhost:5214/api/users';
  private readonly PROJECTS_API_URL = 'http://localhost:5214/api/projects';

  private userSubject = new BehaviorSubject<any>(JSON.parse(localStorage.getItem('cinecore_user') || 'null'));
  user$ = this.userSubject.asObservable();
  updateUserStream(user: any) {
    this.userSubject.next(user);
  }

  // USERS
  login(credentials: any): Observable<any> {
    return this.http.post(`${this.USERS_API_URL}/login`, credentials);
  }
  register(userData: any): Observable<any> {
    return this.http.post(`${this.USERS_API_URL}/register`, userData);
  }
  getUserProfile(id: number): Observable<any> {
    return this.http.get(`${this.USERS_API_URL}/${id}`);
  }
  updateUserProfile(id: number, userData: any): Observable<any> {
    return this.http.put(`${this.USERS_API_URL}/${id}`, userData);
  }
  updateUserPassword(id: number, passwordData: any): Observable<any> {
    return this.http.put(`${this.USERS_API_URL}/${id}/password`, passwordData);
  }
  deleteUserAccount(id: number): Observable<any> {
    return this.http.delete(`${this.USERS_API_URL}/${id}`);
  }

  // PROJECTS
  getGenres(): Observable<any[]> {
    return this.http.get<any[]>(`${this.PROJECTS_API_URL}/genres`);
  }
  createProject(projectData: any): Observable<any> {
    return this.http.post(this.PROJECTS_API_URL, projectData);
  }
  getUserProjects(ownerId: number, role: string = 'All'): Observable<any[]> {
    // Якщо вибрано All, взагалі не чіпляємо параметр - бекенд підставить його сам
    const url = role === 'All'
      ? `${this.PROJECTS_API_URL}/user/${ownerId}`
      : `${this.PROJECTS_API_URL}/user/${ownerId}?role=${encodeURIComponent(role)}`;

    return this.http.get<any[]>(url);
  }

  deleteProject(projectId: number): Observable<any> {
    return this.http.delete(`${this.PROJECTS_API_URL}/${projectId}`);
  }

}
