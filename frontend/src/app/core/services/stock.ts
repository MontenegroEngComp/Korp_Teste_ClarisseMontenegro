import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
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

  create(
    request: CreateProductRequest,
  ): Observable<Product> {
    return this.http.post<Product>(
      this.apiUrl,
      request,
    );
  }
}