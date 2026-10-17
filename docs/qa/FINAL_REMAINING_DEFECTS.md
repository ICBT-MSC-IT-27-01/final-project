# Final Remaining Defects

| ID | Status | Remaining issue | Required action |
|---|---|---|---|
| QA-001 | Open / critical | Live recommendation configuration has no scientifically approved, seven-day-compatible crop factors or soil mapping; all six crops remain unscored and unranked. | Agricultural review of crop-specific, temporal/geographic evidence; administrator configuration; rerun real browser test. |
| QA-002 | Partial / high | Crop-to-period flow exposes official sources but has no verified crop-specific seasonal window, duration, or irrigation applicability. | Extract/review each DoA calendar and record approved windows/assumptions; obtain supervisor approval for this extension. |
| QA-003 | Partial / high | Date-range flow is historical climate exploration with source metadata, not a validated crop recommendation. | Approve the extension and provide time-compatible candidate rules; otherwise retain current exploration wording. |
| QA-004 | Blocked / high | No authorized QA accounts or safe records exist for registered, officer, or admin end-to-end behaviour. | Provide disposable role accounts and non-production records. |
| QA-005 | Open / medium | `System.Security.Cryptography.Pkcs` 9.0.11 advisory warnings remain. | Owner-controlled dependency remediation; `AnuradhapuraAI.Api.csproj` was not modified. |
| QA-006 | Blocked / medium | Fresh provider/model/persistence path not executed while `EnableWeatherModelIntegration` is false. | Run an authorized isolated development configuration without altering production flags. |

No protected API project file was modified, and no database data was reset, migrated, or destructively changed.
