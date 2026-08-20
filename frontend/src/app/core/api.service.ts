import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Invoice, InvoiceItem, PrintResult, Product, ProductPayload } from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  constructor(private readonly http: HttpClient) {}

  getProducts(): Observable<Product[]> {
    return this.http.get<Product[]>('/api/products');
  }

  createProduct(payload: ProductPayload): Observable<Product> {
    return this.http.post<Product>('/api/products', payload);
  }

  updateProduct(id: string, payload: Omit<ProductPayload, 'code'>): Observable<Product> {
    return this.http.put<Product>(`/api/products/${id}`, payload);
  }

  deleteProduct(id: string): Observable<void> {
    return this.http.delete<void>(`/api/products/${id}`);
  }

  getInvoices(): Observable<Invoice[]> {
    return this.http.get<Invoice[]>('/api/invoices');
  }

  createInvoice(items: InvoiceItem[]): Observable<Invoice> {
    return this.http.post<Invoice>('/api/invoices', { items });
  }

  printInvoice(id: number, simulateInventoryFailure: boolean): Observable<PrintResult> {
    const headers = new HttpHeaders({ 'Idempotency-Key': crypto.randomUUID() });
    return this.http.post<PrintResult>(`/api/invoices/${id}/print`,
      { simulateInventoryFailure }, { headers });
  }
}

