import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface FeatureDoc {
  id: string;
  projectKey: string;
  slug: string;
  title: string;
  content: string;
  createdAt: string;
  updatedAt: string;
}

export interface LinkedSession {
  id: string;
  title: string;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class FeatureDocService {
  private readonly base = 'http://10.0.161.21:50888/api/feature-docs';
  constructor(private http: HttpClient) {}

  list(projectKey?: string): Observable<FeatureDoc[]> {
    const qs = projectKey ? `?projectKey=${encodeURIComponent(projectKey)}` : '';
    return this.http.get<FeatureDoc[]>(`${this.base}${qs}`);
  }

  get(id: string): Observable<FeatureDoc> {
    return this.http.get<FeatureDoc>(`${this.base}/${id}`);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  getSessions(docId: string): Observable<LinkedSession[]> {
    return this.http.get<LinkedSession[]>(`${this.base}/${docId}/sessions`);
  }

  linkSession(docId: string, sessionId: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${docId}/sessions/${sessionId}`, null);
  }

  unlinkSession(docId: string, sessionId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${docId}/sessions/${sessionId}`);
  }
}
