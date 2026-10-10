import { JsonPipe } from '@angular/common';
import { Component, Input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable } from 'rxjs';
import { finalize } from 'rxjs/operators';
import {
  AdminRecommendationSummaryResponse,
  AdminResource,
  AdminUserResponse,
  AdminValidationResponse,
  CropRequirementResponse,
  CropResponse,
  SoilCompatibilityResponse,
  SuitabilityConfigurationResponse,
  UserRole,
} from '../../core/api.models';
import { ApiService, FrontendApiError } from '../../core/api.service';

type AdminTab = 'users' | 'crops' | 'crop-requirements' | 'soil-compatibility' | 'suitability-config' | 'recommendations' | 'validations';

@Component({
  selector: 'admin-simple-table',
  imports: [JsonPipe],
  template: `
    <section class="table-panel">
      <h2>{{ title }}</h2>
      @if (!rows.length) {
        <p>No records returned.</p>
      } @else {
        <table>
          <tbody>
            @for (row of rows; track row) {
              <tr><td><code>{{ row | json }}</code></td></tr>
            }
          </tbody>
        </table>
      }
    </section>
  `,
})
export class AdminSimpleTableComponent {
  @Input({ required: true }) title = '';
  @Input({ required: true }) rows: unknown[] = [];
}

@Component({
  selector: 'app-admin-readiness',
  imports: [AdminSimpleTableComponent, FormsModule],
  template: `
    <section class="page-title">
      <p class="eyebrow">Administrator</p>
      <h1>Administration</h1>
      <p>Management screens use only verified Administrator API contracts and leave backend authorization authoritative.</p>
    </section>

    @if (error(); as message) {
      <div class="notice error" role="alert">{{ message }}</div>
    }
    @if (success(); as message) {
      <div class="notice success" role="status">{{ message }}</div>
    }

    <nav class="tab-row" aria-label="Administrator sections">
      @for (tab of tabs; track tab.key) {
        <button type="button" class="button secondary" [class.active-tab]="activeTab() === tab.key" (click)="selectTab(tab.key)">
          {{ tab.label }}
        </button>
      }
    </nav>

    @if (isLoading()) {
      <div class="notice" role="status">Loading administrator data...</div>
    }

    @switch (activeTab()) {
      @case ('users') {
        <section class="table-panel">
          <h2>User management</h2>
          <table>
            <thead><tr><th>Name</th><th>Email</th><th>Role</th><th>Active</th><th>Actions</th></tr></thead>
            <tbody>
              @for (user of users(); track user.id) {
                <tr>
                  <td>{{ user.name }}</td>
                  <td>{{ user.email }}</td>
                  <td>
                    <select [ngModel]="user.role" (ngModelChange)="updateUserRole(user.id, $event)">
                      @for (role of roles; track role) {
                        <option [value]="role">{{ role }}</option>
                      }
                    </select>
                  </td>
                  <td>{{ user.isActive ? 'Active' : 'Inactive' }}</td>
                  <td><button type="button" class="button secondary" (click)="setActive('users', user.id, !user.isActive)">Set {{ user.isActive ? 'inactive' : 'active' }}</button></td>
                </tr>
              }
            </tbody>
          </table>
        </section>
      }
      @case ('crops') {
        <section class="grid-two">
          <form class="panel form-panel" (ngSubmit)="createCrop()">
            <h2>Create crop</h2>
            <label>Name <input name="cropName" [(ngModel)]="cropForm.name" maxlength="100" required /></label>
            <button type="submit" class="button primary">Save crop</button>
          </form>
          <section class="table-panel">
            <h2>Crops</h2>
            <table>
              <thead><tr><th>Name</th><th>Active</th><th>Actions</th></tr></thead>
              <tbody>
                @for (crop of crops(); track crop.id) {
                  <tr>
                    <td><input [ngModel]="crop.name" (ngModelChange)="cropEdits[crop.id] = $event" /></td>
                    <td>{{ crop.isActive ? 'Active' : 'Inactive' }}</td>
                    <td>
                      <button type="button" class="button secondary" (click)="updateCrop(crop)">Update</button>
                      <button type="button" class="button secondary" (click)="setActive('crops', crop.id, !crop.isActive)">Set {{ crop.isActive ? 'inactive' : 'active' }}</button>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </section>
        </section>
      }
      @case ('crop-requirements') {
        <section class="grid-two">
          <form class="panel form-panel" (ngSubmit)="createRequirement()">
            <h2>Crop requirement</h2>
            <label>Crop ID <input type="number" name="reqCropId" [(ngModel)]="requirementForm.cropId" min="1" required /></label>
            <label>Variable <select name="variableType" [(ngModel)]="requirementForm.variableType"><option>Rainfall</option><option>Temperature</option><option>Humidity</option></select></label>
            <label>Minimum <input type="number" name="minimumValue" [(ngModel)]="requirementForm.minimumValue" required /></label>
            <label>Maximum <input type="number" name="maximumValue" [(ngModel)]="requirementForm.maximumValue" required /></label>
            <label>Unit <input name="unit" [(ngModel)]="requirementForm.unit" maxlength="50" required /></label>
            <label><input type="checkbox" name="forecastCompatible" [(ngModel)]="requirementForm.isCompatibleWithSevenDayForecast" /> Seven-day forecast compatible</label>
            <button type="submit" class="button primary">Save requirement</button>
          </form>
          <section class="table-panel">
            <h2>Crop requirements</h2>
            <table>
              <thead><tr><th>Crop</th><th>Variable</th><th>Range</th><th>Unit</th><th>Active</th><th>Actions</th></tr></thead>
              <tbody>
                @for (item of requirements(); track item.id) {
                  <tr>
                    <td>{{ item.cropName }} (#{{ item.cropId }})</td>
                    <td>{{ item.variableType }}</td>
                    <td>
                      <input type="number" [ngModel]="requirementEdit(item).minimumValue" (ngModelChange)="requirementEdit(item).minimumValue = $event" />
                      <input type="number" [ngModel]="requirementEdit(item).maximumValue" (ngModelChange)="requirementEdit(item).maximumValue = $event" />
                    </td>
                    <td><input [ngModel]="requirementEdit(item).unit" (ngModelChange)="requirementEdit(item).unit = $event" /></td>
                    <td>{{ item.isActive ? 'Active' : 'Inactive' }}</td>
                    <td>
                      <button type="button" class="button secondary" (click)="updateRequirement(item)">Update</button>
                      <button type="button" class="button secondary" (click)="setActive('crop-requirements', item.id, !item.isActive)">Set {{ item.isActive ? 'inactive' : 'active' }}</button>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </section>
        </section>
      }
      @case ('soil-compatibility') {
        <section class="grid-two">
          <form class="panel form-panel" (ngSubmit)="createSoilCompatibility()">
            <h2>Soil compatibility</h2>
            <label>Crop ID <input type="number" name="soilCropId" [(ngModel)]="soilForm.cropId" min="1" required /></label>
            <label>Soil type <input name="soilType" [(ngModel)]="soilForm.soilType" maxlength="100" required /></label>
            <label>Compatibility score <input type="number" name="compatibilityScore" [(ngModel)]="soilForm.compatibilityScore" min="0" max="100" required /></label>
            <button type="submit" class="button primary">Save compatibility</button>
          </form>
          <section class="table-panel">
            <h2>Soil compatibility</h2>
            <table>
              <thead><tr><th>Crop</th><th>Soil</th><th>Score</th><th>Active</th><th>Actions</th></tr></thead>
              <tbody>
                @for (item of soilCompatibilities(); track item.id) {
                  <tr>
                    <td>{{ item.cropName }} (#{{ item.cropId }})</td>
                    <td><input [ngModel]="soilEdit(item).soilType" (ngModelChange)="soilEdit(item).soilType = $event" /></td>
                    <td><input type="number" [ngModel]="soilEdit(item).compatibilityScore" (ngModelChange)="soilEdit(item).compatibilityScore = $event" /></td>
                    <td>{{ item.isActive ? 'Active' : 'Inactive' }}</td>
                    <td>
                      <button type="button" class="button secondary" (click)="updateSoilCompatibility(item)">Update</button>
                      <button type="button" class="button secondary" (click)="setActive('soil-compatibility', item.id, !item.isActive)">Set {{ item.isActive ? 'inactive' : 'active' }}</button>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </section>
        </section>
      }
      @case ('suitability-config') {
        <section class="grid-two">
          <form class="panel form-panel" (ngSubmit)="createSuitabilityConfig()">
            <h2>Suitability configuration</h2>
            <label>Type <input name="configurationType" [(ngModel)]="configForm.configurationType" maxlength="50" required /></label>
            <label>Key <input name="configurationKey" [(ngModel)]="configForm.configurationKey" maxlength="100" required /></label>
            <label>Value <input type="number" name="configValue" [(ngModel)]="configForm.value" required /></label>
            <button type="submit" class="button primary">Save configuration</button>
          </form>
          <section class="table-panel">
            <h2>Suitability configuration</h2>
            <table>
              <thead><tr><th>Type</th><th>Key</th><th>Value</th><th>Active</th><th>Actions</th></tr></thead>
              <tbody>
                @for (item of suitabilityConfigs(); track item.id) {
                  <tr>
                    <td><input [ngModel]="configEdit(item).configurationType" (ngModelChange)="configEdit(item).configurationType = $event" /></td>
                    <td><input [ngModel]="configEdit(item).configurationKey" (ngModelChange)="configEdit(item).configurationKey = $event" /></td>
                    <td><input type="number" [ngModel]="configEdit(item).value" (ngModelChange)="configEdit(item).value = $event" /></td>
                    <td>{{ item.isActive ? 'Active' : 'Inactive' }}</td>
                    <td>
                      <button type="button" class="button secondary" (click)="updateSuitabilityConfig(item)">Update</button>
                      <button type="button" class="button secondary" (click)="setActive('suitability-config', item.id, !item.isActive)">Set {{ item.isActive ? 'inactive' : 'active' }}</button>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </section>
        </section>
      }
      @case ('recommendations') {
        <admin-simple-table title="Recommendation records" [rows]="recommendations()"></admin-simple-table>
      }
      @case ('validations') {
        <admin-simple-table title="Validation records" [rows]="validations()"></admin-simple-table>
      }
    }
  `,
})
export class AdminReadinessComponent {
  readonly roles: UserRole[] = ['Registered User', 'Agricultural Officer', 'Administrator'];
  readonly tabs: { key: AdminTab; label: string }[] = [
    { key: 'users', label: 'Users' },
    { key: 'crops', label: 'Crops' },
    { key: 'crop-requirements', label: 'Requirements' },
    { key: 'soil-compatibility', label: 'Soil' },
    { key: 'suitability-config', label: 'Configuration' },
    { key: 'recommendations', label: 'Recommendation records' },
    { key: 'validations', label: 'Validation records' },
  ];
  readonly activeTab = signal<AdminTab>('users');
  readonly isLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly success = signal<string | null>(null);
  readonly users = signal<AdminUserResponse[]>([]);
  readonly crops = signal<CropResponse[]>([]);
  readonly requirements = signal<CropRequirementResponse[]>([]);
  readonly soilCompatibilities = signal<SoilCompatibilityResponse[]>([]);
  readonly suitabilityConfigs = signal<SuitabilityConfigurationResponse[]>([]);
  readonly recommendations = signal<AdminRecommendationSummaryResponse[]>([]);
  readonly validations = signal<AdminValidationResponse[]>([]);
  readonly cropEdits: Record<number, string> = {};
  readonly requirementEdits: Record<number, Partial<CropRequirementResponse>> = {};
  readonly soilEdits: Record<number, Partial<SoilCompatibilityResponse>> = {};
  readonly configEdits: Record<number, Partial<SuitabilityConfigurationResponse>> = {};
  readonly cropForm = { name: '' };
  readonly requirementForm = { cropId: 0, variableType: 'Rainfall', minimumValue: 0, maximumValue: 0, acceptableMinimumValue: null, acceptableMaximumValue: null, unit: '', timeBasis: null, isCompatibleWithSevenDayForecast: true };
  readonly soilForm = { cropId: 0, soilType: '', compatibilityScore: 0 };
  readonly configForm = { configurationType: '', configurationKey: '', value: 0 };

