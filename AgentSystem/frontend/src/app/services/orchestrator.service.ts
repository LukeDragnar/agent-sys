import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface DirEntry {
  name: string;
  path: string;
  isFolder: boolean;
}

export interface GeneratedFile {
  filePath: string;
  content: string;
}

export interface QaBug {
  id: string;
  file: string;
  type: string;
  description: string;
  severity: 'HIGH' | 'MEDIUM' | 'LOW';
}

export interface QaReport {
  bugs: QaBug[];
  coverage?: any;
  test_cases?: any[];
  risks?: any[];
  suggestions?: any[];
}

export interface SubTaskResult {
  specName: string;
  spec: string;
  files: GeneratedFile[];
  qaReport: string;
  status: 'PASS' | 'FAIL';
  retryCount: number;
  error?: string;
}

export interface OrchestratorResult {
  spec: string;
  files: GeneratedFile[];
  qaReport: string;
  gitInstructions: string;
  status: 'PASS' | 'FAIL';
  retryCount: number;
  error?: string;
  outputDir: string;
  subResults?: SubTaskResult[];
}

@Injectable({ providedIn: 'root' })
export class OrchestratorService {

  private readonly base = 'http://10.0.161.21:50888/api/orchestrator';
  constructor(private http: HttpClient) {}

  runFeature(featureRequest: string): Observable<OrchestratorResult> {
    return this.http.post<OrchestratorResult>(`${this.base}/run`, { featureRequest });
  }

  runFromSpec(specJson: string): Observable<OrchestratorResult> {
    return this.http.post<OrchestratorResult>(`${this.base}/run-from-spec`, { specJson });
  }

  runFromSpecSplit(specJson: string): Observable<OrchestratorResult> {
    return this.http.post<OrchestratorResult>(`${this.base}/run-from-spec-split`, { specJson });
  }

  startFromVersion(versionId: string, outputDir?: string, projectId?: string, selectedModules?: string[]): Observable<{ jobId: string }> {
    return this.http.post<{ jobId: string }>(`${this.base}/start-from-version`, { versionId, outputDir, projectId, selectedModules });
  }

  startFromAction(versionId: string, actionName: string, outputDir?: string, projectId?: string, selectedModules?: string[]): Observable<{ jobId: string }> {
    return this.http.post<{ jobId: string }>(`${this.base}/start-from-action`, { versionId, actionName, outputDir, projectId, selectedModules });
  }

  scanProject(bePath: string, fePath: string): Observable<{ context: string }> {
    return this.http.post<{ context: string }>(`${this.base}/scan-project`, { bePath, fePath });
  }

  getProjectContext(): Observable<{ context: string | null }> {
    return this.http.get<{ context: string | null }>(`${this.base}/project-context`);
  }

  getFilesForVersion(versionId: string): Observable<GeneratedFile[]> {
    return this.http.get<GeneratedFile[]>(`${this.base}/version/${versionId}/files`);
  }

  listDir(path: string): Observable<DirEntry[]> {
    return this.http.post<DirEntry[]>(`${this.base}/list-dir`, { path });
  }

  readFile(path: string): Observable<{ content: string }> {
    return this.http.post<{ content: string }>(`${this.base}/read-file`, { path });
  }

  getJob(jobId: string): Observable<JobStatus> {
    return this.http.get<JobStatus>(`${this.base}/job/${jobId}`);
  }
}

export interface JobStatus {
  logs: string[];
  isComplete: boolean;
  result: OrchestratorResult | null;
  error: string | null;
}
