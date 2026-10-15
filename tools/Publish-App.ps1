[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$project = Join-Path $workspace 'src\Bs2Cooler.App\Bs2Cooler.App.csproj'
$output = Join-Path $workspace 'artifacts\Bs2Cooler-win-x64'
& dotnet publish $project -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:DebugType=None -o $output
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
Copy-Item -LiteralPath (Join-Path $workspace 'docs\User-Guide.ko.txt') -Destination (Join-Path $output 'User-Guide.ko.txt') -Force
Copy-Item -LiteralPath (Join-Path $workspace 'THIRD_PARTY_NOTICES.txt') -Destination (Join-Path $output 'THIRD_PARTY_NOTICES.txt') -Force
Copy-Item -LiteralPath (Join-Path $workspace 'tools\Install-Desktop.ps1') -Destination (Join-Path $output 'Install-Desktop.ps1') -Force
$assets = Join-Path $output 'assets'
[void][IO.Directory]::CreateDirectory($assets)
Copy-Item -LiteralPath (Join-Path $workspace 'assets\Bs2Cooler.ico'),(Join-Path $workspace 'assets\Bs2Cooler.png'),(Join-Path $workspace 'assets\Bs2Cooler.svg') -Destination $assets -Force
$documentation = Join-Path $output 'docs'
[void][IO.Directory]::CreateDirectory($documentation)
foreach ($name in @('DESKTOP-STARTUP.ko.md','AUTOMATIC-COOLING.ko.md','RGB-LIGHTING.ko.md','DARK-THEME.ko.md')) {
    Copy-Item -LiteralPath (Join-Path $workspace ('docs\' + $name)) -Destination $documentation -Force
}
$moduleDestination = Join-Path $output 'third_party\PawnIO.Modules'
[void][IO.Directory]::CreateDirectory($moduleDestination)
Copy-Item -LiteralPath (Join-Path $workspace 'third_party\PawnIO.Modules\0.2.11') -Destination $moduleDestination -Recurse -Force
$executable = Join-Path $output 'Bs2Cooler.exe'
$hash = Get-FileHash -LiteralPath $executable -Algorithm SHA256
[IO.File]::WriteAllText((Join-Path $output 'SHA256SUMS.txt'), "$($hash.Hash)  Bs2Cooler.exe`r`n", [Text.UTF8Encoding]::new($false))
$archive = Join-Path $workspace 'artifacts\Bs2Cooler-win-x64.zip'
Compress-Archive -LiteralPath $executable,(Join-Path $output 'User-Guide.ko.txt'),(Join-Path $output 'SHA256SUMS.txt'),(Join-Path $output 'THIRD_PARTY_NOTICES.txt'),(Join-Path $output 'third_party'),(Join-Path $output 'Install-Desktop.ps1'),$assets,$documentation -DestinationPath $archive -Force
& (Join-Path $PSScriptRoot 'Test-Package.ps1')
$archiveHash = Get-FileHash -LiteralPath $archive -Algorithm SHA256
[IO.File]::WriteAllText((Join-Path $workspace 'artifacts\RELEASE-SHA256SUMS.txt'), "$($archiveHash.Hash)  Bs2Cooler-win-x64.zip`r`n$($hash.Hash)  Bs2Cooler.exe`r`n", [Text.UTF8Encoding]::new($false))
Get-Item -LiteralPath $executable,(Join-Path $workspace 'artifacts\Bs2Cooler-win-x64.zip') | Select-Object FullName,Length
Write-Output "SHA256: $($hash.Hash)"
