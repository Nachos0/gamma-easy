param([string]$OutputDirectory = 'build')
. "$PSScriptRoot/common.ps1"

if ($env:OS -ne 'Windows_NT') { throw 'Build this Windows application on Windows.' }
$lock = Get-DependencyLock
$backend = Get-Dependency $lock.backend
$embeddedHash = Select-String -LiteralPath "$script:RepoRoot/src/Program.cs" -Pattern 'BackendHash = "([a-f0-9]{64})"'
if (!$embeddedHash -or $embeddedHash.Matches[0].Groups[1].Value -ne $lock.backend.sha256) { throw 'The source backend hash and dependency lock disagree.' }
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw '.NET Framework 4.8 and its C# compiler are required.' }
$out = Resolve-RepoPath $OutputDirectory
New-Item -ItemType Directory -Path $out -Force | Out-Null
$sources = @(Get-ChildItem -LiteralPath "$script:RepoRoot/src", "$script:RepoRoot/tests" -Filter '*.cs' -File | Sort-Object FullName | ForEach-Object { $_.FullName })
$arguments = @('/nologo', '/target:winexe', '/platform:x64', '/optimize+', '/debug-', "/out:$out/GammaSetup.exe", "/win32manifest:$script:RepoRoot/src/app.manifest", '/r:System.Windows.Forms.dll', '/r:System.Drawing.dll', '/r:System.Management.dll', '/r:System.Net.Http.dll', '/r:System.IO.Compression.dll', '/r:System.IO.Compression.FileSystem.dll', '/r:Microsoft.CSharp.dll', "/resource:$backend,backend.zip", "/resource:$script:RepoRoot/LICENSE,LICENSE") + $sources
& $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw "Compiler exited with $LASTEXITCODE." }
Copy-Item -LiteralPath "$script:RepoRoot/src/GammaSetup.exe.config" -Destination $out -Force
Write-Host "Built $out/GammaSetup.exe"
Get-FileHash -LiteralPath "$out/GammaSetup.exe" -Algorithm SHA256
