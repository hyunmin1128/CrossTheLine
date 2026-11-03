# 수동 검증 도구

이 폴더는 Unity의 Assets 밖에 있어 자동으로 컴파일되지 않는다.
현재 게임 및 에디터 API를 사용하는 진단 스크립트만 남겼다. 과거 206개 도구 전체 원본은 `Docs/Backups/AssetCleanup-20261028/AgentScripts-before.zip`에 있다.

실행 예: Unity Pipeline의 `run_script`에 `Tools/Unity/Validation/ProjectIntegrityTests.cs`와 엔트리 `ProjectIntegrityTests.Run`을 전달한다.
Play 모드를 요구하는 도구는 먼저 게임을 실행한다. 그림 로딩은 씬에 연결된 `JourneyAssetLibrary`를 사용한다.

일반적인 정리 후 확인은 Unity 메뉴 `Tools > CrossTheLine > Cleanup > Verify All Chapters`로 실행할 수 있다.
폴더 구조는 `Docs/PROJECT_STRUCTURE.md`를 참고한다.
