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
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { Product } from '../../core/models/product';
import { Billing } from '../../core/services/billing';
import { Stock } from '../../core/services/stock';
import { Auth } from '../../core/services/auth';

interface SelectedInvoiceItem {
  product: Product;
  quantity: number;
}

@Component({
  selector: 'app-invoice-create',
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: './invoice-create.html',
  styleUrl: './invoice-create.scss',
})
export class InvoiceCreate implements OnInit {
  private readonly stockService = inject(Stock);
  private readonly billingService = inject(Billing);
  private readonly router = inject(Router);
  private readonly auth = inject(Auth);

  readonly currentEmployee = this.auth.currentEmployee;

  readonly products = signal<Product[]>([]);
  readonly selectedItems =
    signal<SelectedInvoiceItem[]>([]);

  readonly isLoading = signal(true);
  readonly isSaving = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly total = computed(() =>
    this.selectedItems().reduce(
      (sum, item) =>
        sum +
        item.product.unitPrice * item.quantity,
      0,
    ),
  );

  readonly itemForm = new FormGroup({
    productId: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    quantity: new FormControl(1, {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.min(1),
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

  addItem(): void {
    this.errorMessage.set(null);

    if (this.itemForm.invalid) {
      this.itemForm.markAllAsTouched();
      return;
    }

    const { productId, quantity } =
      this.itemForm.getRawValue();

    const product = this.products().find(
      (currentProduct) =>
        currentProduct.id === productId,
    );

    if (!product) {
      this.errorMessage.set(
        'Selecione um produto válido.',
      );
      return;
    }

    const alreadySelected = this.selectedItems().some(
      (item) => item.product.id === product.id,
    );

    if (alreadySelected) {
      this.errorMessage.set(
        'O produto já foi adicionado à nota.',
      );
      return;
    }

    if (quantity > product.stockQuantity) {
      this.errorMessage.set(
        'A quantidade informada é maior que o estoque disponível.',
      );
      return;
    }

    this.selectedItems.update((items) => [
      ...items,
      {
        product,
        quantity,
      },
    ]);

    this.itemForm.reset({
      productId: '',
      quantity: 1,
    });
  }

  removeItem(productId: string): void {
    this.selectedItems.update((items) =>
      items.filter(
        (item) => item.product.id !== productId,
      ),
    );
  }

  submit(): void {
    this.errorMessage.set(null);

    if (this.selectedItems().length === 0) {
      this.errorMessage.set(
        'Adicione pelo menos um produto à nota.',
      );
      return;
    }

    this.isSaving.set(true);

    this.billingService
      .create({
        items: this.selectedItems().map((item) => ({
          productId: item.product.id,
          quantity: item.quantity,
        })),
      })
      .pipe(
        finalize(() => this.isSaving.set(false)),
      )
      .subscribe({
        next: () =>
          this.router.navigate(['/invoices']),
        error: (error: HttpErrorResponse) =>
          this.errorMessage.set(
            error.error?.message ??
              'Não foi possível abrir a nota fiscal.',
          ),
      });
  }
}