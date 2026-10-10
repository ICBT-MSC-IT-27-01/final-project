import { Routes } from '@angular/router';
import { roleGuard } from './core/role.guard';
import { LoginComponent } from './features/auth/login.component';
import { RegisterComponent } from './features/auth/register.component';
import { AdminReadinessComponent } from './features/admin/admin-readiness.component';
import { OfficerDetailComponent } from './features/officer/officer-detail.component';
import { OfficerListComponent } from './features/officer/officer-list.component';
import { PublicDashboardComponent } from './features/public-dashboard/public-dashboard.component';
import { RegisteredHistoryDetailComponent } from './features/registered/registered-history-detail.component';
import { RegisteredHistoryBlockedComponent } from './features/registered/registered-history-blocked.component';
import { RecommendationPageComponent } from './features/recommendation/recommendation-page.component';

export const routes: Routes = [
  { path: '', component: PublicDashboardComponent, title: 'Anuradhapura AI' },
  { path: 'recommendations', component: RecommendationPageComponent, title: 'Crop recommendation' },
  { path: 'login', component: LoginComponent, title: 'Login' },
  { path: 'register', component: RegisterComponent, title: 'Register' },
  {
    path: 'officer',
    component: OfficerListComponent,
    canActivate: [roleGuard(['Agricultural Officer'])],
    title: 'Officer validation',
  },
  {
    path: 'officer/recommendations/:id',
    component: OfficerDetailComponent,
    canActivate: [roleGuard(['Agricultural Officer'])],
    title: 'Recommendation review',
  },
  {
    path: 'admin',
    component: AdminReadinessComponent,
    canActivate: [roleGuard(['Administrator'])],
    title: 'Administrator readiness',
  },
  {
    path: 'history',
    component: RegisteredHistoryBlockedComponent,
    canActivate: [roleGuard(['Registered User'])],
    title: 'Recommendation history',
  },
  {
    path: 'history/:id',
    component: RegisteredHistoryDetailComponent,
    canActivate: [roleGuard(['Registered User'])],
    title: 'Saved recommendation',
  },
  { path: '**', redirectTo: '' },
];
