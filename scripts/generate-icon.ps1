param(
    [string]$OutputPath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'src\FocusPace\Assets\FocusPace.ico')
)

Add-Type -AssemblyName System.Drawing

$outputDirectory = Split-Path -Parent $OutputPath
[System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
$sizes = @(16, 24, 32, 48, 64, 128, 256)
$images = [System.Collections.Generic.List[byte[]]]::new()
$palette = @(
    [System.Drawing.Color]::FromArgb(255, 102, 141, 216), # Ocean
    [System.Drawing.Color]::FromArgb(255, 136, 117, 222), # Violet
    [System.Drawing.Color]::FromArgb(255, 208, 111, 145), # Rose
    [System.Drawing.Color]::FromArgb(255, 211, 148, 72),  # Amber
    [System.Drawing.Color]::FromArgb(255, 67, 166, 143)   # Mint
)

function Get-GradientColor([double]$fraction) {
    $clamped = [Math]::Max(0.0, [Math]::Min(1.0, $fraction))
    $scaled = $clamped * ($palette.Count - 1)
    $index = [Math]::Min([int][Math]::Floor($scaled), $palette.Count - 2)
    $local = $scaled - $index
    $from = $palette[$index]
    $to = $palette[$index + 1]
    return [System.Drawing.Color]::FromArgb(
        255,
        [int][Math]::Round($from.R + (($to.R - $from.R) * $local)),
        [int][Math]::Round($from.G + (($to.G - $from.G) * $local)),
        [int][Math]::Round($from.B + (($to.B - $from.B) * $local)))
}

foreach ($size in $sizes) {
    $bitmap = [System.Drawing.Bitmap]::new($size, $size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.Clear([System.Drawing.Color]::Transparent)

    $margin = [Math]::Max(1.0, $size * 0.075)
    $stroke = [Math]::Max(2.0, $size * 0.15)
    $diameter = $size - ($margin * 2)
    $startAngle = -78.0
    $totalSweep = 300.0
    $steps = [Math]::Max(60, $size * 2)
    $stepSweep = $totalSweep / $steps

    for ($step = 0; $step -lt $steps; $step++) {
        $fraction = $step / [double]($steps - 1)
        $pen = [System.Drawing.Pen]::new((Get-GradientColor $fraction), $stroke)
        $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Flat
        $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Flat
        $graphics.DrawArc($pen, $margin, $margin, $diameter, $diameter, $startAngle + ($step * $stepSweep), $stepSweep + 0.7)
        $pen.Dispose()
    }

    $center = $margin + ($diameter / 2)
    $radius = $diameter / 2
    $capRadius = $stroke / 2
    $startRadians = $startAngle * [Math]::PI / 180
    $endRadians = ($startAngle + $totalSweep) * [Math]::PI / 180
    $startBrush = [System.Drawing.SolidBrush]::new($palette[0])
    $endBrush = [System.Drawing.SolidBrush]::new($palette[$palette.Count - 1])
    $graphics.FillEllipse($startBrush, $center + ($radius * [Math]::Cos($startRadians)) - $capRadius, $center + ($radius * [Math]::Sin($startRadians)) - $capRadius, $stroke, $stroke)
    $graphics.FillEllipse($endBrush, $center + ($radius * [Math]::Cos($endRadians)) - $capRadius, $center + ($radius * [Math]::Sin($endRadians)) - $capRadius, $stroke, $stroke)
    $startBrush.Dispose()
    $endBrush.Dispose()

    $stream = [System.IO.MemoryStream]::new()
    $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
    $images.Add($stream.ToArray())
    $stream.Dispose()
    $graphics.Dispose()
    $bitmap.Dispose()
}

$fileStream = [System.IO.File]::Open($OutputPath, [System.IO.FileMode]::Create)
$writer = [System.IO.BinaryWriter]::new($fileStream)
$writer.Write([uint16]0)
$writer.Write([uint16]1)
$writer.Write([uint16]$images.Count)
$offset = 6 + (16 * $images.Count)

for ($index = 0; $index -lt $images.Count; $index++) {
    $size = $sizes[$index]
    $writer.Write([byte]$(if ($size -eq 256) { 0 } else { $size }))
    $writer.Write([byte]$(if ($size -eq 256) { 0 } else { $size }))
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]32)
    $writer.Write([uint32]$images[$index].Length)
    $writer.Write([uint32]$offset)
    $offset += $images[$index].Length
}

foreach ($image in $images) {
    $writer.Write($image)
}

$writer.Dispose()
$fileStream.Dispose()
Write-Host "Generated $OutputPath"
