param([string]$BuildDirectory = 'build', [int]$TimeoutSeconds = 90)
. "$PSScriptRoot/common.ps1"
$exe = Resolve-RepoPath ($BuildDirectory + '/GammaSetup.exe')
if (!(Test-Path -LiteralPath $exe)) { throw 'Build the app first: .\scripts\build.ps1' }
$run = Resolve-RepoPath ('test-results/' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $run -Force | Out-Null
# Use an explicit quoted argument for repository paths containing spaces.
$process = Start-Process -FilePath $exe -ArgumentList ('--self-test "' + $run + '"') -WindowStyle Hidden -PassThru
if (!$process.WaitForExit($TimeoutSeconds * 1000)) {
    Stop-Process -Id $process.Id -Force
    throw "Self-tests exceeded $TimeoutSeconds seconds."
}
if ($process.ExitCode -ne 0) {
    $failure = Join-Path $run 'test-failure.txt'
    if (Test-Path -LiteralPath $failure) { Get-Content -LiteralPath $failure | Write-Host }
    throw "Self-tests failed with exit code $($process.ExitCode)."
}
$report = Join-Path $run 'test-results.txt'
if (!(Test-Path -LiteralPath $report)) { throw 'Test process produced no report.' }
Get-Content -LiteralPath $report | Write-Host
Copy-Item -LiteralPath $report -Destination (Resolve-RepoPath 'test-results/latest.txt') -Force
Write-Host 'No game was downloaded and no Microsoft runtimes were installed by these tests.'
