[CmdletBinding()]
param([ValidateSet('Preserve','On','Off')][string]$StartupCooling = 'Preserve')
$ErrorActionPreference = 'Stop'
$principal = [Security.Principal.WindowsPrincipal]::new([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) { throw 'Run this setup in an administrator PowerShell.' }
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$source = Join-Path $workspace 'artifacts\Bs2Cooler-win-x64'
if (-not (Test-Path -LiteralPath $source)) { $source = $PSScriptRoot }
$sourceExe = Join-Path $source 'Bs2Cooler.exe'
$hashFile = Join-Path $source 'SHA256SUMS.txt'
$expectedHash = ((Get-Content -LiteralPath $hashFile -Raw).Trim() -split '\s+')[0]
if ((Get-FileHash -LiteralPath $sourceExe).Hash -ne $expectedHash) { throw 'Published executable hash mismatch.' }
$programFiles = [Environment]::GetFolderPath('ProgramFiles')
$destination = [IO.Path]::GetFullPath((Join-Path $programFiles 'BS2 Cooler'))
if (-not $destination.StartsWith(($programFiles.TrimEnd('\') + '\'),[StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid install destination.' }
function Invoke-AppSetup([string[]]$arguments) {
    $helper = Start-Process -FilePath $sourceExe -ArgumentList $arguments -WindowStyle Hidden -PassThru
    if (-not $helper.WaitForExit(20000)) { throw 'App setup command timed out.' }
    $helper.Refresh()
    if ($helper.ExitCode -ne 0) { throw "App setup failed ($($helper.ExitCode)); see LocalAppData\Bs2Cooler\desktop.log." }
}
Invoke-AppSetup @('--quit','--background')
foreach ($app in @(Get-Process Bs2Cooler -ErrorAction SilentlyContinue)) {
    if ($app.HasExited) { continue }
    $appPath = $app.Path
    if (-not $appPath) { continue }
    if ($appPath -eq $sourceExe -or $appPath -eq (Join-Path $destination 'Bs2Cooler.exe') -or $appPath.StartsWith($workspace + '\',[StringComparison]::OrdinalIgnoreCase)) {
        if ($app.WaitForExit(5000)) { continue }
        [void]$app.CloseMainWindow()
        if (-not $app.WaitForExit(15000)) { throw 'Exit the existing cooler app from its tray menu before updating.' }
    }
}
[void][IO.Directory]::CreateDirectory($destination)
foreach ($file in Get-ChildItem -LiteralPath $source -Force) { Copy-Item -LiteralPath $file.FullName -Destination $destination -Recurse -Force }
$installedExe = Join-Path $destination 'Bs2Cooler.exe'
if ((Get-FileHash -LiteralPath $installedExe).Hash -ne $expectedHash) { throw 'Installed executable hash mismatch.' }
# Registration always points at the protected Program Files copy, never a workspace or temp executable.
Invoke-AppSetup @('--enable-startup','--background')
if ($StartupCooling -ne 'Preserve') { Invoke-AppSetup @(('--startup-cooling=' + $StartupCooling.ToLowerInvariant()),'--background') }
$shell = New-Object -ComObject WScript.Shell
foreach ($folder in @([Environment]::GetFolderPath('DesktopDirectory'),[Environment]::GetFolderPath('Programs'))) {
    $shortcutPath = Join-Path $folder 'BS2 Cooler.lnk'
    if (Test-Path -LiteralPath $shortcutPath) {
        $existing = $shell.CreateShortcut($shortcutPath)
        if ($existing.TargetPath -ne $installedExe) { $shortcutPath = Join-Path $folder 'BS2 Cooler Control.lnk' }
    }
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $installedExe; $shortcut.WorkingDirectory = $destination; $shortcut.IconLocation = $installedExe + ',0'
    $shortcut.Description = 'BS2 Cooler - fan, temperature and RGB control'; $shortcut.Save()
}
$taskName = 'BS2 Cooler-' + [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
$task = Get-ScheduledTask -TaskName $taskName -ErrorAction Stop
Start-ScheduledTask -TaskName $taskName
[PSCustomObject]@{InstalledExe=$installedExe;SHA256=$expectedHash;TaskName=$taskName;RunLevel=$task.Principal.RunLevel;LogonType=$task.Principal.LogonType;Arguments=$task.Actions.Arguments;StartupCooling=$StartupCooling} | ConvertTo-Json
