# Phase 12 Weather Source Assessment

Status: PLANNING ONLY - OWNER REVIEW REQUIRED

This document assesses candidate operational weather data sources for future automatic forecast generation in the AI-Driven Weather Forecasting and Crop Recommendation System for Anuradhapura District.

No provider integration, API call automation, database write, scheduling, model retraining, or production configuration change is approved by this document.

## Existing Repository Evidence

The approved weather model is a Python TensorFlow/Keras LSTM trained on an ERA5-derived Anuradhapura representative-point dataset covering 2010-01-01 through 2025-12-31. Repository metadata confirms:

- input sequence length: 30 chronological daily observations
- feature order: `Rainfall`, `Temperature`, `Humidity`
- horizon: direct 7-day prediction
- model version: `v1`
- model artifact: `src/forecasting-service/artifacts/v1_era5_2010_2025/v1.keras`
- scaler artifact: `src/forecasting-service/artifacts/v1_era5_2010_2025/v1_scaler.joblib`
- metadata artifact: `src/forecasting-service/artifacts/v1_era5_2010_2025/v1_metadata.json`

ERA5 is reanalysis/modelled historical weather data, not a local Sri Lanka weather-station record. It must not be described as direct station observations or as real-time observed data.

## Candidate Sources

| Candidate | Official documentation | Product type | Anuradhapura coverage | Variables relevant to model | Latency/freshness | Access/auth | Cost/licensing notes | Model compatibility | MSc suitability |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Open-Meteo Historical Weather API | [Open-Meteo Historical Weather API](https://open-meteo.com/en/docs/historical-weather-api), [pricing/licensing](https://open-meteo.com/en/pricing) | Reanalysis/API aggregation including ERA5, ERA5-Land, ECMWF IFS depending on model selection | Coordinate-based global coverage; suitable for representative Anuradhapura coordinate selection | Daily mean temperature, precipitation sum, and mean relative humidity are documented; hourly variables also include temperature, relative humidity, precipitation, and rain | ERA5/ERA5-Land documented with about 5-day delay; newer IFS options can be fresher but may change model domain | Free tier for evaluation/non-commercial; API key for paid/commercial endpoint | Open-Meteo documents free/open-access limits and CC BY 4.0 attribution expectations; commercial usage requires paid plan | Strongest match to current training lineage when selecting ERA5/ERA5-Land. IFS is fresher but creates distribution shift | Preferred for MSc prototype if owner accepts reanalysis-derived operational observations |
| NASA POWER Daily API | [NASA POWER Daily API](https://power.larc.nasa.gov/docs/services/api/temporal/daily/), [parameter dictionary](https://power.larc.nasa.gov/parameters/) | NASA gridded analysis/reanalysis-derived meteorological time series | Point and regional API options; global gridded coverage | Daily API supports daily meteorological parameters; common relevant parameters include T2M, RH2M, and precipitation parameters, but exact parameter selection must be confirmed through the official parameter dictionary before implementation | Daily data available from 1981-01-01 to Near Real Time; default daily time standard is Local Solar Time unless UTC is requested | API endpoint; no project integration currently configured | Cost/licensing must be verified before production or dissertation redistribution; do not assume unrestricted use beyond documented NASA POWER terms | Compatible after strict unit/time-standard mapping; different source family from training ERA5 introduces distribution shift | Strong alternative for reproducibility and academic demonstration |
| Copernicus CDS ERA5 / ERA5-Land | [Copernicus ERA5 overview](https://climate.copernicus.eu/climate-reanalysis), [ERA5 dataset listing](https://cds.climate.copernicus.eu/datasets/reanalysis-era5-single-levels?tab=overview), [ERA5 family documentation](https://confluence.ecmwf.int/pages/viewpage.action?pageId=540946050) | Direct CDS reanalysis data; ERA5, ERA5-Land, daily statistics where available | Global gridded coverage; coordinate or grid extraction possible | ERA5 single-level data can support temperature, precipitation, humidity/dewpoint-derived humidity depending on product and processing | ERA5 is not real-time; documented delays are expected. Exact operational latency must be verified at integration time | CDS account/API setup required | Copernicus licensing and attribution must be reviewed and recorded before use | Scientifically aligned with training data, but operational integration complexity is higher than Open-Meteo | Good fallback for reproducible research pipeline, less convenient for MSc frontend demo |
| Sri Lanka Department of Meteorology | [Department of Meteorology Sri Lanka](https://www.meteo.gov.lk/index.php?lang=en) | National meteorological observations/forecasts/public bulletins | Official national source; station availability for Anuradhapura must be confirmed | Site shows observation data, 24-hour rainfall, forecasts, agromet/drought products; API availability for daily rainfall, mean temperature, mean humidity is not verified | Potentially most authoritative if accessible; availability and latency require owner follow-up | Public site; data access contact listed on site | Licensing/data-sharing conditions require direct confirmation | Best station-observation provenance if daily API/data feed is available; currently unverified | Scientifically attractive but blocked until data access, station coverage, variables, and license are confirmed |

## Decision Matrix

Scoring uses qualitative planning ratings only: High, Medium, Low, or Unknown. These are not fabricated numeric scores.

| Criterion | Open-Meteo historical/recent | NASA POWER | Copernicus CDS ERA5/ERA5-Land | Sri Lanka Department of Meteorology |
| --- | --- | --- | --- | --- |
| Data compatibility | High for ERA5-like workflow | Medium-High after parameter verification | High scientifically; more processing | Unknown until station/API data confirmed |
| Geographic coverage | High, coordinate-based | High, gridded | High, gridded | Potentially High for official stations, but access unknown |
| Freshness | Medium; depends on selected model | Medium; near real time documented | Low-Medium for ERA5 delays | Unknown |
| Scientific defensibility | Medium-High with reanalysis caveat | Medium-High with source caveat | High for reanalysis continuity | Potentially High for observations |
| Ease of integration | High | Medium | Low-Medium | Unknown |
| Reproducibility | High | High | High | Unknown |
| Cost/licensing clarity | Medium-High | Medium; verify terms | Medium; verify CDS license | Unknown |
| Operational complexity | Low-Medium | Medium | High | Unknown |

## Proposed Recommendation

Preferred candidate: PROPOSED - OWNER APPROVAL REQUIRED

Use Open-Meteo Historical Weather API in a controlled prototype mode, selecting an ERA5/ERA5-Land-compatible historical/recent product where possible, because the current model was trained on an ERA5-derived dataset and Open-Meteo already exposes daily mean temperature, precipitation sum, and mean relative humidity variables through a simple coordinate API.

Alternative candidate: PROPOSED - OWNER APPROVAL REQUIRED

Use NASA POWER Daily API if owner prioritizes a NASA-hosted reproducible academic source and accepts explicit unit/time-standard mapping and distribution-shift caveats.

Fallback strategy: PROPOSED - OWNER APPROVAL REQUIRED

Use manually prepared, cited 30-day observation CSV fixtures for MSc demonstration until an approved operational provider is selected. This avoids falsely presenting reanalysis or provider forecasts as live station observations.

## Scientific Limitations To Document

- A provider weather forecast is not an observed 30-day input sequence and must not be fed into the LSTM as though it were observed history.
- Reanalysis and gridded products are estimates informed by observations and models, not ground-truth station readings.
- Station data, if obtained, may differ from ERA5 training distribution.
- Humidity aggregation must match the model's daily mean humidity expectation.
- Rainfall must be daily accumulation in millimeters; hourly intensity or provider forecast totals require careful conversion.
- Anuradhapura District has spatial variability; a single representative coordinate is an MSc prototype simplification.

## Owner Decisions Required

1. Select approved operational weather source.
2. Approve representative coordinate or station mapping for Anuradhapura District.
3. Approve source licensing/attribution wording.
4. Approve data freshness/staleness thresholds.
5. Approve whether reanalysis-derived data is acceptable for operational demonstration.
6. Approve any future schema changes for provider/source provenance.
