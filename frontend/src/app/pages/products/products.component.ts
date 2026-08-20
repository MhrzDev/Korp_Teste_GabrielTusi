import { Component, OnInit, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { finalize } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { NotificationService } from '../../core/notification.service';
import { Product } from '../../core/models';

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, DatePipe],
  template: `
    <section class="page">
      <div class="page-heading">
        <div><span class="section-label">ESTOQUE</span><h1>Produtos</h1><p>Cadastre itens e mantenha os saldos atualizados.</p></div>
        <button class="btn btn-primary" (click)="showForm.set(!showForm())">＋ Novo produto</button>
      </div>

      @if (showForm()) {
        <article class="card" style="margin-bottom:20px">
          <div class="card-title"><h2>{{ editingId() ? 'Editar produto' : 'Cadastrar produto' }}</h2></div>
          <form class="form-grid" [formGroup]="form" (ngSubmit)="save()">
            <div class="field"><label for="code">Código *</label><input id="code" formControlName="code" placeholder="PROD-001"></div>
            <div class="field"><label for="stock">Saldo inicial *</label><input id="stock" type="number" min="0" formControlName="stock"></div>
            <div class="field field-full"><label for="description">Descrição *</label><input id="description" formControlName="description" placeholder="Nome do produto"></div>
            <div class="form-actions">
              <button type="button" class="btn btn-secondary" (click)="cancel()">Cancelar</button>
              <button class="btn btn-primary" [disabled]="form.invalid || saving()">
                @if (saving()) { <span class="spinner"></span> } Salvar produto
              </button>
            </div>
          </form>
        </article>
      }

      <article class="card">
        <div class="card-title"><h2>Catálogo</h2><span class="badge badge-stock">{{ products().length }} produtos</span></div>
        @if (loading()) {
          <div class="loading">Carregando produtos...</div>
        } @else if (products().length === 0) {
          <div class="empty-state"><strong>Nenhum produto cadastrado</strong>Use “Novo produto” para começar.</div>
        } @else {
          <div class="table-wrap"><table>
            <thead><tr><th>Código</th><th>Descrição</th><th>Saldo</th><th>Atualização</th><th></th></tr></thead>
            <tbody>
              @for (product of products(); track product.id) {
                <tr>
                  <td><strong>{{ product.code }}</strong></td><td>{{ product.description }}</td>
                  <td><span class="badge" [class.badge-stock]="product.stock > 5" [class.badge-danger]="product.stock <= 5">{{ product.stock }} un.</span></td>
                  <td>{{ product.updatedAtUtc | date:'dd/MM/yyyy HH:mm' }}</td>
                  <td><div class="actions">
                    <button class="btn btn-secondary btn-small" (click)="edit(product)">Editar</button>
                    <button class="btn btn-danger btn-small" (click)="remove(product)">Excluir</button>
                  </div></td>
                </tr>
              }
            </tbody>
          </table></div>
        }
      </article>
    </section>
  `
})
export class ProductsComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly notifications = inject(NotificationService);
  private readonly fb = inject(FormBuilder);
  readonly products = signal<Product[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly showForm = signal(false);
  readonly editingId = signal<string | null>(null);
  readonly form = this.fb.nonNullable.group({
    code: ['', [Validators.required, Validators.maxLength(50)]],
    description: ['', [Validators.required, Validators.maxLength(200)]],
    stock: [0, [Validators.required, Validators.min(0)]]
  });

  ngOnInit(): void { this.load(); }

  load(): void {
    this.api.getProducts().pipe(finalize(() => this.loading.set(false)))
      .subscribe(products => this.products.set(products));
  }

  save(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    const value = this.form.getRawValue();
    const request = this.editingId()
      ? this.api.updateProduct(this.editingId()!, { description: value.description, stock: value.stock })
      : this.api.createProduct(value);

    request.pipe(finalize(() => this.saving.set(false))).subscribe(() => {
      this.notifications.show('Produto salvo com sucesso.', 'success');
      this.cancel(); this.loading.set(true); this.load();
    });
  }

  edit(product: Product): void {
    this.editingId.set(product.id); this.showForm.set(true);
    this.form.setValue({ code: product.code, description: product.description, stock: product.stock });
    this.form.controls.code.disable();
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  remove(product: Product): void {
    if (!confirm(`Excluir o produto ${product.code}?`)) return;
    this.api.deleteProduct(product.id).subscribe(() => {
      this.notifications.show('Produto excluído.', 'success'); this.load();
    });
  }

  cancel(): void {
    this.editingId.set(null); this.showForm.set(false); this.form.controls.code.enable();
    this.form.reset({ code: '', description: '', stock: 0 });
  }
}
