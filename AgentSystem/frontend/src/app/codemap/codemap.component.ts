import { Component, OnInit, OnDestroy, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CodeMapService, ScanStats, ScanProgressResult, SymbolResult, EndpointResult, DependencyTreeResult, SearchResult, TreeNode, GraphData, GraphNode, GraphEdge } from '../services/codemap.service';
import { ProjectService, Project } from '../services/project.service';
import * as d3 from 'd3';

type Tab = 'overview' | 'search' | 'endpoints' | 'explorer' | 'dependencies';

@Component({
  selector: 'app-codemap',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './codemap.component.html',
  styleUrl: './codemap.component.scss'
})
export class CodeMapComponent implements OnInit, OnDestroy {
  // Project
  projects: Project[] = [];
  selectedProjectId = '';
  scanPath = '';

  // State
  tab: Tab = 'overview';
  loading = false;
  scanning = false;
  scanProgress: ScanProgressResult | null = null;
  stats: ScanStats | null = null;
  private progressTimer: any = null;

  // Search
  searchQuery = '';
  searchResults: SearchResult[] = [];
  selectedSymbol: SymbolResult | null = null;

  // Endpoints
  endpointRoute = '';
  endpointMethod = '';
  endpoints: EndpointResult[] = [];

  // Tree
  tree: TreeNode[] = [];

  // Dependencies
  depSymbol = '';
  depTree: DependencyTreeResult | null = null;

  // Graph
  graphData: GraphData | null = null;
  graphLevel: 'modules' | 'classes' | 'symbol' = 'modules';
  graphCurrentModule = '';
  showEdges = true;
  private simulation: d3.Simulation<GraphNode, GraphEdge> | null = null;

  // Resizable panels
  treePanelWidth = 320;
  detailPanelWidth = 340;
  private resizing: 'tree' | 'detail' | null = null;
  private resizeStartX = 0;
  private resizeStartWidth = 0;

  constructor(
    private codeMap: CodeMapService,
    private projectService: ProjectService
  ) {}

  ngOnInit(): void {
    this.projectService.getAll().subscribe(p => {
      this.projects = p;
      if (p.length) {
        this.selectedProjectId = p[0].id;
        this.loadStats();
      }
    });
  }

  onProjectChange(): void {
    this.stats = null;
    this.searchResults = [];
    this.endpoints = [];
    this.tree = [];
    this.depTree = null;
    this.selectedSymbol = null;
    this.graphData = null;
    this.loadStats();
  }

