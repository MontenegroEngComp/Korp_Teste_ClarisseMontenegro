import { Routes } from '@angular/router';

import { Shell } from './layout/shell/shell';
import { InvoiceCreate } from './pages/invoice-create/invoice-create';
import { Invoices } from './pages/invoices/invoices';
import { Products } from './pages/products/products';

export const routes: Routes = [
  {
    path: '',
    component: Shell,
    children: [
      {
        path: '',
        redirectTo: 'products',
        pathMatch: 'full',
      },
      {
        path: 'products',
        component: Products,
      },
      {
        path: 'invoices/new',
        component: InvoiceCreate,
      },
      {
        path: 'invoices',
        component: Invoices,
      },
    ],
  },
  {
    path: '**',
    redirectTo: '',
  },
];