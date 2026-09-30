import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Project {
  id: string;
  name: string;
  description: string;
  profileJson?: string;
  lastScannedAt?: string;
  createdAt: string;
}

export interface UserProjectConfig {
  id: string;
  projectId: string;
  localBePath: string;
  localFePath: string;
  localOutputDir: string;
  localModulesJson?: string;
  updatedAt: string;
}

@Injectable({ providedIn: 'root' })
export class ProjectService {

  private readonly base = 'http://10.0.161.21:50888/api/projects';
  constructor(private http: HttpClient) {}

  getAll(): Observable<Project[]> {
    return this.http.get<Project[]>(`${this.base}`);
  }

  getById(id: string): Observable<Project> {
    return this.http.get<Project>(`${this.base}/${id}`);
  }

  create(name: string, description: string): Observable<Project> {
    return this.http.post<Project>(`${this.base}`, { name, description });
  }

  update(id: string, name: string, description: string): Observable<Project> {
    return this.http.put<Project>(`${this.base}/${id}`, { name, description });
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  scan(id: string, bePath: string, fePath: string): Observable<{ profile: string }> {
    return this.http.post<{ profile: string }>(`${this.base}/${id}/scan`, { bePath, fePath });
  }

  getMyConfig(projectId: string): Observable<UserProjectConfig | null> {
    return this.http.get<UserProjectConfig | null>(`${this.base}/${projectId}/my-config`);
  }

  upsertMyConfig(projectId: string, localBePath: string, localFePath: string, localOutputDir: string, localModulesJson?: string | null): Observable<UserProjectConfig> {
    return this.http.put<UserProjectConfig>(`${this.base}/${projectId}/my-config`, { localBePath, localFePath, localOutputDir, localModulesJson });
  }
}
