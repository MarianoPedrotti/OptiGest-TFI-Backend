param([Parameter(Mandatory=$true)][string]$FrontendRoot)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$api = $null
$front = $null
$localdb = $null
$localDbStarted = $false
$transcriptStarted = $false

function Stop-OwnedTestingProcess([System.Diagnostics.Process]$Process, [string]$ExpectedName, [string]$CommandMatch) {
    if ($null -eq $Process) { return }

    try {
        $candidate = Get-CimInstance Win32_Process -Filter "ProcessId=$($Process.Id)" -ErrorAction SilentlyContinue
        if ($null -eq $candidate) { return }

        $started = $Process.StartTime.ToUniversalTime()
        $matchesProcess = $candidate.Name -eq $ExpectedName -and
            $null -ne $candidate.CommandLine -and
            $candidate.CommandLine.Contains($CommandMatch) -and
            [Math]::Abs(($candidate.CreationDate.ToUniversalTime() - $started).TotalSeconds) -le 60

        if (!$matchesProcess) {
            Write-Warning "Refusing to stop PID $($Process.Id): it no longer matches this testing run."
            return
        }

        & taskkill /PID $Process.Id /T /F
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Could not stop owned testing PID $($Process.Id)."
        }
    }
    catch {
        Write-Warning "Could not inspect owned testing PID $($Process.Id): $($_.Exception.Message)"
    }
}

function Stop-OwnedTestingLocalDb {
    if (!$localDbStarted -or $null -eq $localdb) { return }

    try {
        & $localdb stop OptiGest_Testing_Phase05
        if ($LASTEXITCODE -ne 0) {
            Write-Warning 'Dedicated LocalDB could not be stopped during launcher cleanup.'
        }
    }
    catch {
        Write-Warning "Could not stop dedicated LocalDB during launcher cleanup: $($_.Exception.Message)"
    }
}

Start-Transcript -Path (Join-Path $root ('.testing-data/launcher-' + [Guid]::NewGuid().ToString('N') + '.log'))
$transcriptStarted = $true

try {
    $dotnet = (Get-Command dotnet).Source
    $node = (Get-Command node).Source
    $localdb = (Get-Command SqlLocalDB.exe).Source
    if (!(Test-Path -LiteralPath "$FrontendRoot/node_modules/vite/bin/vite.js")) { throw 'Existing Vite installation required.' }
    $listeners = [System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners()
    if ($listeners | Where-Object { $_.Port -in @(51850, 51851) }) { throw 'Testing ports are occupied.' }

    # This script runs in a separate PowerShell process. Only OS/tool discovery variables survive.
    $allowed = @{}
    foreach ($key in @('SystemRoot','WINDIR','PATH','TEMP','TMP','USERPROFILE','LOCALAPPDATA','APPDATA','ProgramFiles','ProgramFiles(x86)','ProgramW6432','COMSPEC','PATHEXT','SystemDrive','OS')) {
        $value = [Environment]::GetEnvironmentVariable($key)
        if ($null -ne $value) { $allowed[$key] = $value }
    }
    foreach ($key in @([Environment]::GetEnvironmentVariables('Process').Keys)) { [Environment]::SetEnvironmentVariable($key, $null, 'Process') }
    foreach ($key in $allowed.Keys) { [Environment]::SetEnvironmentVariable($key, $allowed[$key], 'Process') }
    $env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    $env:DOTNET_ENVIRONMENT = 'Testing'
    $env:ASPNETCORE_ENVIRONMENT = 'Testing'
    $env:OPTIGEST_RUN_SQL_TESTS = '1'
    $env:OPTIGEST_PREPARE_E2E = '1'
    $run = Join-Path $root ('.testing-data/phase5a-' + [Guid]::NewGuid().ToString('N'))
    $env:OPTIGEST_E2E_ROOT = $run

    $localDbInfo = & $localdb info OptiGest_Testing_Phase05
    if ($LASTEXITCODE -ne 0) { throw 'Dedicated LocalDB instance missing.' }
    if (($localDbInfo -join [Environment]::NewLine) -notmatch '(?im)^\s*State:\s+Stopped\s*$') {
        throw 'Dedicated LocalDB must be stopped before an isolated run.'
    }

    & $localdb start OptiGest_Testing_Phase05
    if ($LASTEXITCODE -ne 0) { throw 'Dedicated LocalDB could not start.' }
    $localDbStarted = $true

    & $dotnet test "$root/tests/OptiGest.Tests/OptiGest.Tests.csproj" --no-restore --filter 'FullyQualifiedName~IntegratedEnvironment.Prepare_new_owned_database' --logger 'console;verbosity=minimal'
    if ($LASTEXITCODE -ne 0) { throw 'Preparation failed; applications were not started.' }
    $env:OPTIGEST_PREPARE_E2E = $null
    $env:ASPNETCORE_HOSTINGSTARTUPASSEMBLIES = 'OptiGest.Tests'
    $env:OPTIGEST_E2E_HOST = '1'
    $testBin = Join-Path $root 'tests/OptiGest.Tests/bin/Debug/net8.0'
    $api = Start-Process -FilePath $dotnet -ArgumentList @('exec','--depsfile',"`"$testBin/OptiGest.Tests.deps.json`"",'--runtimeconfig',"`"$root/bin/Debug/net8.0/SistemaDeVentas.runtimeconfig.json`"","`"$testBin/SistemaDeVentas.dll`"",'--contentRoot',"`"$run`"",'--urls','http://127.0.0.1:51850') -WorkingDirectory $run -RedirectStandardOutput "$run/api.stdout.log" -RedirectStandardError "$run/api.stderr.log" -PassThru
    Start-Sleep -Seconds 3
    if ($api.HasExited) { throw "API startup rejected; inspect $run/api.stderr.log. Frontend not started." }

    $env:ASPNETCORE_HOSTINGSTARTUPASSEMBLIES = $null
    $env:OPTIGEST_E2E_HOST = $null
    $env:VITE_API_URL = 'http://127.0.0.1:51850/api'
    $front = Start-Process -FilePath $node -ArgumentList @("`"$FrontendRoot/node_modules/vite/bin/vite.js`"",'--config','vite.integrated.config.ts','--mode','testing','--host','127.0.0.1','--port','51851','--strictPort') -WorkingDirectory $FrontendRoot -RedirectStandardOutput "$run/frontend.stdout.log" -RedirectStandardError "$run/frontend.stderr.log" -PassThru
    Start-Sleep -Seconds 3
    if ($front.HasExited) { throw "Frontend startup rejected; inspect $run/frontend.stderr.log." }

    @{ ApiPid = $api.Id; FrontendPid = $front.Id; RunDirectory = $run; StartedUtc = [DateTime]::UtcNow.ToString('o') } | ConvertTo-Json | Set-Content -LiteralPath "$run/processes.json"
    @{ RunDirectory = $run; ApiPid = $api.Id; FrontendPid = $front.Id } | ConvertTo-Json
}
catch {
    Stop-OwnedTestingProcess -Process $front -ExpectedName 'node.exe' -CommandMatch 'vite.integrated.config.ts'
    Stop-OwnedTestingProcess -Process $api -ExpectedName 'dotnet.exe' -CommandMatch 'OptiGest.Tests.deps.json'
    Stop-OwnedTestingLocalDb
    throw
}
finally {
    if ($transcriptStarted) {
        try { Stop-Transcript | Out-Null }
        catch { Write-Warning "Could not stop launcher transcript: $($_.Exception.Message)" }
    }
}
