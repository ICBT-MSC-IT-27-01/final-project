import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-public-dashboard',
  imports: [RouterLink],
  template: `
    <section class="dashboard-hero">
      <div>
        <p class="eyebrow">Anuradhapura District only</p>
        <h1>Seven-day weather context for explainable crop recommendations.</h1>
        <p>
          The frontend presents backend-generated forecast provenance and crop evidence without
          inventing live weather inputs or agricultural scores.
        </p>
        <div class="action-row">
          <a routerLink="/recommendations" class="button primary">Generate recommendation</a>
          <a routerLink="/login" class="button secondary">Registered login</a>
        </div>
      </div>
      <aside class="status-board" aria-label="Backend readiness">
        <span class="status ready">Ready</span>
        <strong>Recommendation API</strong>
        <p>Uses the latest complete persisted seven-day forecast run selected by the backend.</p>
      </aside>
    </section>

    <section class="grid-two">
      <article class="panel">
        <h2>Weather Forecast</h2>
        <p>
          Public read-only forecast display is backend-blocked: the available forecast endpoint
          creates forecasts from exactly 30 observed daily records and there is no approved live
          weather provider or current-forecast read endpoint.
        </p>
        <ul class="evidence-list">
          <li>Rainfall, mean temperature and mean humidity are shown after a recommendation response.</li>
          <li>No synthetic observations are entered by the frontend.</li>
          <li>No weather source is claimed unless returned by the backend.</li>
        </ul>
      </article>

      <article class="panel">
        <h2>Supported Crops</h2>
        <div class="crop-cloud" aria-label="Approved crop scope">
          <span>Paddy</span>
          <span>Maize</span>
          <span>Green Gram</span>
          <span>Cowpea</span>
          <span>Groundnut</span>
          <span>Chilli</span>
        </div>
      </article>
    </section>
  `,
})
export class PublicDashboardComponent {}
