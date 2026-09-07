import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  computed,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import {
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { finalize } from 'rxjs';

import { Product } from '../../core/models/product';
import { Stock } from '../../core/services/stock';

@Component({
  selector: 'app-products',
  imports: [
    CommonModule,
    ReactiveFormsModule,
  ],
  templateUrl: './products.html',
  styleUrl: './products.scss',
})
export class Products implements OnInit {
  private readonly stockService = inject(Stock);

  readonly products = signal<Product[]>([]);
  readonly isLoading = signal(true);
  readonly isSaving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  readonly totalUnits = computed(() =>
    this.products().reduce(
      (total, product) =>
        total + product.stockQuantity,
      0,
    ),
  );

  readonly inventoryValue = computed(() =>
    this.products().reduce(
      (total, product) =>
        total +
        product.stockQuantity * product.unitPrice,
      0,
    ),
  );

  readonly form = new FormGroup({
    code: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.minLength(2),
        Validators.maxLength(50),
      ],
    }),
    description: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.minLength(3),
        Validators.maxLength(200),
      ],
    }),
    stockQuantity: new FormControl(0, {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.min(0),
      ],
    }),
    unitPrice: new FormControl(0.01, {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.min(0.01),
      ],
    }),
  });

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.stockService
      .getAll()
      .pipe(
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (products) =>
          this.products.set(products),
        error: () =>
          this.errorMessage.set(
            'Não foi possível carregar os produtos.',
          ),
      });
  }

  submit(): void {
    this.successMessage.set(null);
    this.errorMessage.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);

    this.stockService
      .create(this.form.getRawValue())
      .pipe(
        finalize(() => this.isSaving.set(false)),
      )
      .subscribe({
        next: (product) => {
          this.products.update((products) =>
            [...products, product].sort((first, second) =>
              first.description.localeCompare(
                second.description,
              ),
            ),
          );

          this.form.reset({
            code: '',
            description: '',
            stockQuantity: 0,
            unitPrice: 0.01,
          });

          this.successMessage.set(
            'Produto cadastrado com sucesso.',
          );
        },
        error: (error: HttpErrorResponse) => {
          this.errorMessage.set(
            error.error?.message ??
              'Não foi possível cadastrar o produto.',
          );
        },
      });
  }
}