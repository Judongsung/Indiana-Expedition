# Windows Graphics Capture 화면 테스트

Indiana Expedition의 Luna UI는 프로젝트 전용 Windows Graphics Capture(WGC) 도구로 실제 창 표면을 PNG로 캡처한다. 테스트 창은 포커스를 얻지 않고 Z-order 맨 뒤로 이동하므로 키보드·마우스 작업을 가로채거나 현재 창을 덮지 않는다.

## 준비 사항

- Windows 10 버전 1903(빌드 18362) 이상
- x64 그래픽 드라이버와 활성 DWM 세션
- .NET Framework 4.8 및 WebView2 Runtime
- .NET SDK 9 이상(WGC 캡처 도구 빌드용)

이 저장소의 캡처 도구는 `IGraphicsCaptureItemInterop.CreateForWindow`로 HWND를 캡처하고, D3D11 프레임을 BGRA PNG로 저장한다. `PrintWindow`, `BitBlt`, 데스크톱 화면 DC, `SetForegroundWindow`, 키보드·마우스 입력 합성은 사용하지 않으며 WGC 실패 시 다른 방식으로 우회하지 않는다.

## 전체 화면 상태 테스트

PowerShell에서 다음 명령을 실행한다.

```powershell
.\scripts\test-visual.ps1
```

최초 상태에서 앱과 WGC 도구를 빌드한 뒤 다음 14개 상태의 PNG와 대응하는 `.capture.json` 파일을 만든다.

```text
artifacts\wgc\
├─ indiana-expedition-main.png
├─ indiana-expedition-main.capture.json
├─ indiana-expedition-favorites.png
├─ indiana-expedition-favorites.capture.json
├─ indiana-expedition-history.png
├─ indiana-expedition-history.capture.json
├─ indiana-expedition-popupblocked.png
├─ indiana-expedition-popupblocked.capture.json
├─ indiana-expedition-finddialog.png
├─ indiana-expedition-finddialog.capture.json
├─ indiana-expedition-deletebrowsingdatadialog.png
├─ indiana-expedition-deletebrowsingdatadialog.capture.json
├─ indiana-expedition-downloadprogressdialog.png
├─ indiana-expedition-downloadprogressdialog.capture.json
├─ indiana-expedition-downloadcompleteddialog.png
├─ indiana-expedition-downloadcompleteddialog.capture.json
├─ indiana-expedition-downloadhistorydialog.png
├─ indiana-expedition-downloadhistorydialog.capture.json
├─ indiana-expedition-permissionrequestdialog.png
├─ indiana-expedition-permissionrequestdialog.capture.json
├─ indiana-expedition-privacytab.png
├─ indiana-expedition-privacytab.capture.json
├─ indiana-expedition-contextmenu.png
├─ indiana-expedition-contextmenu.capture.json
├─ indiana-expedition-helpmenu.png
├─ indiana-expedition-helpmenu.capture.json
├─ indiana-expedition-aboutdialog.png
└─ indiana-expedition-aboutdialog.capture.json
```

각 결과는 캡처 방식이 `wgc`인지, 대상 창이 포그라운드가 아니었는지, PNG 크기와 표본 색상이 정상인지 자동 검증한다. 앱은 준비 파일에 빈 신호 대신 정확한 대상 HWND를 기록하며, 스크립트는 이 HWND가 실행한 앱 프로세스 소유인지 확인한다. 한 조건이라도 어긋나면 스크립트가 실패한다.
시각 기준 창은 선택적인 로컬 참조 이미지 `target.png`와 직접 비교할 수 있도록 800×600으로 고정된다. 권리 관계가 확인된 이미지만 저장소 루트에 이 이름으로 두며, 파일 자체는 `.gitignore`에 의해 공개 저장소와 릴리스에서 제외된다. 일반 실행 창의 기본 크기는 1024×768로 유지된다.

Release 빌드 또는 이미 빌드된 실행 파일을 검사할 때는 다음 옵션을 사용한다.

```powershell
.\scripts\test-visual.ps1 -Configuration Release
.\scripts\test-visual.ps1 -SkipBuild
```

## 단일 상태 캡처

```powershell
.\scripts\capture-wgc.ps1 -State Main
.\scripts\capture-wgc.ps1 -State Favorites
.\scripts\capture-wgc.ps1 -State History -OutputPath artifacts\review\history.png
.\scripts\capture-wgc.ps1 -State PopupBlocked
.\scripts\capture-wgc.ps1 -State FindDialog
.\scripts\capture-wgc.ps1 -State DeleteBrowsingDataDialog
.\scripts\capture-wgc.ps1 -State DownloadProgressDialog
.\scripts\capture-wgc.ps1 -State DownloadCompletedDialog
.\scripts\capture-wgc.ps1 -State DownloadHistoryDialog
.\scripts\capture-wgc.ps1 -State PermissionRequestDialog
.\scripts\capture-wgc.ps1 -State PrivacyTab
.\scripts\capture-wgc.ps1 -State ContextMenu
.\scripts\capture-wgc.ps1 -State HelpMenu
.\scripts\capture-wgc.ps1 -State AboutDialog
```

`--visual-test` 모드에서는 외부 네트워크와 WebView2 첫 프레임 시점에 영향을 받지 않도록 웹 콘텐츠 영역을 흰색 결정론적 표면으로 대체한다. 팝업 상태는 메인 창의 노란 정보 표시줄을 노출한다. 대화상자 상태는 WebView2 없이 대표 검색어·다운로드·사이트 권한 데이터를 인터페이스 스텁으로 주입한다. 다운로드 진행과 완료, 최근 기록, 네 가지 선택이 있는 권한 요청, 개인 정보 탭의 권한 목록을 각각 검증한다. 우클릭 메뉴 상태는 브라우저가 사용하는 동일한 메뉴 팩터리와 XP 렌더러를 검증하고, 도움말 메뉴 상태는 열린 최상위 메뉴의 눌림 표시를 검증한다. 따라서 PNG는 Luna 창 프레임, 메뉴, 도구 모음, 주소 표시줄, 사이드바, 정보 표시줄, 모달 및 상태 표시줄의 회귀 검사에 사용한다. 실제 WebView2 탐색과 DOM 동작은 별도의 기능 테스트 대상으로 유지한다.

## Windows 10 주의 사항

테스트 창에 WinForms의 `ShowInTaskbar = false`를 적용하면 이 환경의 Windows 10 WGC가 창을 캡처 불가능 대상으로 처리했다. 따라서 메인 창과 시각 테스트용 대화상자는 몇 초 동안 작업 표시줄에 표시될 수 있다. 대화상자 상태는 일반 실행의 소유 모달과 달리 테스트에서만 독립 최상위 창으로 만들며, `ShowWithoutActivation`과 `SendToBack`을 함께 적용해 포커스와 전면 표시를 막는다.
