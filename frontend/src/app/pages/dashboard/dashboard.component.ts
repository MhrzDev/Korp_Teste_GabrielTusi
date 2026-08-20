import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { Invoice, Product } from '../../core/models';

@Component({
  standalone: true,
  imports: [DatePipe, RouterLink],
  template: `
    <section class="page">
      <div class="page-heading">
        <div>
          <span class="section-label">VISÃO GERAL</span>
          <h1>Controle fiscal, sem complicação.</h1>
          <p>Acompanhe produtos, estoque e emissão de notas em um só lugar.</p>
        </div>
        <a routerLink="/notas" class="btn btn-primary">＋ Nova nota fiscal</a>
      </div>

      <div class="grid grid-4">
        <article class="card stat-card">
          <span>PRODUTOS CADASTRADOS</span><strong>{{ products().length }}</strong>
          <small>Catálogo atualizado</small>
        </article>
        <article class="card stat-card">
          <span>UNIDADES EM ESTOQUE</span><strong>{{ totalStock() }}</strong>
          <small>Disponíveis para faturamento</small>
        </article>
        <article class="card stat-card">
          <span>NOTAS ABERTAS</span><strong>{{ openInvoices() }}</strong>
          <small>Aguardando impressão</small>
        </article>
        <article class="card stat-card">
          <span>NOTAS FECHADAS</span><strong>{{ closedInvoices() }}</strong>
          <small>Processadas com sucesso</small>
        </article>
      </div>

      <div class="grid grid-2" style="margin-top:20px">
        <article class="card">
          <div class="card-title"><h2>Notas recentes</h2><a routerLink="/notas">Ver todas →</a></div>
          @if (loading()) {
            <div class="loading">Carregando...</div>
          } @else if (invoices().length === 0) {
            <div class="empty-state"><strong>Nenhuma nota emitida</strong>Crie a primeira nota fiscal.</div>
          } @else {
            <div class="table-wrap"><table>
              <thead><tr><th>Número</th><th>Data</th><th>Status</th></tr></thead>
              <tbody>
                @for (invoice of invoices().slice(0, 5); track invoice.id) {
                  <tr>
                    <td><strong>{{ invoice.number }}</strong></td>
                    <td>{{ invoice.createdAtUtc | date:'dd/MM/yyyy HH:mm' }}</td>
                    <td><span class="badge" [class.badge-open]="invoice.status === 'Open'"
                              [class.badge-closed]="invoice.status === 'Closed'">
                      {{ invoice.status === 'Open' ? 'Aberta' : 'Fechada' }}
                    </span></td>
                  </tr>
                }
              </tbody>
            </table></div>
          }
        </article>

        <article class="card">
          <div class="card-title"><h2>Atenção ao estoque</h2><a routerLink="/produtos">Gerenciar →</a></div>
          @if (lowStock().length === 0) {
            <div class="empty-state"><strong>Estoque saudável</strong>Nenhum produto com saldo baixo.</div>
          } @else {
            <div class="table-wrap"><table>
              <thead><tr><th>Produto</th><th>Código</th><th>Saldo</th></tr></thead>
              <tbody>
                @for (product of lowStock(); track product.id) {
                  <tr>
                    <td><strong>{{ product.description }}</strong></td><td>{{ product.code }}</td>
                    <td><span class="badge badge-danger">{{ product.stock }} un.</span></td>
                  </tr>
                }
              </tbody>
            </table></div>
          }
        </article>
      </div>
    </section>
  `
})
export class DashboardComponent implements OnInit {
  private readonly api = inject(ApiService);
  readonly products = signal<Product[]>([]);
  readonly invoices = signal<Invoice[]>([]);
  readonly loading = signal(true);
  readonly totalStock = () => this.products().reduce((total, product) => total + product.stock, 0);
  readonly openInvoices = () => this.invoices().filter(invoice => invoice.status === 'Open').length;
  readonly closedInvoices = () => this.invoices().filter(invoice => invoice.status === 'Closed').length;
  readonly lowStock = () => this.products().filter(product => product.stock <= 5).slice(0, 5);

  ngOnInit(): void {
    forkJoin({ products: this.api.getProducts(), invoices: this.api.getInvoices() }).subscribe({
      next: result => { this.products.set(result.products); this.invoices.set(result.invoices); },
      complete: () => this.loading.set(false),
      error: () => this.loading.set(false)
    });
  }
}

