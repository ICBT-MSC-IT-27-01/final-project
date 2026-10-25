$ErrorActionPreference = 'Stop'
$baseUrl = 'http://localhost:5099'
$suffix = [Guid]::NewGuid().ToString('N').Substring(0, 12)

function New-LocalQaUser([string]$roleName) {
    $email = "qa-$($roleName.ToLower().Replace(' ', '-'))-$suffix@example.test"
    $password = "Qa-$([Guid]::NewGuid().ToString('N'))!"
    $registerBody = @{ name = "Disposable $roleName QA"; email = $email; password = $password } | ConvertTo-Json
    Invoke-RestMethod -Uri "$baseUrl/api/auth/register" -Method Post -ContentType 'application/json' -Body $registerBody | Out-Null

    $quotedEmail = $email.Replace("'", "''")
    $sql = "UPDATE dbo.[User] SET RoleId = (SELECT Id FROM dbo.UserRole WHERE Name = '$roleName') WHERE Email = '$quotedEmail' AND RoleId = 1; IF @@ROWCOUNT <> 1 THROW 51000, 'Disposable QA role assignment failed.', 1;"
    & sqlcmd -S '(localdb)\MSSQLLocalDB' -d AnuradhapuraAI -E -b -Q $sql -h -1 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Local disposable role assignment failed.' }

    $login = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -ContentType 'application/json' -Body (@{ email = $email; password = $password } | ConvertTo-Json)
    return @{ headers = @{ Authorization = "Bearer $($login.accessToken)" }; userId = $login.userId; role = $login.role }
}

function Get-Status([scriptblock]$request) {
    try { $null = & $request; return 200 }
    catch { if ($_.Exception.Response) { return [int]$_.Exception.Response.StatusCode }; throw }
}

$user = New-LocalQaUser 'Registered User'
$userHeaders = $user.headers
$saved = Invoke-RestMethod -Uri "$baseUrl/api/recommendations" -Method Post -ContentType 'application/json' -Body (@{ district = 'Anuradhapura District'; soilType = $null } | ConvertTo-Json) -Headers $userHeaders

$officer = New-LocalQaUser 'Agricultural Officer'
$pending = Invoke-RestMethod -Uri "$baseUrl/api/validations/pending" -Headers $officer.headers
$review = Invoke-RestMethod -Uri "$baseUrl/api/validations/recommendations/$($saved.recommendationId)" -Headers $officer.headers
$validation = Invoke-RestMethod -Uri "$baseUrl/api/validations/recommendations/$($saved.recommendationId)" -Method Post -ContentType 'application/json' -Body (@{ status = 'Needs Review'; comment = 'Disposable local QA workflow check.' } | ConvertTo-Json) -Headers $officer.headers
$officerAdminStatus = Get-Status { Invoke-RestMethod -Uri "$baseUrl/api/admin/crops" -Headers $officer.headers }

$admin = New-LocalQaUser 'Administrator'
$adminCheck = Invoke-RestMethod -Uri "$baseUrl/api/auth/admin-check" -Headers $admin.headers
$crops = Invoke-RestMethod -Uri "$baseUrl/api/admin/crops?page=1&pageSize=20" -Headers $admin.headers
$unsupportedCropStatus = Get-Status { Invoke-RestMethod -Uri "$baseUrl/api/admin/crops" -Method Post -ContentType 'application/json' -Body (@{ name = 'Rice' } | ConvertTo-Json) -Headers $admin.headers }
$registeredAdminStatus = Get-Status { Invoke-RestMethod -Uri "$baseUrl/api/admin/users" -Headers $userHeaders }

$forecastGenerationStatus = 0
$forecastGeneration = $null
try {
    $forecastGeneration = Invoke-RestMethod -Uri "$baseUrl/api/admin/forecasts/operational" -Method Post -Headers $admin.headers
    $forecastGenerationStatus = 200
} catch {
    if ($_.Exception.Response) { $forecastGenerationStatus = [int]$_.Exception.Response.StatusCode }
    else { throw }
}

$latest = Invoke-RestMethod -Uri "$baseUrl/api/forecasts/latest"
$summary = [ordered]@{
    checkedAtUtc = (Get-Date).ToUniversalTime().ToString('o')
    accountTypes = @('Disposable local QA accounts; credentials held in process memory only')
    registeredUserCanSaveRecommendation = $saved.crops.Count -eq 6
    registeredUserAdminStatus = $registeredAdminStatus
    officerRole = $officer.role
    officerPendingListReturned = $null -ne $pending
    officerReviewCropCount = @($review.crops).Count
    officerValidationStatus = $validation.status
    officerAdminStatus = $officerAdminStatus
    administratorRole = $adminCheck.role
    administratorCropCount = @($crops.items).Count
    unsupportedCropCreateStatus = $unsupportedCropStatus
    operationalForecastGenerationStatus = $forecastGenerationStatus
    generatedForecastRunId = if ($forecastGenerationStatus -eq 200) { $forecastGeneration.forecastRunId } else { $null }
    generatedForecastDayCount = if ($forecastGenerationStatus -eq 200) { @($forecastGeneration.forecasts).Count } else { 0 }
    generatedObservationStartDate = if ($forecastGenerationStatus -eq 200) { $forecastGeneration.observationWindowStart } else { $null }
    generatedObservationEndDate = if ($forecastGenerationStatus -eq 200) { $forecastGeneration.observationWindowEnd } else { $null }
    latestForecastDayCount = @($latest.dailyForecasts).Count
    latestForecastFreshnessStatus = $latest.freshnessStatus
}
$outputPath = Join-Path $PSScriptRoot 'evidence/live-staff-and-forecast-summary.json'
$summary | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $outputPath
$summary | ConvertTo-Json -Depth 4
