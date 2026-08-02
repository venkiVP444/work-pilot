import { Routes } from '@angular/router';
import { OwnerDashboardComponent } from './owner/owner-dashboard.component';
import { CustomerBookingComponent } from './customer/customer-booking.component';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: OwnerDashboardComponent },
  { path: 'owner', component: OwnerDashboardComponent },
  { path: 'book', redirectTo: 'book/11111111-1111-1111-1111-111111111111', pathMatch: 'full' },
  { path: 'book/:businessId', component: CustomerBookingComponent },
  { path: '**', redirectTo: 'dashboard' }
];
