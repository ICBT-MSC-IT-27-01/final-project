import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiService, FrontendApiError } from '../../core/api.service';
import { AdminReadinessComponent } from './admin-readiness.component';

describe('AdminReadinessComponent', () => {
  const api = {
    listAdminResources: vi.fn(),
    createAdminResource: vi.fn(),
    updateAdminResource: vi.fn(),
    setAdminResourceActiveStatus: vi.fn(),
    updateAdminUserRole: vi.fn(),
  };
  let fixture: ComponentFixture<AdminReadinessComponent>;
  let component: AdminReadinessComponent;

  beforeEach(async () => {
    vi.clearAllMocks();
    api.listAdminResources.mockReturnValue(of({ items: [], page: 1, pageSize: 20, totalCount: 0 }));
    await TestBed.configureTestingModule({
      imports: [AdminReadinessComponent],
      providers: [{ provide: ApiService, useValue: api }],
    }).compileComponents();

    fixture = TestBed.createComponent(AdminReadinessComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads the verified administrator users endpoint by default', () => {
    expect(api.listAdminResources).toHaveBeenCalledWith('users', 1, 20);
    expect(fixture.nativeElement.textContent).toContain('User management');
  });

  it('submits crop creation through the approved admin crop contract', () => {
    api.createAdminResource.mockReturnValue(of({ id: 1, name: 'Paddy', isActive: true }));
    component.cropForm.name = '  Paddy  ';

    component.createCrop();

    expect(api.createAdminResource).toHaveBeenCalledWith('crops', { name: 'Paddy' });
  });

  it('blocks empty crop names before calling the backend', () => {
    component.cropForm.name = '  ';

    component.createCrop();

    expect(api.createAdminResource).not.toHaveBeenCalled();
    expect(component.error()).toBe('Crop name is required.');
  });

  it('uses confirmation before active-status updates', () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false);

    component.setActive('users', 5, false);

    expect(api.setAdminResourceActiveStatus).not.toHaveBeenCalled();
  });

  it('shows safe admin API errors', () => {
    api.updateAdminUserRole.mockReturnValue(throwError(() => new FrontendApiError('Role is invalid.', 400)));

    component.updateUserRole(9, 'Administrator');

    expect(component.error()).toBe('Role is invalid.');
  });
});
