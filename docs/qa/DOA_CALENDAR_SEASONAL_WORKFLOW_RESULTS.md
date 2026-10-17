# DoA Calendar Seasonal Workflow Results

Date: 2026-10-17. The original `Crop_Calandar_Book_Sinhala_compressed.pdf` was rendered and visually reviewed on pages 1, 2, 5, 7, 8, 15 and 19. The supplied extraction audit is confirmed at **month-level precision only**.

## Integrated behavior

- `POST /api/seasonal-recommendations/by-date-range` intersects a user range only with documented sowing/transplanting months and returns `CALENDAR_SUPPORTED_CANDIDATE` or `CLIMATE_CONTEXT_ONLY` for each of the six crops.
- `POST /api/seasonal-recommendations/by-crop` returns matching Maha/Yala candidate periods, source PDF page, conditions and the original interpretation.
- Every response retains `HISTORICAL_SEASONAL_CLIMATE` for ERA5 context. It never labels 3-, 6- or 12-month climatology as an LSTM forecast.

## Actual visible-Chrome results

The headed workflow audit completed with `backend_ready=True frontend_ready=True`.

| Test | Result |
|---|---|
| Seven days, 2026-10-17 to 2026-10-24 | Maha candidates: Paddy, Maize, Groundnut, Chilli. Green Gram and Cowpea correctly remain climate context because their documented establishment month is November. |
| Thirty days, 2026-10-17 to 2026-11-16 | All six Maha candidates. |
| Three months | All six Maha candidates. |
| Six and twelve months | All six show both conditional Maha and Yala candidate windows. |
| Crop → period (Paddy, Maize, Green Gram, Cowpea, Groundnut, Chilli) | Each returned `CALENDAR_SUPPORTED_CANDIDATE` with both Maha and Yala windows for the tested six-month search. |
| Invalid past/reversed/over-366 ranges | API returned validation errors; no candidate was fabricated. |

Evidence: `qa-evidence/visible-browser-observations.json`, `qa-evidence/SP-01-seven-days.png`, `qa-evidence/SP-02-thirty-days.png`, and `qa-evidence/CP-*.png`.

## Remaining limits

The candidates are not agronomist-approved planting prescriptions or full suitability recommendations. Paddy remains conditional on separate rainfed/irrigated calendars; maize requires water/rainfall consideration; groundnut requires DL-1d/local-area and drainage review; chilli requires Dry Zone and nursery/irrigation review. Soil, rainfall sufficiency and humidity evidence are not inferred from the calendars.
