# Phase 17 Figure Register

Generated: 2026-10-13

## Figure Register

| Figure | Proposed title | Chapter | Source | Evidence status | Required action |
| --- | --- | --- | --- | --- | --- |
| Fig. 5.1 | Implemented three-tier architecture | Chapter 5 | `docs/phase-1-architecture.md`, backend projects | Documented, no diagram file found | Create final dissertation diagram from approved architecture if required |
| Fig. 5.2 | Weather forecasting pipeline | Chapter 5 | `docs/phase-5-weather-forecasting.md`, Python modules | Documented, no diagram file found | Prepare diagram showing ingestion, preprocessing, training, evaluation, inference |
| Fig. 5.3 | Operational forecast integration | Chapter 5 | `docs/PHASE13_FINAL_VERIFICATION_REPORT.md` | Documented | Use Phase 13 flow; label Open-Meteo and source-shift limitation |
| Fig. 5.4 | Crop recommendation workflow | Chapter 5 | `docs/PHASE8_RECOMMENDATION_WORKFLOW_SPECIFICATION.md`, Phase 14 report | Documented | Use existing workflow and mark insufficient-evidence behavior |
| Fig. 6.1 | Testing pyramid / strategy | Chapter 6 | Phase 16 evidence | Draft needed | Build from automated/backend/Python/frontend/E2E layers |
| Fig. 7.1 | LSTM versus baseline metric comparison | Chapter 7 | `v1_evaluation.json` | Data available | Create chart from verified MAE/RMSE only |
| Fig. 7.2 | Evidence limitation map | Chapter 7 | Phase 15/16 reports | Draft needed | Show technical, scientific, UAT and ethics boundaries |
| Fig. 8.1 | Future work roadmap | Chapter 8 | Phase 17 outstanding actions | Draft needed | Keep future work outside implemented claims |

## Screenshot Status

No application screenshots were committed in Phase 17. The repository supports deterministic browser states through Playwright fixtures, but screenshots should be captured in a clean local run only when needed for the dissertation document. Do not treat illustrative screenshots as scientific validation.

## Screenshot Capture Checklist

1. Start frontend with deterministic mocked or local test data.
2. Avoid showing JWTs, passwords, database strings, personal names, real participant data or browser developer tools.
3. Capture public dashboard, recommendation result, registered history, officer validation and admin readiness screens.
4. Label mocked fixture screenshots as test-state screenshots.
5. Store final sanitized images in the dissertation document package, not in source control unless explicitly required.
