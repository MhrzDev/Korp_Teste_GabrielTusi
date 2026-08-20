export interface Product {
  id: string;
  code: string;
  description: string;
  stock: number;
  updatedAtUtc: string;
}

export interface ProductPayload {
  code: string;
  description: string;
  stock: number;
}

export interface InvoiceItem {
  productId: string;
  productCode: string;
  productDescription: string;
  quantity: number;
}

export interface Invoice {
  id: number;
  number: string;
  status: 'Open' | 'Closed';
  createdAtUtc: string;
  closedAtUtc: string | null;
  items: InvoiceItem[];
}

export interface PrintResult {
  invoice: Invoice;
  alreadyProcessed: boolean;
  message: string;
}

