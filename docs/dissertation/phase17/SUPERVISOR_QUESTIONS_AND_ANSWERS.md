# Phase 17 Supervisor Questions And Answers

Generated: 2026-10-13

## Likely Questions

| Question | Defensible answer |
| --- | --- |
| Why was LSTM selected? | The project requires a time-series weather forecasting component. LSTM is suitable for sequential data and was implemented as the approved Python forecasting approach. |
| Why use ERA5 for training? | ERA5-derived data provided a complete daily 2010-2025 historical dataset for the representative Anuradhapura point. It is reanalysis data, not station observations. |
| Why use Open-Meteo operationally? | Open-Meteo provided accessible past-days weather variables for recent operational input. Its source compatibility with ERA5 remains a limitation. |
| Does the LSTM recommend crops directly? | No. The LSTM predicts weather variables. Crop recommendations are produced separately by the explainable suitability engine. |
| How is seven-day suitability justified? | It is a prototype short-term suitability decision-support view. It does not replace full-season agronomic planning. |
| Are crop thresholds scientifically validated? | Partially. Contextual FAO evidence exists, but fully validated seven-day thresholds for all crops are not available. |
| Why is soil optional? | Soil source and selection method are unresolved. The engine renormalizes valid available factors rather than fabricating soil scores. |
| What is the Agricultural Officer role? | Officers review recommendation evidence and assign validation statuses. They do not configure crop requirements or system settings. |
| How was model accuracy measured? | Historical ERA5 test split metrics use MAE and RMSE against a persistence baseline. |
| What does the baseline represent? | The baseline repeats the last observed weather vector across the seven-day forecast horizon. |
| Were real users involved? | No real participant UAT has been performed. Instruments are prepared pending ethics approval. |
| Is ethics approval obtained? | Ethics approval is pending. No participant data was collected by Phase 17. |
| Is the system production-ready? | No. It is an MSc prototype with deployment readiness gaps and scientific limitations. |
