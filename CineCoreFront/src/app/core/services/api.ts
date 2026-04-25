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
  private readonly LOCATIONS_API_URL = 'http://localhost:5214/api/locations';
  private readonly PROPS_API_URL = 'http://localhost:5214/api/props';
  private readonly CREW_API_URL = 'http://localhost:5214/api/crew';
  private readonly SCRIPT_API_URL = 'http://localhost:5214/api/script';
  private readonly ACTORS_API_URL = 'http://localhost:5214/api/actors';

  private roleSubject = new BehaviorSubject<string>('none');
  currentRole$ = this.roleSubject.asObservable();

  setProjectRole(role: string) {
    this.roleSubject.next(role);
  }

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
  getProjectById(projectId: number): Observable<any> {
    return this.http.get(`${this.PROJECTS_API_URL}/${projectId}`);
  }
  getProjectRole(projectId: number, userId: number): Observable<any> {
    return this.http.get(`${this.PROJECTS_API_URL}/${projectId}/role/${userId}`);
  }


  // LOCATIONS
  getLocationsByProject(projectId: number, type: string = 'All', search: string = ''): Observable<any[]> {
    const url = `${this.LOCATIONS_API_URL}/project/${projectId}?type=${type}&search=${encodeURIComponent(search)}`;
    return this.http.get<any[]>(url);
  }

  createLocation(locationData: any): Observable<any> {
    return this.http.post(this.LOCATIONS_API_URL, locationData);
  }

  updateLocation(id: number, locationData: any): Observable<any> {
    return this.http.put(`${this.LOCATIONS_API_URL}/${id}`, locationData);
  }

  deleteLocation(id: number): Observable<any> {
    return this.http.delete(`${this.LOCATIONS_API_URL}/${id}`);
  }



  // PROPS
  getPropsByProject(projectId: number, category: string = 'All', search: string = ''): Observable<any[]> {
    const url = `${this.PROPS_API_URL}/project/${projectId}?category=${category}&search=${encodeURIComponent(search)}`;
    return this.http.get<any[]>(url);
  }

  createProp(propData: any): Observable<any> {
    return this.http.post(this.PROPS_API_URL, propData);
  }

  updateProp(id: number, propData: any): Observable<any> {
    return this.http.put(`${this.PROPS_API_URL}/${id}`, propData);
  }

  deleteProp(id: number): Observable<any> {
    return this.http.delete(`${this.PROPS_API_URL}/${id}`);
  }

  // CREW
  getProjectCrew(projectId: number): Observable<any> {
    return this.http.get<any>(`${this.CREW_API_URL}/project/${projectId}`);
  }

  searchUserByEmail(email: string): Observable<any> {
    return this.http.get<any>(`${this.CREW_API_URL}/search-user?email=${encodeURIComponent(email)}`);
  }

  inviteProjectMember(inviteData: any): Observable<any> {
    return this.http.post(this.CREW_API_URL + '/invite', inviteData);
  }

  // Відправляє PUT-запит на редагування
  updateProjectMember(projectId: number, targetUserId: number, currentUserId: number, data: any) {
    return this.http.put(`${this.CREW_API_URL}/project/${projectId}/member/${targetUserId}?currentUserId=${currentUserId}`, data);
  }

  removeProjectMember(projectId: number, targetUserId: number, currentUserId: number) {
    return this.http.delete(`${this.CREW_API_URL}/project/${projectId}/member/${targetUserId}?currentUserId=${currentUserId}`);
  }
  deleteProjectInvite(inviteId: number) {
    return this.http.delete(`${this.CREW_API_URL}/invites/${inviteId}`);
  }


  // SCRIPT ENDPOINTS
  getSceneScript(sceneId: number) {
    return this.http.get<any[]>(`${this.SCRIPT_API_URL}/scene/${sceneId}`);
  }
  autoSaveScript(sceneId: number, projectId: number, blocks: any[]) {
    const payload = {
      projectId: projectId,
      blocks: blocks.map(b => ({ type: b.type, content: b.content }))
    };
    return this.http.post(`${this.SCRIPT_API_URL}/scene/${sceneId}/autosave`, payload);
  }
  updateSceneNotes(sceneId: number, notes: string) {
    return this.http.put(`${this.SCRIPT_API_URL}/scene/${sceneId}/notes`, JSON.stringify(notes), {
      headers: { 'Content-Type': 'application/json' }
    });
  }
  getProjectScenes(projectId: number) {
    return this.http.get<any[]>(`${this.SCRIPT_API_URL}/project/${projectId}/scenes`);
  }
  reorderScenes(projectId: number, orderedIds: number[]) {
    return this.http.put(`${this.SCRIPT_API_URL}/project/${projectId}/scenes/reorder`, orderedIds);
  }
  createScene(projectId: number) {
    return this.http.post<any>(`${this.SCRIPT_API_URL}/project/${projectId}/scene`, {});
  }
  getFullScript(projectId: number) {
    return this.http.get<any[]>(`${this.SCRIPT_API_URL}/project/${projectId}/full`);
  }
  deleteScene(sceneId: number) {
    return this.http.delete(`${this.SCRIPT_API_URL}/scene/${sceneId}`);
  }

  getProjectResources(projectId: number) {
    return this.http.get<any>(`${this.SCRIPT_API_URL}/project/${projectId}/resources`);
  }
  linkResource(sceneId: number, resourceId: number) {
    return this.http.post(`${this.SCRIPT_API_URL}/scene/${sceneId}/resource/${resourceId}`, {});
  }
  unlinkResource(sceneId: number, resourceId: number) {
    return this.http.delete(`${this.SCRIPT_API_URL}/scene/${sceneId}/resource/${resourceId}`);
  }
  updateRoleColor(roleId: number, colorHex: string) {
    return this.http.put(`${this.SCRIPT_API_URL}/role/${roleId}/color`, JSON.stringify(colorHex), { headers: { 'Content-Type': 'application/json' } });
  }
  quickCreateResource(projectId: number, type: string, name: string) {
    return this.http.post<any>(`${this.SCRIPT_API_URL}/project/${projectId}/${type}`, { name });
  }


  // CASTING
  getProjectRoles(projectId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.PROJECTS_API_URL}/${projectId}/roles`);
  }

  createRole(projectId: number, payload: any): Observable<any> {
    return this.http.post<any>(`${this.PROJECTS_API_URL}/${projectId}/roles`, payload);
  }

  updateRole(projectId: number, roleId: number, payload: any): Observable<any> {
    return this.http.put<any>(`${this.PROJECTS_API_URL}/${projectId}/roles/${roleId}`, payload);
  }

  deleteRole(projectId: number, roleId: number): Observable<any> {
    return this.http.delete<any>(`${this.PROJECTS_API_URL}/${projectId}/roles/${roleId}`);
  }

  getRoleCandidates(projectId: number, roleId: number): Observable<any[]> {
    return this.http.get<any[]>(`${this.PROJECTS_API_URL}/${projectId}/roles/${roleId}/candidates`);
  }

  addCandidate(projectId: number, roleId: number, payload: { actorId: number, notes?: string }): Observable<any> {
    return this.http.post<any>(`${this.PROJECTS_API_URL}/${projectId}/roles/${roleId}/candidates`, payload);
  }

  updateCandidateStatus(projectId: number, roleId: number, actorId: number, status: string): Observable<any> {
    return this.http.put<any>(`${this.PROJECTS_API_URL}/${projectId}/roles/${roleId}/candidates/${actorId}/status`, { castStatus: status });
  }

  removeCandidate(projectId: number, roleId: number, actorId: number) {
    return this.http.delete(`${this.PROJECTS_API_URL}/${projectId}/roles/${roleId}/candidates/${actorId}`);
  }

  getActorProfile(userId: number): Observable<any> {
    return this.http.get<any>(`${this.ACTORS_API_URL}/${userId}/profile`);
  }

  updateActorCharacteristics(userId: number, characteristicsJson: string): Observable<any> {
    return this.http.put<any>(`${this.ACTORS_API_URL}/${userId}/characteristics`, { characteristics: characteristicsJson });
  }
}