  constructor(private readonly api: ApiService) {
    this.loadActiveTab();
  }

  selectTab(tab: AdminTab): void {
    this.activeTab.set(tab);
    this.loadActiveTab();
  }

  createCrop(): void {
    if (!this.cropForm.name.trim()) {
      this.error.set('Crop name is required.');
      return;
    }

    this.save(this.api.createAdminResource('crops', { name: this.cropForm.name.trim() }), 'Crop saved.');
  }

  updateCrop(crop: CropResponse): void {
    const name = (this.cropEdits[crop.id] ?? crop.name).trim();
    if (!name) {
      this.error.set('Crop name is required.');
      return;
    }

    this.save(this.api.updateAdminResource('crops', crop.id, { name, isActive: crop.isActive }), 'Crop updated.');
  }

  createRequirement(): void {
    this.save(this.api.createAdminResource('crop-requirements', this.requirementForm), 'Crop requirement saved.');
  }

  updateRequirement(item: CropRequirementResponse): void {
    const edit = this.requirementEdit(item);
    this.save(this.api.updateAdminResource('crop-requirements', item.id, {
      cropId: item.cropId,
      variableType: item.variableType,
      minimumValue: edit.minimumValue,
      maximumValue: edit.maximumValue,
      acceptableMinimumValue: item.acceptableMinimumValue,
      acceptableMaximumValue: item.acceptableMaximumValue,
      unit: edit.unit,
      timeBasis: item.timeBasis,
      isCompatibleWithSevenDayForecast: item.isCompatibleWithSevenDayForecast,
      isActive: item.isActive,
    }), 'Crop requirement updated.');
  }

