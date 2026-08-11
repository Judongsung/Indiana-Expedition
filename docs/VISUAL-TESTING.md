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

최초 상태에서 앱과 WGC 도구를 빌드한 뒤 다음 파일을 만든다.

```text
artifacts\wgc\
├─ indiana-expedition-main.png
├─ indiana-expedition-main.capture.json
├─ indiana-expedition-favorites.png
├─ indiana-expedition-favorites.capture.json
├─ indiana-expedition-history.png
└─ indiana-expedition-history.capture.json
```

각 결과는 캡처 방식이 `wgc`인지, 대상 창이 포그라운드가 아니었는지, PNG 크기와 표본 색상이 정상인지 자동 검증한다. 한 조건이라도 어긋나면 스크립트가 실패한다.
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
```

`--visual-test` 모드에서는 외부 네트워크와 WebView2 첫 프레임 시점에 영향을 받지 않도록 웹 콘텐츠 영역을 흰색 결정론적 표면으로 대체한다. 따라서 PNG는 Luna 창 프레임, 메뉴, 도구 모음, 주소 표시줄, 사이드바 및 상태 표시줄의 회귀 검사에 사용한다. 실제 WebView2 탐색과 DOM 동작은 별도의 기능 테스트 대상으로 유지한다.

## Windows 10 주의 사항

테스트 창에 WinForms의 `ShowInTaskbar = false`를 적용하면 이 환경의 Windows 10 WGC가 창을 캡처 불가능 대상으로 처리했다. 따라서 테스트 창은 몇 초 동안 작업 표시줄에 표시될 수 있지만, `ShowWithoutActivation`과 `SendToBack`을 함께 적용해 포커스와 전면 표시를 막는다.
