import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface SymbolResult {
  id: string;
  name: string;
  fullName: string;
  symbolType: string;
  filePath: string;
  lineNumber: number;
  namespace: string;
  accessibility: string;
  returnType?: string;
  parameters?: string;
  baseTypes?: string;
  children: SymbolResult[];
  dependencies: DependencyResult[];
}

export interface EndpointResult {
  httpMethod: string;
  routeTemplate: string;
  controllerName: string;
  actionName: string;
  filePath: string;
  lineNumber: number;
  serviceChain: string[];
}

export interface DependencyResult {
  fromSymbol: string;
  toSymbol: string;
  dependencyType: string;
}

export interface DependencyTreeResult {
  symbol: string;
  dependsOn: DependencyResult[];
  usedBy: DependencyResult[];
}

export interface SearchResult {
  name: string;
  fullName: string;
  symbolType: string;
  filePath: string;
  lineNumber: number;
}

export interface ScanStats {
  projectId: string;
  projectName: string;
  totalFiles: number;
  classes: number;
  interfaces: number;
  methods: number;
  properties: number;
  endpoints: number;
  dependencies: number;
  lastScannedAt?: string;
}

export interface TreeNode {
  name: string;
  type: string;
  filePath?: string;
  lineNumber?: number;
  children: TreeNode[];
  expanded?: boolean;
}

export interface ScanProgressResult {
  isRunning: boolean;
  totalFiles: number;
  processedFiles: number;
  percent: number;
  phase: string;
}

export interface GraphNode {
  id: string;
  name: string;
  type: string;
  filePath: string;
  lineNumber: number;
  methodCount: number;
  dependencyCount: number;
  // D3 runtime fields
  x?: number;
  y?: number;
  fx?: number | null;
  fy?: number | null;
}

export interface GraphEdge {
  source: string | GraphNode;
  target: string | GraphNode;
  type: string;
}

export interface GraphData {
  nodes: GraphNode[];
  edges: GraphEdge[];
}

@Injectable({ providedIn: 'root' })
export class CodeMapService {

   private readonly base = 'http://10.0.161.21:50888/api/codemap';
  constructor(private http: HttpClient) {}

  scan(projectId: string, rootPath: string): Observable<ScanStats> {
    return this.http.post<ScanStats>(`${this.base}/${projectId}/scan`, { rootPath });
  }

  getScanProgress(projectId: string): Observable<ScanProgressResult> {
    return this.http.get<ScanProgressResult>(`${this.base}/${projectId}/scan/progress`);
  }

  clear(projectId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${projectId}`);
  }

  findSymbol(projectId: string, name: string, type?: string): Observable<SymbolResult[]> {
    const params: any = { name };
    if (type) params.type = type;
    return this.http.get<SymbolResult[]>(`${this.base}/${projectId}/symbols`, { params });
  }

  findEndpoint(projectId: string, route?: string, method?: string): Observable<EndpointResult[]> {
    const params: any = {};
    if (route) params.route = route;
    if (method) params.method = method;
    return this.http.get<EndpointResult[]>(`${this.base}/${projectId}/endpoints`, { params });
  }

  getDependencies(projectId: string, symbol: string, depth = 1): Observable<DependencyTreeResult> {
    return this.http.get<DependencyTreeResult>(`${this.base}/${projectId}/dependencies`, { params: { symbol, depth } });
  }

  search(projectId: string, q: string, limit = 20): Observable<SearchResult[]> {
    return this.http.get<SearchResult[]>(`${this.base}/${projectId}/search`, { params: { q, limit } });
  }

  getStats(projectId: string): Observable<ScanStats> {
    return this.http.get<ScanStats>(`${this.base}/${projectId}/stats`);
  }

  getTree(projectId: string): Observable<TreeNode[]> {
    return this.http.get<TreeNode[]>(`${this.base}/${projectId}/tree`);
  }

  getGraph(projectId: string, filter?: string): Observable<GraphData> {
    const params: any = {};
    if (filter) params.filter = filter;
    return this.http.get<GraphData>(`${this.base}/${projectId}/graph`, { params });
  }

  getModuleGraph(projectId: string): Observable<GraphData> {
    return this.http.get<GraphData>(`${this.base}/${projectId}/graph/modules`);
  }
}
