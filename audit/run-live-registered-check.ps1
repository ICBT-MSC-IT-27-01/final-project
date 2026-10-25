$ErrorActionPreference = 'Stop'
trap { Write-Output 'Live registered QA check failed; inspect the local API without printing authorization headers.'; exit 1 }
$baseUrl = 'http://localhost:5099'
$suffix = [Guid]::NewGuid().ToString('N').Substring(0, 12)
$email = "qa-audit-$suffix@example.test"
$password = "Qa-$([Guid]::NewGuid().ToString('N'))!"
$registerBody = @{ name = 'Disposable QA Audit User'; email = $email; password = $password } | ConvertTo-Json
$registration = Invoke-RestMethod -Uri "$baseUrl/api/auth/register" -Method Post -ContentType 'application/json' -Body $registerBody
$login = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -ContentType 'application/json' -Body (@{ email = $email; password = $password } | ConvertTo-Json)
$headers = @{ Authorization = "Bearer $($login.accessToken)" }
$me = Invoke-RestMethod -Uri "$baseUrl/api/auth/me" -Headers $headers
$saved = Invoke-RestMethod -Uri "$baseUrl/api/recommendations" -Method Post -ContentType 'application/json' -Body (@{ district = 'Anuradhapura District'; soilType = $null } | ConvertTo-Json) -Headers $headers
$history = Invoke-RestMethod -Uri "$baseUrl/api/recommendations/history?page=1&pageSize=10" -Headers $headers
$detail = Invoke-RestMethod -Uri "$baseUrl/api/recommendations/$($saved.recommendationId)" -Headers $headers

function Get-ForbiddenStatus($uri, $headers) {
    try { Invoke-WebRequest -Uri $uri -Headers $headers -UseBasicParsing | Out-Null; return 200 }
    catch { return [int]$_.Exception.Response.StatusCode }
}

$summary = [ordered]@{
    checkedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
    accountType = 'Disposable local QA account'
    registeredRole = $registration.role
    authenticatedRole = $me.role
    savedCropCount = @($saved.crops).Count
    historyContainsSavedRecommendation = @($history.items | Where-Object recommendationId -eq $saved.recommendationId).Count -eq 1
    detailMatchesSavedRecommendation = $detail.recommendationId -eq $saved.recommendationId
    adminStatus = Get-ForbiddenStatus "$baseUrl/api/admin/users" $headers
    officerStatus = Get-ForbiddenStatus "$baseUrl/api/validations/pending" $headers
    accountEmail = $email
    recommendationId = $saved.recommendationId
}
$summary | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'evidence/live-registered-summary.json')
$summary | ConvertTo-Json -Depth 3
