export type EmployeeRole =
  | 'Administrator'
  | 'Operator';

export interface Employee {
  id: string;
  name: string;
  cpf: string;
  email: string;
  phone: string;
  role: EmployeeRole;
  isActive: boolean;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateEmployeeRequest {
  name: string;
  cpf: string;
  email: string;
  phone: string;
  role: EmployeeRole;
  password: string;
}

export interface UpdateEmployeeRequest {
  name: string;
  cpf: string;
  email: string;
  phone: string;
  role: EmployeeRole;
}

export interface UpdateEmployeePasswordRequest {
  password: string;
}