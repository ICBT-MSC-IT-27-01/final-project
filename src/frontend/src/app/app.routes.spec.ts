import { describe, expect, it } from 'vitest';
import { routes } from './app.routes';
import { supportedCrops, validationStatuses } from './core/api.models';

describe('Phase 10 routing and model contracts', () => {
  it('keeps public recommendation routes accessible without a guard', () => {
    const publicRoute = routes.find(route => route.path === 'recommendations');

    expect(publicRoute?.canActivate).toBeUndefined();
  });

  it('protects officer and administrator routes with role guards', () => {
    const officerRoute = routes.find(route => route.path === 'officer');
    const adminRoute = routes.find(route => route.path === 'admin');
    const historyRoute = routes.find(route => route.path === 'history');
    const historyDetailRoute = routes.find(route => route.path === 'history/:id');

    expect(officerRoute?.canActivate?.length).toBe(1);
    expect(adminRoute?.canActivate?.length).toBe(1);
    expect(historyRoute?.canActivate?.length).toBe(1);
    expect(historyDetailRoute?.canActivate?.length).toBe(1);
  });

  it('renders only the approved six crop names in frontend scope constants', () => {
    expect(supportedCrops).toEqual(['Paddy', 'Maize', 'Green Gram', 'Cowpea', 'Groundnut', 'Chilli']);
  });

  it('uses only approved officer validation statuses', () => {
    expect(validationStatuses).toEqual(['Pending Validation', 'Validated', 'Needs Review']);
  });
});
