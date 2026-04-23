import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class Api {
  private http = inject(HttpClient);
  private readonly USERS_API_URL = 'http://localhost:5214/api/users';
  private readonly PROJECTS_API_URL = 'http://localhost:5214/api/projects';

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

  // PROJECTS
  getGenres(): Observable<any[]> {
    return this.http.get<any[]>(`${this.PROJECTS_API_URL}/genres`);
  }
  createProject(projectData: any): Observable<any> {
    return this.http.post(this.PROJECTS_API_URL, projectData);
  }
}
