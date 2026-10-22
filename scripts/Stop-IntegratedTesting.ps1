param([Parameter(Mandatory=$true)][string]$RunDirectory, [switch]$StopDedicatedSql)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$run = [IO.Path]::GetFullPath($RunDirectory)
$allowed = [IO.Path]::GetFullPath((Join-Path $root '.testing-data')) + [IO.Path]::DirectorySeparatorChar
if (!$run.StartsWith($allowed, [StringComparison]::OrdinalIgnoreCase) -or
    (Split-Path -Leaf $run) -notmatch '^phase5a-[a-f0-9]{32}$') { throw 'Foreign run directory rejected.' }
$manifest = Get-Content -LiteralPath (Join-Path $run 'processes.json') -Raw | ConvertFrom-Json
if ($manifest.RunDirectory -ne $run) { throw 'Run manifest mismatch.' }
$started = [DateTime]::Parse($manifest.StartedUtc).ToUniversalTime()
$unrelated = @()
foreach ($entry in @(@{ Id=$manifest.ApiPid; Name='dotnet.exe'; Match=$run }, @{ Id=$manifest.FrontendPid; Name='node.exe'; Match='vite.integrated.config.ts' })) {
    $process = Get-CimInstance Win32_Process -Filter "ProcessId=$($entry.Id)"
    if ($null -eq $process) { continue }
    if ($process.Name -ne $entry.Name -or !$process.CommandLine.Contains($entry.Match) -or
        [Math]::Abs(($process.CreationDate.ToUniversalTime() - $started).TotalSeconds) -gt 60) {
        # Windows may already have reused a PID after our process exited. Never kill its new owner.
        $unrelated += $entry.Id
        continue
    }
    & taskkill /PID $entry.Id /T /F
    if ($LASTEXITCODE -ne 0) { throw 'Could not stop a Testing process.' }
}
$listeners = @([System.Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners() | Where-Object { $_.Port -in @(51850, 51851) })
if ($listeners.Count -ne 0) { throw 'Testing ports are still occupied; no unrelated process was stopped.' }
if ($StopDedicatedSql) {
    & SqlLocalDB.exe stop OptiGest_Testing_Phase05
    if ($LASTEXITCODE -ne 0) { throw 'Dedicated LocalDB did not stop.' }
}
$evidence = @{ StoppedUtc=[DateTime]::UtcNow.ToString('o'); ApiPid=$manifest.ApiPid; FrontendPid=$manifest.FrontendPid; UnrelatedReusedPidsNotStopped=$unrelated; ListeningTestingPorts=@(); DedicatedLocalDbStopped=[bool]$StopDedicatedSql; DatabasesDeleted=$false }
$evidence | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $run ('shutdown-' + [Guid]::NewGuid().ToString('N') + '.json'))
$evidence | ConvertTo-Json
