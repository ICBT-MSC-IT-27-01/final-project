# Phase 12 Owner Decision Register

Status: OWNER REVIEW REQUIRED

This register lists decisions that must be made before Phase 12 designs can become implementation work.

| ID | Decision | Options | Recommendation | Approval required before |
| --- | --- | --- | --- | --- |
| OD-01 | Operational weather source | Open-Meteo, NASA POWER, Copernicus CDS, Sri Lanka Department of Meteorology, manual fixture | Open-Meteo for prototype, NASA POWER fallback, DoM follow-up for station data | Any provider integration or provider API call |
| OD-02 | Representative Anuradhapura location | single coordinate, station ID, district centroid, approved DoM station | single documented representative coordinate for MSc prototype unless station data approved | Data retrieval and dissertation method |
| OD-03 | Reanalysis acceptability | allow reanalysis-derived operational input, require station observations, allow manual fixture only | allow reanalysis for prototype with explicit limitations | Public/demo forecast workflow |
| OD-04 | Daily boundary/timezone | UTC, Asia/Colombo local day, provider default, Local Solar Time | Prefer explicit Asia/Colombo or documented provider daily boundary; avoid implicit default | Observation adapter design |
| OD-05 | Missing-day handling | reject, interpolate, carry forward, provider fallback | reject until scientifically justified imputation is approved | Any automatic forecast generation |
| OD-06 | Freshness/staleness threshold | no threshold, 48h, 72h, owner-defined | propose 72h warning threshold for demo, not hard block | Latest forecast API implementation |
| OD-07 | Read-only forecast feature flag behavior | allow persisted reads when integration disabled, or block all forecast access | allow persisted reads if clearly labelled; block generation only | Latest forecast API implementation |
| OD-08 | Forecast provenance schema | no schema change, add columns, add JSON provenance, add ForecastRun table | minimal endpoint can derive existing fields; full provenance needs future schema | Production-grade provider provenance |
| OD-09 | Manual versus scheduled generation | manual only, scheduled, both | manual for MSc demonstration | Any scheduling/background-job work |
| OD-10 | Live SQL Server test approach | no live writes, dedicated test DB, existing dev DB | dedicated test DB only after approval | End-to-end live verification |
| OD-11 | Browser visual QA method | manual, Playwright, screenshots only, not verified | manual or browser automation after environment approval | Final UI validation |
| OD-12 | Weather provider licensing/attribution | provider-specific | verify and cite selected provider terms | Dissertation and public UI attribution |

## Explicit Non-Approvals

The following remain not approved:

- weather provider integration
- automatic scheduling
- new backend latest-forecast API implementation
- database schema changes or migrations
- live database writes
- package installation or upgrades
- server configuration changes
- model retraining
- Git staging, commit, or push

## Recommended Phase 13 Scope Candidate

After owner design review, a future phase could implement:

1. read-only latest forecast endpoint using existing `ForecastRecord`,
2. Angular dashboard integration for latest persisted forecast,
3. optional manual operator-only forecast-generation workflow using approved observation fixture,
4. no automatic provider integration until OD-01 through OD-08 are resolved.
