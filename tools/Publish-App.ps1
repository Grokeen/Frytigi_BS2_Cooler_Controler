[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$project = Join-Path $workspace 'src\Bs2Cooler.App\Bs2Cooler.App.csproj'
$output = Join-Path $workspace 'artifacts\Bs2Cooler-win-x64'
& dotnet publish $project -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:DebugType=None -o $output
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
Copy-Item -LiteralPath (Join-Path $workspace 'docs\User-Guide.ko.txt') -Destination (Join-Path $output 'User-Guide.ko.txt') -Force
$executable = Join-Path $output 'Bs2Cooler.exe'
$hash = Get-FileHash -LiteralPath $executable -Algorithm SHA256
[IO.File]::WriteAllText((Join-Path $output 'SHA256SUMS.txt'), "$($hash.Hash)  Bs2Cooler.exe`r`n", [Text.UTF8Encoding]::new($false))
Compress-Archive -LiteralPath $executable,(Join-Path $output 'User-Guide.ko.txt'),(Join-Path $output 'SHA256SUMS.txt') -DestinationPath (Join-Path $workspace 'artifacts\Bs2Cooler-win-x64.zip') -Force
Get-Item -LiteralPath $executable,(Join-Path $workspace 'artifacts\Bs2Cooler-win-x64.zip') | Select-Object FullName,Length
Write-Output "SHA256: $($hash.Hash)"
