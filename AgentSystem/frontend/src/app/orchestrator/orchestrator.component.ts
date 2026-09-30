import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { OrchestratorService, OrchestratorResult, GeneratedFile, QaBug, SubTaskResult } from '../services/orchestrator.service';
import { ProjectService, Project } from '../services/project.service';

@Component({
  selector: 'app-orchestrator',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './orchestrator.component.html',
  styleUrl: './orchestrator.component.scss'
})
export class OrchestratorComponent implements OnInit, OnDestroy {
  featureRequest = '';
  loading = false;
  result: OrchestratorResult | null = null;
  error = '';
  fromSpecName = '';

  // Output directory (persisted in localStorage)
  outputDir = localStorage.getItem('orch_outputDir') ?? '';

  // Project selector
  projects: Project[] = [];
  selectedProjectId: string = localStorage.getItem('orch_projectId') ?? '';

  get activeSelectedModules(): string[] {
    const id = this.selectedProjectId;
    if (!id) return [];
    try {
      const saved = localStorage.getItem('selected_modules_' + id);
      if (!saved) return [];
      const map: Record<string, boolean> = JSON.parse(saved);
      return Object.keys(map).filter(k => map[k]);
    } catch { return []; }
  }

  // Project scan (legacy in-memory)
  beScanPath = localStorage.getItem('orch_bePath') ?? '';
  feScanPath = localStorage.getItem('orch_fePath') ?? '';
  projectContext: string | null = null;
  scanLoading = false;
  showProjectPanel = false;

  // Job-based progress
  jobId: string | null = null;
  jobLogs: string[] = [];
  private pollInterval: any = null;

  // Sub-task
  selectedSubTask: SubTaskResult | null = null;

  // File viewer
  selectedFile: GeneratedFile | null = null;

  // Tab: 'spec' | 'code' | 'qa'
  activeTab: 'spec' | 'code' | 'qa' = 'spec';

  constructor(
    private orchestratorService: OrchestratorService,
    private router: Router,
    private projectService: ProjectService
  ) {}

  ngOnInit(): void {
    this.orchestratorService.getProjectContext().subscribe(r => {
      this.projectContext = r.context;
    });
    this.projectService.getAll().subscribe(list => { this.projects = list; });

    const state = history.state;
    if (state?.versionId) {
      this.fromSpecName = state.specName ?? 'Spec';
      this.featureRequest = state.specName ?? '';
      if (state.actionName) {
        this.startFromAction(state.versionId, state.actionName);
      } else {
        this.startFromVersion(state.versionId);
      }
    } else if (state?.specJson) {
      this.fromSpecName = state.specName ?? 'Spec';
      this.featureRequest = state.specName ?? '';
      this.runFromSpecSplit(state.specJson);
    }
  }

  selectProject(id: string): void {
    this.selectedProjectId = id;
    localStorage.setItem('orch_projectId', id);
    if (!id) return;
    // Auto-fill outputDir from user's config for this project
    this.projectService.getMyConfig(id).subscribe(cfg => {
      if (cfg?.localOutputDir && !this.outputDir) {
        this.outputDir = cfg.localOutputDir;
        this.saveOutputDir();
      }
    });
  }

  saveOutputDir(): void {
    localStorage.setItem('orch_outputDir', this.outputDir);
  }

  scanProject(): void {
    if (!this.beScanPath && !this.feScanPath) return;
    this.scanLoading = true;
    localStorage.setItem('orch_bePath', this.beScanPath);
    localStorage.setItem('orch_fePath', this.feScanPath);
    this.orchestratorService.scanProject(this.beScanPath, this.feScanPath).subscribe({
      next: r => { this.projectContext = r.context; this.scanLoading = false; },
      error: err => { this.error = err.error ?? 'Scan lỗi'; this.scanLoading = false; }
    });
  }

  copyOutputDir(): void {
    const dir = this.result?.outputDir ?? this.result?.subResults?.[0]?.spec ?? '';
    if (this.result?.outputDir) navigator.clipboard.writeText(this.result.outputDir);
  }

  ngOnDestroy(): void {
    this.stopPolling();
  }

  startFromVersion(versionId: string): void {
    this.loading = true;
    this.result = null;
    this.error = '';
    this.jobId = null;
    this.jobLogs = [];
    this.selectedFile = null;
    this.selectedSubTask = null;

    this.orchestratorService.startFromVersion(versionId, this.outputDir || undefined, this.selectedProjectId || undefined, this.activeSelectedModules).subscribe({
      next: res => {
        this.jobId = res.jobId;
        this.startPolling();
      },
      error: err => {
        this.error = err.error ?? err.message ?? 'Có lỗi xảy ra';
        this.loading = false;
      }
    });
  }

