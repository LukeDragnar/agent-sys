import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface SpecQaResult {
  hasIssues: boolean;
  issues: string[];
  improvedSpec: string | null;
}

export interface AgentMessage {
  id: string;
  sessionId: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  createdAt: string;
}

export interface AgentSession {
  id: string;
  title: string;
  agentType: string;
  createdAt: string;
  isFavorite: boolean;
  isArchived: boolean;
}

export interface SpecVersion {
  id: string;
  version: number;
  jsonSpec: string;
  createdAt: string;
}

export interface SpecTask {
  id: string;
  name: string;
  slug: string;
  description: string;
  createdAt: string;
  versions: SpecVersion[];
}

export interface Spec {
  id: string;
  name: string;
  slug: string;
  projectKey: string;
  description: string;
  createdAt: string;
  tasks: SpecTask[];
}

export interface SessionRef {
  id: string;
  title: string;
}

export interface AllSpec extends Spec {
  session: SessionRef;
}

@Injectable({ providedIn: 'root' })
export class AgentService {
  private readonly base = 'http://10.0.161.21:50888/api/agent';

  constructor(private http: HttpClient) {}

  createSession(title: string): Observable<AgentSession> {
    return this.http.post<AgentSession>(`${this.base}/session`, JSON.stringify(title), {
      headers: { 'Content-Type': 'application/json' }
    });
  }

  chat(sessionId: string, message: string, specId?: string, projectId?: string, selectedModules?: string[]): Observable<string> {
    return this.http.post(`${this.base}/chat`, { sessionId, message, specId, projectId, selectedModules }, {
      responseType: 'text'
    });
  }

  getHistory(sessionId: string): Observable<AgentMessage[]> {
    return this.http.get<AgentMessage[]>(`${this.base}/history/${sessionId}`);
  }

  getAllSessions(): Observable<AgentSession[]> {
    return this.http.get<AgentSession[]>(`${this.base}/sessions`);
  }

  getAllSpecs(): Observable<AllSpec[]> {
    return this.http.get<AllSpec[]>(`${this.base}/specs/all`);
  }

  getSpecs(sessionId: string): Observable<Spec[]> {
    return this.http.get<Spec[]>(`${this.base}/spec/${sessionId}/versions`);
  }

  deleteSession(sessionId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/session/${sessionId}`);
  }

  updateVersion(versionId: string, jsonSpec: string): Observable<SpecVersion> {
    return this.http.patch<SpecVersion>(`${this.base}/version/${versionId}`, JSON.stringify(jsonSpec), {
      headers: { 'Content-Type': 'application/json' }
    });
  }

  deleteVersion(versionId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/version/${versionId}`);
  }

  deleteTask(taskId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/task/${taskId}`);
  }

  deleteSpec(specId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/spec/${specId}`);
  }

  qaSpec(versionId: string, projectId?: string, selectedModules?: string[]): Observable<SpecQaResult> {
    return this.http.post<SpecQaResult>(`${this.base}/qa-spec`, { versionId, projectId, selectedModules });
  }

  renameSession(sessionId: string, newTitle: string): Observable<void> {
    return this.http.patch<void>(`${this.base}/session/${sessionId}/rename`, JSON.stringify(newTitle), {
      headers: { 'Content-Type': 'application/json' }
    });
  }

  setFavorite(sessionId: string, isFavorite: boolean): Observable<void> {
    return this.http.patch<void>(`${this.base}/session/${sessionId}/favorite`, JSON.stringify(isFavorite), {
      headers: { 'Content-Type': 'application/json' }
    });
  }

  setArchived(sessionId: string, isArchived: boolean): Observable<void> {
    return this.http.patch<void>(`${this.base}/session/${sessionId}/archive`, JSON.stringify(isArchived), {
      headers: { 'Content-Type': 'application/json' }
    });
  }
}
