$ErrorActionPreference = 'Stop'
$qaRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
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
    $backendReady = $false
    $frontendReady = $false
    for ($i = 0; $i -lt 90; $i++) {
        if (-not $backendReady) {
            try { $backendReady = (Invoke-WebRequest -UseBasicParsing 'http://127.0.0.1:5055/health' -TimeoutSec 2).StatusCode -eq 200 } catch { }
        }
        if (-not $frontendReady) {
            try { $frontendReady = (Invoke-WebRequest -UseBasicParsing 'http://127.0.0.1:4300/' -TimeoutSec 2).StatusCode -eq 200 } catch { }
        }
        if ($backendReady -and $frontendReady) { break }
        Start-Sleep -Seconds 1
    }
    "backend_ready=$backendReady frontend_ready=$frontendReady"
    if (-not ($backendReady -and $frontendReady)) { throw 'Application startup did not complete.' }
    node (Join-Path $qaRoot 'qa\visible-workflow-audit.mjs')
}
finally {
    Stop-Job $backendJob, $frontendJob -ErrorAction SilentlyContinue
    Remove-Job $backendJob, $frontendJob -Force -ErrorAction SilentlyContinue
}
