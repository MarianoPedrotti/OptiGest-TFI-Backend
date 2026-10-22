param([switch]$PendingRules)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$instance = 'OptiGest_Testing_Phase05'

# Never create/adopt an unknown instance automatically and never read User Secrets.
& SqlLocalDB.exe info $instance
if ($LASTEXITCODE -ne 0) { throw "Provision the exclusive $instance instance explicitly first." }
& SqlLocalDB.exe start $instance
if ($LASTEXITCODE -ne 0) { throw 'Dedicated LocalDB could not start. No SQL tests executed.' }

$previous = $env:OPTIGEST_RUN_SQL_TESTS
try {
    $env:OPTIGEST_RUN_SQL_TESTS = '1'
    $filter = if ($PendingRules) { 'Category=PendingRule' } else { 'Category!=PendingRule' }
    $report = if ($PendingRules) { 'pending.trx' } else { 'characterization.trx' }
    & dotnet test "$root/tests/OptiGest.Tests/OptiGest.Tests.csproj" --filter $filter --logger 'console;verbosity=minimal' --logger "trx;LogFileName=$report" --results-directory "$root/.testing-data/results"
    $result = $LASTEXITCODE
} finally {
    $env:OPTIGEST_RUN_SQL_TESTS = $previous
}
exit $result
