import { Component, OnInit, ViewChild, ElementRef, AfterViewChecked, HostListener, NgZone } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink, ActivatedRoute } from '@angular/router';
import { AgentService, AgentMessage, AgentSession, Spec, SpecTask, SpecVersion, SpecQaResult } from '../services/agent.service';
import { AuthService } from '../services/auth.service';
import { OrchestratorService, GeneratedFile } from '../services/orchestrator.service';
import { ProjectService, Project } from '../services/project.service';

export interface ScreenItem {
  name: string;
  description: string;
}

export interface ScreenListResponse {
  type: 'screen_list';
  module: string;
  screens: ScreenItem[];
}

export interface DiffLine {
  key: string;
  status: 'added' | 'removed' | 'changed' | 'unchanged';
  oldVal?: string;
  newVal?: string;
}

export interface SpecAction {
  name: string;
  actor: string;
  description: string;
  input?: Record<string, string>;
  output?: Record<string, string>;
  preconditions: string[];
  postconditions: string[];
}

export interface SpecField {
  name: string;
  type: string;
  required: boolean;
  validation: string;
  notes: string;
}

export interface ParsedSpec {
  feature_name: string;
  description: string;
  actors: string[];
  fields: SpecField[];
  actions: SpecAction[];
  business_flow: string[];
  validations: string[];
  non_functional: { performance: string; security: string; notes: string };
}

@Component({
  selector: 'app-chat',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './chat.component.html',
  styleUrl: './chat.component.scss'
})
export class ChatComponent implements OnInit, AfterViewChecked {
  @ViewChild('messageList') messageList!: ElementRef;

  // Sessions
  sessions: AgentSession[] = [];
  activeSession: AgentSession | null = null;

  // Messages
  messages: AgentMessage[] = [];
  input = '';
  loading = false;

  // Spec panel — 3-level hierarchy
  specs: Spec[] = [];
  selectedSpec: Spec | null = null;
  selectedTask: SpecTask | null = null;
  selectedVersion: SpecVersion | null = null;
  expandedSpecs = new Set<string>();

  compareVersion: SpecVersion | null = null;
  diffLines: DiffLine[] = [];
  showDiff = false;

  // Generated code files linked to selected version
  versionFiles: GeneratedFile[] = [];
  selectedGenFile: GeneratedFile | null = null;
  specViewMode: 'structured' | 'json' | 'code' = 'structured';

  tokenWarning = '';
  specPanelOpen = true;
  expandedActions = new Set<number>();

  // Active project (first checked project from Projects screen)
  allProjects: Project[] = [];

  get activeProjectId(): string | undefined {
    try {
      const saved = localStorage.getItem('selected_project_ids');
      if (!saved) return undefined;
      const map: Record<string, boolean> = JSON.parse(saved);
      return Object.keys(map).find(k => map[k]);
    } catch { return undefined; }
  }

  get activeProjectName(): string {
    const id = this.activeProjectId;
    if (!id) return '';
    return this.allProjects.find(p => p.id === id)?.name ?? '';
  }

  get activeSelectedModules(): string[] {
    const id = this.activeProjectId;
    if (!id) return [];
    try {
      const saved = localStorage.getItem('selected_modules_' + id);
      if (!saved) return [];
      const map: Record<string, boolean> = JSON.parse(saved);
      return Object.keys(map).filter(k => map[k]);
    } catch { return []; }
  }

  // Session filter & rename
  sessionFilter: 'all' | 'favorite' | 'archived' = 'all';
  renamingSession: { id: string; title: string } | null = null;

  get filteredSessions(): AgentSession[] {
    if (this.sessionFilter === 'favorite') return this.sessions.filter(s => s.isFavorite && !s.isArchived);
    if (this.sessionFilter === 'archived') return this.sessions.filter(s => s.isArchived);
    return this.sessions.filter(s => !s.isArchived);
  }

  startRename(session: AgentSession, event: MouseEvent): void {
    event.stopPropagation();
    this.contextMenu = null;
    this.renamingSession = { id: session.id, title: session.title };
    setTimeout(() => (document.querySelector('.rename-input') as HTMLInputElement)?.focus(), 0);
  }

  confirmRename(): void {
    if (!this.renamingSession) return;
    const { id, title } = this.renamingSession;
    const trimmed = title.trim();
    this.renamingSession = null;
    if (!trimmed) return;
    this.agentService.renameSession(id, trimmed).subscribe(() => {
      const s = this.sessions.find(x => x.id === id);
      if (s) s.title = trimmed;
    });
  }

