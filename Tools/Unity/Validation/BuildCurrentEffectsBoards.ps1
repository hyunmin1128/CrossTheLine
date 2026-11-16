Add-Type -AssemblyName System.Drawing
$out=Join-Path $PSScriptRoot '../../../Docs/QA/TopDownEffects-20261116'
$out=[IO.Path]::GetFullPath($out)
$script:title=[Drawing.Font]::new('Malgun Gothic',24,[Drawing.FontStyle]::Bold)
$script:label=[Drawing.Font]::new('Malgun Gothic',14,[Drawing.FontStyle]::Bold)
$script:small=[Drawing.Font]::new('Malgun Gothic',11)
$script:white=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(235,239,246))
$script:muted=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(159,175,196))
function Board($name,$heading,$subtitle,$width,$height){
 $script:b=[Drawing.Bitmap]::new($width,$height);$script:g=[Drawing.Graphics]::FromImage($b);$g.Clear([Drawing.Color]::FromArgb(17,22,31));$g.InterpolationMode=[Drawing.Drawing2D.InterpolationMode]::NearestNeighbor;$g.PixelOffsetMode=[Drawing.Drawing2D.PixelOffsetMode]::Half
 $g.DrawString($heading,$title,$white,28,18);$g.DrawString($subtitle,$small,$muted,30,64);$script:boardName=$name
}
function Tile($file,$text,$x,$y,$w,$h){
 $c1=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(35,43,56));$c2=[Drawing.SolidBrush]::new([Drawing.Color]::FromArgb(43,52,66))
 for($ty=0;$ty -lt $h;$ty+=16){for($tx=0;$tx -lt $w;$tx+=16){$brush=if((($tx/16+$ty/16)%2) -eq 0){$c1}else{$c2};$g.FillRectangle($brush,($x+$tx),($y+$ty),[Math]::Min(16,$w-$tx),[Math]::Min(16,$h-$ty))}}
 $im=[Drawing.Bitmap]::new((Join-Path $out "png/$file.png"));$g.DrawImage($im,[Drawing.Rectangle]::new($x,$y,$w,$h));$im.Dispose();$g.DrawString($text,$small,$white,$x,($y+$h+6));$c1.Dispose();$c2.Dispose()
}
function Finish{$b.Save((Join-Path $out $boardName),[Drawing.Imaging.ImageFormat]::Png);$g.Dispose();$b.Dispose()}
Board '01-vignettes.png' '현재 미니게임 · 비네트 전체' '장애물별 14종 + 기본 피해 + 방패 방어 · 실제 코드 메쉬 / 피격 0.22초 시점 · 체크무늬 = 투명' 1140 860
$keys=@('rock','sand','cactus','quicksand','bat','collapse','crack','tomb','wall','poison','fire','thorns','pillar','reaper','damage','shield')
$labels=@('바위','모래바람','선인장','모래지옥','박쥐','낙석 / 붕괴','균열','무덤','벽','독','불','가시','기둥','사신','기본 피해','방패 방어')
for($i=0;$i -lt $keys.Count;$i++){Tile "vignette-$($keys[$i])" $labels[$i] (28+($i%4)*280) (108+[Math]::Floor($i/4)*182) 254 143}
Finish
Board '02-particles.png' '현재 미니게임 · 파티클과 보호막' '위 3줄: 피격 진행 20% → 50% → 80% (입자만 분리) · 아래: 보호막 / 획득 / 파괴 / 가속' 1140 1120
$kinds=@('rock','cactus','sand','quicksand');$labels=@('돌 파편','가시 입자','모래 입자','모래지옥 입자');$times=@(20,50,80)
for($r=0;$r -lt 3;$r++){for($c=0;$c -lt 4;$c++){Tile "hit-$($kinds[$c])-$($times[$r])" "$($labels[$c]) · $($times[$r])%" (28+$c*280) (110+$r*244) 220 220}}
$keys=@('shield','acquire','break','water');$labels=@('방패 보호막','방패 획득 반짝임','보호막 파괴 입자','수통 가속 입자')
for($i=0;$i -lt 4;$i++){Tile "aura-$($keys[$i])" $labels[$i] (28+$i*280) 850 220 220}
Finish
Board '03-effects.png' '현재 미니게임 · 애니메이션과 화면 효과' '모래바람 / 불꽃 / 방패 파괴 / 기도와 피버 / 화면 파편 · 원본 씬 수정 없이 추출' 1140 2340
$g.DrawString('모래바람 · 5프레임',$label,$white,28,108)
for($i=0;$i -lt 5;$i++){Tile "sandstorm-$i" "프레임 $($i+1)" (28+$i*220) 145 170 170}
$g.DrawString('불꽃 · 4프레임',$label,$white,28,360)
for($i=0;$i -lt 4;$i++){Tile "fire-$i" "프레임 $($i+1)" (28+$i*220) 397 170 170}
$g.DrawString('방패 파괴 · 떠오름 → 균열 → 파편 → 소멸',$label,$white,28,610)
$keys=@('15','55','105','150');$labels=@('0.15초 · 확대 중','0.55초 · 균열','1.05초 · 분해','1.50초 · 사라짐')
for($i=0;$i -lt 4;$i++){Tile "shield-break-$($keys[$i])" $labels[$i] (28+$i*280) 650 254 143}
$keys=@('ring-charging','ring-fever','player-white-flash','prayer-preparing','prayer-praying','prayer-blocked','prayer-window','world-rock','world-cactus','world-quicksand')
$labels=@('불꽃 충전 링','피버 활성 링','피격 흰색 점멸','기도 준비','기도 중','기도 차단','기도 가능 구간','바위 (회전)','선인장','모래지옥 (확대)')
for($i=0;$i -lt $keys.Count;$i++){Tile $keys[$i] $labels[$i] (28+($i%5)*220) (850+[Math]::Floor($i/5)*215) 170 170}
$keys=@('edge-rock','edge-cactus','edge-sand','edge-quicksand','screen-vision','screen-transition');$labels=@('바위 · 화면 파편','선인장 / 가시 · 화면 파편','모래바람 · 화면 입자','모래지옥 · 화면 입자','시야 제한 · 코드 기준 예시','단계 전환 · 코드 기준 예시')
for($i=0;$i -lt $keys.Count;$i++){Tile $keys[$i] $labels[$i] (28+($i%2)*556) (1320+[Math]::Floor($i/2)*330) 520 293}
Finish
$title.Dispose();$label.Dispose();$small.Dispose();$white.Dispose();$muted.Dispose()


