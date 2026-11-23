import fs from 'node:fs';
const dir='Docs/BalanceTests/2026-11-23/TenMinutes';
const request=JSON.parse(fs.readFileSync(`${dir}/request.json`,'utf8').replace(/^\uFEFF/,''));
const data=JSON.parse(fs.readFileSync(`${dir}/${request.label}.json`));
const mean=a=>a.reduce((s,x)=>s+x,0)/a.length;
const fmt=s=>{const n=Math.round(s);return `${Math.floor(n/60)}분 ${n%60}초`;};
const clear=data.filter(x=>x.outcome==='clear'),planned=[120,120,180,180];
const stages=planned.map((goal,i)=>{
 const completed=data.flatMap(t=>t.stages).filter(s=>s.stage===i+1&&s.completed);
 return {stage:i+1,target:request.targets[i],plannedSeconds:goal,completed:completed.length,meanSeconds:mean(completed.map(s=>s.seconds)),minSeconds:Math.min(...completed.map(s=>s.seconds)),maxSeconds:Math.max(...completed.map(s=>s.seconds)),meanGateAt:mean(completed.map(s=>s.gateAt))};
});
const summary={label:request.label,targets:request.targets,trials:data.length,clears:clear.length,totalMeanSeconds:mean(clear.map(t=>t.seconds)),totalMinSeconds:Math.min(...clear.map(t=>t.seconds)),totalMaxSeconds:Math.max(...clear.map(t=>t.seconds)),stages};
fs.writeFileSync(`${dir}/summary.json`,JSON.stringify(summary,null,2));
let rounds=fs.readdirSync(dir).filter(x=>/^round\d+\.json$/.test(x)).map(file=>{
 const a=JSON.parse(fs.readFileSync(`${dir}/${file}`));return `<li>${file}: ${a.length}회 · 목표 ${a[0].targets.join(' / ')} · 완주 ${a.filter(t=>t.outcome==='clear').length}회</li>`;
}).join('');
const html=`<!doctype html><html lang="ko"><meta charset="utf-8"><title>광야 40일 평균 10분 조정</title><style>body{font:16px/1.7 system-ui,sans-serif;max-width:1050px;margin:40px auto;padding:0 24px;background:#faf9f6;color:#222}table{border-collapse:collapse;background:white;width:100%}td,th{border:1px solid #ccc;padding:10px;text-align:left}th{background:#eee9dd}code{background:#eee}</style>
<h1>광야 40일: 평균 2·2·3·3분을 위한 기도 목표 조정</h1><p>2026-11-23 · 실제 Unity Play 모드에서 연속 자동 플레이 측정</p>
<p>총 평균 ${fmt(summary.totalMeanSeconds)} · 완주 ${summary.clears}/${summary.trials}회 · 최단 ${fmt(summary.totalMinSeconds)} · 최장 ${fmt(summary.totalMaxSeconds)}</p>
<table><tr><th>스테이지</th><th>Prayer Target</th><th>원하는 평균</th><th>관측 평균</th><th>관측 최단~최장</th></tr>${stages.map(s=>`<tr><td>${s.stage}</td><td>${s.target}</td><td>${fmt(s.plannedSeconds)}</td><td>${fmt(s.meanSeconds)}</td><td>${fmt(s.minSeconds)} ~ ${fmt(s.maxSeconds)}</td></tr>`).join('')}</table>
<h2>변경 범위</h2><p>각 스테이지 프리팹의 <code>Fasting Stage Configuration → 기도와 말씀 아이템 충전 → Prayer Target</code>만 수정했습니다. 충전 속도·기도 중 이동 15%·기본 이동 6.6·스폰·피해·아이템 충전 비율·피버·상태이상은 유지했습니다. 원본 프리팹과 .meta는 Backup 폴더에 보관했습니다. 전환을 강제로 지연시키는 타이머는 추가하지 않았습니다.</p>
<h2>검사 방법</h2><p>매 시도 1스테이지에서 시작해 4스테이지까지 이어서 진행했습니다. 스테이지마다 체력을 임의로 초기화하지 않았습니다. 위험을 예측해 이동·기도 해제를 결정하는 같은 자동 조작 정책을 사용했습니다. 실제 게임의 Tick, 스폰·충돌·아이템·기도·출구·상태 전환을 1/30초 간격으로 호출했고 상한은 테스트에만 900초를 뒀습니다. 기도 완료 후 전환선까지 이동하는 시간과 0.6초 장면 전환도 구간 시간에 포함됩니다.</p><ul>${rounds}</ul>
<h2>해석과 한계</h2><p>이는 자동 조작 정책에서의 평균이며 사람의 실제 평균 시간을 보장하지 않습니다. 난수·회피 숙련도·기도 유지·말씀과 불꽃 획득 때문에 개별 완주 시간은 달라집니다. 전체 평균은 완주한 시도만 계산했고 실패 횟수는 별도로 표시합니다. 같은 시작 난수라도 이후 이동과 획득에 따라 배치가 달라질 수 있습니다. 본문 모든 챕터나 그래픽을 검수한 결과가 아닙니다. 초기 추정에 사용한 시작 난수가 최종 측정에도 일부 포함되므로 독립 사용자 검증으로 해석하면 안 됩니다.</p>
<h2>시도별 시간</h2><table><tr><th>시작 난수 번호</th><th>결과</th><th>1스테이지</th><th>2스테이지</th><th>3스테이지</th><th>4스테이지</th><th>전체</th></tr>${data.map(t=>`<tr><td>${t.seed}</td><td>${t.outcome}</td>${[1,2,3,4].map(i=>{const s=t.stages.find(s=>s.stage===i);return `<td>${s?fmt(s.seconds):'-'}</td>`;}).join('')}<td>${fmt(t.seconds)}</td></tr>`).join('')}</table>
<p>원자료: ${request.label}.json / summary.json / applied.json / final-state.json / ${request.label}-restored.json</p></html>`;
fs.writeFileSync(`${dir}/TenMinute-Balance-Test.html`,html);
console.log(JSON.stringify(summary,null,2));
