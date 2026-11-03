# Unity 체크인 → GitHub 자동 복사

대상: https://github.com/hyunmin1128/CrossTheLine (비공개)

Unity Version Control은 지금처럼 사용합니다. 이 PC의 원본 작업 폴더에서 **체크인 성공** 또는 **실제 변경 받기 성공** 이벤트가 발생하면 업로드를 요청합니다. 주기 확인, 로그인 타이머, 상주 감시 프로그램은 없습니다. 시작 기준은 cs:97이며, 그 이후 `/main` 체크인은 번호 순서대로 GitHub `main` 커밋으로 복사합니다. 체크인 전 수정 파일은 올리지 않습니다. 기존 cs:0~96의 이력을 이전하는 작업은 아닙니다. 새로 받을 변경이 없는 no-op 업데이트에서는 UVCS가 완료 훅을 생략할 수 있습니다.

GitHub의 변경을 Unity로 가져오지 않는 단방향 백업입니다. 자동 체크인이나 원본 워크스페이스 업데이트는 하지 않습니다. 형·노트북의 `/main` 체크인은 이 PC에서 변경을 받거나 다음 체크인을 한 뒤 복사됩니다. 이 PC가 켜져 있기만 해서는 업로드하지 않습니다. 다른 브랜치는 포함하지 않습니다.

포함: 체크인된 Assets, Packages, ProjectSettings, AgentScripts, Tools와 자동 생성 README/.gitignore.
제외: Docs(백업·QA·미리보기), output, ProfilerCaptures, Library, Temp, Logs, Builds, UserSettings 및 개인 인증 파일 패턴. 원본 ZIP 백업이 GitHub 일반 파일 제한을 초과하므로 전체 UVCS 이력을 직접 GitSync하지 않고 별도 워크스페이스의 체크인 스냅샷을 복사합니다. 향후 포함된 파일이 100,000,000바이트 이상이면 자동 업로드를 멈추고 로그에 기록합니다. 해당 시점에 LFS 적용 여부를 결정해야 합니다.

별도 작업 폴더: `D:\Unity\CrossTheLine-GitHubMirror.private`
- `source`: 내려받은 체크인 전용 UVCS 워크스페이스. 직접 편집하지 마세요.
- `git`: 생성된 Git 복사본. 직접 편집하지 마세요.
- `last-pushed.txt`: 마지막 성공 체크인
- `sync.log`: 성공·오류 기록

예약 작업 `CrossTheLine-UnityToGitHub`는 타이머가 없는 요청 시 실행 작업입니다. 완료 훅은 요청 파일을 남기고 숨김 실행을 요청한 뒤 즉시 반환합니다. 업로드 실패가 Unity 체크인·받기 성공을 취소하거나 기다리게 하지 않습니다. 네트워크·인증 문제로 실패하면 요청을 보존하고 **다음 체크인/받기 이벤트**에서 재시도합니다. 다음 로그인만으로 자동 재시도하지 않습니다. 토큰은 파일이나 스크립트에 저장하지 않고 기존 Git Credential Manager를 사용합니다. GitHub 브랜치가 다른 곳에서 수정돼 충돌하면 강제 푸시하지 않고 멈춥니다.

콘솔 창 깜박임 방지: 예약 작업은 PowerShell을 직접 실행하지 않고 `wscript.exe` → `Run-Hidden.vbs`로 시작합니다. 이 실행기는 PowerShell을 처음부터 숨김 상태로 실행하고 종료 코드를 예약 작업에 돌려줍니다. 동기화 오류는 기존 `sync.log`에서 확인합니다. Windows 보안 설정이나 시스템 실행 정책은 변경하지 않습니다.

연결: UVCS 서버의 `CTL-GitHub-Checkin` (`after-clientcheckin`)과 `CTL-GitHub-Update` (`after-update`) 클라이언트 훅입니다. 로컬 client.conf의 두 전용 TriggerVariable이 있는 이 PC만 실행합니다. 설정이 없는 PC에서 실제 업데이트해도 오류 없이 건너뛰는 것을 검증했습니다. 원본 작업 폴더와 `/main`을 확인하고, 업로드용 별도 워크스페이스나 업로드 작업 자신의 업데이트에서는 업로드를 다시 요청하지 않습니다. `requests` 폴더로 겹친 이벤트를 보존하며 예약 작업은 Queue 방식으로 처리합니다.

설치/다시 연결:
```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/GitHubMirror/Install-EventHooks.ps1
```
`Install-AutoSync.ps1`은 타이머 없는 실행 작업만 만듭니다. 전체 이벤트 연결은 위 도구로 설치합니다. UVCS 훅에는 슬래시 옵션을 넣지 않습니다. UVCS가 경로로 해석해 변환할 수 있기 때문입니다.

로그: `hook.log`는 완료 훅 호출, `events.log`는 요청 처리 완료, `sync.log`는 업로드 성공·실패를 기록합니다. 이전 타이머 방식 검증 파일은 당시 기록이며 현재 설정은 이 문서를 따릅니다.

공식 문서: https://docs.unity.com/en-us/unity-version-control/triggers/concepts/detailed-trigger-reference
https://docs.unity.com/en-us/unity-version-control/triggers/concepts/client-variables

수동 실행(프로젝트 폴더 PowerShell):

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/GitHubMirror/Sync-GitHub.ps1 -Status
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/GitHubMirror/Sync-GitHub.ps1 -Push
```

업로드 없이 복사본만 준비하려면 `-Push`를 빼세요. 실행 정책 옵션은 해당 프로세스에만 적용되며 시스템 정책은 변경하지 않습니다.

일시 중지:
```powershell
Disable-ScheduledTask -TaskName CrossTheLine-UnityToGitHub
```
재개:
```powershell
Enable-ScheduledTask -TaskName CrossTheLine-UnityToGitHub
```

프로젝트 경로를 옮기면 작업 경로도 갱신해야 합니다. 이 도구 파일은 아직 원본 UVCS에 자동 체크인하지 않았으므로 다음 일반 체크인 때 포함해주세요. Unity Package나 게임 코드는 변경하지 않았습니다.
