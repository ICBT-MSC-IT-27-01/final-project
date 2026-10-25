$ErrorActionPreference = 'Stop'
$baseUrl = 'http://localhost:5099'
$validRange = @{ startDate = '2026-11-01'; endDate = '2026-11-30' } | ConvertTo-Json
$forecast = Invoke-RestMethod -Uri "$baseUrl/api/forecasts/latest"
$recommendation = Invoke-RestMethod -Uri "$baseUrl/api/recommendations/evaluate" -Method Post -ContentType 'application/json' -Body (@{ district = 'Anuradhapura District'; soilType = $null } | ConvertTo-Json)
$range = Invoke-RestMethod -Uri "$baseUrl/api/seasonal-recommendations/by-date-range" -Method Post -ContentType 'application/json' -Body $validRange
$cropResults = @()
foreach ($cropName in @('Paddy', 'Maize', 'Green Gram', 'Cowpea', 'Groundnut', 'Chilli')) {
    $body = @{ cropName = $cropName; startDate = '2026-11-01'; endDate = '2026-11-30' } | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "$baseUrl/api/seasonal-recommendations/by-crop" -Method Post -ContentType 'application/json' -Body $body
    $cropResults += @{ cropName = $cropName; evidenceStatus = $response.evidenceStatus; candidateWindowCount = @($response.candidateWindows).Count }
}

function Get-HttpStatus($uri, $body) {
    try {
        if ($null -eq $body) { Invoke-WebRequest -Uri $uri -UseBasicParsing | Out-Null }
        else { Invoke-WebRequest -Uri $uri -Method Post -ContentType 'application/json' -Body $body -UseBasicParsing | Out-Null }
        return 200
    } catch {
        return [int]$_.Exception.Response.StatusCode
    }
}

$summary = [ordered]@{
    checkedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
    forecastDayCount = @($forecast.dailyForecasts).Count
    forecastFreshnessStatus = $forecast.freshnessStatus
    recommendationCropCount = @($recommendation.crops).Count
    recommendationCompleteCount = @($recommendation.crops | Where-Object evaluationStatus -eq 'Complete').Count
    recommendationUnsuitableCount = @($recommendation.crops | Where-Object suitabilityCategory -eq 'Unsuitable').Count
    dateRangeEvidenceStatus = $range.evidenceStatus
    dateRangeCropCount = @($range.crops).Count
    byCrop = $cropResults
    pastDateStatus = Get-HttpStatus "$baseUrl/api/seasonal-recommendations/by-date-range" (@{ startDate = '2026-10-01'; endDate = '2026-10-31' } | ConvertTo-Json)
    over366DaysStatus = Get-HttpStatus "$baseUrl/api/seasonal-recommendations/by-date-range" (@{ startDate = '2026-11-01'; endDate = '2027-11-03' } | ConvertTo-Json)
    unsupportedCropStatus = Get-HttpStatus "$baseUrl/api/seasonal-recommendations/by-crop" (@{ cropName = 'Rice'; startDate = '2026-11-01'; endDate = '2026-11-30' } | ConvertTo-Json)
    anonymousAdminStatus = Get-HttpStatus "$baseUrl/api/admin/users" $null
    anonymousOfficerStatus = Get-HttpStatus "$baseUrl/api/validations/pending" $null
}
$summary | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'evidence/live-api-summary.json')
$summary | ConvertTo-Json -Depth 5
