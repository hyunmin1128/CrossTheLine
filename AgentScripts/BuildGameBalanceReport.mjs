import fs from 'node:fs';
import path from 'node:path';
const root=process.cwd();
const folder=path.join(root,'Docs/Balance/2026-11-23');
const read=(name)=>JSON.parse(fs.readFileSync(path.join(folder,name),'utf8'));
const raw=read('fasting-snapshot.json');
const globals=Object.fromEntries(Object.entries(raw).filter(([k,v])=>typeof v==='string'&&v.startsWith('{')).map(([k,v])=>[k,JSON.parse(v)]));
const stages=raw.stages.map(s=>({source:s.source,advancedSource:s.advancedSource,p:JSON.parse(s.profile),a:JSON.parse(s.advanced)}));
const canaRaw=read('cana-snapshot.json');
const cana=Object.fromEntries(Object.entries(canaRaw).map(([k,v])=>[k,typeof v==='string'&&v.startsWith('{')?JSON.parse(v):v]));
const sceneRows=read('scenes-0.json').rows;
const f=n=>typeof n==='number'?Number(n.toFixed(5)):String(n??'—');
const pct=n=>`${f(n*100)}%`;
const sections=[];
const add=(title,headers,rows,note='')=>sections.push({title,headers,rows,note});
const names=['돌/석순/잔해','모래바람','선인장','모래지옥','박쥐','낙석','균열','해골무덤','벽','독 우물','용암 구멍(Fire)','가시덤불','화염기둥','사신'];
add('광야: 스테이지별 핵심 수치',['항목',...stages.map(s=>s.p.displayName)],
[
 ['기도 목표량',...stages.map(s=>s.p.prayerTarget)],
 ['기도 기본 충전/초',...stages.map(s=>s.p.prayerPerSecond)],
 ['수동 이동속도(월드 단위/초)',...stages.map(s=>s.p.playerMoveSpeed)],
 ['기도 중 수동 이동 배율',...stages.map(s=>pct(s.p.prayerMoveMultiplier))],
 ['기도 중 수동 이동속도',...stages.map(s=>f(s.p.playerMoveSpeed*s.p.prayerMoveMultiplier))],
 ['기본 스크롤 속도',...stages.map(s=>s.p.scrollSpeed)],
 ['스크롤 배율',...stages.map(s=>f(s.a.scrollSpeedMultiplier))],
 ['계산상 스크롤 속도/초',...stages.map(s=>f(s.p.scrollSpeed*s.a.scrollSpeedMultiplier))],
 ['장애물 속도 배율',...stages.map(s=>f(s.p.hazardSpeedMultiplier))],
 ['스테이지 스폰 배율',...stages.map(s=>s.p.obstacleFrequency)],
 ['생성 공격 구간(초)',...stages.map(s=>s.p.attackSeconds)],
 ['기존 장애물 통과 후 휴식(초)',...stages.map(s=>f(s.p.restSeconds))],
 ['무작위 생성 시도 간격(초)',...stages.map(s=>`${f(s.p.randomSpawnIntervals.x*s.p.spawnIntervalScale/(globals.spawnSettings.globalFrequency*s.p.obstacleFrequency))}~${f(s.p.randomSpawnIntervals.y*s.p.spawnIntervalScale/(globals.spawnSettings.globalFrequency*s.p.obstacleFrequency))}`)],
 ['패턴 생성 시도 간격(초)',...stages.map(s=>{const lo=Math.max(s.a.minimumSpawnInterval,s.p.waveInterval*s.a.spawnIntervalMultiplier.x),hi=Math.max(lo,s.p.waveInterval*s.a.spawnIntervalMultiplier.y);const factor=s.p.patternIntervalMultiplier*s.p.spawnIntervalScale/(globals.spawnSettings.globalFrequency*s.p.obstacleFrequency);return `${f(lo*factor)}~${f(hi*factor)}`;})],
 ['스폰 가로 범위(화면)',...stages.map(s=>`${pct(s.p.roadEdges.x)}~${pct(s.p.roadEdges.y)}`)]
],`출처: 현재 씬에 연결된 4개 스테이지 프리팹과 각 Advanced Settings. 생성 간격은 설정으로 계산한 '시도 간격'이며, 공격/정리/휴식·충돌 방지·생성 거절로 실제 개체 수는 달라집니다. 공통 스폰 배율=${globals.spawnSettings.globalFrequency}.`);
add('광야: 장애물 종류 선택 비율과 피해',['스테이지','장애물','가중치','정규화 선택 비율','피해(체력 칸)','설정 출처'],stages.flatMap(s=>{const total=s.p.obstacles.reduce((a,e)=>a+e.spawnWeight,0);return s.p.obstacles.map(e=>[s.p.displayName,names[e.kind],f(e.spawnWeight),total?pct(e.spawnWeight/total):'선택 중지',e.damage,s.source]);}), '선택 비율은 종류를 고르는 비율입니다. 배치별 개수, 안전 공간, 사신 중복 제한 등의 영향으로 화면에 실제 보이는 개체 비율과 같지 않습니다.');
add('광야: 아이템 생성 확률과 충전',['항목',...stages.map(s=>s.p.displayName)],
[['성경 생성 확률',...stages.map(s=>pct(s.p.bibleChance))],['페이지 생성 확률',...stages.map(s=>pct(s.p.pageChance))],['조각 생성 확률',...stages.map(s=>pct(s.p.fragmentChance))],['불꽃 생성 확률',...stages.map(s=>pct(s.p.flameChance))],['방패 생성 확률',...stages.map(s=>pct(s.p.shieldChance))],['물 보급 생성 확률',...stages.map(s=>pct(s.p.waterChance))],['물 보급 재배치 간격 기준(초)',...stages.map(s=>s.p.waterInterval)],['성경 충전 비율 / 양',...stages.map(s=>`${pct(s.p.bibleCharge)} / ${f(s.p.prayerTarget*s.p.bibleCharge)}`)],['페이지 충전 비율 / 양',...stages.map(s=>`${pct(s.p.pageCharge)} / ${f(s.p.prayerTarget*s.p.pageCharge)}`)],['조각 충전 비율 / 양',...stages.map(s=>`${pct(s.p.fragmentCharge)} / ${f(s.p.prayerTarget*s.p.fragmentCharge)}`)]], '확률은 해당 종류의 배치 후보를 활성화할 때 적용됩니다. 전체 아이템 중 구성 비율이 아니며 후보 오브젝트 수와 재배치에 따라 실제 획득 빈도가 달라집니다. 기도량은 목표에서 상한 처리합니다.');
const a=stages[0].a;
add('광야: 체력·기도·피버·물·감속',['항목','값','근거 / 의미'],[
 ['최대 체력',a.maximumHealth,'Advanced Settings'],['기본 피격 무적(초)',a.hitGrace,'상태이상에 따라 더 길어질 수 있음'],['화면에 보인 후 피격 유예(초)',a.minimumVisibleReactionSeconds,'표시는 불투명, 유예 동안 피해 차단'],['스테이지 전환 회복',1,'체력 1칸, 최대 체력까지'],['기도 시작 준비(초)',a.prayerPreparationSeconds,'Space를 뗐다 누르면 다시 준비'],['불꽃 기도 배율',1.5,'8초 버프'],['불꽃 버프(초)',8,'불꽃 추가 획득 시 갱신'],['피버 발동 불꽃 수',a.flamesForFever,'기본 스택'],['피버 최대 시간(초)',a.feverSeconds,'기도 여부와 무관하게 매초 1초 감소'],['피버 기도 추가 배율',2,'불꽃 버프와 결합 시 기본 대비 3배'],['피버 중 불꽃 추가 충전(초)',stages[0].p.feverRefillSeconds,'최대 피버 시간까지'],['피버 게이지 자연 감소',`${f(100/a.feverSeconds)}%/초`,'추가 충전·버프 변경이 없을 때'],['일반 슬로우 이동 배율',.55,'정상 속도의 55%, 45% 감소'],['일반 슬로우 수동 이동속도',f(stages[0].p.playerMoveSpeed*.55),'기도·가속과 겹치면 추가 곱셈'],['가시덤불 이동 배율',a.thornMoveMultiplier,'정상 속도의 75%, 25% 감소'],['수통 가속 배율',1.3,'정상 대비 30% 증가'],['수통 가속 수동 속도',f(stages[0].p.playerMoveSpeed*1.3),'다른 상태 없을 때'],['수통 사용 회복',1,'체력 1칸'],['수통 사용 가속(초)',3,'슬로우·속박·기도 금지·시야 방해·방향 반전 해제'],['수통 사용 최소 무적(초)',.5,'기존 무적 시간이 더 길면 유지'],['물 보급 접촉 무적(초)',.15,'빈 수통 충전 시'],['방패 최대 보유',3,'피격 시 1개 자동 소모'],['방패 파괴 연출(초)',1.8,'화면 효과'],['방패 파괴 최소 보호(초)',.9,'실제 보호는 max(기본 피격 무적, 0.9)'],['목표 달성 후 전환선 거리',a.finishLineDistance,'플레이어 앞 월드 단위'],['스테이지 화면 전환(초)',.6,'코드 고정값']
]);
const h=globals.hit,v=globals.vision,r=globals.reaper;
add('광야: 장애물 상태이상·연출',['항목','값','단위 / 의미'],[
 ['모래바람 슬로우',1.2,'초'],['모래바람 공중 연출',f(h.sandAirSeconds),'초'],['모래바람 상승 높이',f(h.sandAirHeight),'월드 단위 / 그림만 이동'],['모래지옥 속박',a.quicksandStunSeconds,'초'],['모래지옥 잠김 연출',h.quicksandSinkSeconds,'초'],['박쥐 슬로우',f(a.batSlowSeconds),'초'],['박쥐 시야 방해 요청',f(a.batVisionSeconds),'초 / 마스크 전환 시간과 별도'],['박쥐 공중 연출',f(h.batAirSeconds),'초'],['박쥐 상승 높이',f(h.batAirHeight),'월드 단위'],['균열 낙하',f(h.crackFallSeconds),'초'],['균열 복귀 점멸',f(h.crackBlinkSeconds),'초 / 플레이어 연출'],['균열 추락 깊이',f(h.crackFallDepth),'월드 단위'],['해골무덤 기도 금지',a.tombPrayerBlockSeconds,'초'],['독 우물 방향 반전',a.poisonReverseSeconds,'초'],['용암·화염기둥 기도 금지',a.burnPrayerBlockSeconds,'초'],['가시덤불 슬로우',a.thornSlowSeconds,'초'],['화염기둥 공중 연출',h.pillarAirSeconds,'초'],['화염기둥 상승 높이',h.pillarAirHeight,'월드 단위'],['화염기둥 보호',h.pillarProtectionSeconds,'초'],['사신 속박',.4,'초'],['사신 시야 방해 요청',f(a.visionSeconds),'초'],['암전 진입',f(v.fadeInSeconds),'초'],['완전 암전',f(v.blackoutSeconds),'초'],['밝아지는 시간',f(v.fadeOutSeconds),'초'],['암전 회복 보호',f(v.recoveryGrace),'초'],['주변 시야 반경 가로/세로',`${f(v.visibleWorldRadius.x)} / ${f(v.visibleWorldRadius.y)}`,'월드 단위'],['공격 주기(균열·기둥)',2,'초 주기, 앞 1.1초 피해 가능 / 뒤 0.9초 피해 비활성'],['피격 연출 파티클 수',20,'코드 고정 개수']
], '장애물 그림의 점멸은 제거된 상태입니다. 균열 피격 후 플레이어가 사라졌다 복귀하는 연출과 피격 무적 점멸은 별도입니다.');
add('광야: 사신과 배치 수치',['항목','값','설명'],[
 ['사신 추적',f(r.followSeconds),'초 / 화면 상단'],['공격 위치 고정·준비',f(r.lockSeconds),'초'],['추적 가로 속도',r.followSpeed,'월드 단위/초'],['사신 하강 속도',r.dashSpeed,'월드 단위/초'],['휘두르기 재생',f(r.attackAnimationSeconds),'초'],['공격 그림 수',r.slashFrames.length,'프레임'],['휘두르기 시작 인덱스',r.firstSwingFrame,'0부터 시작, 7번째 그림'],['상단 여백',f(r.topInset),'월드 단위'],['사신 공격 폭',4,'스폰 통로 4개 폭'],['박쥐 묶음 최대',globals.cave.maximumBatCount,'1~3개'],['동굴 고정 장애물 묶음 최대',globals.cave.maximumFixedCount,'1~2개'],['무덤 묶음 최대',globals.ruins.maximumTombCount,'2~4개'],['가시덤불 묶음 최대',globals.trial.maximumThornCount,'1~3개'],['화염기둥 배치', '2개 또는 2×2개','공간 부족 시 축소 가능'],['용암 배치',1,'한 생성 요청당 1개'],['일반 이후 스테이지 장애물 크기 배율',f(a.laterStageObstacleScale),'접근한다고 커지는 배율은 아님'],['폐유적 추가 크기 배율',f(globals.ruins.obstacleScale),'장애물 종류별 사용 경로 참고'],['폐유적 벽 두께 배율',f(globals.ruins.wallThicknessScale),'공통 크기 배율과 곱해짐'],['좌우 모래바람 기본 속도',a.sideSandSpeed,'월드 단위/초, 주기적 ±18% 변화'],['상단 모래바람 기본 속도',f(2.2*stages[0].p.hazardSpeedMultiplier),'월드 단위/초, 스크롤 상대 이동과 별도'],['상단 돌 기본 속도',f(3.1*stages[0].p.hazardSpeedMultiplier),'월드 단위/초, 주기적 ±18% 변화']
]);
add('광야: 현재 효과 표시',['항목','값'],[['보호막 불투명도',pct(globals.effects.shieldBubbleOpacity)],['기도 오라 불투명도',pct(globals.effects.prayerAuraOpacity)],['보호막 파괴 그림',globals.effects.shieldBreak.length],['보호막 유지/파괴 포함 그림',globals.effects.bubble.length],['기도 오라 그림',globals.effects.prayerAura.length],['머리 위 기도 표시 그림',globals.effects.prayer.length],['보호막 획득 효과 시간',f(globals.aura.acquireSeconds)],['주변 막 파괴 효과 시간',f(globals.aura.breakSeconds)]]);
add('가나 혼인잔치: 기본 플레이',['항목','값','근거'],[
 ['라운드 시간(초)',cana.round.duration,'Minigame2 / CanaRound'],['수동 이동속도',cana.player.moveSpeed,'CanaPlayer'],['가득 든 항아리 이동 배율',.5,'1 - 0.5 × 용량 비율'],['빈 항아리 / 가득 든 항아리 속도',`${cana.player.moveSpeed} / ${f(cana.player.moveSpeed*.5)}`,'특수 항아리는 무게 감속 없음'],['기본 / 큰 항아리 용량','5 / 10','특수 항아리는 무한 포도주'],['물 1단위 충전 시간(초)',f(cana.player.fillInterval),'연속 충전'],['기본 항아리 완충 시간(초)',f(cana.player.fillInterval*5),'빈 상태부터'],['청소 시간(초)',f(cana.player.cleaningDuration),'CanaPlayer'],['Q 스킬 쿨다운(초)',cana.player.skillCooldown,'CanaPlayer'],['대시 거리',cana.player.dashDistance,'월드 단위'],['대시 시간(초)',f(cana.player.dashDuration),'CanaPlayer'],['순간이동 근처 후보 수',cana.player.nearbyTeleportChoices,'장비 조건 있음'],['물→포도주 기도(초)',f(cana.jesus.prayerDuration),'CanaJesus'],['예수님 상호작용 거리',cana.jesus.interactionDistance,'월드 단위'],['손님 생성 시도 간격(초)',cana.spawner.spawnInterval,'빈 좌석·경로·일행 조건에 따라 실제 생성 거절/대기'],['일행 구성원 생성 간격(초)',f(cana.spawner.groupSpawnGap),'4명 일행'],['문제 손님 쓰레기 간격(초)',cana.spawner.troubleTrashInterval,'대기 중'],['좌석 수',cana.seats,'현재 씬'],['불만 게이지 그림 수',cana.complaintFrames,'현재 코드상 가득 찬 뒤 추가 불만 때 게임오버'],['하트 보상',cana.wallet.heartReward,'골드'],['웃음 보상',cana.wallet.smileReward,'골드'],['화남 보상',0,'골드'],['강화 가격',cana.shop.prices.join(' / '),'현재 씬 저장값'],['강화 최대 단계',cana.shop.maxTrainingLevel,'현재 씬 저장값']
], '저장된 골드·강화 진행도는 별도 PlayerPrefs 데이터입니다. 이 표는 초기 설정과 코드 계산 기준이며 실제 보유 재화·개인 진행도를 조사하지 않았습니다.');
const special=cana.spawner.specialChance;
add('가나 혼인잔치: 손님 종류 선택',['경과 시간','일반','대식가','성급한 손님','4인 일행','문제 손님'],Array.from({length:5},(_,n)=>[n===4?'240초 이후':`${n*60}~${(n+1)*60}초`,pct(1-n*special),...[1,2,3,4].map(i=>i<=n?pct(special):'미등장')]),'종류 해금 시각에는 해당 특수 손님이 보장 등장하는 로직이 별도로 있습니다. 표는 해금 이후 일반 무작위 추첨 기준입니다.');
add('가나 혼인잔치: 연결된 손님 프리팹',['프리팹','기본 이동속도','게이지 1칸(초)','게이지 수','기본 최대 대기(초)','초록 마지막 인덱스','반응(초)','출처'],cana.npcs.map(n=>[n.name,n.moveSpeed,n.secondsPerGauge,n.patienceFrames,f(n.secondsPerGauge*n.patienceFrames),n.lastGreenFrame,n.reactionSeconds,n.path]),'실행 시 대식가는 이동속도 ×0.5, 성급한 손님은 게이지 시간 ×0.5가 추가됩니다. 대식가는 1회 2잔·총 3회 주문, 일행은 총 4잔을 요청합니다.');
const primary=new Set(['JourneyContinuousWorld','JesusController','JordanScrollSettings','TemptationScrollSettings','JourneyStageLayout','NativityCutscene']);
const mainRows=sceneRows.filter(r=>primary.has(r.component)&&!r.property.includes('.'));
add('본편: 씬에 저장된 이동·타이밍 설정',['씬','컴포넌트','항목','값','오브젝트'],mainRows.map(r=>[r.scene,r.component,r.property,Number.isFinite(Number(r.value))?f(Number(r.value)):r.value,r.objectPath]),'같은 컴포넌트가 여러 씬에 있어도 값이 다르면 별도로 표시합니다. 해당 씬/단계가 실제 실행되는지는 실행 경로에 따라 달라집니다.');
add('전체 비프로토타입 씬: 상세 저장 수치',['씬','컴포넌트','인스펙터 속성','값','자료형','대표 오브젝트'],sceneRows.map(r=>[r.scene,r.component,r.property,Number.isFinite(Number(r.value))?f(Number(r.value)):r.value,r.type,r.objectPath]),'22개 씬의 숫자·열거형·토글 저장값을 조회했습니다. 좌표·색상 채널과 불필요한 참조 개수는 제외했으며, 동일 씬·컴포넌트·속성·값이 반복되면 대표 오브젝트 하나로 묶었습니다. 이것만으로 모든 항목이 실행 중 사용된다고 단정하지 않습니다.');
const codeRows=[];
for(const dir of ['Assets/Scripts/Minigames/Fasting','Assets/Scripts/Minigames/Cana','Assets/Scripts/CrossTheLine','Assets/Scripts/Home'])
for(const file of fs.readdirSync(path.join(root,dir)).filter(n=>n.endsWith('.cs'))){
 const source=dir+'/'+file;
 fs.readFileSync(path.join(root,source),'utf8').split(/\r?\n/).forEach((line,i)=>{
  if(line.trim().startsWith('//'))return;
  const fragments=line.split(';');
  fragments.forEach(text=>{
   if(!/\d/.test(text)||/new Vector[234]|Quaternion|anchor|fontSize|font=|Color\(|Color32\(|localScale|localPosition|sortingOrder|Create\(/i.test(text))return;
   if(!/const |(?:public|private).*\b(float|int)\b|Seconds|Speed|Damage|Chance|Interval|Duration|fever|Fever|Prayer|Health|Integrity|Progress|Balance|Fear|Time\.deltaTime|Time\.time|WaitForSeconds|Random\.(Range|value)|Score|Gold|trainingLevels|prices|Capacity|complaints/.test(text))return;
   codeRows.push([source,i+1,text.trim()]);
  });
 });
}
add('코드에 고정된 수치·계산식 찾아보기',['파일','줄','코드 발췌'],codeRows,'현재 소스에서 플레이 수치와 관련된 숫자·수식 후보를 추출했습니다. 코드 기본값·미사용 경로가 섞일 수 있으므로 위의 실제 프리팹/씬 저장값을 우선합니다. 고정 로직의 최종 의미는 해당 파일을 함께 읽어야 합니다.');
const oldFolder=path.join(root,'Docs/Balance/2026-11-22');const changes=[];
if(fs.existsSync(path.join(oldFolder,'fasting-snapshot.json'))){
 const old=JSON.parse(fs.readFileSync(path.join(oldFolder,'fasting-snapshot.json'),'utf8'));
 function flatten(x,p='',out={}){for(const [k,v]of Object.entries(x??{})){if(k==='instanceID')continue;const key=p?p+'.'+k:k;if(v&&typeof v==='object')flatten(v,key,out);else out[key]=typeof v==='number'?f(v):v;}return out;}
 for(let i=0;i<stages.length;i++){const before=old.stages?.[i]?.profile?JSON.parse(old.stages[i].profile):JSON.parse(fs.readFileSync(path.join(oldFolder,`stage-${i}-profile.json`),'utf8'));const b=flatten(before),n=flatten(stages[i].p);for(const k of new Set([...Object.keys(b),...Object.keys(n)]))if(b[k]!==n[k])changes.push([stages[i].p.displayName,k,b[k]??'없음',n[k]??'없음']);}
 for(const group of ['player','spawner','round','wallet','shop','jesus']){const oldCana=JSON.parse(fs.readFileSync(path.join(oldFolder,'cana-snapshot.json'),'utf8'));const b=flatten(JSON.parse(oldCana[group])),n=flatten(cana[group]);for(const k of new Set([...Object.keys(b),...Object.keys(n)]))if(b[k]!==n[k]&&!/^m_/.test(k))changes.push(['가나 '+group,k,b[k]??'없음',n[k]??'없음']);}
}
add('전일 스냅샷과 비교',['범위','항목','11월 22일','11월 23일'],changes.length?changes:[['광야 4개 프로필·가나 주요 설정','조사한 주요 저장 수치','동일','동일']], '이 비교는 확인한 광야 프로필과 가나 주요 설정에 한정합니다. 프로젝트 전체의 모든 코드와 배치가 동일하다는 뜻은 아닙니다.');
const repr=x=>Array.isArray(x)?x.map(v=>typeof v==='number'?f(v):v).join(' / '):x&&typeof x==='object'?Object.entries(x).map(([k,v])=>k+'='+f(v)).join(' / '):f(x);
add('광야: Advanced Settings 에셋의 전체 바탕값',['설정 항목',...stages.map(s=>s.p.displayName)],Object.keys(stages[0].a).filter(k=>k!=='places').map(k=>[k,...stages.map(s=>repr(s.a[k]))]),'이 표는 고급 에셋에 저장된 바탕값입니다. 기도 목표·충전량·확률·이동 등 Fasting Stage Configuration에서 덮어쓰는 항목의 실제 적용값은 앞쪽 스테이지별 표를 우선합니다. 숫자가 서로 다르더라도 양쪽이 동시에 적용되는 것이 아닙니다.');
const eventCount=cana.shop.onTrainSelected?.m_PersistentCalls?.m_Calls?.length??0;
add('확인된 미연결 수치',['항목','현재 확인 내용'],[['가나 강화 단계별 능력 증가율',`구매 이벤트의 저장된 연결 ${eventCount}개. 단계 증가·저장은 있으나 검색한 코드에는 강화 단계별 이동·청소·충전 수치를 적용하는 계산식이 없습니다.`]],'증가율을 임의로 만들어 표에 넣지 않았습니다. 이 요청은 조사이므로 게임 로직은 수정하지 않았습니다.');
const esc=x=>String(typeof x==='number'?f(x):x??'—').replaceAll('&','&amp;').replaceAll('<','&lt;').replaceAll('>','&gt;').replaceAll('"','&quot;');
const html=`<!doctype html><html lang="ko"><meta charset="utf-8"><title>CrossTheLine 현재 수치표 · 2026-11-23</title><style>body{font:15px/1.6 'Malgun Gothic',sans-serif;background:#f3f5f8;color:#172b42;max-width:1450px;margin:32px auto;padding:0 20px}h1{font-size:28px}h2{font-size:21px;margin-top:36px}p{max-width:1000px}.note{background:#eaf0f8;padding:12px 16px;border-left:4px solid #3570a8}.search{position:sticky;top:0;background:#f3f5f8ee;padding:14px 0;z-index:2}input{font:inherit;padding:10px;width:min(600px,90%);border:1px solid #9aabc0;border-radius:5px}.wrap{overflow:auto;background:white}table{border-collapse:collapse;width:100%;font-size:14px}th,td{border:1px solid #d8e0e8;padding:9px 11px;text-align:left;vertical-align:top}th{background:#203d5b;color:white;white-space:nowrap}tr:nth-child(even){background:#f4f8fc}td:last-child{overflow-wrap:anywhere}nav a{display:inline-block;margin:4px 12px 4px 0;color:#275f95}details{margin-top:30px}summary{font-size:21px;font-weight:700;cursor:pointer}footer{padding:30px;color:#526475}</style><h1>CrossTheLine 현재 수치표</h1><p>조사 기준: 2026년 11월 23일(한국 시간). Unity 6000.6.0f1. 본편·광야·가나와 연결된 프리팹을 읽기 전용으로 조사했습니다.</p><p class="note">원본 게임 설정·씬·이미지를 수정하지 않았습니다. Play 실행이나 실제 시간당 스폰 측정은 하지 않았습니다. 설정값·코드로 계산한 값·실행 조건을 구분해 표시합니다. 좌표와 색상 등 배치 수치는 주요 표에서 제외했습니다. 이 파일의 수치를 바꿔도 게임에는 반영되지 않습니다.</p><nav>${sections.map((s,i)=>`<a href="#s${i}">${esc(s.title)}</a>`).join('')}</nav><div class="search"><input id="q" placeholder="항목·파일·스테이지 검색 (예: fever, 사신, CanaPlayer)"><span id="status"></span></div>${sections.map((s,i)=>`${i>=8?`<details id="s${i}"><summary>${esc(s.title)} (${s.rows.length}행)</summary>`:`<section id="s${i}"><h2>${esc(s.title)}</h2>`}${s.note?`<p class="note">${esc(s.note)}</p>`:''}<div class="wrap"><table><thead><tr>${s.headers.map(h=>`<th>${esc(h)}</th>`).join('')}</tr></thead><tbody>${s.rows.map(r=>`<tr>${r.map(x=>`<td>${esc(x)}</td>`).join('')}</tr>`).join('')}</tbody></table></div>${i>=8?'</details>':'</section>'}`).join('')}<footer>원본 스냅샷: Docs/Balance/2026-11-23. 모든 속성의 실행 적용 여부를 Play로 검증한 보고서는 아닙니다.</footer><script>const q=document.getElementById('q');q.addEventListener('input',()=>{const term=q.value.toLowerCase();let n=0;document.querySelectorAll('tbody tr').forEach(r=>{r.hidden=!!term&&!r.textContent.toLowerCase().includes(term);if(!r.hidden)n++});document.querySelectorAll('details').forEach(d=>{if(term)d.open=true});document.getElementById('status').textContent=' '+n+'행 표시';});</script></html>`;
fs.writeFileSync(path.join(folder,'CrossTheLine-Balance-20261123.html'),html);
fs.writeFileSync(path.join(folder,'balance-tables.json'),JSON.stringify({date:'2026-11-23',sections},null,2));
console.log(JSON.stringify({report:'Docs/Balance/2026-11-23/CrossTheLine-Balance-20261123.html',sceneCount:new Set(sceneRows.map(r=>r.scene)).size,serializedRows:sceneRows.length,codeRows:codeRows.length,tableRows:sections.reduce((a,s)=>a+s.rows.length,0),changes,stages:stages.map(s=>({name:s.p.displayName,prayer:s.p.prayerTarget,scroll:s.p.scrollSpeed*s.a.scrollSpeedMultiplier})),canaNPCs:cana.npcs,seats:cana.seats,complaints:cana.complaintFrames},null,2));
