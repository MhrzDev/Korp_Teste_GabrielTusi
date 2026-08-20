import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { NotificationService } from './core/notification.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="shell">
      <aside class="sidebar">
        <a routerLink="/" class="brand" aria-label="NotaFlow início">
          <span class="brand-mark">N</span>
          <span><strong>NotaFlow</strong><small>Gestão Fiscal</small></span>
        </a>

        <nav aria-label="Navegação principal">
          <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">
            <span>⌂</span> Visão geral
          </a>
          <a routerLink="/produtos" routerLinkActive="active">
            <span>□</span> Produtos
          </a>
          <a routerLink="/notas" routerLinkActive="active">
            <span>▤</span> Notas fiscais
          </a>
        </nav>

        <div class="sidebar-footer">
          <span class="status-dot"></span>
          <span><strong>Sistema online</strong><small>Microsserviços ativos</small></span>
        </div>
      </aside>

      <main class="main-content">
        <header class="topbar">
          <div>
            <span class="eyebrow">PAINEL OPERACIONAL</span>
          </div>
          <div class="profile"><span>GT</span><div><strong>Gabriel Tusi</strong><small>Administrador</small></div></div>
        </header>
        <router-outlet />
      </main>
    </div>

    @if (notifications.current(); as notification) {
      <button class="toast" [class.toast-error]="notification.type === 'error'"
              (click)="notifications.clear()" aria-label="Fechar notificação">
        <span>{{ notification.type === 'success' ? '✓' : '!' }}</span>
        {{ notification.message }}
      </button>
    }
  `
})
export class AppComponent {
  readonly notifications = inject(NotificationService);
}
