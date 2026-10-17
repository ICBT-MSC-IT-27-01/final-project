# Seasonal planning implementation note

The public application now exposes `/seasonal-planning` with two workflows: date-range crop exploration and crop-based period exploration. Both call the ASP.NET Core API and read the repository's `docs/anuradhapura_weather_2010_2025_era5.csv` dataset at runtime.

Endpoints:

- `POST /api/seasonal-recommendations/by-date-range`
- `POST /api/seasonal-recommendations/by-crop`
- `GET /api/seasonal-recommendations/supported-crops`

The service validates a forward-looking range of at most 366 days, aggregates matching calendar months across the locally available 2010–2025 ERA5 reanalysis rows, and returns monthly mean temperature, monthly rainfall, mean humidity, rainfall variability, observations and year coverage. It explicitly labels the result as historical seasonal climate, not a long-range forecast.

No verified seasonal crop requirements or locally applicable planting calendars are configured in the current database. Consequently the API returns `CLIMATE_EXPLORATION_ONLY` / `INSUFFICIENT_AGRONOMIC_EVIDENCE` rather than fabricating planting windows or suitability categories. This seasonal-planning feature is an explicit scope extension and requires supervisor review.

Verification completed: backend Release build, frontend production build, and existing backend regression suite (281 passed). Live API/browser verification remains required because this execution environment blocked background service startup.
