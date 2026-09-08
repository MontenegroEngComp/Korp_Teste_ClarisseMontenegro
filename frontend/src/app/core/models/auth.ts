export type EmployeeRole =
  | 'Administrator'
  | 'Operator';

export interface AuthenticatedEmployee {
  id: string;
  name: string;
  email: string;
  role: EmployeeRole;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  employee: AuthenticatedEmployee;
}