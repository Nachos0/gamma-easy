Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:RepoRoot = Split-Path $PSScriptRoot -Parent

function Resolve-RepoPath([string]$Relative) {
    $path = [IO.Path]::GetFullPath((Join-Path $script:RepoRoot $Relative))
    $prefix = [IO.Path]::GetFullPath($script:RepoRoot).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    if (!$path.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) { throw "Path must stay inside the repository: $Relative" }
    return $path
}

function Get-Dependency([object]$Entry) {
    if ($Entry.file -ne [IO.Path]::GetFileName($Entry.file) -or $Entry.sha256 -notmatch '^[a-f0-9]{64}$') { throw 'Invalid dependency lock entry.' }
    $target = Resolve-RepoPath ('.cache/dependencies/' + $Entry.file)
    New-Item -ItemType Directory -Path (Split-Path $target -Parent) -Force | Out-Null
    if (Test-Path -LiteralPath $target) {
        if ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash.ToLowerInvariant() -eq $Entry.sha256) { return $target }
        # Delete only this checked cache file, never a generated directory tree.
        Remove-Item -LiteralPath $target -Force
    }
    $bundled = Resolve-RepoPath ('upstream-sources/' + $Entry.file)
    if (Test-Path -LiteralPath $bundled) {
        if ((Get-FileHash -LiteralPath $bundled -Algorithm SHA256).Hash.ToLowerInvariant() -ne $Entry.sha256) { throw "Bundled source dependency hash mismatch: $($Entry.file)" }
        return $bundled
    }
    $uri = [Uri]$Entry.url
    if ($uri.Scheme -ne 'https' -or $uri.Host -notin @('github.com', 'codeload.github.com')) { throw 'Unexpected dependency host.' }
    Write-Host "Fetching $($Entry.file)..."
    $temporary = $target + '.partial'
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    Invoke-WebRequest -Uri $uri -OutFile $temporary -UseBasicParsing
    if ((Get-FileHash -LiteralPath $temporary -Algorithm SHA256).Hash.ToLowerInvariant() -ne $Entry.sha256) {
        Remove-Item -LiteralPath $temporary -Force
        throw "SHA256 mismatch: $($Entry.file). No downloaded code was executed."
    }
    Move-Item -LiteralPath $temporary -Destination $target -Force
    return $target
}

function Get-DependencyLock {
    return Get-Content -Raw -LiteralPath (Join-Path $script:RepoRoot 'dependencies.lock.json') | ConvertFrom-Json
}
