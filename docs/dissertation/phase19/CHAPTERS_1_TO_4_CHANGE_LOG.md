# Phase 19 Chapters 1-4 Change Log

Generated: 2026-10-13

The source Word document was preserved. The corrected Chapters 1-4 draft appends a correction register instead of silently rewriting authorial text.

| ID | Severity | Original Location | Issue | Evidence | Proposed Change | Final Disposition |
| --- | --- | --- | --- | --- | --- | --- |
| P19-ISS-01 | High | Chapter 1 Proposed Solution; Chapter 2 weather-source discussion | NASA POWER appears as proposed/potential data-source wording, while the implemented model used ERA5-derived data. | Source mentions NASA/POWER; model metadata and Phase 5/15 evidence identify ERA5 dataset. | Clarify NASA POWER as planning/literature context and ERA5 as final training/evaluation source. | Resolved in correction register and integrated draft. |
| P19-ISS-02 | Medium | Chapter 1 model evaluation wording | Source allows additional metrics; approved reproducible evaluation uses MAE and RMSE only. | v1_evaluation.json and project specification. | State final evaluation used MAE/RMSE against a persistence baseline. | Resolved in Chapters 6-7 integration. |
| P19-ISS-03 | High | Chapter 1 evaluation scope | Professional/officer review is described as feasible, but real expert validation was not performed. | Phase 15-18 reports. | Describe officer validation as workflow only and expert validation as pending. | Resolved in limitations. |
| P19-ISS-04 | High | Testing/UAT interpretation | UAT instruments exist but real participant UAT was not performed. | Phase 16-18 reports. | Separate prepared UAT instruments from completed UAT. | Resolved in limitations. |
| P19-ISS-05 | Medium | Chapter 4 database design | Implementation added ForecastRun provenance after the original ten-entity foundation. | AddForecastRunProvenance migration and Phase 13 report. | Add implementation note in development/evaluation chapters. | Resolved in Chapters 5 and 7. |
| P19-ISS-06 | High | Weather-source claims | ERA5 and Open-Meteo operational data must not be treated as equivalent. | Phase 13 limitations. | State source shift as a threat to validity. | Resolved in Chapter 7. |
| P19-ISS-07 | High | Crop suitability claims | Seven-day agronomic thresholds are not fully validated for all six crops. | Phase 15 scientific and temporal compatibility reports. | Use partial/insufficient-evidence wording. | Resolved in Chapters 7-8. |
| P19-ISS-08 | Medium | Browser/system testing claims | Browser E2E used mocked API fixtures, not live production operation. | Phase 16 browser E2E report. | Label Playwright as mocked browser workflow validation. | Resolved in Chapter 6. |
