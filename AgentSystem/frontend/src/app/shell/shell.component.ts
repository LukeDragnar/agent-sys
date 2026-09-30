import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NavRailComponent } from '../nav-rail/nav-rail.component';
import { ProjectHeaderComponent } from '../project-header/project-header.component';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, NavRailComponent, ProjectHeaderComponent],
  template: `
    <div class="shell">
      <app-nav-rail></app-nav-rail>
      <div class="shell-main">
        <app-project-header></app-project-header>
        <div class="shell-content">
          <router-outlet></router-outlet>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .shell { display: flex; height: 100vh; overflow: hidden; }
    .shell-main { flex: 1; min-width: 0; display: flex; flex-direction: column; overflow: hidden; }
    .shell-content { flex: 1; min-width: 0; min-height: 0; display: flex; flex-direction: column; overflow: hidden; }
  `]
})
export class ShellComponent {}