  cancelRename(): void { this.renamingSession = null; }

  toggleFavorite(session: AgentSession, event: MouseEvent): void {
    event.stopPropagation();
    this.contextMenu = null;
    const next = !session.isFavorite;
    this.agentService.setFavorite(session.id, next).subscribe(() => {
      session.isFavorite = next;
    });
  }

  toggleArchive(session: AgentSession): void {
    this.contextMenu = null;
    const next = !session.isArchived;
    this.agentService.setArchived(session.id, next).subscribe(() => {
      session.isArchived = next;
    });
  }

  // Multi-select
  selectedIds = new Set<string>();
  confirmDeleteSessions: AgentSession[] = [];

  toggleSelect(session: AgentSession, event: MouseEvent): void {
    if (!event.ctrlKey && !event.metaKey) return;
    event.preventDefault();
    if (this.selectedIds.has(session.id)) this.selectedIds.delete(session.id);
    else this.selectedIds.add(session.id);
    this.selectedIds = new Set(this.selectedIds); // trigger change detection
  }

  clearSelection(): void { this.selectedIds = new Set(); }

  // Context menu - session
  contextMenu: { x: number; y: number; session: AgentSession } | null = null;
  confirmDeleteSession: AgentSession | null = null;

  // Context menu - message
  msgContextMenu: { x: number; y: number; content: string } | null = null;

  // Context menu - spec (task or action)
  specContextMenu: { x: number; y: number; type: 'task' | 'action'; actionName?: string } | null = null;

  // QA Spec
  qaSpecLoading = false;
  qaSpecModal: { version: SpecVersion; result: SpecQaResult } | null = null;
  qaSpecApplying = false;

  // Voice
  isListening = false;
  voiceSupported = false;
  private recognition: any = null;

  toggleVoice(): void {
    if (this.isListening) { this.stopVoice(); return; }
    this.startVoice();
  }

  startVoice(): void {
    const SR = (window as any).SpeechRecognition ?? (window as any).webkitSpeechRecognition;
    if (!SR) { alert('Trình duyệt không hỗ trợ voice. Dùng Chrome hoặc Edge.'); return; }

    this.recognition = new SR();
    this.recognition.lang = 'vi-VN';
    this.recognition.continuous = true;
    this.recognition.interimResults = true;

    let finalTranscript = this.input;

    this.recognition.onresult = (event: any) => {
      let interim = '';
      for (let i = event.resultIndex; i < event.results.length; i++) {
        const t = event.results[i][0].transcript;
        if (event.results[i].isFinal) finalTranscript += (finalTranscript ? ' ' : '') + t.trim();
        else interim = t;
      }
      this.ngZone.run(() => { this.input = finalTranscript + (interim ? ' ' + interim : ''); });
    };

    this.recognition.onerror = (e: any) => {
      this.ngZone.run(() => {
        this.isListening = false;
        if (e.error === 'not-allowed') alert('Chưa cấp quyền microphone. Vào Settings trình duyệt để cho phép.');
      });
    };

    this.recognition.onend = () => {
      this.ngZone.run(() => {
        this.isListening = false;
        this.input = this.input.trim();
      });
    };

    this.recognition.start();
    this.isListening = true;
  }

  stopVoice(): void {
    this.recognition?.stop();
    this.isListening = false;
  }

  @HostListener('document:keydown.space', ['$event'])
  onSpaceKey(e: KeyboardEvent): void {
    const tag = (e.target as HTMLElement).tagName;
    // Only trigger when not typing in an input/textarea
    if (tag === 'TEXTAREA' || tag === 'INPUT') return;
    e.preventDefault();
    if (this.activeSession && this.voiceSupported) this.toggleVoice();
  }

  // Scroll
  private shouldScrollToBottom = false;

  constructor(
    private agentService: AgentService,
    private authService: AuthService,
    private orchestratorService: OrchestratorService,
    private router: Router,
    private route: ActivatedRoute,
    private projectService: ProjectService,
    private ngZone: NgZone
  ) {}

  get currentUser() { return this.authService.getCurrentUser(); }
  get isAdmin() { return this.authService.isAdmin(); }
  logout() {
    if (confirm('Bạn có chắc muốn đăng xuất?')) this.authService.logout();
  }
  goToAdmin() { this.router.navigate(['/admin']); }
  goToOrchestrator() { this.router.navigate(['/orchestrator']); }

