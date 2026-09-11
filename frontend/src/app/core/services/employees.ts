import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  CreateEmployeeRequest,
  Employee,
  UpdateEmployeePasswordRequest,
  UpdateEmployeeRequest,
} from '../models/employee';

@Injectable({
  providedIn: 'root',
})
export class Employees {
  private readonly http = inject(HttpClient);
  private readonly apiUrl =
    'http://localhost:5002/api/employees';

  getAll(
    includeInactive = true,
  ): Observable<Employee[]> {
    return this.http.get<Employee[]>(
      this.apiUrl,
      {
        params: {
          includeInactive,
        },
      },
    );
  }

  getById(id: string): Observable<Employee> {
    return this.http.get<Employee>(
      `${this.apiUrl}/${id}`,
    );
  }

  create(
    request: CreateEmployeeRequest,
  ): Observable<Employee> {
    return this.http.post<Employee>(
      this.apiUrl,
      request,
    );
  }

  update(
    id: string,
    request: UpdateEmployeeRequest,
  ): Observable<Employee> {
    return this.http.put<Employee>(
      `${this.apiUrl}/${id}`,
      request,
    );
  }

  updatePassword(
    id: string,
    request: UpdateEmployeePasswordRequest,
  ): Observable<void> {
    return this.http.patch<void>(
      `${this.apiUrl}/${id}/password`,
      request,
    );
  }

  deactivate(id: string): Observable<Employee> {
    return this.http.patch<Employee>(
      `${this.apiUrl}/${id}/deactivate`,
      {},
    );
  }

  activate(id: string): Observable<Employee> {
    return this.http.patch<Employee>(
      `${this.apiUrl}/${id}/activate`,
      {},
    );
  }
}