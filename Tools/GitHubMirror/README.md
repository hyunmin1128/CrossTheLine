# Unity 체크인 → GitHub 자동 복사

대상: https://github.com/hyunmin1128/CrossTheLine (비공개)

Unity Version Control은 지금처럼 사용합니다. 이 PC에 로그인한 동안 Windows 작업 `CrossTheLine-UnityToGitHub`가 1분마다 `/main`의 새 체크인을 확인합니다. 시작 기준은 cs:97이며, 그 이후 체크인은 번호 순서대로 GitHub `main` 커밋으로 복사합니다. 체크인 전 수정 파일은 올리지 않습니다. 기존 cs:0~96의 이력을 이전하는 작업은 아닙니다.

GitHub의 변경을 Unity로 가져오지 않는 단방향 백업입니다. 자동 체크인이나 원본 워크스페이스 업데이트는 하지 않습니다. 팀원의 `/main` 체크인도 서버에 올라온 뒤 이 PC가 실행 중이면 복사됩니다. 다른 브랜치는 포함하지 않습니다.

포함: 체크인된 Assets, Packages, ProjectSettings, AgentScripts, Tools와 자동 생성 README/.gitignore.
제외: Docs(백업·QA·미리보기), output, ProfilerCaptures, Library, Temp, Logs, Builds, UserSettings 및 개인 인증 파일 패턴. 원본 ZIP 백업이 GitHub 일반 파일 제한을 초과하므로 전체 UVCS 이력을 직접 GitSync하지 않고 별도 워크스페이스의 체크인 스냅샷을 복사합니다. 향후 포함된 파일이 100,000,000바이트 이상이면 자동 업로드를 멈추고 로그에 기록합니다. 해당 시점에 LFS 적용 여부를 결정해야 합니다.

별도 작업 폴더: `D:\Unity\CrossTheLine-GitHubMirror.private`
- `source`: 내려받은 체크인 전용 UVCS 워크스페이스. 직접 편집하지 마세요.
- `git`: 생성된 Git 복사본. 직접 편집하지 마세요.
- `last-pushed.txt`: 마지막 성공 체크인
- `sync.log`: 성공·오류 기록

예약 작업은 현재 사용자 권한이며 PC가 꺼져 있거나 로그아웃 상태에서는 실행하지 않습니다. 다음 로그인 시 밀린 체크인을 올립니다. 네트워크·인증 문제로 실패하면 다음 실행에서 재시도합니다. 토큰은 파일이나 스크립트에 저장하지 않고 기존 Git Credential Manager를 사용합니다. GitHub 브랜치가 다른 곳에서 수정돼 충돌하면 강제 푸시하지 않고 멈춥니다.

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