  createSoilCompatibility(): void {
    this.save(this.api.createAdminResource('soil-compatibility', this.soilForm), 'Soil compatibility saved.');
  }

  updateSoilCompatibility(item: SoilCompatibilityResponse): void {
    const edit = this.soilEdit(item);
    this.save(this.api.updateAdminResource('soil-compatibility', item.id, {
      cropId: item.cropId,
      soilType: edit.soilType,
      compatibilityScore: edit.compatibilityScore,
      isActive: item.isActive,
    }), 'Soil compatibility updated.');
  }

  createSuitabilityConfig(): void {
    this.save(this.api.createAdminResource('suitability-config', this.configForm), 'Suitability configuration saved.');
  }

  updateSuitabilityConfig(item: SuitabilityConfigurationResponse): void {
    const edit = this.configEdit(item);
    this.save(this.api.updateAdminResource('suitability-config', item.id, {
      configurationType: edit.configurationType,
      configurationKey: edit.configurationKey,
      value: edit.value,
      isActive: item.isActive,
    }), 'Suitability configuration updated.');
  }

  setActive(resource: Exclude<AdminResource, 'recommendations' | 'validations'>, id: number, isActive: boolean): void {
    if (!confirm(`Set this record ${isActive ? 'active' : 'inactive'}?`)) {
      return;
    }

    this.save(this.api.setAdminResourceActiveStatus(resource, id, isActive), 'Active status updated.');
  }

