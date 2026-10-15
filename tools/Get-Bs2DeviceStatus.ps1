<#
.SYNOPSIS
Read-only connection check for Flydigi BS2 Pro on Windows.
.DESCRIPTION
Reads PnP and service state. Does not open the cooler for control, send HID
commands, start services, install drivers, or change device settings.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$presentDevices = @(Get-PnpDevice -PresentOnly -ErrorAction Stop)
$flydigi = @($presentDevices | Where-Object {
    $_.InstanceId -match '37D7' -or $_.FriendlyName -match 'Flydigi|BS2'
})
$bs2Pro = @($flydigi | Where-Object { $_.InstanceId -match 'PID[&_](?:0*)1002' })
$usbErrors = @($presentDevices | Where-Object {
    $_.Class -eq 'USB' -and $_.Status -ne 'OK'
})
$services = @(Get-CimInstance Win32_Service -ErrorAction Stop | Where-Object {
    $_.Name -match 'Flydigi|SpaceStation|FDGMonitorService'
} | Select-Object Name, State, ProcessId)
$processes = @(Get-Process -ErrorAction Stop | Where-Object {
    $_.ProcessName -match 'Flydigi|SpaceStation|GameController'
} | Select-Object Id, ProcessName)

[pscustomobject]@{
    CapturedAt = [DateTimeOffset]::Now.ToString('o')
    ExpectedBs2ProId = 'VID_37D7 / PID_1002 (mapping in installed SDK)'
    FlydigiPresentCount = $flydigi.Count
    Bs2ProPresentCount = $bs2Pro.Count
    FlydigiDevices = @($flydigi | Select-Object Status, Class, FriendlyName, InstanceId)
    UsbErrors = @($usbErrors | Select-Object Status, Class, FriendlyName, InstanceId)
    Services = $services
    Processes = $processes
    Note = 'Counts are PnP nodes, not physical devices. An unknown USB device cannot be identified as the cooler from this scan alone.'
}
