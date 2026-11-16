Add-Type -AssemblyName System.Drawing
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../Assets/Sprite/Effect/Fasting'))
$atlas=[Drawing.Bitmap]::new((Join-Path $root 'Source/IllustratedEffectsAtlas.png'))
function Cut($name,$x,$y,$size,$target){
 $b=[Drawing.Bitmap]::new($target,$target,[Drawing.Imaging.PixelFormat]::Format32bppArgb)
 for($py=0;$py -lt $target;$py++){for($px=0;$px -lt $target;$px++){
  $sx=[Math]::Min($atlas.Width-1,[Math]::Max(0,[int][Math]::Floor($x+($px+.5)*$size/$target)))
  $sy=[Math]::Min($atlas.Height-1,[Math]::Max(0,[int][Math]::Floor($y+($py+.5)*$size/$target)))
  $c=$atlas.GetPixel($sx,$sy);if($c.A -ge 128){$b.SetPixel($px,$py,[Drawing.Color]::FromArgb(255,$c.R,$c.G,$c.B))}
 }}
 $b.Save((Join-Path $root "$name.png"),[Drawing.Imaging.ImageFormat]::Png);$b.Dispose()
}
$centers=@(355,560,766,974,1189,1417);for($i=0;$i -lt 6;$i++){Cut "ShieldBreak_$i" ($centers[$i]-110) 18 220 64}
$centers=@(390,690,990,1300);for($i=0;$i -lt 4;$i++){Cut "Bubble_$i" ($centers[$i]-120) 415 240 64;Cut "Water_$i" ($centers[$i]-80) 640 160 32}
$centers=@(382,680,990,1320);for($i=0;$i -lt 4;$i++){Cut "Prayer_$i" ($centers[$i]-110) 800 220 64}
Cut 'Spark' 355 884 56 16
Cut 'Rock_0' 382 303 80 16;Cut 'Rock_1' 488 322 64 16;Cut 'Rock_2' 565 325 56 16
Cut 'Thorn_0' 808 299 80 16;Cut 'Thorn_1' 885 313 72 16;Cut 'Thorn_2' 960 325 64 16
Cut 'Sand_0' 1235 320 24 8;Cut 'Sand_1' 1254 347 24 8;Cut 'Sand_2' 1289 335 24 8
$atlas.Dispose()
