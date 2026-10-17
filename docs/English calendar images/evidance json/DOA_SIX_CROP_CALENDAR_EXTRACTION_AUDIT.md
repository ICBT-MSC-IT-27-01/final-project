# DoA Six-Crop Calendar Extraction — Review Baseline

**Status: EXTRACTED FOR MONTH-LEVEL CANDIDATE PLANNING; NOT EXPERT-APPROVED.**

Source: user-supplied `Crop_Calandar_Book_Sinhala_compressed.pdf`, Department of Agriculture / AFACI calendar compilation (charts show 2013; paddy charts indicate 2015). The PDF is image-based; the charts were read visually, not via unreliable text encoding.

## Calendar-based candidate establishment months

| Crop | PDF pages | Maha (approx.) | Yala (approx.) | Conditions / limitations |
|---|---:|---|---|---|
| Paddy | 1–2 | Oct–Nov | Apr–May | 2½ / 3 / 3½ months; irrigation/rainfed separate |
| Maize | 5 | Oct | Apr–May | Dry Zone; use PDF instead of Intermediate Zone image |
| Green Gram | 8 | Nov | Apr | Variety-dependent ~55–65 days; conditional third season |
| Cowpea | 19 | Nov | Apr | Variety-dependent ~55–90 days; conditional third season |
| Groundnut | 7 | Oct–Nov | Apr–May | Anuradhapura DL-1d listed; drainage and water matter |
| Chilli | 15 | Oct–Nov | Apr–May | Nursery precedes planting; Dry Zone only |

**Important:** The months above are approximate crop-establishment/sowing/transplanting windows interpreted from illustrated timelines, not exact week-level prescriptions. The separate `landPreparationMonths`, `sowingOrTransplantingMonths` and `harvestMonths` fields in JSON are indicative and should be reviewed by an agricultural officer/agronomist before production planting advice.

## Evidence and applicability notes

- **Paddy:** page 1 irrigated, page 2 rainfed, both Dry/Intermediate Zones. Preserve irrigation distinction; water availability and variety change feasibility. `paddy.jpg` is an integrated pest management calendar, not the source of sowing windows.
- **Maize:** page 5 is Dry Zone and appropriate as the starting calendar for Anuradhapura. `maiz_inter.jpg` covers Intermediate Zone and must not replace it.
- **Green Gram:** page 8 Dry Zone includes Anuradhapura; a separate third-season row is conditional, not universal.
- **Cowpea:** page 19 Dry Zone includes Anuradhapura; third-season paddy-field context is conditional.
- **Groundnut:** page 7 covers Dry and Intermediate Zones and names Anuradhapura DL-1d. Applicability outside that zone must be checked.
- **Chilli:** page 15 Dry Zone includes Anuradhapura. Page 16 is Intermediate Zone and `Chili_wet.jpg` is Wet Zone; exclude as default district guidance.

## Scientific scoring boundary

These calendars support **candidate seasonal windows**, not numeric 7-day weather suitability. Existing FAO EcoCrop temperature data remains global, not locally validated. Annual rainfall ranges cannot be compared to seven-day rainfall totals. Humidity, soil, and locally approved rainfall scoring remain missing; QA-001 full scoring is still open.

## Integration checklist

1. Load the JSON as **review-only** calendar evidence; do not overwrite existing `crop-evidence.json` or approved SQL rows.
2. Match by crop + agroecological zone + irrigation/rainfed condition; if condition unknown, show conditional candidate and ask for it or show limitations.
3. Date-range → crops: intersect user range with candidate **sowing/transplanting** months, not land preparation or harvest months. Use `CALENDAR_SUPPORTED_CANDIDATE`, not `SUITABILITY_ASSESSED`.
4. Crop → periods: show candidate months and season, plus source PDF page, assumptions and limitation. Do not assert exact dates or forecast 3–12 months with LSTM.
5. Obtain local agronomist validation for month precision, cultivation conditions, and varieties before claiming field-ready advice.
6. Keep QA-001 open until separate scientifically defensible weather/soil assessment and scoring is validated.

## Source-page map

Paddy: pp. 1–2; Maize: p. 5; Groundnut: p. 7; Green Gram: p. 8; Chilli: p. 15; Cowpea: p. 19.
