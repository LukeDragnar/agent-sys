import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-nav-rail',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterLinkActive],
  templateUrl: './nav-rail.component.html',
  styleUrl: './nav-rail.component.scss'
})
export class NavRailComponent {
  constructor(public auth: AuthService) {}
  logout() {
    if (confirm('Bạn có chắc muốn đăng xuất?')) this.auth.logout();
  }
}