  runSpecWithOrchestrator(): void {
    if (!this.selectedVersion) return;
    this.router.navigate(['/orchestrator'], {
      state: {
        versionId: this.selectedVersion.id,
        specName: this.selectedTask?.name ?? ''
      }
    });
  }

  runActionWithOrchestrator(actionName: string): void {
    if (!this.selectedVersion) return;
    this.router.navigate(['/orchestrator'], {
      state: {
        versionId: this.selectedVersion.id,
        actionName,
        specName: `${this.selectedTask?.name ?? ''}.${actionName}`
      }
    });
  }

  ngOnInit(): void {
    this.voiceSupported = 'SpeechRecognition' in window || 'webkitSpeechRecognition' in window;
    this.loadSessions();
    this.projectService.getAll().subscribe(list => { this.allProjects = list; });

  }

  ngAfterViewChecked(): void {
    if (this.shouldScrollToBottom) {
      this.scrollToBottom();
      this.shouldScrollToBottom = false;
    }
  }

  // ── Sessions ──────────────────────────────────────────────

  loadSessions(): void {
    this.agentService.getAllSessions().subscribe(sessions => {
      this.sessions = sessions;
      const targetId = this.route.snapshot.queryParamMap.get('session');
      const target = targetId ? sessions.find(s => s.id === targetId) : null;
      if (target) {
        this.selectSession(target);
      } else if (sessions.length > 0 && !this.activeSession) {
        this.selectSession(sessions[0]);
      }
    });
  }

  selectSession(session: AgentSession): void {
    this.activeSession = session;
    this.messages = [];
    this.specs = [];
    this.selectedSpec = null;
    this.selectedTask = null;
    this.selectedVersion = null;
    this.compareVersion = null;
    this.showDiff = false;
    this.expandedSpecs.clear();

    this.agentService.getHistory(session.id).subscribe(msgs => {
      this.messages = msgs;
      this.shouldScrollToBottom = true;
    });

    this.loadSpecs();
  }

  onContextMenu(event: MouseEvent, session: AgentSession): void {
    event.preventDefault();
    // If right-clicking outside current selection, reset to just this session
    if (!this.selectedIds.has(session.id)) {
      this.selectedIds = new Set([session.id]);
    }
    this.contextMenu = { x: event.clientX, y: event.clientY, session };
  }

  get contextMenuSelectionCount(): number {
    return this.selectedIds.size;
  }

  @HostListener('document:keydown.f2')
  onF2(): void {
    if (this.activeSession && !this.renamingSession) {
      this.renamingSession = { id: this.activeSession.id, title: this.activeSession.title };
      setTimeout(() => (document.querySelector('.rename-input') as HTMLInputElement)?.focus(), 0);
    }
  }

  @HostListener('document:click', ['$event'])
  closeContextMenus(e: MouseEvent): void {
    this.contextMenu = null;
    this.msgContextMenu = null;
    this.specContextMenu = null;
    // Clear selection when clicking outside sidebar without Ctrl
    if (!e.ctrlKey && !e.metaKey) {
      const inSidebar = (e.target as HTMLElement).closest('.session-list');
      if (!inSidebar) this.selectedIds = new Set();
    }
  }

  onSpecTaskContextMenu(event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.specContextMenu = { x: event.clientX, y: event.clientY, type: 'task' };
  }

  onSpecActionContextMenu(event: MouseEvent, actionName: string): void {
    event.preventDefault();
    event.stopPropagation();
    this.specContextMenu = { x: event.clientX, y: event.clientY, type: 'action', actionName };
  }

  runSpecFromContextMenu(): void {
    if (!this.specContextMenu) return;
    if (this.specContextMenu.type === 'action' && this.specContextMenu.actionName) {
      this.runActionWithOrchestrator(this.specContextMenu.actionName);
    } else {
      this.runSpecWithOrchestrator();
    }
    this.specContextMenu = null;
  }

  onMsgContextMenu(event: MouseEvent, content: string): void {
    event.preventDefault();
    event.stopPropagation();
    this.contextMenu = null;
    this.msgContextMenu = { x: event.clientX, y: event.clientY, content };
  }

  copyMessage(content: string): void {
    const selected = window.getSelection()?.toString().trim();
    navigator.clipboard.writeText(selected || content);
    this.msgContextMenu = null;
  }

