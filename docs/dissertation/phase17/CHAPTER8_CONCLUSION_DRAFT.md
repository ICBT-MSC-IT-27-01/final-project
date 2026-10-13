# Chapter 8 Conclusion Draft

Generated: 2026-10-13

Status: proposed draft material.

## 8.1 Research Summary

This project developed an AI-driven weather forecasting and crop recommendation prototype for Anuradhapura District. The system forecasts seven days of rainfall, temperature and humidity using an LSTM model and applies an explainable weighted scoring engine to evaluate the six approved crops.

## 8.2 Achievement Of Objectives

The technical objectives were achieved at prototype level: the backend, frontend, forecasting service, database foundation, authentication, role workflows, forecast integration and recommendation evaluation are implemented and tested. The scientific validation objectives are partially achieved because historical weather model evaluation exists, but full agronomic validation of recommendations is not complete.

## 8.3 Technical Contributions

The system contributes a modular architecture, a reproducible weather forecasting pipeline, operational forecast provenance, explainable suitability scoring, feature-flagged recommendation history and role-based validation workflows.

## 8.4 Research Findings

The LSTM outperformed a persistence baseline on historical ERA5 test data for overall MAE and RMSE. The recommendation engine can produce transparent evidence-based suitability outputs when configuration evidence is available and can clearly report insufficient evidence when it is not.

## 8.5 Limitations

The project is not production-ready. Real participant UAT and Agricultural Officer expert validation were not performed. Ethics approval is pending. Crop threshold validation is partial, and source compatibility between ERA5 and Open-Meteo operational input remains unresolved.

## 8.6 Future Improvements

Future work should include ethics-approved UAT, qualified Agricultural Officer review, agronomic threshold validation, longitudinal operational forecast evaluation, source compatibility analysis, production deployment hardening and improved local soil-data workflows.

## 8.7 Final Conclusion

The project provides a technically validated MSc prototype that integrates AI weather forecasting with explainable crop suitability assessment. It should be presented as a decision-support prototype with documented limitations, not as an autonomous or fully validated agricultural decision system.
