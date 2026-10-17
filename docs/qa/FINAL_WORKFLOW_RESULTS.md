# Final Workflow Results

## Public results

| Workflow | Actual result | Status |
|---|---|---|
| Forecast | Seven stored `v1` forecast days displayed through the real API. | PASS — stored display only |
| Recommendation | Six crop cards returned; each reports no evaluable configured factors, no score/category/rank. | PARTIAL / QA-001 open |
| Date range | 7-day, 30-day, 3-, 6-, 12-month, and custom future intervals return historical calendar-month climate and six evidence cards. Partial-month day counts are now shown. | PARTIAL |
| Crop period | Paddy, Maize, Green Gram, Cowpea, Groundnut, and Chilli each return official source links and Dry Zone evidence; no periods asserted. | PARTIAL |
| Invalid input | Past, reversed, empty and >366-day ranges show clear feedback. | PASS |
| Responsive / public authorization | Desktop, tablet, mobile and anonymous protected-route behaviour captured. | PASS |

## Crop-period output summary

| Crop | Documented official source condition | Actual output |
|---|---|---|
| Paddy | Dry & Intermediate Zone, rainfed and irrigated calendar links | Source located; no cultivation period asserted |
| Maize | Dry Zone calendar link | Source located; no cultivation period asserted |
| Green Gram | Dry Zone calendar link | Source located; no cultivation period asserted |
| Cowpea | Dry Zone calendar link | Source located; no cultivation period asserted |
| Groundnut | Intermediate & Dry Zone calendar link | Source located; no cultivation period asserted |
| Chilli | Dry Zone calendar link | Source located; no cultivation period asserted |

The source index and the DoA Anuradhapura office URL are delivered in every seasonal response. This is source traceability, not expert validation or a crop-period recommendation.
