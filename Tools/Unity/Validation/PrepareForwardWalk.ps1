Add-Type -AssemblyName System.Drawing
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../Assets/Sprite/Minigames/FastingTopDown/JesusForwardWalk'))
$src=[Drawing.Bitmap]::new((Join-Path $root 'Source/GeneratedWalkReference.png'))
$reference=[Drawing.Bitmap]::new((Join-Path $root '../TempJesus64.png'))
$colors=@{};for($y=0;$y -lt $reference.Height;$y++){for($x=0;$x -lt $reference.Width;$x++){$c=$reference.GetPixel($x,$y);if($c.A -ge 128){$key="$($c.R),$($c.G),$($c.B)";$colors[$key]=$c}}};$palette=@($colors.Values);$reference.Dispose()
$cells=@();$cellWidth=[int][Math]::Floor($src.Width/4)
for($i=0;$i -lt 4;$i++){
 $minY=$src.Height;$maxY=-1
 for($y=0;$y -lt $src.Height;$y++){for($x=$i*$cellWidth;$x -lt ($i+1)*$cellWidth;$x++){if($src.GetPixel($x,$y).A -ge 160){$minY=[Math]::Min($minY,$y);$maxY=[Math]::Max($maxY,$y)}}}
 $headMin=($i+1)*$cellWidth;$headMax=$i*$cellWidth
 for($y=$minY;$y -lt ($minY+($maxY-$minY)*.25);$y++){for($x=$i*$cellWidth;$x -lt ($i+1)*$cellWidth;$x++){if($src.GetPixel($x,$y).A -ge 160){$headMin=[Math]::Min($headMin,$x);$headMax=[Math]::Max($headMax,$x)}}}
 $cells+=@{Top=$minY;Bottom=$maxY;Head=($headMin+$headMax+1)/2.0;Height=$maxY-$minY+1}
}
$scale=57.0/($cells|Measure-Object Height -Maximum).Maximum
$sheet=[Drawing.Bitmap]::new(256,64,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
$cache=@{}
for($i=0;$i -lt 4;$i++){
 $b=[Drawing.Bitmap]::new(64,64,[Drawing.Imaging.PixelFormat]::Format32bppArgb);$cell=$cells[$i]
 for($y=0;$y -lt 64;$y++){for($x=0;$x -lt 64;$x++){
  $sx=[int][Math]::Floor(($x+.5-32)/$scale+$cell.Head);$sy=[int][Math]::Floor(($y+.5-61)/$scale+$cell.Bottom+1)
  if($sx -lt ($i*$cellWidth) -or $sx -ge (($i+1)*$cellWidth) -or $sy -lt 0 -or $sy -ge $src.Height){continue};$c=$src.GetPixel($sx,$sy);if($c.A -lt 160){continue}
  $key="$($c.R),$($c.G),$($c.B)";if(-not $cache.ContainsKey($key)){$best=$null;$distance=[double]::MaxValue;foreach($p in $palette){$d=[Math]::Pow($c.R-$p.R,2)+[Math]::Pow($c.G-$p.G,2)+[Math]::Pow($c.B-$p.B,2);if($d -lt $distance){$distance=$d;$best=$p}};$cache[$key]=$best};$c=$cache[$key];$c=[Drawing.Color]::FromArgb(255,$c.R,$c.G,$c.B);$b.SetPixel($x,$y,$c);$sheet.SetPixel($i*64+$x,$y,$c)
 }}
 $b.Save((Join-Path $root "Walk_$i.png"),[Drawing.Imaging.ImageFormat]::Png);$b.Dispose()
}
$sheet.Save((Join-Path $root 'ForwardWalk-Sheet.png'),[Drawing.Imaging.ImageFormat]::Png)
$qa=[IO.Path]::GetFullPath((Join-Path $root '../../../../../Docs/QA/ForwardWalk-20261116'));New-Item -ItemType Directory -Path $qa -Force|Out-Null
$preview=[Drawing.Bitmap]::new(1024,256);$g=[Drawing.Graphics]::FromImage($preview);$g.Clear([Drawing.Color]::FromArgb(30,37,49));$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::NearestNeighbor;$g.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::Half;$g.DrawImage($sheet,[Drawing.Rectangle]::new(0,0,1024,256));$preview.Save((Join-Path $qa 'walk-frames.png'),[Drawing.Imaging.ImageFormat]::Png);$g.Dispose();$preview.Dispose();$sheet.Dispose();$src.Dispose()

