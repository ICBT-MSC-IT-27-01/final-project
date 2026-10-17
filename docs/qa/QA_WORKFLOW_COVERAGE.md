# QA Workflow Coverage

## Counting basis

The denominator is the ten workflow groups WF-01 through WF-10 stated in the audit request. It deliberately excludes unavailable diagrams and does not count implementation existence as verified behaviour.

| Result | Workflow count | Workflows |
|---|---:|---|
| MATCHES PROPOSED DESIGN | 1 | WF-10 error handling |
| PARTIALLY MATCHES | 2 | WF-01 forecast display, WF-07 stored forecast retrieval |
| DOES NOT MATCH | 1 | WF-08 live scored/ranked suitability |
| NOT IMPLEMENTED | 0 | — |
| BLOCKED | 4 | WF-04 history, WF-05 officer, WF-06 admin, WF-09 persistence/history |
| NOT VERIFIABLE FROM AVAILABLE DESIGN | 2 | WF-02 date-range crops, WF-03 crop-period planning |

**Verified full-match coverage: 1/10 (10%).**

**At least partially evidenced coverage: 3/10 (30%).**

The two seasonal workflows are intentionally excluded from approved-design verification: Chapters 3–4 specify a seven-day forecast-to-suitability flow, not long-range date planning or crop calendar period selection.

## Browser evidence index

- `qa-evidence/PUB-01-home-desktop.png` — public forecast dashboard.
- `qa-evidence/PUB-04-live-recommendation.png` — real forecast-linked recommendation response, insufficient evidence.
- `qa-evidence/SP-01-seven-days.png` through `SP-08-empty.png` — range scenarios.
- `qa-evidence/CP-paddy.png` through `CP-chilli.png` — all six crop-period attempts.
- `qa-evidence/RSP-01-tablet.png`, `RSP-02-mobile.png` — responsive checks.
- `qa-evidence/SEC-01-admin-anonymous.png`, `SEC-02-officer-anonymous.png` — anonymous guard behaviour.
- `qa-evidence/visible-browser-observations.json` — API status and console observation log.
