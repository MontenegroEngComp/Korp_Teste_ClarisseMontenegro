import { CommonModule } from '@angular/common';
import {
  Component,
  computed,
  inject,
  OnInit,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  catchError,
  finalize,
  forkJoin,
  of,
} from 'rxjs';

import { Invoice } from '../../core/models/invoice';
import { Product } from '../../core/models/product';
import { Billing } from '../../core/services/billing';
import { Stock } from '../../core/services/stock';

interface DailyFlow {
  label: string;
  count: number;
  height: number;
}

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule, RouterLink],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss',
})
export class Dashboard implements OnInit {
  private readonly stockService = inject(Stock);
  private readonly billingService = inject(Billing);

  readonly products = signal<Product[]>([]);
  readonly invoices = signal<Invoice[]>([]);
  readonly isLoading = signal(true);
  readonly stockHealthy = signal(true);
  readonly billingHealthy = signal(true);
  readonly errorMessage = signal<string | null>(null);

  readonly invoicesToday = computed(() => {
    const today = new Date();

    return this.invoices().filter((invoice) =>
      this.isSameDay(
        new Date(invoice.createdAt),
        today,
      ),
    );
  });

  readonly unitsMoved = computed(() =>
    this.invoices()
      .filter((invoice) => invoice.status === 'Closed')
      .reduce(
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

  readonly lowStockProducts = computed(() =>
    [...this.products()]
      .filter((product) => product.stockQuantity <= 15)
      .sort(
        (first, second) =>
          first.stockQuantity - second.stockQuantity,
      )
      .slice(0, 4),
  );

  readonly recentInvoices = computed(() =>
    [...this.invoices()]
      .sort(
        (first, second) =>
          new Date(second.createdAt).getTime() -
          new Date(first.createdAt).getTime(),
      )
      .slice(0, 5),
  );

  readonly serviceHealth = computed(() => {
    const healthyServices = [
      this.stockHealthy(),
      this.billingHealthy(),
    ].filter(Boolean).length;

    return `${healthyServices * 50}%`;
  });

  readonly weeklyFlow = computed<DailyFlow[]>(() => {
    const days = Array.from(
      { length: 7 },
      (_, index) => {
        const date = new Date();
        date.setDate(date.getDate() - (6 - index));

        const count = this.invoices().filter(
          (invoice) =>
            this.isSameDay(
              new Date(invoice.createdAt),
              date,
            ),
        ).length;

        return {
          date,
          count,
        };
      },
    );

    const highestCount = Math.max(
      ...days.map((day) => day.count),
      1,
    );

    return days.map((day) => ({
      label: new Intl.DateTimeFormat('pt-BR', {
        weekday: 'short',
      })
        .format(day.date)
        .replace('.', ''),
      count: day.count,
      height:
        day.count === 0
          ? 8
          : Math.max(
              20,
              Math.round(
                (day.count / highestCount) * 100,
              ),
            ),
    }));
  });

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.stockHealthy.set(true);
    this.billingHealthy.set(true);

    forkJoin({
      products: this.stockService.getAll().pipe(
        catchError(() => {
          this.stockHealthy.set(false);
          return of<Product[]>([]);
        }),
      ),
      invoices: this.billingService.getAll().pipe(
        catchError(() => {
          this.billingHealthy.set(false);
          return of<Invoice[]>([]);
        }),
      ),
    })
      .pipe(
        finalize(() => this.isLoading.set(false)),
      )
      .subscribe(({ products, invoices }) => {
        this.products.set(products);
        this.invoices.set(invoices);

        if (
          !this.stockHealthy() ||
          !this.billingHealthy()
        ) {
          this.errorMessage.set(
            'Alguns serviços estão indisponíveis. Os dados podem estar incompletos.',
          );
        }
      });
  }

  getInvoiceUnits(invoice: Invoice): number {
  return invoice.items.reduce(
    (total, item) => total + item.quantity,
    0,
  );
  }
  
  private isSameDay(
    firstDate: Date,
    secondDate: Date,
  ): boolean {
    return (
      firstDate.getFullYear() ===
        secondDate.getFullYear() &&
      firstDate.getMonth() ===
        secondDate.getMonth() &&
      firstDate.getDate() ===
        secondDate.getDate()
    );
  }
}