# Phase 17 Dissertation Consistency Audit

Generated: 2026-10-13

## Available Materials

The repository contains project guidance, phase reports, source code, tests, model artifacts, and Phase 16 dissertation evidence. Approved Chapter 1, Chapter 2, Chapter 3, Chapter 4, final architecture diagrams, UML diagrams, ER diagrams, and supervisor feedback files were not found as editable chapter documents in the repository.

Because approved chapter source files are unavailable, Phase 17 creates clearly labelled draft material for supervisor review. It does not overwrite approved dissertation text.

## Consistent Claims To Preserve

| Area | Consistent wording |
| --- | --- |
| Project scope | Anuradhapura District, Sri Lanka |
| Crops | Paddy, Maize, Green Gram, Cowpea, Groundnut, Chilli |
| Weather model role | The LSTM forecasts weather variables only |
| Recommendation role | Crop recommendation uses explainable weighted suitability scoring |
| Historical weather source | ERA5-derived daily weather data accessed through Open-Meteo |
| Operational weather source | Open-Meteo Forecast API past-days data, used with caution |
| Forecast horizon | Seven daily predictions |
| Training/evaluation status | Historical ERA5 evaluation completed |
| Operational accuracy | Not scientifically verified |
| UAT | Real participant UAT not performed |
| Expert validation | Agricultural Officer expert validation not performed |
| Ethics | Pending before participant recruitment |
| Browser E2E | Passed with deterministic mocked API responses |

## Claims Requiring Correction If Present In Draft Chapters

| Claim to avoid | Correction |
| --- | --- |
| "The model predicts the best crop" | The model predicts rainfall, temperature and humidity; a separate suitability engine ranks crops |
| "ERA5 and Open-Meteo operational data are equivalent" | Source compatibility remains a limitation and threat to validity |
| "The system is production ready" | Deployment readiness is partial and no public deployment is claimed |
| "UAT has passed" | UAT instruments are prepared; real participant UAT is not performed |
| "Agricultural Officers validated the recommendations" | An officer validation workflow exists; real expert validation is not performed |
| "Browser E2E proves live production behavior" | Browser E2E used deterministic mocked responses to avoid side effects |
| "Seven-day forecasts prove full crop-season suitability" | Seven-day suitability is a prototype decision-support view with scientific limitations |
| "Crop thresholds are fully scientifically validated" | Scientific evidence is partial; annual/seasonal values were not activated as seven-day thresholds |

## Unresolved Inconsistencies

No contradictory approved chapter text could be audited because chapter source documents were not present. Any external Word/PDF dissertation draft should be reviewed against this audit before submission.
