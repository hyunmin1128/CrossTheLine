Add-Type -AssemblyName System.Drawing
$out=Join-Path $PSScriptRoot '../../../Docs/QA/TopDownEffects-20261113'
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
Board '01-sprites-and-ui.png' '탑다운 · 스프라이트와 UI 이펙트' '실제 Unity 스프라이트 추출 · 체크무늬는 투명 영역 · 칸별 크기는 비교용' 1120 940
$g.DrawString('모래바람 · 5프레임',$label,$white,30,105)
for($i=0;$i -lt 5;$i++){Tile "sandstorm-$i" "프레임 $($i+1)" (30+$i*214) 142 180 180}
$g.DrawString('불꽃 · 4프레임',$label,$white,30,365)
for($i=0;$i -lt 4;$i++){Tile "fire-$i" "프레임 $($i+1)" (30+$i*214) 402 180 180}
Tile 'world-rock' '바위 (회전하는 장애물)' 30 675 144 144
Tile 'world-cactus' '선인장' 210 675 144 144
Tile 'world-quicksand' '모래지옥 (접근 시 확대)' 390 675 144 144
Tile 'player-white-flash' '피격 시 흰색 점멸' 570 675 144 144
Tile 'ring-charging' '불꽃 충전 링' 750 675 144 144
Tile 'ring-fever' '피버 활성 링' 930 675 144 144
$g.DrawString('피버의 불꽃도 위 4프레임을 사용합니다. 좌우 모래바람은 같은 그림에 이동 방향/반전을 적용합니다.',$small,$muted,30,887)
Finish
Board '02-hit-particles.png' '탑다운 · 피격 입자 시간별 모양' '실제 FastingHitFeedback 메쉬 · 캐릭터를 제외한 입자만 분리 · 왼쪽 → 오른쪽으로 시간 진행' 1020 1150
$kinds=@('rock','cactus','sand','quicksand');$names=@('돌 파편','가시 / 선인장','모래바람 피격','모래지옥 피격')
for($r=0;$r -lt 4;$r++){$y=110+$r*245;$g.DrawString($names[$r],$label,$white,28,($y+85));$phases=@(20,50,80);for($c=0;$c -lt 3;$c++){Tile "hit-$($kinds[$r])-$($phases[$c])" "진행 $($phases[$c])%" (240+$c*245) $y 205 205}}
$g.DrawString('모래바람: 캐릭터가 위로 뜸 / 모래지옥: 아래로 가라앉고 세로로 눌림 (캐릭터 변형은 이 입자 그림에 미포함)',$small,$muted,28,1100)
Finish
Board '03-screen-effects.png' '탑다운 · 화면 가장자리와 전환 효과' '게임 UI 메쉬 / 코드 기준 · 체크무늬로 투명도 확인 · 효과가 가장 잘 보이는 시점의 정지 화면' 1120 1230
$files=@('edge-rock','edge-cactus','edge-sand','edge-quicksand','edge-shield','screen-vision','screen-transition')
$labels=@('바위 / 기본 피해 · 가장자리 파편','선인장 / 가시 · 가시 테두리','모래바람 · 가장자리 모래 알갱이','모래지옥 · 아래에서 올라오는 모래','방패 방어 성공 · 파란 테두리 (현재 설정)','시야 제한 · 플레이어 주변만 남음','단계 전환 · 어두운 화면 페이드')
for($i=0;$i -lt $files.Count;$i++){$x=30+($i%2)*550;$y=110+[Math]::Floor($i/2)*270;Tile $files[$i] $labels[$i] $x $y 510 215}
Finish
$title.Dispose();$label.Dispose();$small.Dispose();$white.Dispose();$muted.Dispose()

