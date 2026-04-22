import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class Api {
  private http = inject(HttpClient);
  // Вказуємо адресу вашого C# бекенду (з профілю http)
  private readonly API_URL = 'http://localhost:5214/api/users';

  login(credentials: any): Observable<any> {
    return this.http.post(`${this.API_URL}/login`, credentials);
  }

  register(userData: any): Observable<any> {
    return this.http.post(`${this.API_URL}/register`, userData);
  }

  getUserProfile(id: number): Observable<any> {
    return this.http.get(`${this.API_URL}/${id}`);
  }
}
