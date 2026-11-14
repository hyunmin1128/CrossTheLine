Add-Type -AssemblyName System.Drawing
$folder = Join-Path $PSScriptRoot '../Docs/ArtReferences/EffectsCatalogue-20261112'
$folder = [IO.Path]::GetFullPath($folder)
$names = @('01-Rock','02-Cactus','03-Sandstorm','04-Quicksand','05-WaterBoost','06-ShieldActive','07-ShieldBreak','08-Invulnerability','09-Fever')
$labels = @('01  돌 피격 · 파편과 흔들림','02  선인장 · 가시','03  모래바람 · 공중으로 뜸','04  모래지옥 · 잠김','05  물 가속 · 푸른 잔광','06  방패 보유 · 현재 타원형','07  방패 파괴 · 푸른 파편','08  피격 무적 · 흰색 점멸','09  성령불 피버 · 초상화 UI')
$font = [Drawing.Font]::new('Malgun Gothic',12,[Drawing.FontStyle]::Regular)
$titleFont = [Drawing.Font]::new('Malgun Gothic',19,[Drawing.FontStyle]::Bold)
$white = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(238,240,246))
$muted = [Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(158,176,198))
$sheet = [Drawing.Bitmap]::new(1024,1190)
$graphics = [Drawing.Graphics]::FromImage($sheet)
$graphics.Clear([Drawing.Color]::FromArgb(22,29,40))
$graphics.InterpolationMode = [Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$graphics.PixelOffsetMode = [Drawing.Drawing2D.PixelOffsetMode]::Half
$graphics.DrawString('현재 게임 이펙트 모음', $titleFont, $white, 16, 10)
$graphics.DrawString('실제 Unity 화면 캡처 · 정지 화면이며 애니메이션의 한 순간입니다', $font, $muted, 16, 44)
for($i=0;$i -lt 9;$i++) {
    $x = 16 + ($i % 3)*336; $y = 82 + [Math]::Floor($i/3)*366
    $graphics.DrawString($labels[$i],$font,$white,$x,$y)
    $img = [Drawing.Bitmap]::FromFile((Join-Path $folder ($names[$i]+'.png')))
    if($i -eq 8) {
        $source = [Drawing.RectangleF]::new(0,$img.Height*.69,$img.Width*.27,$img.Height*.31)
        $destination = [Drawing.RectangleF]::new($x,$y+70,320,207)
    } else {
        $source = [Drawing.RectangleF]::new($img.Width*.3958,$img.Height*.3148,$img.Width*.2083,$img.Height*.3704)
        $destination = [Drawing.RectangleF]::new($x,$y+34,320,320)
    }
    $graphics.DrawImage($img,$destination,$source,[Drawing.GraphicsUnit]::Pixel)
    $img.Dispose()
}
$sheet.Save((Join-Path $folder 'Current-Character-Effects.png'),[Drawing.Imaging.ImageFormat]::Png)
$graphics.Dispose();$sheet.Dispose()
$screenIndices=@(0,1,2,3,7,6)
$screenLabels=@('돌 · 화면 파편','선인장 · 화면 가장자리 가시','모래바람 · 화면 모래','모래지옥 · 아래에서 올라오는 모래','실제 피해 · 옅은 붉은 테두리','방패 방어 · 파란 가장자리 효과')
$sheet=[Drawing.Bitmap]::new(1008,1032);$graphics=[Drawing.Graphics]::FromImage($sheet);$graphics.Clear([Drawing.Color]::FromArgb(22,29,40));$graphics.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::NearestNeighbor
$graphics.DrawString('현재 화면 피격 이펙트',$titleFont,$white,16,10)
for($j=0;$j -lt 6;$j++) {
    $x=16+($j%2)*496;$y=66+[Math]::Floor($j/2)*318
    $graphics.DrawString($screenLabels[$j],$font,$white,$x,$y)
    $img=[Drawing.Bitmap]::FromFile((Join-Path $folder ($names[$screenIndices[$j]]+'.png')))
    $graphics.DrawImage($img,[Drawing.Rectangle]::new($x,$y+32,480,270));$img.Dispose()
}
$sheet.Save((Join-Path $folder 'Current-Screen-Effects.png'),[Drawing.Imaging.ImageFormat]::Png)
$graphics.Dispose();$sheet.Dispose();$font.Dispose();$titleFont.Dispose();$white.Dispose();$muted.Dispose()
Write-Output 'Created character and screen effect contact sheets from actual captures.'
