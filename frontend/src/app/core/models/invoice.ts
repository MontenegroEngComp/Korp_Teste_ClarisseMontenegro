export type InvoiceStatus = 'Open' | 'Closed';

export interface InvoiceItem {
  id: string;
  invoiceId: string;
  productId: string;
  productCode: string;
  productDescription: string;
  quantity: number;
  unitPrice: number;
  subtotal: number;
}

export interface Invoice {
  id: string;
  number: number;
  status: InvoiceStatus;
  createdAt: string;
  closedAt: string | null;
  items: InvoiceItem[];
  total: number;
}