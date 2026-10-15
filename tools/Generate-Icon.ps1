[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore,WindowsBase
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$assetDirectory = Join-Path $root 'assets'
[xml]$svg = Get-Content -LiteralPath (Join-Path $assetDirectory 'Bs2Cooler.svg') -Raw
$images = @()
$sizes = @(16,20,24,32,40,48,64,128,256)
foreach ($size in $sizes) {
    $visual = New-Object Windows.Media.DrawingVisual
    $drawing = $visual.RenderOpen()
    $drawing.PushTransform((New-Object Windows.Media.ScaleTransform(($size / 256.0),($size / 256.0))))
    foreach ($shape in $svg.DocumentElement.ChildNodes) {
        if ($shape -isnot [Xml.XmlElement]) { continue }
        $brush = if ($shape.fill -and $shape.fill -ne 'none') { [Windows.Media.BrushConverter]::new().ConvertFromString($shape.fill) } else { $null }
        $pen = if ($shape.stroke) { [Windows.Media.Pen]::new([Windows.Media.BrushConverter]::new().ConvertFromString($shape.stroke), [double]$shape.'stroke-width') } else { $null }
        $rotated = $shape.transform -match '^rotate\((\d+) (\d+) (\d+)\)$'
        if ($rotated) { $drawing.PushTransform([Windows.Media.RotateTransform]::new([double]$Matches[1],[double]$Matches[2],[double]$Matches[3])) }
        switch ($shape.LocalName) {
            'rect' { $drawing.DrawRoundedRectangle($brush,$pen,[Windows.Rect]::new([double]$shape.x,[double]$shape.y,[double]$shape.width,[double]$shape.height),[double]$shape.rx,[double]$shape.rx) }
            'circle' { $drawing.DrawEllipse($brush,$pen,[Windows.Point]::new([double]$shape.cx,[double]$shape.cy),[double]$shape.r,[double]$shape.r) }
            'path' { $drawing.DrawGeometry($brush,$pen,[Windows.Media.Geometry]::Parse($shape.d)) }
        }
        if ($rotated) { $drawing.Pop() }
    }
    $drawing.Pop(); $drawing.Close()
    $bitmap = [Windows.Media.Imaging.RenderTargetBitmap]::new($size,$size,96,96,[Windows.Media.PixelFormats]::Pbgra32)
    $bitmap.Render($visual)
    $encoder = [Windows.Media.Imaging.PngBitmapEncoder]::new()
    $encoder.Frames.Add([Windows.Media.Imaging.BitmapFrame]::Create($bitmap))
    $memory = [IO.MemoryStream]::new()
    $encoder.Save($memory)
    $images += ,$memory.ToArray()
    if ($size -eq 256) { [IO.File]::WriteAllBytes((Join-Path $assetDirectory 'Bs2Cooler.png'), $memory.ToArray()) }
    $memory.Dispose()
}
$stream = [IO.File]::Create((Join-Path $assetDirectory 'Bs2Cooler.ico'))
$writer = [IO.BinaryWriter]::new($stream)
try {
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]$sizes.Count)
    $offset = 6 + 16 * $sizes.Count
    for ($i = 0; $i -lt $sizes.Count; $i++) {
        $dimension = if ($sizes[$i] -eq 256) { 0 } else { $sizes[$i] }
        $writer.Write([byte]$dimension); $writer.Write([byte]$dimension); $writer.Write([byte]0); $writer.Write([byte]0)
        $writer.Write([uint16]1); $writer.Write([uint16]32); $writer.Write([uint32]$images[$i].Length); $writer.Write([uint32]$offset)
        $offset += $images[$i].Length
    }
    foreach ($bytes in $images) { $writer.Write([byte[]]$bytes) }
} finally { $writer.Dispose(); $stream.Dispose() }
Get-Item -LiteralPath (Join-Path $assetDirectory 'Bs2Cooler.ico'),(Join-Path $assetDirectory 'Bs2Cooler.png') | Select-Object Name,Length