  updateUserRole(userId: number, role: string): void {
    this.save(this.api.updateAdminUserRole(userId, role), 'User role updated.');
  }

  requirementEdit(item: CropRequirementResponse): Partial<CropRequirementResponse> {
    this.requirementEdits[item.id] ??= { minimumValue: item.minimumValue, maximumValue: item.maximumValue, unit: item.unit };
    return this.requirementEdits[item.id];
  }

  soilEdit(item: SoilCompatibilityResponse): Partial<SoilCompatibilityResponse> {
    this.soilEdits[item.id] ??= { soilType: item.soilType, compatibilityScore: item.compatibilityScore };
    return this.soilEdits[item.id];
  }

  configEdit(item: SuitabilityConfigurationResponse): Partial<SuitabilityConfigurationResponse> {
    this.configEdits[item.id] ??= { configurationType: item.configurationType, configurationKey: item.configurationKey, value: item.value };
    return this.configEdits[item.id];
  }

  private loadActiveTab(): void {
    const tab = this.activeTab();
    this.isLoading.set(true);
    this.error.set(null);
    this.success.set(null);
    this.api
      .listAdminResources<unknown>(tab, 1, 20)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: page => this.assignRows(tab, page.items),
        error: (error: FrontendApiError) => this.error.set(error.message),
      });
  }

  private save(request: Observable<unknown>, message: string): void {
    this.error.set(null);
    this.success.set(null);
    request.subscribe({
      next: () => {
        this.success.set(message);
        this.loadActiveTab();
      },
      error: (error: FrontendApiError) => this.error.set(error.message),
    });
  }

  private assignRows(tab: AdminTab, rows: unknown[]): void {
    if (tab === 'users') this.users.set(rows as AdminUserResponse[]);
    if (tab === 'crops') this.crops.set(rows as CropResponse[]);
    if (tab === 'crop-requirements') this.requirements.set(rows as CropRequirementResponse[]);
    if (tab === 'soil-compatibility') this.soilCompatibilities.set(rows as SoilCompatibilityResponse[]);
    if (tab === 'suitability-config') this.suitabilityConfigs.set(rows as SuitabilityConfigurationResponse[]);
    if (tab === 'recommendations') this.recommendations.set(rows as AdminRecommendationSummaryResponse[]);
    if (tab === 'validations') this.validations.set(rows as AdminValidationResponse[]);
  }
}
