import { Routes } from '@angular/router';

import { authGuard } from './core/guards/auth.guard';
import { adminGuard } from './core/guards/admin.guard';
import { Shell } from './layout/shell/shell';
import { Dashboard } from './pages/dashboard/dashboard';
import { InvoiceCreate } from './pages/invoice-create/invoice-create';
import { Invoices } from './pages/invoices/invoices';
import { Login } from './pages/login/login';
import { Products } from './pages/products/products';
import { Employees } from './pages/employees/employees';

export const routes: Routes = [
  {
    path: 'login',
    component: Login,
  },
  {
    path: '',
    component: Shell,
    canActivate: [authGuard],
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
      {
        path: 'employees',
        component: Employees,
        canActivate: [adminGuard],
      },
    ],
  },
  {
    path: '**',
    redirectTo: 'dashboard',
  },
];