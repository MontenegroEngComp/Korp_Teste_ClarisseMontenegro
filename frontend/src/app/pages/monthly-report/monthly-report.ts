import { CommonModule } from '@angular/common';
import {
  Component,
  computed,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { finalize } from 'rxjs';

import { Invoice } from '../../core/models/invoice';
import { Auth } from '../../core/services/auth';
import { Billing } from '../../core/services/billing';

@Component({
  selector: 'app-monthly-report',
  imports: [CommonModule],
  templateUrl: './monthly-report.html',
  styleUrl: './monthly-report.scss',
})
export class MonthlyReport implements OnInit {
  private readonly billingService = inject(Billing);
  private readonly authService = inject(Auth);

  readonly invoices = signal<Invoice[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);
  readonly selectedMonth = signal(
    this.currentMonthValue(),
  );

  readonly currentEmployee =
    this.authService.currentEmployee;

  readonly filteredInvoices = computed(() => {
    const [year, month] = this.selectedMonth()
      .split('-')
      .map(Number);

    return this.invoices().filter((invoice) => {
      const emissionDate = new Date(invoice.createdAt);

      return (
        emissionDate.getFullYear() === year &&
        emissionDate.getMonth() === month - 1
      );
    });
  });

  readonly closedCount = computed(
    () =>
      this.filteredInvoices().filter(
        (invoice) => invoice.status === 'Closed',
      ).length,
  );

  readonly openCount = computed(
    () =>
      this.filteredInvoices().filter(
        (invoice) => invoice.status === 'Open',
      ).length,
  );

  readonly totalValue = computed(
    () =>
      this.filteredInvoices().reduce(
        (total, invoice) => total + invoice.total,
        0,
      ),
  );

  readonly totalUnits = computed(
    () =>
      this.filteredInvoices().reduce(
        (total, invoice) =>
          total +
          invoice.items.reduce(
            (itemTotal, item) =>
              itemTotal + item.quantity,
            0,
          ),
        0,
      ),
  );

  readonly periodLabel = computed(() => {
    const [year, month] = this.selectedMonth()
      .split('-')
      .map(Number);

    const label = new Intl.DateTimeFormat(
      'pt-BR',
      {
        month: 'long',
        year: 'numeric',
      },
    ).format(new Date(year, month - 1, 1));

    return (
      label.charAt(0).toUpperCase() +
      label.slice(1)
    );
  });

  ngOnInit(): void {
    this.loadInvoices();
  }

  changeMonth(event: Event): void {
    const input = event.target as HTMLInputElement;

    if (input.value) {
      this.selectedMonth.set(input.value);
    }
  }

  printReport(): void {
    window.print();
  }

  downloadCsv(): void {
    const header = [
      'Número',
      'Emissão',
      'Emissor',
      'Status',
      'Itens',
      'Unidades',
      'Valor total',
    ];

    const rows = this.filteredInvoices().map(
      (invoice) => [
        invoice.number.toString(),
        new Date(invoice.createdAt).toLocaleString(
          'pt-BR',
        ),
        invoice.issuedByName ?? 'Registro anterior',
        invoice.status === 'Closed'
          ? 'Fechada'
          : 'Aberta',
        invoice.items.length.toString(),
        invoice.items
          .reduce(
            (total, item) =>
              total + item.quantity,
            0,
          )
          .toString(),
        invoice.total.toFixed(2).replace('.', ','),
      ],
    );

    const csv = [header, ...rows]
      .map((row) =>
        row
          .map((value) => this.csvCell(value))
          .join(';'),
      )
      .join('\r\n');

    const blob = new Blob(
      ['\uFEFF', csv],
      {
        type: 'text/csv;charset=utf-8;',
      },
    );

    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');

    link.href = url;
    link.download =
      `relatorio-faturamento-${this.selectedMonth()}.csv`;
    link.click();

    URL.revokeObjectURL(url);
  }

  invoiceUnits(invoice: Invoice): number {
    return invoice.items.reduce(
      (total, item) => total + item.quantity,
      0,
    );
  }

  private loadInvoices(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    this.billingService
      .getAll()
      .pipe(
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe({
        next: (invoices) =>
          this.invoices.set(invoices),
        error: (error: HttpErrorResponse) =>
          this.errorMessage.set(
            error.error?.message ??
              'Não foi possível carregar o relatório.',
          ),
      });
  }

  private currentMonthValue(): string {
    const today = new Date();
    const month = String(
      today.getMonth() + 1,
    ).padStart(2, '0');

    return `${today.getFullYear()}-${month}`;
  }

  private csvCell(value: string): string {
    return `"${value.replaceAll('"', '""')}"`;
  }
}