  startFromAction(versionId: string, actionName: string): void {
    this.loading = true;
    this.result = null;
    this.error = '';
    this.jobId = null;
    this.jobLogs = [];
    this.selectedFile = null;
    this.selectedSubTask = null;

    this.orchestratorService.startFromAction(versionId, actionName, this.outputDir || undefined, this.selectedProjectId || undefined, this.activeSelectedModules).subscribe({
      next: res => {
        this.jobId = res.jobId;
        this.startPolling();
      },
      error: err => {
        this.error = err.error ?? err.message ?? 'Có lỗi xảy ra';
        this.loading = false;
      }
    });
  }

  private startPolling(): void {
    this.pollInterval = setInterval(() => this.pollJob(), 2000);
  }

  private stopPolling(): void {
    if (this.pollInterval) {
      clearInterval(this.pollInterval);
      this.pollInterval = null;
    }
  }

  private pollJob(): void {
    if (!this.jobId) return;
    this.orchestratorService.getJob(this.jobId).subscribe({
      next: status => {
        this.jobLogs = status.logs;
        if (status.isComplete) {
          this.stopPolling();
          this.loading = false;
          if (status.error) {
            this.error = status.error;
          } else if (status.result) {
            this.result = status.result;
            if (status.result.subResults?.length) {
              this.selectSubTask(status.result.subResults[0]);
            } else {
              this.activeTab = status.result.status === 'PASS' ? 'code' : 'qa';
              if (status.result.files?.length) this.selectedFile = status.result.files[0];
            }
          }
        }
      },
      error: () => { /* polling errors are ignored, will retry */ }
    });
  }

  runFromSpecSplit(specJson: string): void {
    this.loading = true;
    this.result = null;
    this.error = '';
    this.selectedFile = null;
    this.selectedSubTask = null;

    this.orchestratorService.runFromSpecSplit(specJson).subscribe({
      next: res => {
        this.result = res;
        this.loading = false;
        if (res.subResults?.length) {
          this.selectSubTask(res.subResults[0]);
        } else {
          this.activeTab = res.status === 'PASS' ? 'code' : 'qa';
          if (res.files?.length) this.selectedFile = res.files[0];
        }
      },
      error: err => {
        this.error = err.error ?? err.message ?? 'Có lỗi xảy ra';
        this.loading = false;
      }
    });
  }

  selectSubTask(sub: SubTaskResult): void {
    this.selectedSubTask = sub;
    this.selectedFile = sub.files?.[0] ?? null;
    this.activeTab = sub.status === 'PASS' ? 'code' : 'qa';
  }

  get activeFiles(): GeneratedFile[] {
    return this.selectedSubTask?.files ?? this.result?.files ?? [];
  }

  get activeQaReport(): string {
    return this.selectedSubTask?.qaReport ?? this.result?.qaReport ?? '';
  }

  get activeSpec(): string {
    return this.selectedSubTask?.spec ?? this.result?.spec ?? '';
  }

  run(): void {
    if (!this.featureRequest.trim() || this.loading) return;
    this.loading = true;
    this.result = null;
    this.error = '';
    this.selectedFile = null;

    this.orchestratorService.runFeature(this.featureRequest).subscribe({
      next: res => {
        this.result = res;
        this.loading = false;
        this.activeTab = res.status === 'PASS' ? 'code' : 'qa';
        if (res.files?.length) this.selectedFile = res.files[0];
      },
      error: err => {
        this.error = err.error ?? err.message ?? 'Có lỗi xảy ra';
        this.loading = false;
      }
    });
  }

  selectFile(file: GeneratedFile): void {
    this.selectedFile = file;
    this.activeTab = 'code';
  }

  formatJson(raw: string): string {
    try { return JSON.stringify(JSON.parse(raw), null, 2); }
    catch { return raw; }
  }

  get parsedQa(): { bugs: QaBug[] } | null {
    if (!this.activeQaReport) return null;
    try {
      const match = this.activeQaReport.match(/\{[\s\S]*\}/);
      if (!match) return null;
      return JSON.parse(match[0]);
    } catch { return null; }
  }

  get bugsByFile(): Map<string, QaBug[]> {
    const map = new Map<string, QaBug[]>();
    const bugs = this.parsedQa?.bugs ?? [];
    for (const bug of bugs) {
      const file = (bug.file ?? 'unknown').replace(/^[/\\]/, '');
      if (!map.has(file)) map.set(file, []);
      map.get(file)!.push(bug);
    }
    return map;
  }

  fileHasBugs(filePath: string): boolean {
    return this.bugsByFile.has(filePath.replace(/^[/\\]/, ''));
  }

  severityClass(s: string): string {
    return s === 'HIGH' ? 'sev-high' : s === 'MEDIUM' ? 'sev-med' : 'sev-low';
  }
}
