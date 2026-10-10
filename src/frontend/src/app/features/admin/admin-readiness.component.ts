import { Component } from '@angular/core';

interface ReadinessRow {
  area: string;
  endpoint: string;
  status: 'Backend ready' | 'Frontend deferred' | 'Blocked';
}

@Component({
  selector: 'app-admin-readiness',
  template: `
    <section class="page-title">
      <p class="eyebrow">Administrator</p>
      <h1>Backend Readiness</h1>
      <p>
        Administrator APIs exist, but Phase 10 keeps management screens conservative until the
        owner approves the specific admin UX and data-editing flows.
      </p>
    </section>

    <section class="table-panel">
      <table>
        <thead>
          <tr>
            <th>Area</th>
            <th>Backend endpoint</th>
            <th>Phase 10 status</th>
          </tr>
        </thead>
        <tbody>
          @for (row of rows; track row.area) {
            <tr>
              <td>{{ row.area }}</td>
              <td><code>{{ row.endpoint }}</code></td>
              <td>{{ row.status }}</td>
            </tr>
          }
        </tbody>
      </table>
    </section>
  `,
})
export class AdminReadinessComponent {
  readonly rows: ReadinessRow[] = [
    { area: 'Users', endpoint: '/api/admin/users', status: 'Backend ready' },
    { area: 'Roles', endpoint: '/api/admin/users/{id}/role', status: 'Backend ready' },
    { area: 'Crops', endpoint: '/api/admin/crops', status: 'Backend ready' },
    { area: 'Crop requirements', endpoint: '/api/admin/crop-requirements', status: 'Backend ready' },
    { area: 'Soil compatibility', endpoint: '/api/admin/soil-compatibility', status: 'Backend ready' },
    { area: 'Suitability configuration', endpoint: '/api/admin/suitability-config', status: 'Backend ready' },
    { area: 'Full admin editing UI', endpoint: 'Angular workflow', status: 'Frontend deferred' },
  ];
}
