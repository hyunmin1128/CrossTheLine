import fs from 'node:fs';
const dir='Docs/BalanceTests/2026-11-23/TargetOnly';
const trials=JSON.parse(fs.readFileSync(`${dir}/continuous-trials.json`));
const fmt=s=>`${Math.floor(s/60)}분 ${Math.round(s%60)}초`;
const avg=a=>a.reduce((s,x)=>s+x,0)/a.length;
const summaries=[0,1].map(arm=>{
 const a=trials.filter(t=>t.arm===arm),clear=a.filter(t=>t.outcome==='clear'),times=clear.map(t=>t.seconds).sort((x,y)=>x-y);
 return {arm,trials:a.length,clears:clear.length,deaths:a.filter(t=>t.outcome==='dead').length,timeouts:a.filter(t=>t.outcome==='timeout').length,reached180:a.filter(t=>t.at180).length,within180:clear.filter(t=>t.seconds<=180).length,min:times.length?times[0]:null,max:times.length?times.at(-1):null,average:times.length?avg(times):null,median:times.length?(times[Math.floor((times.length-1)/2)]+times[Math.floor(times.length/2)])/2:null};
});
const result={date:'2026-11-23',scope:'Four-stage continuous automatic gameplay at 30 Hz; adaptive prayer; six start seeds; unchanged movement, spawning, damage and status settings',summaries};
fs.writeFileSync(`${dir}/summary.json`,JSON.stringify(result,null,2));
const rows=trials.map(t=>`<tr><td>${t.seed+1}</td><td>${t.arm?'목표만 감소':'이전 목표'}</td><td>${t.outcome}</td><td>${fmt(t.seconds)}</td><td>${t.at180?`${t.at180.stage}스테이지 · 체력 ${t.at180.health} · 기도 ${t.at180.prayer.toFixed(1)}/${t.at180.target}`:'3분 전에 종료'}</td><td>${t.hits}</td><td>${t.stages.map(s=>`${s.stage}: ${fmt(s.seconds)}`).join(' / ')}</td></tr>`).join('');
const html=`<!doctype html><html lang="ko"><meta charset="utf-8"><title>기도 목표만 감소: 3분 플레이 검사</title><style>body{font:16px/1.7 system-ui,sans-serif;max-width:1150px;margin:40px auto;padding:0 24px;background:#faf9f6;color:#222}table{border-collapse:collapse;width:100%;background:white}th,td{padding:9px;border:1px solid #ccc;text-align:left;font-size:14px}th{background:#eee9dd}pre{white-space:pre-wrap;background:#eee;padding:16px}</style>
<h1>기도 목표만 감소한 설정의 연속 플레이 검사</h1><p>2026-11-23 · 실제 Unity Play 모드에서 게임 Tick과 충돌·수집·상태·스테이지 전환 로직을 자동 실행했습니다.</p>
<p>영구 적용: 3스테이지 Prayer Target 234→196, 4스테이지 252→216. 프리팹 원본과 비교해 이 두 줄만 변경됐습니다. 1·2스테이지는 144·160, 기도 중 이동 15%, 기본 이동 6.6, 스폰·피해·상태이상·피버는 유지했습니다.</p>
<h2>시험 방법</h2><p>6개 시작 난수에서 이전 목표와 새 목표를 각각 한 번씩, 총 12번 검사했습니다. 1스테이지부터 4스테이지까지 연속으로 진행했고 스테이지 진입마다 체력을 임의로 초기화하지 않았습니다. 위험을 예측해 이동하고 기도를 해제하는 동일한 자동 조작 정책을 사용했습니다. 계산 간격은 1/30초, 한 시도의 상한은 600초입니다. 게임에 600초 제한을 추가한 것은 아닙니다. 테스트 후 임시 비교 값·진행 저장·통계 파일을 복구했습니다.</p>
<h2>결과</h2><table><tr><th>설정</th><th>완주 / 시도</th><th>3분 이상 진행</th><th>3분 이내 완주</th><th>완주 최단</th><th>완주 평균</th><th>완주 최장</th></tr>${summaries.map(s=>`<tr><td>${s.arm?'목표만 감소':'이전 목표'}</td><td>${s.clears}/${s.trials}</td><td>${s.reached180}/${s.trials}</td><td>${s.within180}/${s.trials}</td><td>${s.min===null?'없음':fmt(s.min)}</td><td>${s.average===null?'없음':fmt(s.average)}</td><td>${s.max===null?'없음':fmt(s.max)}</td></tr>`).join('')}</table>
<p>3분 이상 플레이할 분량인지와 3분 안에 완주할 수 있는지는 다릅니다. 이 시험은 사람의 체감 난이도나 최단 기록을 증명하지 않습니다. 난수 초기값이 같아도 이동·획득·활성 프레임에 따라 이후 장애물 배치는 달라질 수 있습니다. 조작 정책과 시야 제한도 사람을 완벽히 재현하지 않습니다.</p>
<p>아이템·버프·피격·기도 중단을 제외한 기본 기도 시간의 합은 144/2.05 + 160/2.3 + 196/2.45 + 216/2.4 ≈ 310초입니다. 따라서 기도 목표만 이번 수준으로 줄였다고 전체가 3분이 되지는 않습니다. 성령 불꽃과 말씀 수집은 단축시키고 회피·기도 준비·상태이상·전환선 이동은 늘릴 수 있습니다. 추가 밸런스 값은 변경하지 않았습니다.</p>
<h2>시도별 기록</h2><table><tr><th>시작 난수 번호</th><th>설정</th><th>결과</th><th>전체 시간</th><th>180초 시점</th><th>피격</th><th>스테이지별 시간</th></tr>${rows}</table>
<p>원자료: continuous-trials.json / summary.json / restored.json / final-state.json. 기존 글꼴 문제는 별도이며, 이번 작업에서는 글꼴·UI·이미지를 수정하지 않았습니다.</p></html>`;
fs.writeFileSync(`${dir}/TargetOnly-ThreeMinute-Test.html`,html);
console.log(JSON.stringify(result,null,2));
