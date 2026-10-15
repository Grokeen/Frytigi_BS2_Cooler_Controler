[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$packagePath = Join-Path $workspace 'artifacts\Bs2Cooler-win-x64.zip'
$executablePath = Join-Path $workspace 'artifacts\Bs2Cooler-win-x64\Bs2Cooler.exe'
$rootExecutable = Join-Path $workspace 'BS Pro Controller.exe'
if (-not (Test-Path -LiteralPath $rootExecutable -PathType Leaf) -or (Get-FileHash -LiteralPath $rootExecutable).Hash -ne (Get-FileHash -LiteralPath $executablePath).Hash) {
    throw 'Project-root BS Pro Controller.exe is missing or differs from the published executable.'
}
$archive = [IO.Compression.ZipFile]::OpenRead($packagePath)
try {
    $required = @('Bs2Cooler.exe','User-Guide.ko.txt','SHA256SUMS.txt','THIRD_PARTY_NOTICES.txt','Install-Desktop.ps1','assets/Bs2Cooler.ico','assets/Bs2Cooler.png','assets/Bs2Cooler.svg','docs/DESKTOP-STARTUP.ko.md','docs/AUTOMATIC-COOLING.ko.md','docs/RGB-LIGHTING.ko.md','third_party/PawnIO.Modules/0.2.11/COPYING','third_party/PawnIO.Modules/0.2.11/IntelMSR.p','third_party/PawnIO.Modules/0.2.11/IntelMSR.bin')
    $required += 'docs/DARK-THEME.ko.md'
    $required += 'BS Pro Controller.exe'
    $entries = @{}
    foreach ($entry in $archive.Entries) {
        $name = $entry.FullName.Replace('\','/')
        if ($name.StartsWith('/') -or $name -match '(^|/)\.\.(/|$)|:') { throw "Unsafe package path: $name" }
        if ($entries.ContainsKey($name)) { throw "Duplicate package entry: $name" }
        $entries[$name] = $entry
    }
    foreach ($name in $required) { if (-not $entries.ContainsKey($name)) { throw "Package is missing: $name" } }
    $stream = $entries['Bs2Cooler.exe'].Open()
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $packedHash = [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-','') }
    finally { $stream.Dispose(); $sha.Dispose() }
    $stream = $entries['BS Pro Controller.exe'].Open()
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $launcherHash = [BitConverter]::ToString($sha.ComputeHash($stream)).Replace('-','') }
    finally { $stream.Dispose(); $sha.Dispose() }
    if ($launcherHash -ne $packedHash) { throw 'ZIP launcher differs from its installation executable.' }
    $reader = [IO.StreamReader]::new($entries['SHA256SUMS.txt'].Open())
    try { $expectedHash = ($reader.ReadToEnd().Trim() -split '\s+')[0] } finally { $reader.Dispose() }
    if ($packedHash -ne $expectedHash -or $packedHash -ne (Get-FileHash -LiteralPath $executablePath).Hash) { throw 'ZIP executable does not match its SHA-256 manifest and published executable.' }
} finally { $archive.Dispose() }
Write-Output "PASS: project-root launcher, required files, unique safe paths, archived EXE and SHA-256 match. EXE SHA256: $packedHash"
