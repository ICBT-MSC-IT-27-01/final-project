# Phase 15 Agricultural Officer Validation Protocol

Generated: 2026-10-13

## Workflow Verification

Automated tests verify that:

- Agricultural Officers must be authenticated.
- Agricultural Officers can use the validation workflow.
- Public and ordinary users cannot perform officer validation.
- Officers do not receive Administrator privileges.
- Officers cannot modify crop thresholds, weather forecasts, model settings, or system configuration through the validation workflow.

## Expert Validation Status

Agricultural Officer expert validation was `NOT PERFORMED` in Phase 15.

No real officer feedback, survey response, field assessment, or expert review result was fabricated.

## Future Expert Review Protocol

Recommended review package:

1. Show the officer the seven-day forecast provenance and model limitations.
2. Show each crop result with factor scores and missing-data indicators.
3. Ask whether the recommendation explanation is understandable.
4. Ask whether missing soil or humidity evidence is clearly disclosed.
5. Ask whether the crop ranking is agronomically plausible for the stated forecast window.
6. Ask whether annual/seasonal requirements should be used only as context or mapped through an approved agronomic method.
7. Record validation status: Pending Validation, Validated, or Needs Review.
8. Record free-text comments.
9. Preserve reviewer identity and timestamp through the existing validation schema.

Expert feedback must be reported as expert review, not as statistical accuracy.
