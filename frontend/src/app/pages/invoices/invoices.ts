import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  computed,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';

import { Invoice } from '../../core/models/invoice';
import { Billing } from '../../core/services/billing';

@Component({
  selector: 'app-invoices',
  imports: [CommonModule, RouterLink],
  templateUrl: './invoices.html',
  styleUrl: './invoices.scss',
})
export class Invoices implements OnInit {
  private readonly billingService = inject(Billing);

  readonly invoices = signal<Invoice[]>([]);
  readonly isLoading = signal(true);
  readonly isClosing = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  readonly openInvoices = computed(() =>
    this.invoices().filter(
      (invoice) => invoice.status === 'Open',
    ).length,
  );

  readonly closedInvoices = computed(() =>
    this.invoices().filter(
      (invoice) => invoice.status === 'Closed',
    ).length,
  );

  readonly totalValue = computed(() =>
    this.invoices().reduce(
      (total, invoice) => total + invoice.total,
      0,
    ),
  );

  ngOnInit(): void {
    this.loadInvoices();
  }

  loadInvoices(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.billingService
      .getAll()
      .pipe(
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (invoices) =>
          this.invoices.set(
            [...invoices].sort(
              (first, second) =>
                second.number - first.number,
            ),
          ),
        error: () =>
          this.errorMessage.set(
            'Não foi possível carregar as notas fiscais.',
          ),
      });
  }

  closeInvoice(invoice: Invoice): void {
    const confirmed = window.confirm(
      `Deseja fechar a nota #${invoice.number}?`,
    );

    if (!confirmed) {
      return;
    }

    this.errorMessage.set(null);
    this.successMessage.set(null);
    this.isClosing.set(invoice.id);

    this.billingService
      .close(invoice.id)
      .pipe(
        finalize(() => this.isClosing.set(null)),
      )
      .subscribe({
        next: (closedInvoice) => {
          this.invoices.update((invoices) =>
            invoices.map((currentInvoice) =>
              currentInvoice.id === closedInvoice.id
                ? closedInvoice
                : currentInvoice,
            ),
          );

          this.successMessage.set(
            `Nota #${closedInvoice.number} fechada com sucesso.`,
          );
        },
        error: (error: HttpErrorResponse) =>
          this.errorMessage.set(
            error.error?.message ??
              'Não foi possível fechar a nota fiscal.',
          ),
      });
  }
}