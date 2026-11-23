import fs from 'node:fs';
const dir='Docs/BalanceTests/2026-11-23';
const main=JSON.parse(fs.readFileSync(`${dir}/trials-6seeds.json`));
const fine=JSON.parse(fs.readFileSync(`${dir}/trials-2seeds-60hz.json`));
const mechanics=JSON.parse(fs.readFileSync(`${dir}/mechanics.json`));
const names=['현재 설정','기도 중 이동 30%','이동 30% + 목표 196 / 216','위 설정 + 기도 금지 2초'];
const mean=(a,k)=>a.length?a.reduce((s,x)=>s+x[k],0)/a.length:0;
function summarize(data){return names.flatMap((name,arm)=>[0,1].map(policy=>{
  const rows=data.filter(t=>t.arm===arm&&t.policy===policy),clear=rows.filter(t=>t.outcome==='clear');
  return {arm,name,policy:policy?'위험 시 기도 해제':'계속 기도',trials:rows.length,clear:clear.length,dead:rows.filter(t=>t.outcome==='dead').length,timeout:rows.filter(t=>t.outcome==='timeout').length,clearSeconds:mean(clear,'seconds'),hits:mean(rows,'hits'),holdPercent:100*rows.reduce((s,x)=>s+x.heldSeconds,0)/rows.reduce((s,x)=>s+x.seconds,0),blockedSeconds:mean(rows,'blockedSeconds')};
}));}
const summary={date:'2026-11-23',main:summarize(main),fine:summarize(fine),mechanics};
fs.writeFileSync(`${dir}/summary.json`,JSON.stringify(summary,null,2));
function table(rows){return '<table><thead><tr><th>설정</th><th>조작 정책</th><th>성공 / 시도</th><th>사망</th><th>180초 초과</th><th>성공한 시도 평균 시간</th><th>평균 피격 횟수</th><th>기도키 유지 비율</th></tr></thead><tbody>'+rows.map(r=>`<tr><td>${r.name}</td><td>${r.policy}</td><td>${r.clear} / ${r.trials}</td><td>${r.dead}</td><td>${r.timeout}</td><td>${r.clearSeconds.toFixed(1)}초</td><td>${r.hits.toFixed(2)}</td><td>${r.holdPercent.toFixed(1)}%</td></tr>`).join('')+'</tbody></table>';}
const html=`<!doctype html><html lang="ko"><meta charset="utf-8"><title>CrossTheLine 밸런스 비교 검사</title><style>body{font:16px/1.65 system-ui,sans-serif;max-width:1250px;margin:40px auto;padding:0 24px;color:#222;background:#faf9f6}table{border-collapse:collapse;width:100%;font-size:14px;background:white}td,th{border:1px solid #ccc;padding:9px;text-align:left}th{background:#eee9dd}h1,h2{line-height:1.3}.note{padding:16px;border-left:4px solid #74623c;background:#eee9dd}img{width:min(960px,100%)}code{background:#eee;padding:2px}pre{white-space:pre-wrap;background:#eee;padding:18px}</style>
<h1>CrossTheLine 밸런스 비교 검사</h1><p>2026-11-23 · Unity 6000.6.0f1 · 광야 40일 미니게임, 가나의 강화 연결과 불만 실패 조건</p>
<p class="note">게임 값을 영구 변경하지 않은 테스트 보고서입니다. 예비 64회 + 본 비교 192회 + 60Hz 교차 검사 64회, 총 320회 자동 실행했습니다. 각 시도는 한 스테이지를 초기 체력 5칸으로 시작한 독립 검사입니다. 본편 전체 또는 40일 연속 완주 검사가 아닙니다.</p>
<h2>검사 방법과 한계</h2><p>실제 Play 모드에서 게임의 Tick·충돌·기도·스폰·아이템 처리 코드를 호출했습니다. 본 비교는 6개 시작 난수 × 4개 스테이지 × 2개 조작 정책 × 4개 설정입니다. 0.1초 고정 간격으로 빠르게 비교한 뒤, 2개 시작 난수에서 1/60초 간격으로 다시 검사했습니다. 수동 키보드 조작이나 사람의 체감 평가를 대신하지 않습니다. 조작 정책은 약 0.2초마다 현재 보이는 장애물의 예상 위치를 보고 9방향 중 이동을 고릅니다. 위험 판단 정책은 기도를 해제할 수도 있습니다. 시야 제한도 제한적으로 반영했지만 실제 사람이 암전을 기억하고 대응하는 능력을 재현하지 않습니다.</p><p>같은 시작 난수를 사용해도 이동 경로·수집·활성 프레임에 따라 이후 난수 소비와 장애물 배치가 달라질 수 있습니다. 독립적인 통계 표본 320명으로 해석하면 안 됩니다. 성공 시간은 성공한 시도만의 평균이므로 설정 간 생존률이 다를 때 시간만 직접 비교하면 편향됩니다. 한 시도 제한 180초는 테스트 종료 조건이며 게임에 시간 제한을 추가하지 않았습니다.</p>
<h2>본 비교 192회</h2>${table(summary.main)}
<h2>60Hz 교차 검사 64회</h2>${table(summary.fine)}
<h2>판단</h2><p>기도 중 이동 30%는 현재 15%보다 회피 여유를 늘리는 후보입니다. 다만 계속 기도해도 살아남기 쉬워지는 방향이므로 기도 해제 판단을 없애지 않는지 사람이 확인해야 합니다. 후반 목표 234→196, 252→216은 후반의 길어지는 진행을 줄이는 후보입니다. 기도 금지를 동시에 2초로 낮추는 것은 추가로 난이도를 완화하므로 우선 유지하고 비교하는 편을 권합니다. 스폰·피해·피버 값을 함께 바꾸지 않고 한 단계씩 평가해야 원인을 알 수 있습니다. 이 판단은 최적값 확정이 아닙니다.</p>
<h2>개별 기능 검사</h2><p>기도 중 오른쪽 입력 1초 이동 거리는 15%에서 0.99, 30%에서 1.98입니다. 화면 하단에서도 좌우 이동을 확인했습니다. 연속 피격 요청은 첫 번째만 체력 1칸 감소하고 즉시 두 번째 요청은 무적으로 거부됐습니다. 피버는 6초 시작 → 기도하지 않아도 3초 후 약 3초 → 불꽃 획득 시 약 5초 → 추가 획득 시 최대 6초 → 자동 종료를 확인했습니다. 기도 금지 3초/2초도 회복을 확인했습니다. 0.1초 검사에서 3초 종료가 3.1초로 관측된 것은 부동소수점 잔여와 검사 간격의 한계입니다.</p>
<h2>가나에서 확인한 문제</h2><p><code>CanaDissatisfaction.cs:29</code>에서 증가 전에 최대치를 검사해 불만 11칸이 모두 찬 시점에는 실패하지 않고 12번째에 실패했습니다. <code>CanaShop.cs</code>는 강화 레벨·저장·게이지 변경과 이벤트 호출을 수행하지만, 전체 C# 검색상 레벨을 읽어 능력치에 적용하는 다른 코드가 없고 해당 씬의 지속 이벤트 연결도 0개였습니다. 현재 강화 설명이 기대하는 실제 능력치 증가 연결이 빠져 있습니다. 미리보기 씬에서 실패 조건만 재현했으며 구매·저장 데이터는 변경하지 않았습니다.</p>
<h2>화면과 복구</h2><img src="baseline-screen.png" alt="현재 설정의 게임 화면"><p>현재 960×540 캡처에서 예수님·하트·기도 게이지 표시를 확인했습니다. 자동 검사에서 실제 키보드·전체 애니메이션 프레임·모든 시각 효과를 검수한 것은 아닙니다. 검사 시작 이후 콘솔의 새 오류는 없었습니다. 기존 TextMeshPro NullReferenceException 기록은 별도 문제로 남아 있으며 이번 검사에서 수정하지 않았습니다. 시험용 프리팹 값과 고급 설정 에셋, 테스트가 건드린 진행 기록 및 결과 파일은 복구했습니다. 원본 씬을 저장하지 않았습니다.</p>
<h2>기능 검사 원자료</h2><pre>${JSON.stringify(mechanics,null,2)}</pre><p>원자료: trials-6seeds.json / trials-2seeds-60hz.json / mechanics.json / restored.json / final-state.json</p></html>`;
fs.writeFileSync(`${dir}/CrossTheLine-Balance-Test.html`,html.replace('</html>','<h2>남은 글꼴 경고</h2><p>Play 종료 후 Heading의 크기를 계산하는 FastingHeaderFit.OnEnable에서 LiberationSans SDF가 한글을 찾지 못한다는 경고가 다시 발생했습니다. 실행 중 캡처에서는 한글이 표시됐지만 에디터에서의 폰트 초기화 순서 문제 가능성이 남아 있습니다. 이번 밸런스 검사에서는 수정하지 않았습니다.</p></html>'));
console.log(JSON.stringify({main:summary.main,fine:summary.fine},null,2));
