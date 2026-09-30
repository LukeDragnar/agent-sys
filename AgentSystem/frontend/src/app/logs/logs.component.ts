import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { LogsService, OrchestratorLog } from '../services/logs.service';

const CATEGORIES  = ['', 'ORCHESTRATOR', 'CHAT', 'CLAUDE', 'MCP', 'AUTH'];
const AGENT_TYPES = ['', 'BA', 'DEV', 'DEV_PATCH', 'QA', 'FIX_COMPILE', 'GIT_PUSH', 'SPLITTER', 'LOGIN'];

@Component({
  selector: 'app-logs',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './logs.component.html',
  styleUrl: './logs.component.scss'
})
export class LogsComponent implements OnInit {
  logs: OrchestratorLog[] = [];
  loading = false;
  categories  = CATEGORIES;
  agentTypes  = AGENT_TYPES;
  filterCategory  = '';
  filterAgentType = '';
  filterFrom = '';
  filterTo   = '';
  selected: OrchestratorLog | null = null;

  constructor(private logsService: LogsService, private router: Router) {}

  ngOnInit(): void { this.load(); }

  load(): void {
    this.loading = true;
    this.logsService.getLogs(
      this.filterCategory  || undefined,
      this.filterAgentType || undefined,
      undefined, undefined,
      this.filterFrom || undefined,
      this.filterTo   || undefined
    ).subscribe({
      next: data => { this.logs = data; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  select(log: OrchestratorLog): void {
    this.selected = this.selected?.id === log.id ? null : log;
  }

  categoryBadgeClass(cat: string): string {
    const map: Record<string, string> = {
      ORCHESTRATOR: 'badge-orch', CHAT: 'badge-chat',
      CLAUDE: 'badge-claude', MCP: 'badge-mcp', AUTH: 'badge-auth'
    };
    return map[cat] ?? 'badge-default';
  }

  agentBadgeClass(type: string): string {
    const map: Record<string, string> = {
      BA: 'badge-ba', DEV: 'badge-dev', DEV_PATCH: 'badge-patch',
      QA: 'badge-qa', FIX_COMPILE: 'badge-fix', GIT_PUSH: 'badge-git',
      SPLITTER: 'badge-split', LOGIN: 'badge-auth'
    };
    return map[type] ?? 'badge-default';
  }

  back(): void { this.router.navigate(['/dashboard']); }
}
