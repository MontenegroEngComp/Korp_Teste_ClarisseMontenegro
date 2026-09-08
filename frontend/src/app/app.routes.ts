import { Routes } from '@angular/router';

import { Shell } from './layout/shell/shell';
import { Dashboard } from './pages/dashboard/dashboard';
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
        pathMatch: 'full',
        redirectTo: 'dashboard',
      },
      {
        path: 'dashboard',
        component: Dashboard,
      },
      {
        path: 'products',
        component: Products,
      },
      {
        path: 'invoices',
        component: Invoices,
      },
      {
        path: 'invoices/new',
        component: InvoiceCreate,
      },
    ],
  },
  {
    path: '**',
    redirectTo: 'dashboard',
   },
];