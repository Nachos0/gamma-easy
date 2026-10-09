param([string]$Version = '0.1.1')
. "$PSScriptRoot/common.ps1"
if ($Version -notmatch '^\d+\.\d+\.\d+(?:-[A-Za-z0-9.-]+)?$') { throw 'Invalid release version.' }
$exe = Resolve-RepoPath 'build/GammaSetup.exe'
if (!(Test-Path -LiteralPath $exe)) { throw 'Build and test the app before packaging.' }
$report = Resolve-RepoPath 'test-results/latest.txt'
if (!(Test-Path -LiteralPath $report)) { throw 'Run the self-tests before packaging.' }
$dist = Resolve-RepoPath 'dist'
$stage = Resolve-RepoPath ('.cache/packages/' + [Guid]::NewGuid().ToString('N'))
$installer = Join-Path $stage 'installer'
$source = Join-Path $stage 'source'
New-Item -ItemType Directory -Path $dist, $installer, $source -Force | Out-Null
Copy-Item -LiteralPath $exe -Destination $installer
Copy-Item -LiteralPath (Resolve-RepoPath 'build/GammaSetup.exe.config') -Destination $installer
Copy-Item -LiteralPath (Resolve-RepoPath 'LICENSE') -Destination $installer
Copy-Item -LiteralPath (Resolve-RepoPath 'docs/INSTALLATION.md') -Destination (Join-Path $installer 'INSTALLATION.md')
Copy-Item -LiteralPath $report -Destination (Join-Path $installer 'TEST-RESULTS.txt')
Copy-Item -LiteralPath (Resolve-RepoPath 'third-party') -Destination $installer -Recurse
# Explicit allowlist keeps caches, credentials, local Git metadata and user data out.
foreach ($name in @('src','tests','scripts','docs','third-party','.github')) { Copy-Item -LiteralPath (Resolve-RepoPath $name) -Destination $source -Recurse }
foreach ($name in @('README.md','LICENSE','CHANGELOG.md','CONTRIBUTING.md','SECURITY.md','dependencies.lock.json','.gitignore','.gitattributes','.editorconfig')) { Copy-Item -LiteralPath (Resolve-RepoPath $name) -Destination $source }
$lock = Get-DependencyLock
$dependencies = Join-Path $source 'upstream-sources'
New-Item -ItemType Directory -Path $dependencies -Force | Out-Null
Copy-Item -LiteralPath (Get-Dependency $lock.backend) -Destination $dependencies
foreach ($entry in $lock.sources) { Copy-Item -LiteralPath (Get-Dependency $entry) -Destination $dependencies }
$binaryZip = Join-Path $dist "GammaEasy-$Version-windows-x64.zip"
$sourceZip = Join-Path $dist "GammaEasy-$Version-source.zip"
Compress-Archive -Path (Join-Path $installer '*') -DestinationPath $binaryZip -Force
# .github/.gitignore may be skipped as hidden paths by Compress-Archive; use ZipFile.
Add-Type -AssemblyName System.IO.Compression.FileSystem
if (Test-Path -LiteralPath $sourceZip) { Remove-Item -LiteralPath $sourceZip -Force }
[IO.Compression.ZipFile]::CreateFromDirectory($source, $sourceZip)
$checksums = @($binaryZip, $sourceZip) | ForEach-Object { (Get-FileHash -LiteralPath $_ -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + [IO.Path]::GetFileName($_) }
$checksums | Set-Content -LiteralPath (Join-Path $dist 'SHA256SUMS.txt') -Encoding Ascii
Write-Host 'Packaged installer, corresponding source and checksums:'
Get-ChildItem -LiteralPath $dist -File | Select-Object Name, Length