  quoteMessage(content: string): void {
    const selected = window.getSelection()?.toString().trim();
    const text = selected || content;
    const quoted = text.split('\n').map(l => `> ${l}`).join('\n');
    this.input = quoted + '\n\n' + this.input;
    this.msgContextMenu = null;
  }

  requestDelete(session: AgentSession): void {
    this.contextMenu = null;
    // Delete all selected; if only 1 selected (the right-clicked one), just that
    const ids = this.selectedIds.size > 0 ? [...this.selectedIds] : [session.id];
    this.confirmDeleteSessions = this.sessions.filter(s => ids.includes(s.id));
    this.confirmDeleteSession = session; // keep for legacy modal compat
  }

  cancelDelete(): void {
    this.confirmDeleteSession = null;
    this.confirmDeleteSessions = [];
  }

  confirmDelete(): void {
    if (this.confirmDeleteSessions.length === 0) return;
    const toDelete = [...this.confirmDeleteSessions];
    this.confirmDeleteSession = null;
    this.confirmDeleteSessions = [];
    this.selectedIds = new Set();

    const deleteNext = (i: number) => {
      if (i >= toDelete.length) return;
      const s = toDelete[i];
      this.agentService.deleteSession(s.id).subscribe(() => {
        this.sessions = this.sessions.filter(x => x.id !== s.id);
        if (this.activeSession?.id === s.id) {
          this.activeSession = null;
          this.messages = [];
          this.specs = [];
          this.selectedSpec = null;
          this.selectedTask = null;
          this.selectedVersion = null;
        }
        deleteNext(i + 1);
      });
    };
    deleteNext(0);

    // After all deletions, select first available session
    setTimeout(() => {
      if (!this.activeSession && this.sessions.length > 0) {
        this.selectSession(this.sessions[0]);
      }
    }, toDelete.length * 100 + 50);
  }

  runQaSpec(version: SpecVersion): void {
    this.qaSpecLoading = true;
    this.qaSpecModal = null;
    this.agentService.qaSpec(version.id, this.activeProjectId, this.activeSelectedModules).subscribe({
      next: result => {
        this.qaSpecLoading = false;
        this.qaSpecModal = { version, result };
      },
      error: () => { this.qaSpecLoading = false; }
    });
  }

  applyImprovedSpec(): void {
    if (!this.qaSpecModal?.result.improvedSpec) return;
    const { version, result } = this.qaSpecModal;
    this.qaSpecApplying = true;
    this.agentService.updateVersion(version.id, result.improvedSpec!).subscribe({
      next: updated => {
        // Replace version in the tree
        for (const spec of this.specs) {
          for (const task of spec.tasks) {
            const idx = task.versions.findIndex(v => v.id === version.id);
            if (idx >= 0) {
              task.versions[idx] = updated;
              if (this.selectedVersion?.id === version.id) this.selectedVersion = updated;
            }
          }
        }
        this.qaSpecApplying = false;
        this.qaSpecModal = null;
      },
      error: () => { this.qaSpecApplying = false; }
    });
  }

  newSession(): void {
    const title = 'BA Session ' + new Date().toLocaleString('vi-VN', {
      day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit'
    });
    this.agentService.createSession(title).subscribe(session => {
      this.sessions.unshift(session);
      this.selectSession(session);
    });
  }

  // ── Chat ─────────────────────────────────────────────────

  send(): void {
    const text = this.input.trim();
    if (!text || this.loading || !this.activeSession) return;

    this.messages.push({
      id: crypto.randomUUID(),
      sessionId: this.activeSession.id,
      role: 'user',
      content: text,
      createdAt: new Date().toISOString()
    });
    this.shouldScrollToBottom = true;

    this.input = '';
    this.loading = true;

    this.agentService.chat(this.activeSession.id, text, this.selectedVersion?.id, this.activeProjectId, this.activeSelectedModules).subscribe({
      next: response => {
        this.messages.push({
          id: crypto.randomUUID(),
          sessionId: this.activeSession!.id,
          role: 'assistant',
          content: response,
          createdAt: new Date().toISOString()
        });
        this.loading = false;
        this.shouldScrollToBottom = true;
        this.loadSpecs();
      },
      error: (err) => {
        this.loading = false;
        if (err.status === 401) { this.authService.logout(); }
        else if (err.status === 429) {
          this.tokenWarning = 'Bạn đã vượt giới hạn token tuần này. Liên hệ admin để tăng giới hạn.';
        }
      }
    });
  }

