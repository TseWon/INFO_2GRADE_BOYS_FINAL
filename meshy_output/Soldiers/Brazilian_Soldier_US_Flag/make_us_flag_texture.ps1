Add-Type -AssemblyName System.Drawing

$folder = Split-Path -Parent $MyInvocation.MyCommand.Path
$source = Join-Path $folder 'Original_BaseColor.png'
$target = Join-Path $folder 'US_LeftFlag_BaseColor.png'
$bitmap = [System.Drawing.Bitmap]::new($source)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias

$tl = [System.Drawing.PointF]::new(3480, 3685)
$tr = [System.Drawing.PointF]::new(3825, 3785)
$br = [System.Drawing.PointF]::new(3825, 3930)
$bl = [System.Drawing.PointF]::new(3445, 3850)

function At([float]$u, [float]$v) {
    $lx = $tl.X + ($bl.X - $tl.X) * $v
    $ly = $tl.Y + ($bl.Y - $tl.Y) * $v
    $rx = $tr.X + ($br.X - $tr.X) * $v
    $ry = $tr.Y + ($br.Y - $tr.Y) * $v
    return [System.Drawing.PointF]::new($lx + ($rx - $lx) * $u, $ly + ($ry - $ly) * $u)
}

$red = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(150, 25, 35))
$white = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(230, 230, 220))
$blue = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(13, 35, 92))
$outline = [System.Drawing.Pen]::new([System.Drawing.Color]::FromArgb(46, 50, 49), 3)

for ($stripe = 0; $stripe -lt 13; $stripe++) {
    $v0 = [float]$stripe / 13
    $v1 = [float]($stripe + 1) / 13
    $quad = [System.Drawing.PointF[]]@((At 0 $v0), (At 1 $v0), (At 1 $v1), (At 0 $v1))
    $graphics.FillPolygon($(if ($stripe % 2 -eq 0) { $red } else { $white }), $quad)
}
$canton = [System.Drawing.PointF[]]@((At 0 0), (At .4 0), (At .4 (7.0/13)), (At 0 (7.0/13)))
$graphics.FillPolygon($blue, $canton)
for ($row = 0; $row -lt 9; $row++) {
    $columns = $(if ($row % 2 -eq 0) { 6 } else { 5 })
    for ($column = 0; $column -lt $columns; $column++) {
        $u = (($column + .5) / $columns) * .4
        $v = (($row + .5) / 9) * (7.0/13)
        $p = At $u $v
        $graphics.FillEllipse($white, $p.X - 2.5, $p.Y - 2.5, 5, 5)
    }
}
$graphics.DrawPolygon($outline, [System.Drawing.PointF[]]@($tl, $tr, $br, $bl))

$bitmap.Save($target, [System.Drawing.Imaging.ImageFormat]::Png)
$outline.Dispose(); $blue.Dispose(); $white.Dispose(); $red.Dispose()
$graphics.Dispose(); $bitmap.Dispose()
Write-Output $target
