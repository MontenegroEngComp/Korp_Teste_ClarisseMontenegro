import { CommonModule } from '@angular/common';
import {
  Component,
  computed,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import {
  FormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import {
  finalize,
  map,
  Observable,
  of,
  switchMap,
} from 'rxjs';

import {
  CreateEmployeeRequest,
  Employee,
  EmployeeRole,
  UpdateEmployeeRequest,
} from '../../core/models/employee';
import {
  Employees as EmployeesService,
} from '../../core/services/employees';

@Component({
  selector: 'app-employees',
  imports: [
    CommonModule,
    ReactiveFormsModule,
  ],
  templateUrl: './employees.html',
  styleUrl: './employees.scss',
})
export class Employees implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly employeesService =
    inject(EmployeesService);

  readonly employees = signal<Employee[]>([]);
  readonly isLoading = signal(true);
  readonly isSaving = signal(false);
  readonly processingId = signal<string | null>(null);
  readonly editingId = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  readonly activeCount = computed(
    () =>
      this.employees().filter(
        (employee) => employee.isActive,
      ).length,
  );

  readonly inactiveCount = computed(
    () => this.employees().length - this.activeCount(),
  );

  readonly form = this.formBuilder.nonNullable.group({
    name: [
      '',
      [
        Validators.required,
        Validators.maxLength(150),
      ],
    ],
    cpf: [
      '',
      [
        Validators.required,
        Validators.pattern(/^\d{11}$/),
      ],
    ],
    email: [
      '',
      [
        Validators.required,
        Validators.email,
        Validators.maxLength(200),
      ],
    ],
    phone: [
      '',
      [
        Validators.required,
        Validators.maxLength(20),
      ],
    ],
    role: this.formBuilder.nonNullable.control<EmployeeRole>(
      'Operator',
      {
        validators: [Validators.required],
      },
    ),
    password: [
      '',
      [
        Validators.required,
        Validators.minLength(8),
        Validators.maxLength(100),
      ],
    ],
  });

  ngOnInit(): void {
    this.loadEmployees();
  }

  loadEmployees(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.employeesService
      .getAll(true)
      .pipe(
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (employees) =>
          this.employees.set(employees),
        error: (error: HttpErrorResponse) =>
          this.errorMessage.set(
            this.getErrorMessage(
              error,
              'Não foi possível carregar os funcionários.',
            ),
          ),
      });
  }

  edit(employee: Employee): void {
    this.editingId.set(employee.id);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const passwordControl =
      this.form.controls.password;

    passwordControl.setValidators([
      Validators.minLength(8),
      Validators.maxLength(100),
    ]);
    passwordControl.updateValueAndValidity();

    this.form.setValue({
      name: employee.name,
      cpf: employee.cpf,
      email: employee.email,
      phone: employee.phone,
      role: employee.role,
      password: '',
    });

    window.scrollTo({
      top: 0,
      behavior: 'smooth',
    });
  }

  cancelEdit(): void {
    this.editingId.set(null);
    this.errorMessage.set(null);

    const passwordControl =
      this.form.controls.password;

    passwordControl.setValidators([
      Validators.required,
      Validators.minLength(8),
      Validators.maxLength(100),
    ]);

    this.form.reset({
      name: '',
      cpf: '',
      email: '',
      phone: '',
      role: 'Operator',
      password: '',
    });

    passwordControl.updateValueAndValidity();
  }

  save(): void {
    this.errorMessage.set(null);
    this.successMessage.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const employeeId = this.editingId();

    this.isSaving.set(true);

    let request$: Observable<Employee>;

    if (employeeId) {
      const updateRequest: UpdateEmployeeRequest = {
        name: value.name.trim(),
        cpf: value.cpf.replace(/\D/g, ''),
        email: value.email.trim().toLowerCase(),
        phone: value.phone.trim(),
        role: value.role,
      };

      request$ = this.employeesService
        .update(employeeId, updateRequest)
        .pipe(
          switchMap((employee) => {
            if (!value.password) {
              return of(employee);
            }

            return this.employeesService
              .updatePassword(employeeId, {
                password: value.password,
              })
              .pipe(map(() => employee));
          }),
        );
    } else {
      const createRequest: CreateEmployeeRequest = {
        name: value.name.trim(),
        cpf: value.cpf.replace(/\D/g, ''),
        email: value.email.trim().toLowerCase(),
        phone: value.phone.trim(),
        role: value.role,
        password: value.password,
      };

      request$ =
        this.employeesService.create(createRequest);
    }

    request$
      .pipe(
        finalize(() => this.isSaving.set(false)),
      )
      .subscribe({
        next: () => {
          this.successMessage.set(
            employeeId
              ? 'Funcionário atualizado com sucesso.'
              : 'Funcionário cadastrado com sucesso.',
          );

          this.cancelEdit();
          this.loadEmployees();
        },
        error: (error: HttpErrorResponse) =>
          this.errorMessage.set(
            this.getErrorMessage(
              error,
              'Não foi possível salvar o funcionário.',
            ),
          ),
      });
  }

  toggleStatus(employee: Employee): void {
    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.processingId.set(employee.id);

    const request$ = employee.isActive
      ? this.employeesService.deactivate(employee.id)
      : this.employeesService.activate(employee.id);

    request$
      .pipe(
        finalize(() => this.processingId.set(null)),
      )
      .subscribe({
        next: () => {
          this.successMessage.set(
            employee.isActive
              ? 'Funcionário desativado com sucesso.'
              : 'Funcionário reativado com sucesso.',
          );

          this.loadEmployees();
        },
        error: (error: HttpErrorResponse) =>
          this.errorMessage.set(
            this.getErrorMessage(
              error,
              'Não foi possível alterar o status.',
            ),
          ),
      });
  }

  roleLabel(role: EmployeeRole): string {
    return role === 'Administrator'
      ? 'Administrador'
      : 'Operador';
  }

  employeeInitials(name: string): string {
    return name
      .trim()
      .split(/\s+/)
      .slice(0, 2)
      .map((part) => part[0])
      .join('')
      .toUpperCase();
  }

  private getErrorMessage(
    error: HttpErrorResponse,
    fallback: string,
  ): string {
    return error.error?.message ?? fallback;
  }
}