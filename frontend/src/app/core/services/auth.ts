import {
  computed,
  inject,
  Injectable,
  signal,
} from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

import {
  AuthenticatedEmployee,
  LoginRequest,
  LoginResponse,
} from '../models/auth';

@Injectable({
  providedIn: 'root',
})
export class Auth {
  private readonly http = inject(HttpClient);

  private readonly apiUrl =
    'http://localhost:5002/api/auth';

  private readonly tokenKey = 'korp_access_token';
  private readonly employeeKey = 'korp_employee';
  private readonly expirationKey = 'korp_token_expiration';

  private readonly accessToken = signal<string | null>(
    this.readStoredToken(),
  );

  readonly currentEmployee =
    signal<AuthenticatedEmployee | null>(
      this.readStoredEmployee(),
    );

  readonly isAuthenticated = computed(
    () =>
      this.accessToken() !== null &&
      this.currentEmployee() !== null,
  );

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(
        `${this.apiUrl}/login`,
        request,
      )
      .pipe(
        tap((response) => {
          localStorage.setItem(
            this.tokenKey,
            response.accessToken,
          );

          localStorage.setItem(
            this.employeeKey,
            JSON.stringify(response.employee),
          );

          localStorage.setItem(
            this.expirationKey,
            response.expiresAt,
          );

          this.accessToken.set(response.accessToken);
          this.currentEmployee.set(response.employee);
        }),
      );
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.employeeKey);
    localStorage.removeItem(this.expirationKey);

    this.accessToken.set(null);
    this.currentEmployee.set(null);
  }

  getAccessToken(): string | null {
    return this.accessToken();
  }

  private readStoredToken(): string | null {
    const token = localStorage.getItem(this.tokenKey);
    const expiration = localStorage.getItem(
      this.expirationKey,
    );

    if (
      !token ||
      !expiration ||
      new Date(expiration).getTime() <= Date.now()
    ) {
      localStorage.removeItem(this.tokenKey);
      localStorage.removeItem(this.employeeKey);
      localStorage.removeItem(this.expirationKey);

      return null;
    }

    return token;
  }

  private readStoredEmployee():
    | AuthenticatedEmployee
    | null {
    const storedEmployee = localStorage.getItem(
      this.employeeKey,
    );

    if (!storedEmployee) {
      return null;
    }

    try {
      return JSON.parse(
        storedEmployee,
      ) as AuthenticatedEmployee;
    } catch {
      localStorage.removeItem(this.employeeKey);
      return null;
    }
  }
}