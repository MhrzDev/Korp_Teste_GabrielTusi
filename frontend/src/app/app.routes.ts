import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/dashboard/dashboard.component')
      .then(module => module.DashboardComponent)
  },
  {
    path: 'produtos',
    loadComponent: () => import('./pages/products/products.component')
      .then(module => module.ProductsComponent)
  },
  {
    path: 'notas',
    loadComponent: () => import('./pages/invoices/invoices.component')
      .then(module => module.InvoicesComponent)
  },
  { path: '**', redirectTo: '' }
];

