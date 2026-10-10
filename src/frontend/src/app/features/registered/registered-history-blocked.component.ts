import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-registered-history-blocked',
  imports: [RouterLink],
  template: `
    <section class="page-title">
      <p class="eyebrow">Registered User</p>
      <h1>Recommendation History</h1>
      <p>
        Recommendation generation is available for authenticated registered users, but history
        retrieval is backend-blocked because no authorized history or recommendation-detail
        endpoint currently exists.
      </p>
    </section>

    <article class="panel">
      <h2>Backend dependency</h2>
      <p>
        The frontend does not fabricate history records and does not expose public history. Once
        an approved backend history contract is implemented, this route can render authenticated
        user-owned records.
      </p>
      <a routerLink="/recommendations" class="button primary">Generate a recommendation</a>
    </article>
  `,
})
export class RegisteredHistoryBlockedComponent {}
