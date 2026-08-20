import { Component, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize, forkJoin } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { Invoice, InvoiceItem, Product } from '../../core/models';
import { NotificationService } from '../../core/notification.service';

@Component({
  standalone: true,
  imports: [ReactiveFormsModule, DatePipe],
  template: `
    <section class="page">
      <div class="page-heading">
        <div><span class="section-label">FATURAMENTO</span><h1>Notas fiscais</h1><p>Crie, acompanhe e feche as notas emitidas.</p></div>
        <button class="btn btn-primary" (click)="toggleForm()">＋ Nova nota fiscal</button>
      </div>

      @if (showForm()) {
        <article class="card" style="margin-bottom:20px">
          <div class="card-title"><h2>Nova nota fiscal</h2><span class="badge badge-open">Status inicial: Aberta</span></div>
          <form [formGroup]="form" (ngSubmit)="createInvoice()">
            <div formArrayName="items">
              @for (item of items.controls; track $index; let index = $index) {
                <div [formGroupName]="index" class="form-grid" style="padding:16px 0;border-bottom:1px solid var(--line)">
                  <div class="field">
                    <label [for]="'product-' + index">Produto *</label>
                    <select [id]="'product-' + index" formControlName="productId">
                      <option value="">Selecione um produto</option>
                      @for (product of products(); track product.id) {
                        <option [value]="product.id" [disabled]="product.stock === 0">
                          {{ product.code }} — {{ product.description }} ({{ product.stock }} un.)
                        </option>
                      }
                    </select>
                  </div>
                  <div class="field">
                    <label [for]="'quantity-' + index">Quantidade *</label>
                    <input [id]="'quantity-' + index" type="number" min="1" formControlName="quantity">
                  </div>
                  @if (items.length > 1) {
                    <div class="form-actions"><button type="button" class="btn btn-danger btn-small" (click)="removeItem(index)">Remover item</button></div>
                  }
                </div>
              }
            </div>
            <div style="display:flex;justify-content:space-between;gap:10px;margin-top:18px;flex-wrap:wrap">
              <button type="button" class="btn btn-secondary" (click)="addItem()">＋ Adicionar produto</button>
              <div style="display:flex;gap:10px">
                <button type="button" class="btn btn-secondary" (click)="toggleForm()">Cancelar</button>
                <button class="btn btn-primary" [disabled]="form.invalid || saving()">
                  @if (saving()) { <span class="spinner"></span> } Criar nota
                </button>
              </div>
            </div>
          </form>
        </article>
      }

      <article class="card">
        <div class="card-title"><h2>Histórico de notas</h2><span class="badge badge-stock">{{ invoices().length }} notas</span></div>

        <label class="failure-box" style="margin-bottom:17px">
          <input type="checkbox" [checked]="simulateFailure()" (change)="simulateFailure.set(!simulateFailure())">
          <span><strong>Modo de demonstração de falha</strong><br>
          Ao imprimir uma nota aberta, o serviço de estoque responderá com erro. A nota continuará aberta e nenhum saldo será alterado.</span>
        </label>

        @if (loading()) {
          <div class="loading">Carregando notas...</div>
        } @else if (invoices().length === 0) {
          <div class="empty-state"><strong>Nenhuma nota emitida</strong>Crie uma nota com um ou mais produtos.</div>
        } @else {
          <div class="table-wrap"><table>
            <thead><tr><th>Número</th><th>Produtos</th><th>Data</th><th>Status</th><th></th></tr></thead>
            <tbody>
              @for (invoice of invoices(); track invoice.id) {
                <tr>
                  <td><strong>{{ invoice.number }}</strong></td>
                  <td>{{ itemSummary(invoice) }}</td>
                  <td>{{ invoice.createdAtUtc | date:'dd/MM/yyyy HH:mm' }}</td>
                  <td><span class="badge" [class.badge-open]="invoice.status === 'Open'" [class.badge-closed]="invoice.status === 'Closed'">
                    {{ invoice.status === 'Open' ? 'Aberta' : 'Fechada' }}
                  </span></td>
                  <td><div class="actions">
                    <button class="btn btn-primary btn-small" (click)="print(invoice)"
                            [disabled]="invoice.status !== 'Open' || printingId() !== null">
                      @if (printingId() === invoice.id) { <span class="spinner"></span> Processando }
                      @else { ⎙ Imprimir }
                    </button>
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
export class InvoicesComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly notifications = inject(NotificationService);
  private readonly fb = inject(FormBuilder);
  readonly products = signal<Product[]>([]);
  readonly invoices = signal<Invoice[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly showForm = signal(false);
  readonly printingId = signal<number | null>(null);
  readonly simulateFailure = signal(false);
  readonly form = this.fb.group({ items: this.fb.array([this.createItemGroup()]) });

  get items(): FormArray { return this.form.controls.items; }

  ngOnInit(): void { this.loadAll(); }

  loadAll(): void {
    this.loading.set(true);
    forkJoin({ products: this.api.getProducts(), invoices: this.api.getInvoices() })
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe(result => { this.products.set(result.products); this.invoices.set(result.invoices); });
  }

  addItem(): void { this.items.push(this.createItemGroup()); }
  removeItem(index: number): void { this.items.removeAt(index); }

  toggleForm(): void {
    this.showForm.set(!this.showForm());
    if (!this.showForm()) this.resetForm();
  }

  createInvoice(): void {
    if (this.form.invalid) return;
    const selectedItems: InvoiceItem[] = this.items.getRawValue().map(value => {
      const product = this.products().find(item => item.id === value.productId)!;
      return {
        productId: product.id,
        productCode: product.code,
        productDescription: product.description,
        quantity: Number(value.quantity)
      };
    });

    const invalidItem = selectedItems.find(item => {
      const product = this.products().find(productItem => productItem.id === item.productId);
      return !product || item.quantity > product.stock;
    });
    if (invalidItem) {
      this.notifications.show(`Quantidade maior que o estoque de ${invalidItem.productCode}.`, 'error');
      return;
    }

    this.saving.set(true);
    this.api.createInvoice(selectedItems).pipe(finalize(() => this.saving.set(false))).subscribe(() => {
      this.notifications.show('Nota fiscal criada com status Aberta.', 'success');
      this.showForm.set(false); this.resetForm(); this.loadAll();
    });
  }

  print(invoice: Invoice): void {
    if (invoice.status !== 'Open' || this.printingId() !== null) return;
    this.printingId.set(invoice.id);
    this.api.printInvoice(invoice.id, this.simulateFailure())
      .pipe(finalize(() => this.printingId.set(null)))
      .subscribe(result => {
        this.notifications.show(result.message, 'success');
        this.loadAll();
        setTimeout(() => window.print(), 350);
      });
  }

  itemSummary(invoice: Invoice): string {
    const units = invoice.items.reduce((total, item) => total + item.quantity, 0);
    return `${invoice.items.length} produto(s) · ${units} unidade(s)`;
  }

  private createItemGroup() {
    return this.fb.nonNullable.group({
      productId: ['', Validators.required],
      quantity: [1, [Validators.required, Validators.min(1)]]
    });
  }

  private resetForm(): void {
    this.items.clear(); this.items.push(this.createItemGroup());
  }
}