  loadStats(): void {
    if (!this.selectedProjectId) return;
    this.loading = true;
    this.codeMap.getStats(this.selectedProjectId).subscribe({
      next: s => { this.stats = s; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  ngOnDestroy(): void {
    this.stopProgressPolling();
    this.destroyGraph();
  }

  scan(): void {
    if (!this.selectedProjectId || !this.scanPath) return;
    this.scanning = true;
    this.scanProgress = { isRunning: true, totalFiles: 0, processedFiles: 0, percent: 0, phase: 'discovering' };
    this.startProgressPolling();

    this.codeMap.scan(this.selectedProjectId, this.scanPath).subscribe({
      next: s => { this.stats = s; this.scanning = false; this.scanProgress = null; this.stopProgressPolling(); this.tab = 'overview'; },
      error: () => { this.scanning = false; this.scanProgress = null; this.stopProgressPolling(); }
    });
  }

  private startProgressPolling(): void {
    this.stopProgressPolling();
    this.progressTimer = setInterval(() => {
      if (!this.selectedProjectId) return;
      this.codeMap.getScanProgress(this.selectedProjectId).subscribe({
        next: p => { this.scanProgress = p; },
        error: () => {}
      });
    }, 1000);
  }

  private stopProgressPolling(): void {
    if (this.progressTimer) { clearInterval(this.progressTimer); this.progressTimer = null; }
  }

  // Search
  doSearch(): void {
    if (!this.selectedProjectId || !this.searchQuery) return;
    this.loading = true;
    this.selectedSymbol = null;
    this.codeMap.search(this.selectedProjectId, this.searchQuery, 30).subscribe({
      next: r => { this.searchResults = r; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  viewSymbol(name: string, type: string): void {
    this.codeMap.findSymbol(this.selectedProjectId, name, type !== 'Method' && type !== 'Property' ? type : undefined).subscribe({
      next: r => {
        this.selectedSymbol = r.find(s => s.name === name) ?? r[0] ?? null;
      },
      error: () => {}
    });
  }

  // Endpoints
  loadEndpoints(): void {
    if (!this.selectedProjectId) return;
    this.loading = true;
    this.codeMap.findEndpoint(this.selectedProjectId, this.endpointRoute || undefined, this.endpointMethod || undefined).subscribe({
      next: e => { this.endpoints = e; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  // Tree
  loadTree(): void {
    if (!this.selectedProjectId) return;
    this.loading = true;
    this.codeMap.getTree(this.selectedProjectId).subscribe({
      next: t => { this.tree = t; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  toggleNode(node: TreeNode): void {
    node.expanded = !node.expanded;
  }

  onTreeNodeClick(node: TreeNode): void {
    if (node.type === 'class' || node.type === 'interface' || node.type === 'struct' || node.type === 'record'
        || node.type === 'component' || node.type === 'service') {
      this.viewSymbol(node.name, node.type.charAt(0).toUpperCase() + node.type.slice(1));
      this.loadGraphForSymbol(node.name);
    } else if (node.type === 'folder' && !node.filePath) {
      // Top-level folder = module → load module classes graph
      this.toggleNode(node);
      this.loadGraphForModule(node.name);
    } else {
      this.toggleNode(node);
    }
  }

  onTreeSymbolClick(name: string): void {
    this.viewSymbol(name, 'Class');
    this.loadGraphForSymbol(name);
  }

  // Dependencies
  loadDeps(): void {
    if (!this.selectedProjectId || !this.depSymbol) return;
    this.loading = true;
    this.codeMap.getDependencies(this.selectedProjectId, this.depSymbol, 2).subscribe({
      next: d => { this.depTree = d; this.loading = false; },
      error: () => { this.loading = false; }
    });
  }

  // Graph
  loadModuleGraph(): void {
    if (!this.selectedProjectId) return;
    this.graphLevel = 'modules';
    this.graphCurrentModule = '';
    this.codeMap.getModuleGraph(this.selectedProjectId).subscribe({
      next: data => {
        this.graphData = data;
        setTimeout(() => this.renderGraph(), 100);
      },
      error: () => {}
    });
  }

  private loadGraphForModule(moduleName: string): void {
    if (!this.selectedProjectId) return;
    this.graphLevel = 'classes';
    this.graphCurrentModule = moduleName;
    this.codeMap.getGraph(this.selectedProjectId, moduleName).subscribe({
      next: data => {
        this.graphData = data;
        setTimeout(() => this.renderGraph(), 100);
      },
      error: () => {}
    });
  }

  private loadGraphForSymbol(name: string): void {
    if (!this.selectedProjectId) return;
    this.graphLevel = 'symbol';
    this.codeMap.getGraph(this.selectedProjectId, name).subscribe({
      next: data => {
        this.graphData = data;
        setTimeout(() => this.renderGraph(), 100);
      },
      error: () => {}
    });
  }

  toggleEdgeVisibility(): void {
    d3.select('#graph-container').selectAll('line')
      .attr('stroke-opacity', this.showEdges ? 0.15 : 0);
  }

  private destroyGraph(): void {
    if (this.simulation) { this.simulation.stop(); this.simulation = null; }
    d3.select('#graph-container').selectAll('svg').remove();
  }

  private renderGraph(): void {
    this.destroyGraph();
    if (!this.graphData || !this.graphData.nodes.length) return;

    const container = document.getElementById('graph-container');
    if (!container) return;

    const rect = container.getBoundingClientRect();
    const width = rect.width || 400;
    const height = rect.height || 400;

    const svg = d3.select('#graph-container')
      .append('svg')
      .attr('width', '100%')
      .attr('height', '100%')
      .attr('viewBox', `0 0 ${width} ${height}`);

    const g = svg.append('g');

    const nodes = this.graphData.nodes;
    const edges = this.graphData.edges;

    const colorMap: Record<string, string> = {
      Module: '#c084fc', Controller: '#f472b6', Service: '#38bdf8', Repository: '#34d399',
      Interface: '#a78bfa', Entity: '#fbbf24', Model: '#fb923c',
      Hub: '#f87171', Consumer: '#2dd4bf', Class: '#94a3b8', Component: '#f472b6',
    };

    const isModuleLevel = this.graphLevel === 'modules';
    const nodeRadius = (d: GraphNode) => {
      if (isModuleLevel) return 10 + Math.min(Math.sqrt(d.methodCount || 1) * 3, 40);
      return 5 + Math.min(d.methodCount || 0, 30) * 0.5;
    };

    const chargeStrength = isModuleLevel ? -400 - nodes.length * 5 : -150;
    const linkDist = isModuleLevel ? 100 + nodes.length * 2 : 80;

    this.simulation = d3.forceSimulation<GraphNode>(nodes)
      .force('link', d3.forceLink<GraphNode, GraphEdge>(edges).id(d => d.id).distance(linkDist))
      .force('charge', d3.forceManyBody().strength(chargeStrength))
      .force('center', d3.forceCenter(width / 2, height / 2))
      .force('x', d3.forceX(width / 2).strength(0.05))
      .force('y', d3.forceY(height / 2).strength(0.05))
      .force('collision', d3.forceCollide<GraphNode>().radius(d => nodeRadius(d) + 8));

    // Edges — hidden by default on module level, show on hover
    const link = g.append('g')
      .selectAll('line')
      .data(edges)
      .join('line')
      .attr('stroke', d => {
        if (isModuleLevel) return '#6366f1';
        return d.type === 'Injection' ? '#38bdf8' : d.type === 'Inheritance' ? '#f472b6' : '#34d399';
      })
      .attr('stroke-opacity', isModuleLevel ? 0.15 : 0.35)
      .attr('stroke-width', d => isModuleLevel ? Math.min(parseInt(d.type as string) || 1, 5) * 0.5 : 1);

    const self = this;

    const nodeG = g.append('g')
      .selectAll('g')
      .data(nodes)
      .join('g')
      .attr('cursor', 'pointer')
      .call(d3.drag<SVGGElement, GraphNode>()
        .on('start', (event, d) => { if (!event.active) self.simulation!.alphaTarget(0.3).restart(); d.fx = d.x; d.fy = d.y; })
        .on('drag', (event, d) => { d.fx = event.x; d.fy = event.y; })
        .on('end', (event, d) => { if (!event.active) self.simulation!.alphaTarget(0); d.fx = null; d.fy = null; }) as any);

    nodeG.append('circle')
      .attr('r', d => nodeRadius(d))
      .attr('fill', d => colorMap[d.type] || '#94a3b8')
      .attr('stroke', '#1e1e2e')
      .attr('stroke-width', 1.5)
      .on('click', (_event: any, d: GraphNode) => {
        if (d.type === 'Module') {
          self.loadGraphForModule(d.name);
        } else {
          self.viewSymbol(d.name, d.type);
          self.loadGraphForSymbol(d.name);
        }
      })
      .on('mouseenter', (_event: any, d: GraphNode) => {
        // Highlight connected edges on hover
        link.attr('stroke-opacity', (e: any) => {
          const src = typeof e.source === 'string' ? e.source : e.source.id;
          const tgt = typeof e.target === 'string' ? e.target : e.target.id;
          return src === d.id || tgt === d.id ? 0.7 : (isModuleLevel ? 0.05 : 0.1);
        }).attr('stroke-width', (e: any) => {
          const src = typeof e.source === 'string' ? e.source : e.source.id;
          const tgt = typeof e.target === 'string' ? e.target : e.target.id;
          return src === d.id || tgt === d.id ? 2 : 1;
        });
      })
      .on('mouseleave', () => {
        link.attr('stroke-opacity', isModuleLevel ? 0.15 : 0.35)
            .attr('stroke-width', d => isModuleLevel ? Math.min(parseInt(d.type as string) || 1, 5) * 0.5 : 1);
      });

    nodeG.append('text')
      .text(d => d.name)
      .attr('dx', d => nodeRadius(d) + 4)
      .attr('dy', 4)
      .attr('font-size', isModuleLevel ? '12px' : '10px')
      .attr('font-weight', isModuleLevel ? '600' : '400')
      .attr('fill', '#a6adc8')
      .attr('pointer-events', 'none');

    // Module level: show class count inside circle
    if (isModuleLevel) {
      nodeG.append('text')
        .text(d => d.methodCount > 0 ? d.methodCount.toString() : '')
        .attr('text-anchor', 'middle')
        .attr('dy', 4)
        .attr('font-size', '9px')
        .attr('fill', '#1e1e2e')
        .attr('font-weight', '700')
        .attr('pointer-events', 'none');
    }

    const zoomBehavior = d3.zoom<SVGSVGElement, unknown>()
      .scaleExtent([0.1, 4])
      .on('zoom', (event) => g.attr('transform', event.transform));
    svg.call(zoomBehavior as any);

    const fitToView = () => {
      const xs = nodes.map(n => n.x ?? 0);
      const ys = nodes.map(n => n.y ?? 0);
      if (!xs.length) return;
      const pad = 80;
      const minX = Math.min(...xs) - pad;
      const maxX = Math.max(...xs) + pad;
      const minY = Math.min(...ys) - pad;
      const maxY = Math.max(...ys) + pad;
      const bw = maxX - minX || 1;
      const bh = maxY - minY || 1;
      const scale = Math.min(width / bw, height / bh, 1.2) * 0.85;
      const tx = (width - bw * scale) / 2 - minX * scale;
      const ty = (height - bh * scale) / 2 - minY * scale;
      svg.transition().duration(400).call(
        zoomBehavior.transform as any,
        d3.zoomIdentity.translate(tx, ty).scale(scale)
      );
    };

    let fitted = false;
    this.simulation.on('tick', () => {
      link
        .attr('x1', d => (d.source as GraphNode).x ?? 0)
        .attr('y1', d => (d.source as GraphNode).y ?? 0)
        .attr('x2', d => (d.target as GraphNode).x ?? 0)
        .attr('y2', d => (d.target as GraphNode).y ?? 0);
      nodeG.attr('transform', d => `translate(${d.x ?? 0},${d.y ?? 0})`);
    });

    // Fit after simulation cools down
    this.simulation.on('end', () => { if (!fitted) { fitted = true; fitToView(); } });
    // Also fit after 2 seconds in case simulation is slow
    setTimeout(() => { if (!fitted) { fitted = true; fitToView(); } }, 2000);
  }

  // Resizable panels
  startResize(event: MouseEvent, panel: 'tree' | 'detail'): void {
    event.preventDefault();
    this.resizing = panel;
    this.resizeStartX = event.clientX;
    this.resizeStartWidth = panel === 'tree' ? this.treePanelWidth : this.detailPanelWidth;
  }

  @HostListener('document:mousemove', ['$event'])
  onMouseMove(event: MouseEvent): void {
    if (!this.resizing) return;
    const dx = event.clientX - this.resizeStartX;
    const newWidth = Math.max(150, Math.min(800, this.resizeStartWidth + dx));
    if (this.resizing === 'tree') this.treePanelWidth = newWidth;
    else this.detailPanelWidth = newWidth;
  }

  @HostListener('document:mouseup')
  onMouseUp(): void {
    if (this.resizing) {
      this.resizing = null;
      // Re-render graph with new size
      if (this.graphData) setTimeout(() => this.renderGraph(), 50);
    }
  }

  setTab(tab: Tab): void {
    this.tab = tab;
    if (tab === 'endpoints' && !this.endpoints.length) this.loadEndpoints();
    if (tab === 'explorer') {
      if (!this.tree.length) this.loadTree();
      if (!this.graphData) this.loadModuleGraph();
    }
  }

  symbolTypeIcon(type: string): string {
    const map: Record<string, string> = { Class: 'C', Interface: 'I', Enum: 'E', Struct: 'S', Record: 'R', Method: 'M', Property: 'P', Type: 'T' };
    return map[type] ?? '?';
  }

  symbolTypeClass(type: string): string {
    const map: Record<string, string> = { Class: 'sym-class', Interface: 'sym-interface', Enum: 'sym-enum', Method: 'sym-method', Property: 'sym-property' };
    return map[type] ?? 'sym-default';
  }

  httpMethodClass(method: string): string {
    const map: Record<string, string> = { GET: 'http-get', POST: 'http-post', PUT: 'http-put', DELETE: 'http-delete', PATCH: 'http-patch' };
    return map[method] ?? '';
  }

  depTypeClass(type: string): string {
    const map: Record<string, string> = { Injection: 'dep-inject', Inheritance: 'dep-inherit', Implementation: 'dep-impl' };
    return map[type] ?? 'dep-default';
  }

  treeNodeIcon(type: string): string {
    if (type === 'folder') return '>';
    if (type === 'file') return '#';
    return this.symbolTypeIcon(type.charAt(0).toUpperCase() + type.slice(1));
  }
}
