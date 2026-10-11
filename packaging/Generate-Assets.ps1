# Reproducible, code-drawn camera icon; no external artwork or fonts required.
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$assetDirectory = Join-Path $PSScriptRoot 'Assets'
$null = New-Item -ItemType Directory -Path $assetDirectory -Force
function New-CameraLogo([int]$size, [string]$file) {
    $bitmap = [System.Drawing.Bitmap]::new($size, $size)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $background = [System.Drawing.SolidBrush]::new([System.Drawing.ColorTranslator]::FromHtml('#145FA8'))
    $white = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::White)
    $pen = [System.Drawing.Pen]::new([System.Drawing.Color]::White, 5)
    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.ScaleTransform($size / 100.0, $size / 100.0)
        $graphics.FillEllipse($background, 2, 2, 96, 96)
        $graphics.DrawRectangle($pen, 21, 35, 40, 30)
        $points = [System.Drawing.PointF[]]@(
            [System.Drawing.PointF]::new(65, 42), [System.Drawing.PointF]::new(80, 34),
            [System.Drawing.PointF]::new(80, 66), [System.Drawing.PointF]::new(65, 58))
        $graphics.FillPolygon($white, $points)
        $graphics.DrawLine($pen, 35, 77, 58, 77)
        $graphics.DrawLine($pen, 46, 68, 46, 76)
        $graphics.DrawLine($pen, 32, 25, 52, 25)
    }
    finally { $graphics.Dispose(); $background.Dispose(); $white.Dispose(); $pen.Dispose() }
    try { $bitmap.Save((Join-Path $assetDirectory $file), [System.Drawing.Imaging.ImageFormat]::Png) }
    finally { $bitmap.Dispose() }
}
New-CameraLogo 44 'Square44x44Logo.png'
New-CameraLogo 150 'Square150x150Logo.png'
New-CameraLogo 50 'StoreLogo.png'
New-CameraLogo 256 'Camera256.png'
$png = [System.IO.File]::ReadAllBytes((Join-Path $assetDirectory 'Camera256.png'))
$writer = [System.IO.BinaryWriter]::new([System.IO.File]::Create((Join-Path $assetDirectory 'CamControl.ico')))
try {
    $writer.Write([uint16]0); $writer.Write([uint16]1); $writer.Write([uint16]1)
    $writer.Write([byte]0); $writer.Write([byte]0); $writer.Write([byte]0); $writer.Write([byte]0)
    $writer.Write([uint16]1); $writer.Write([uint16]32)
    $writer.Write([uint32]$png.Length); $writer.Write([uint32]22); $writer.Write($png)
}
finally { $writer.Dispose() }
