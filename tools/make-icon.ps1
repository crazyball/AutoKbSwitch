# Generates AutoKbSwitch.ico (embedded in the exe through <ApplicationIcon> and used by the app at runtime)
# from the master artwork assets\icon.png (512x512, transparent corners).
#
# Every standard size is stored in a single .ico file: 32-bit BMP entries up to 128 px (read by every
# Windows API) and a PNG entry for 256 px (the usual layout, keeps the file small).
#
# Usage (Windows PowerShell 5.1 or PowerShell 7):  .\tools\make-icon.ps1

param(
    [string]$Source = (Join-Path $PSScriptRoot '..\assets\icon.png'),
    [string]$Out = (Join-Path $PSScriptRoot '..\AutoKbSwitch.ico')
)

Add-Type -AssemblyName System.Drawing

function Resize-Bitmap([System.Drawing.Bitmap]$Bmp, [int]$Size) {
    # Halve step by step while the image is more than twice the target: much less aliasing than one big jump
    $cur = $Bmp
    while ($cur.Width -gt 2 * $Size) {
        $next = New-Object System.Drawing.Bitmap ([int]($cur.Width / 2)), ([int]($cur.Height / 2)), ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
        Copy-Scaled $cur $next
        if ($cur -ne $Bmp) { $cur.Dispose() }
        $cur = $next
    }
    $final = New-Object System.Drawing.Bitmap $Size, $Size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    Copy-Scaled $cur $final
    if ($cur -ne $Bmp) { $cur.Dispose() }
    return , $final
}

function Copy-Scaled([System.Drawing.Bitmap]$From, [System.Drawing.Bitmap]$To) {
    $g = [System.Drawing.Graphics]::FromImage($To)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::Half
    $g.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
    $attrs = New-Object System.Drawing.Imaging.ImageAttributes
    $attrs.SetWrapMode([System.Drawing.Drawing2D.WrapMode]::TileFlipXY)   # no dark fringe at the edges
    $g.DrawImage($From, (New-Object System.Drawing.Rectangle 0, 0, $To.Width, $To.Height), 0, 0, $From.Width, $From.Height, [System.Drawing.GraphicsUnit]::Pixel, $attrs)
    $attrs.Dispose(); $g.Dispose()
}

$master = [System.Drawing.Bitmap]::FromFile([System.IO.Path]::GetFullPath($Source))
$sizes = 16, 20, 24, 32, 40, 48, 64, 128, 256
$images = foreach ($size in $sizes) {
    $bmp = Resize-Bitmap $master $size
    $ms = New-Object System.IO.MemoryStream
    if ($size -ge 256) {
        $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    else {
        # BITMAPINFOHEADER (height doubled: XOR image + AND mask), then BGRA pixels bottom-up, then an empty AND mask
        $maskRow = [int][Math]::Ceiling($size / 32.0) * 4
        $w = New-Object System.IO.BinaryWriter $ms
        $w.Write([UInt32]40); $w.Write([Int32]$size); $w.Write([Int32]($size * 2))
        $w.Write([UInt16]1); $w.Write([UInt16]32); $w.Write([UInt32]0)
        $w.Write([UInt32]($size * $size * 4 + $maskRow * $size))
        $w.Write([Int32]0); $w.Write([Int32]0); $w.Write([UInt32]0); $w.Write([UInt32]0)
        for ($py = $size - 1; $py -ge 0; $py--) {
            for ($px = 0; $px -lt $size; $px++) {
                $c = $bmp.GetPixel($px, $py)
                $w.Write([byte]$c.B); $w.Write([byte]$c.G); $w.Write([byte]$c.R); $w.Write([byte]$c.A)
            }
        }
        $w.Write((New-Object byte[] ($maskRow * $size)))
        $w.Flush()
    }
    $bmp.Dispose()
    , @{ Size = $size; Bytes = $ms.ToArray() }
}
$master.Dispose()

# ICO layout: ICONDIR (6 bytes) + one ICONDIRENTRY (16 bytes) per image + the image data
$fs = [System.IO.File]::Create([System.IO.Path]::GetFullPath($Out))
$bw = New-Object System.IO.BinaryWriter $fs
$bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]$images.Count)
$offset = 6 + 16 * $images.Count
foreach ($img in $images) {
    $dim = if ($img.Size -ge 256) { 0 } else { $img.Size }   # 0 means 256
    $bw.Write([byte]$dim); $bw.Write([byte]$dim); $bw.Write([byte]0); $bw.Write([byte]0)
    $bw.Write([UInt16]1); $bw.Write([UInt16]32)
    $bw.Write([UInt32]$img.Bytes.Length); $bw.Write([UInt32]$offset)
    $offset += $img.Bytes.Length
}
foreach ($img in $images) { $bw.Write($img.Bytes) }
$bw.Dispose()
Write-Host "Written $Out ($($images.Count) sizes)"
