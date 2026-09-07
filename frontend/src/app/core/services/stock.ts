import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  CreateProductRequest,
  Product,
} from '../models/product';

@Injectable({
  providedIn: 'root',
})
export class Stock {
  private readonly http = inject(HttpClient);
  private readonly apiUrl =
    'http://localhost:5001/api/products';

  getAll(): Observable<Product[]> {
    return this.http.get<Product[]>(this.apiUrl);
  }

  getById(id: string): Observable<Product> {
    return this.http.get<Product>(
      `${this.apiUrl}/${id}`,
    );
  }

  create(
    request: CreateProductRequest,
  ): Observable<Product> {
    return this.http.post<Product>(
      this.apiUrl,
      request,
    );
  }
}