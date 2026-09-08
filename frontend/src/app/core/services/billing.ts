import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  CreateInvoiceRequest,
  Invoice,
} from '../models/invoice';

@Injectable({
  providedIn: 'root',
})
export class Billing {
  private readonly http = inject(HttpClient);
  private readonly apiUrl =
    'http://localhost:5002/api/invoices';

  getAll(): Observable<Invoice[]> {
    return this.http.get<Invoice[]>(this.apiUrl);
  }

  getById(id: string): Observable<Invoice> {
    return this.http.get<Invoice>(
      `${this.apiUrl}/${id}`,
    );
  }

  create(
    request: CreateInvoiceRequest,
  ): Observable<Invoice> {
    return this.http.post<Invoice>(
      this.apiUrl,
      request,
    );
  }

  close(id: string): Observable<Invoice> {
    return this.http.post<Invoice>(
      `${this.apiUrl}/${id}/close`,
      {},
    );
  }
}