  onEnter(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.send();
    }
  }

  // ── Spec ─────────────────────────────────────────────────

  loadSpecs(): void {
    if (!this.activeSession) return;
    this.agentService.getSpecs(this.activeSession.id).subscribe(specs => {
      this.specs = specs;

      if (specs.length > 0) {
        // Auto-expand the first spec module
        if (this.expandedSpecs.size === 0) {
          this.expandedSpecs.add(specs[0].id);
        }

        // Try to refresh selected version
        if (this.selectedVersion) {
          let found = false;
          for (const spec of specs) {
            for (const task of spec.tasks) {
              const refreshed = task.versions.find(v => v.id === this.selectedVersion!.id);
              if (refreshed) {
                this.selectedVersion = refreshed;
                this.selectedTask = task;
                this.selectedSpec = spec;
                found = true;
                break;
              }
            }
            if (found) break;
          }
          if (!found) {
            // Auto-select latest version in first task of first spec
            this.autoSelectLatest(specs);
          }
        } else {
          this.autoSelectLatest(specs);
        }
      }
    });
  }

  private autoSelectLatest(specs: Spec[]): void {
    if (specs.length === 0) return;
    const firstSpec = specs[0];
    if (firstSpec.tasks.length === 0) return;
    const firstTask = firstSpec.tasks[0];
    if (firstTask.versions.length === 0) return;
    this.selectedSpec = firstSpec;
    this.selectedTask = firstTask;
    this.selectedVersion = firstTask.versions[0];
    this.expandedSpecs.add(firstSpec.id);
    this.showDiff = false;
    this.compareVersion = null;
    this.expandedActions.clear();
  }

  toggleSpec(spec: Spec): void {
    if (this.expandedSpecs.has(spec.id)) {
      this.expandedSpecs.delete(spec.id);
    } else {
      this.expandedSpecs.add(spec.id);
    }
  }

  isSpecExpanded(spec: Spec): boolean {
    return this.expandedSpecs.has(spec.id);
  }

  selectTask(task: SpecTask): void {
    if (this.selectedTask?.id === task.id) {
      this.selectedTask = null;
      this.selectedVersion = null;
    } else {
      this.selectedTask = task;
      this.selectedVersion = task.versions.length > 0 ? task.versions[0] : null;
      this.showDiff = false;
      this.compareVersion = null;
      this.expandedActions.clear();
    }
  }

  selectVersion(v: SpecVersion, task: SpecTask, spec: Spec): void {
    if (this.selectedVersion?.id === v.id) {
      this.selectedVersion = null;
      return;
    }
    this.selectedSpec = spec;
    this.selectedTask = task;
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
    this.orchestratorService.getFilesForVersion(versionId).subscribe({
      next: files => {
        this.versionFiles = files;
        if (files.length > 0 && this.specViewMode === 'code') {
          this.selectedGenFile = files[0];
        }
      },
      error: () => { this.versionFiles = []; }
    });
  }

  selectGenFile(f: GeneratedFile): void {
    this.selectedGenFile = f;
  }

  deleteAction(index: number): void {
    const spec = this.parsedSpec;
    if (!spec || !this.selectedVersion) return;
    spec.actions.splice(index, 1);
    const updated = JSON.stringify(spec, null, 2);
    this.agentService.updateVersion(this.selectedVersion.id, updated).subscribe(saved => {
      if (this.selectedVersion) {
        this.selectedVersion = saved;
      }
      // Refresh the version in the tree
      this.loadSpecs();
      this.expandedActions.clear();
    });
  }

  deleteVersion(v: SpecVersion, event: MouseEvent): void {
    event.stopPropagation();
    if (!confirm(`Xóa version v${v.version}?`)) return;
    this.agentService.deleteVersion(v.id).subscribe(() => {
      if (this.selectedVersion?.id === v.id) {
        this.selectedVersion = null;
        this.expandedActions.clear();
      }
      this.loadSpecs();
    });
  }

  deleteTask(task: SpecTask, spec: Spec, event: MouseEvent): void {
    event.stopPropagation();
    if (!confirm(`Xóa task "${task.name}" và tất cả versions?`)) return;
    this.agentService.deleteTask(task.id).subscribe(() => {
      if (this.selectedTask?.id === task.id) {
        this.selectedTask = null;
        this.selectedVersion = null;
        this.expandedActions.clear();
      }
      this.loadSpecs();
    });
  }

  deleteSpec(spec: Spec, event: MouseEvent): void {
    event.stopPropagation();
    if (!confirm(`Xóa module "${spec.name}" và tất cả tasks và versions?`)) return;
    this.agentService.deleteSpec(spec.id).subscribe(() => {
      if (this.selectedSpec?.id === spec.id) {
        this.selectedSpec = null;
        this.selectedTask = null;
        this.selectedVersion = null;
        this.expandedActions.clear();
      }
      this.expandedSpecs.delete(spec.id);
      this.loadSpecs();
    });
  }

  get parsedSpec(): ParsedSpec | null {
    if (!this.selectedVersion) return null;
    try { return JSON.parse(this.selectedVersion.jsonSpec); }
    catch { return null; }
  }

  toggleAction(index: number): void {
    if (this.expandedActions.has(index)) this.expandedActions.delete(index);
    else this.expandedActions.add(index);
  }

  isActionExpanded(index: number): boolean {
    return this.expandedActions.has(index);
  }

  toggleDiff(v: SpecVersion): void {
    if (this.compareVersion?.id === v.id) {
      this.showDiff = false;
      this.compareVersion = null;
      return;
    }
    this.compareVersion = v;
    this.showDiff = true;
    this.buildDiff();
  }

  buildDiff(): void {
    if (!this.selectedVersion || !this.compareVersion) return;
    try {
      const oldObj = JSON.parse(this.compareVersion.jsonSpec);
      const newObj = JSON.parse(this.selectedVersion.jsonSpec);
      this.diffLines = this.diffObjects(oldObj, newObj, '');
    } catch {
      this.diffLines = [];
    }
  }

  private diffObjects(oldObj: any, newObj: any, prefix: string): DiffLine[] {
    const lines: DiffLine[] = [];
    const allKeys = new Set([...Object.keys(oldObj || {}), ...Object.keys(newObj || {})]);

    for (const key of allKeys) {
      const fullKey = prefix ? `${prefix}.${key}` : key;
      const oldVal = oldObj?.[key];
      const newVal = newObj?.[key];

      if (!(key in (oldObj || {}))) {
        lines.push({ key: fullKey, status: 'added', newVal: this.stringify(newVal) });
      } else if (!(key in (newObj || {}))) {
        lines.push({ key: fullKey, status: 'removed', oldVal: this.stringify(oldVal) });
      } else if (typeof oldVal === 'object' && typeof newVal === 'object' &&
                 !Array.isArray(oldVal) && !Array.isArray(newVal) &&
                 oldVal !== null && newVal !== null) {
        lines.push(...this.diffObjects(oldVal, newVal, fullKey));
      } else {
        const ov = this.stringify(oldVal);
        const nv = this.stringify(newVal);
        if (ov !== nv) {
          lines.push({ key: fullKey, status: 'changed', oldVal: ov, newVal: nv });
        } else {
          lines.push({ key: fullKey, status: 'unchanged', newVal: nv });
        }
      }
    }
    return lines;
  }

  private stringify(val: any): string {
    if (val === null || val === undefined) return 'null';
    if (typeof val === 'object') return JSON.stringify(val);
    return String(val);
  }

  formatSpec(version: SpecVersion | null): string {
    if (!version) return '';
    try { return JSON.stringify(JSON.parse(version.jsonSpec), null, 2); }
    catch { return version.jsonSpec; }
  }

  get hasDiffChanges(): boolean {
    return this.diffLines.some(l => l.status !== 'unchanged');
  }

  // ── Screen list ──────────────────────────────────────────

  parseScreenList(content: string): ScreenListResponse | null {
    try {
      const match = content.match(/```(?:json)?\s*(\{[\s\S]*?\})\s*```/);
      const raw = match ? match[1] : content.match(/\{[\s\S]*\}/)?.[0];
      if (!raw) return null;
      const parsed = JSON.parse(raw);
      if (parsed?.type === 'screen_list' && Array.isArray(parsed.screens)) return parsed;
      return null;
    } catch { return null; }
  }

  requestScreenDetail(screen: ScreenItem, moduleName: string): void {
    if (!this.activeSession || this.loading) return;
    const text = `[SCREEN_DETAIL]\nMàn hình: ${screen.name}\nModule: ${moduleName}\nMô tả: ${screen.description}`;
    this.input = text;
    this.send();
  }

  // ── Util ─────────────────────────────────────────────────

  private scrollToBottom(): void {
    try {
      const el = this.messageList?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    } catch {}
  }
}
