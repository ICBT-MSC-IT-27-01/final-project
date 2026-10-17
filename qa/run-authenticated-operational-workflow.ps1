$ErrorActionPreference = 'Stop'
$qaRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if ([string]::IsNullOrWhiteSpace($env:QA_ADMIN_EMAIL) -or [string]::IsNullOrWhiteSpace($env:QA_ADMIN_PASSWORD)) {
    throw 'Set QA_ADMIN_EMAIL and QA_ADMIN_PASSWORD only for this local authenticated audit.'
}

$pythonJob = Start-Job -ScriptBlock {
    param($root)
    Set-Location (Join-Path $root 'src\forecasting-service')
    python -m uvicorn forecasting_service.api:app --host 127.0.0.1 --port 8000
} -ArgumentList $qaRoot
$backendJob = Start-Job -ScriptBlock {
    param($root)
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    Set-Location (Join-Path $root 'src\backend\AnuradhapuraAI.Api')
    dotnet 'bin\Debug\net10.0\AnuradhapuraAI.Api.dll' --urls 'http://127.0.0.1:5055'
} -ArgumentList $qaRoot
$frontendJob = Start-Job -ScriptBlock {
    param($root)
    Set-Location (Join-Path $root 'src\frontend')
    npm.cmd start -- --host 127.0.0.1 --port 4300
} -ArgumentList $qaRoot
try {
    foreach ($url in @('http://127.0.0.1:8000/health', 'http://127.0.0.1:5055/health', 'http://127.0.0.1:4300/')) {
        $ready = $false
        for ($index = 0; $index -lt 90; $index++) {
            try { $ready = (Invoke-WebRequest -UseBasicParsing $url -TimeoutSec 2).StatusCode -eq 200 } catch { }
            if ($ready) { break }
            Start-Sleep -Seconds 1
        }
        "ready $url=$ready"
        if (-not $ready) { throw "Application startup did not complete: $url" }
    }
    node (Join-Path $qaRoot 'qa\authenticated-operational-workflow.mjs')
}
finally {
    Stop-Job $pythonJob, $backendJob, $frontendJob -ErrorAction SilentlyContinue
    Remove-Job $pythonJob, $backendJob, $frontendJob -Force -ErrorAction SilentlyContinue
}
