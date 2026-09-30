import { Component, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { FeatureDocService, FeatureDoc, LinkedSession } from '../services/feature-doc.service';
import { AgentService, AgentSession, AllSpec, SpecTask, SpecVersion } from '../services/agent.service';
import { OrchestratorService, GeneratedFile } from '../services/orchestrator.service';

export interface DiffLine {
  key: string;
  status: 'added' | 'removed' | 'changed' | 'unchanged';
  oldVal?: string;
  newVal?: string;
}

export interface ParsedSpec {
  feature_name: string;
  description: string;
  actors: string[];
  fields: any[];
  actions: any[];
  business_flow: string[];
  validations: string[];
  non_functional: any;
}

@Component({
  selector: 'app-specs',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './specs.component.html',
  styleUrl: './specs.component.scss'
})
export class SpecsComponent implements OnInit {

  activeTab: 'generated' | 'docs' = 'generated';

  // ── Generated Specs ───────────────────────────────────────
  allSpecs: AllSpec[] = [];
  specsLoading = false;

  selectedSpec: AllSpec | null = null;
  selectedTask: SpecTask | null = null;
  selectedVersion: SpecVersion | null = null;

  specViewMode: 'structured' | 'json' | 'code' = 'structured';
  expandedSpecs = new Set<string>();
  expandedActions = new Set<number>();

  compareVersion: SpecVersion | null = null;
  diffLines: DiffLine[] = [];
  showDiff = false;

  versionFiles: GeneratedFile[] = [];
  selectedGenFile: GeneratedFile | null = null;

  qaSpecLoading = false;
  qaSpecModal: { version: SpecVersion; result: any } | null = null;
  qaSpecApplying = false;

  get projectGroups(): { key: string; specs: AllSpec[] }[] {
    const map = new Map<string, AllSpec[]>();
    for (const s of this.allSpecs) {
      const k = s.projectKey || '(chưa gán project)';
      if (!map.has(k)) map.set(k, []);
      map.get(k)!.push(s);
    }
    return [...map.entries()].map(([key, specs]) => ({ key, specs }));
  }

  get parsedSpec(): ParsedSpec | null {
    if (!this.selectedVersion) return null;
    try { return JSON.parse(this.selectedVersion.jsonSpec); }
    catch { return null; }
  }

  get hasDiffChanges(): boolean {
    return this.diffLines.some(l => l.status !== 'unchanged');
  }

  // ── Docs (FeatureDoc) ─────────────────────────────────────
  docs: FeatureDoc[] = [];
  filtered: FeatureDoc[] = [];
  selectedDoc: FeatureDoc | null = null;
  docsLoading = false;
  filterProject = '';
  linkedSessions: LinkedSession[] = [];
  allSessions: AgentSession[] = [];
  showLinkPanel = false;
  linkSearch = '';

  get docProjects(): string[] {
    return [...new Set(this.docs.map(d => d.projectKey))].sort();
  }

  get filteredAllSessions(): AgentSession[] {
    const q = this.linkSearch.toLowerCase();
    const linkedIds = new Set(this.linkedSessions.map(s => s.id));
    return this.allSessions
      .filter(s => !linkedIds.has(s.id) && (!q || s.title.toLowerCase().includes(q)));
  }

  constructor(
    private agentSvc: AgentService,
    private docSvc: FeatureDocService,
    private orchestratorSvc: OrchestratorService,
    private router: Router,
    private route: ActivatedRoute,
  ) {}

  ngOnInit(): void {
    this.loadGeneratedSpecs();
    this.loadDocs();
    this.agentSvc.getAllSessions().subscribe(s => this.allSessions = s);

    // Auto-select từ URL params
    this.route.params.subscribe(p => {
      if (p['specSlug'] && p['taskSlug'] && this.allSpecs.length) {
        this.autoSelectFromUrl(p['specSlug'], p['taskSlug']);
      }
    });
  }

  // ── Generated Specs ───────────────────────────────────────

  loadGeneratedSpecs(): void {
    this.specsLoading = true;
    this.agentSvc.getAllSpecs().subscribe({
      next: specs => {
        this.allSpecs = specs;
        this.specsLoading = false;
        const p = this.route.snapshot.params;
        if (p['specSlug'] && p['taskSlug']) {
          this.autoSelectFromUrl(p['specSlug'], p['taskSlug']);
        }
      },
      error: () => { this.specsLoading = false; }
    });
  }

  private autoSelectFromUrl(specSlug: string, taskSlug: string): void {
    for (const spec of this.allSpecs) {
      if (spec.slug !== specSlug) continue;
      const task = spec.tasks.find(t => t.slug === taskSlug);
      if (task) {
        this.selectedSpec = spec;
        this.selectTask(task);
        this.expandedSpecs.add(spec.id);
        return;
      }
    }
  }

  toggleSpecGroup(spec: AllSpec): void {
    if (this.expandedSpecs.has(spec.id)) this.expandedSpecs.delete(spec.id);
    else this.expandedSpecs.add(spec.id);
  }

  selectTask(task: SpecTask): void {
    this.selectedTask = task;
    this.selectedVersion = task.versions.length ? task.versions[0] : null;
    this.showDiff = false;
    this.compareVersion = null;
    this.diffLines = [];
    this.expandedActions.clear();
    this.versionFiles = [];
    this.selectedGenFile = null;
    if (this.selectedVersion) this.loadVersionFiles(this.selectedVersion.id);
  }

  selectVersion(v: SpecVersion): void {
    this.selectedVersion = v;
    this.showDiff = false;
    this.compareVersion = null;
    this.diffLines = [];
    this.expandedActions.clear();
    this.versionFiles = [];
    this.selectedGenFile = null;
    this.loadVersionFiles(v.id);
  }

  loadVersionFiles(versionId: string): void {
    this.orchestratorSvc.getFilesForVersion(versionId).subscribe({
      next: files => {
        this.versionFiles = files;
        if (files.length && this.specViewMode === 'code') this.selectedGenFile = files[0];
      },
      error: () => { this.versionFiles = []; }
    });
  }

  copySpecUrl(): void {
    if (!this.selectedSpec || !this.selectedTask) return;
    const url = `${window.location.origin}/specs/${this.selectedSpec.slug}/${this.selectedTask.slug}`;
    navigator.clipboard.writeText(url);
  }

  goToSession(sessionId: string): void {
    this.router.navigate(['/'], { queryParams: { session: sessionId } });
  }

  toggleAction(i: number): void {
    if (this.expandedActions.has(i)) this.expandedActions.delete(i);
    else this.expandedActions.add(i);
  }

  toggleDiff(v: SpecVersion): void {
    if (this.compareVersion?.id === v.id) { this.showDiff = false; this.compareVersion = null; return; }
    this.compareVersion = v;
    this.showDiff = true;
    this.buildDiff();
  }

  buildDiff(): void {
    if (!this.selectedVersion || !this.compareVersion) return;
    try {
      const a = JSON.parse(this.compareVersion.jsonSpec);
      const b = JSON.parse(this.selectedVersion.jsonSpec);
      this.diffLines = this.diffObjects(a, b, '');
    } catch { this.diffLines = []; }
  }

  private diffObjects(o: any, n: any, prefix: string): DiffLine[] {
    const lines: DiffLine[] = [];
    const keys = new Set([...Object.keys(o || {}), ...Object.keys(n || {})]);
    for (const key of keys) {
      const full = prefix ? `${prefix}.${key}` : key;
      const ov = o?.[key]; const nv = n?.[key];
      if (!(key in (o || {}))) lines.push({ key: full, status: 'added', newVal: this.str(nv) });
      else if (!(key in (n || {}))) lines.push({ key: full, status: 'removed', oldVal: this.str(ov) });
      else if (typeof ov === 'object' && typeof nv === 'object' && !Array.isArray(ov) && !Array.isArray(nv) && ov && nv)
        lines.push(...this.diffObjects(ov, nv, full));
      else {
        const os = this.str(ov); const ns = this.str(nv);
        lines.push({ key: full, status: os !== ns ? 'changed' : 'unchanged', oldVal: os, newVal: ns });
      }
    }
    return lines;
  }

  private str(v: any): string {
    if (v == null) return 'null';
    return typeof v === 'object' ? JSON.stringify(v) : String(v);
  }

  formatSpec(v: SpecVersion | null): string {
    if (!v) return '';
    try { return JSON.stringify(JSON.parse(v.jsonSpec), null, 2); }
    catch { return v.jsonSpec; }
  }

  deleteVersion(v: SpecVersion, event: MouseEvent): void {
    event.stopPropagation();
    if (!confirm(`Xóa version v${v.version}?`)) return;
    this.agentSvc.deleteVersion(v.id).subscribe(() => {
      if (this.selectedVersion?.id === v.id) { this.selectedVersion = null; this.expandedActions.clear(); }
      this.loadGeneratedSpecs();
    });
  }

  deleteTask(task: SpecTask, event: MouseEvent): void {
    event.stopPropagation();
    if (!confirm(`Xóa task "${task.name}"?`)) return;
    this.agentSvc.deleteTask(task.id).subscribe(() => {
      if (this.selectedTask?.id === task.id) { this.selectedTask = null; this.selectedVersion = null; }
      this.loadGeneratedSpecs();
    });
  }

  deleteSpec(spec: AllSpec, event: MouseEvent): void {
    event.stopPropagation();
    if (!confirm(`Xóa module "${spec.name}"?`)) return;
    this.agentSvc.deleteSpec(spec.id).subscribe(() => {
      if (this.selectedSpec?.id === spec.id) { this.selectedSpec = null; this.selectedTask = null; this.selectedVersion = null; }
      this.expandedSpecs.delete(spec.id);
      this.loadGeneratedSpecs();
    });
  }

  deleteAction(index: number): void {
    const spec = this.parsedSpec;
    if (!spec || !this.selectedVersion) return;
    spec.actions.splice(index, 1);
    const updated = JSON.stringify(spec, null, 2);
    this.agentSvc.updateVersion(this.selectedVersion.id, updated).subscribe(saved => {
      if (this.selectedVersion) this.selectedVersion = saved;
      this.loadGeneratedSpecs();
      this.expandedActions.clear();
    });
  }

  runQaSpec(v: SpecVersion): void {
    this.qaSpecLoading = true;
    this.qaSpecModal = null;
    this.agentSvc.qaSpec(v.id).subscribe({
      next: result => { this.qaSpecLoading = false; this.qaSpecModal = { version: v, result }; },
      error: () => { this.qaSpecLoading = false; }
    });
  }

  applyImprovedSpec(): void {
    if (!this.qaSpecModal?.result.improvedSpec || !this.selectedVersion) return;
    this.qaSpecApplying = true;
    this.agentSvc.updateVersion(this.qaSpecModal.version.id, this.qaSpecModal.result.improvedSpec).subscribe({
      next: updated => {
        this.selectedVersion = updated;
        this.qaSpecApplying = false;
        this.qaSpecModal = null;
        this.loadGeneratedSpecs();
      },
      error: () => { this.qaSpecApplying = false; }
    });
  }

  runWithOrchestrator(actionName?: string): void {
    if (!this.selectedVersion) return;
    this.router.navigate(['/orchestrator'], {
      state: {
        versionId: this.selectedVersion.id,
        actionName,
        specName: actionName
          ? `${this.selectedTask?.name ?? ''}.${actionName}`
          : (this.selectedTask?.name ?? '')
      }
    });
  }

  @HostListener('document:click')
  closeQaModal(): void { /* handled in template */ }

  // ── Docs tab ─────────────────────────────────────────────

  loadDocs(): void {
    this.docsLoading = true;
    this.docSvc.list().subscribe({
      next: data => {
        this.docs = data;
        this.applyDocFilter();
        this.docsLoading = false;
      },
      error: () => { this.docsLoading = false; }
    });
  }

  applyDocFilter(): void {
    this.filtered = this.filterProject
      ? this.docs.filter(d => d.projectKey === this.filterProject)
      : this.docs;
    if (this.selectedDoc && !this.filtered.find(d => d.id === this.selectedDoc!.id))
      this.selectedDoc = null;
  }

  selectDoc(doc: FeatureDoc): void {
    this.selectedDoc = this.selectedDoc?.id === doc.id ? null : doc;
    this.linkedSessions = [];
    this.showLinkPanel = false;
    this.linkSearch = '';
    if (this.selectedDoc) {
      this.docSvc.getSessions(this.selectedDoc.id).subscribe(s => this.linkedSessions = s);
    }
  }

  deleteDoc(doc: FeatureDoc, event: Event): void {
    event.stopPropagation();
    if (!confirm(`Xóa spec "${doc.title}"?`)) return;
    this.docSvc.delete(doc.id).subscribe(() => {
      this.docs = this.docs.filter(d => d.id !== doc.id);
      if (this.selectedDoc?.id === doc.id) this.selectedDoc = null;
      this.applyDocFilter();
    });
  }

  groupDocsByProject(docs: FeatureDoc[]): { key: string; items: FeatureDoc[] }[] {
    const map = new Map<string, FeatureDoc[]>();
    for (const d of docs) {
      if (!map.has(d.projectKey)) map.set(d.projectKey, []);
      map.get(d.projectKey)!.push(d);
    }
    return [...map.entries()].map(([key, items]) => ({ key, items }));
  }

  linkSession(session: AgentSession): void {
    if (!this.selectedDoc) return;
    this.docSvc.linkSession(this.selectedDoc.id, session.id).subscribe(() => {
      this.linkedSessions = [{ id: session.id, title: session.title, createdAt: session.createdAt }, ...this.linkedSessions];
      this.showLinkPanel = false;
      this.linkSearch = '';
    });
  }

  unlinkSession(session: LinkedSession, event: Event): void {
    event.stopPropagation();
    if (!this.selectedDoc) return;
    this.docSvc.unlinkSession(this.selectedDoc.id, session.id).subscribe(() => {
      this.linkedSessions = this.linkedSessions.filter(s => s.id !== session.id);
    });
  }
}
