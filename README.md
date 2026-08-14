# Indiana Expedition

Indiana Expedition은 Windows XP SP2의 Internet Explorer 6 사용 경험을 현대 Windows에서 다시 느낄 수 있도록 만든 개인용 WinForms 브라우저입니다. 브라우저 셸은 C# WinForms로 구성하고 웹 콘텐츠는 Microsoft Edge WebView2로 렌더링합니다.

이 저장소는 학습·연구와 과거 사용자 경험의 재현을 목적으로 제작·운영하는 독립적인 비상업 프로젝트입니다. 이는 프로젝트 관리자의 현재 제작·운영 목적에 대한 설명이며, [MIT License](LICENSE)가 허용하는 이용 범위를 제한하지 않습니다.

## 프로젝트 상태

현재 버전 `0.3.0`은 기능과 UI를 검증하는 프리뷰입니다. 배포용 실행 파일에는 아직 코드 서명이 적용되지 않아 Windows가 SmartScreen 경고를 표시할 수 있습니다. 소스 코드를 직접 빌드하거나 이 저장소의 공식 Releases에서 받은 파일만 사용하는 것을 권장합니다.

## WebView2를 선택한 이유

Indiana Expedition은 브라우저 렌더링 엔진을 자체적으로 포크하거나 앱에 내장하지 않고 WebView2 Evergreen Runtime을 사용합니다. HTML·CSS·JavaScript 엔진, 최신 웹 표준 호환성 및 엔진 보안 업데이트를 Microsoft의 WebView2 업데이트 경로에 맡기므로, 프로젝트 내부의 브라우저 엔진 유지보수 의존성과 보안 패치 부담을 낮춥니다.

다만 의존성이 완전히 없어지는 것은 아닙니다. 실행 환경에는 WebView2 Evergreen Runtime이 필요하며, Indiana Expedition은 WinForms 셸, XP UI, 사용자 데이터 계층과 WebView2 API 통합 및 호환성 검증을 직접 유지보수합니다.

## 개발 방식

이 프로젝트는 OpenAI Codex를 활용해 기획, 코드 작성, 리팩터링, XP Luna UI 반복 개선, 테스트 자동화 및 문서화를 진행한 바이브 코딩 프로젝트입니다. 생성·수정된 결과물은 저장소 관리자가 검토하고 프로젝트 요구 사항에 맞게 관리합니다.

## 현재 구현된 기능

- XP Luna Blue 사용자 창 프레임과 IE6풍 한국어 메뉴, 탐색 도구 모음, 주소 표시줄, 연결 바 및 상태 표시줄
- 주소 자동 보완과 Google 검색
- 뒤로, 앞으로, 중지, 새로 고침, 홈 및 전체 화면
- WebView2 프로필을 공유하는 탭 없는 다중 창
- Windows XP SP2식 자동 팝업 차단, 노란 정보 표시줄 및 사이트별 허용 목록
- Luna 모달 페이지 찾기와 시스템 인쇄
- 앱 전체에 즉시 적용되고 다음 실행에도 유지되는 67%·80%·100%·125%·150% 확대 단계
- 폴더를 지원하는 즐겨찾기 추가·구성·이동·삭제
- 30일 또는 최대 2,000개 방문 기록과 날짜별 탐색창
- 방문 기록, 다운로드 기록, 캐시, 쿠키, 사이트 저장소, 자동 완성, 암호 및 저장된 사이트 권한을 선택하는 검색 기록 삭제
- 홈페이지, 시작 방식, 다운로드 폴더와 저장 위치 확인 방식을 설정하는 인터넷 옵션
- 파일별 Luna 다운로드 진행 창, 일시 중지·계속·취소와 최대 200개의 최근 다운로드 기록
- IE6풍 페이지 우클릭 메뉴와 1회/영구 선택을 지원하는 사이트 권한 요청
- WebView2 프로필에 저장된 사이트별 권한을 조회·변경·초기화하는 개인 정보 설정
- WebView2 프로세스 실패 복구와 손상된 JSON 설정 백업
- 한국어 `.resx` 리소스, 역할별 `Constants` 디렉터리 및 교체 가능한 브랜딩 진입점

반복되는 URL, 저장 파일명, 보관 정책, 브라우저 명령과 Luna 메트릭은 `src/IndianaExpedition.Core/Constants` 및 `src/IndianaExpedition.App/Constants`에 모아 두었습니다. 사용자 표시 문구는 `Strings.resx`에서 관리합니다.

Luna 창 크롬은 `LunaMetrics`와 `XpPalette`를 조정 지점으로 사용합니다. `LunaLayout`이 아이콘·제목·캡션 버튼 사각형을 한 번만 계산하고, 실제 버튼 컨트롤이 같은 사각형을 그리기와 입력 영역에 함께 사용합니다. 창 가장자리 판정은 `LunaHitTest`, 네이티브 메시지 처리는 `LunaForm`으로 분리했습니다.

한국어 UI 글꼴은 Microsoft의 [언어별 UI 글꼴 표](https://learn.microsoft.com/windows/win32/controls/use-font-binding-in-rich-edit-controls)에 따라 굴림 9pt를 사용하며, Luna 제목은 Trebuchet MS Bold 10pt를 사용합니다. 일반적인 영문 Windows XP UI의 기준은 [Microsoft Win32 글꼴 지침](https://learn.microsoft.com/windows/win32/uxguide/vis-fonts)에 설명된 8pt Tahoma입니다.

## 개발 환경

- Windows 10/11 x64
- Visual Studio 2022
- .NET Framework 4.8 Runtime
- Microsoft Edge WebView2 Evergreen Runtime

페이지 찾기 API 사용을 위해 WebView2 Evergreen Runtime `139.0.3405.78` 이상이 필요합니다. Runtime이 없거나 이보다 오래된 경우 앱이 설치 또는 업데이트 안내를 구분해 표시합니다. 최소 버전은 Microsoft의 [WebView2 1.0.3405.78 릴리스 안내](https://learn.microsoft.com/microsoft-edge/webview2/release-notes/)를 기준으로 합니다.

저장소는 `Microsoft.NETFramework.ReferenceAssemblies.net48` 패키지를 사용하므로 별도의 4.8 Targeting Pack이 없는 환경에서도 CLI 빌드할 수 있습니다. WebView2 Runtime은 앱 실행에 필요하며 [Microsoft 공식 페이지](https://developer.microsoft.com/microsoft-edge/webview2/)에서 받을 수 있습니다.

```powershell
./scripts/verify.ps1
```

이 공통 검증은 솔루션 복원, Release/x64 빌드, Core 테스트, 전경을 획득하지 않는 STA App 동작 테스트, 배포 구성·라이선스 검사와 `git diff --check`를 순서대로 실행합니다. UI 테스트는 외부 입력이나 네트워크를 사용하지 않고 실제 컨트롤의 `PerformClick()` 이벤트 연결과 우클릭 메뉴 deferral 순서를 검증합니다.

실행용 폴더는 다음 스크립트로 만듭니다.

```powershell
.\scripts\build-release.ps1
```

결과는 `artifacts\IndianaExpedition-win-x64`에 생성됩니다.

## 비간섭 화면 테스트

Windows Graphics Capture로 메인 화면과 사이드바, 팝업·찾기·검색 기록 삭제 상태, 다운로드 진행·완료·기록 창, 권한 요청·개인 정보 탭, 페이지 우클릭 메뉴, 도움말 메뉴 및 정보 창까지 14개 상태를 실제 PNG로 캡처할 수 있습니다. 테스트 창은 포커스를 얻지 않고 뒤쪽에서 실행되며 WGC가 실패해도 `PrintWindow` 같은 방식으로 우회하지 않습니다. 준비 파일에는 앱이 직접 만든 캡처 대상 HWND가 기록되고, 스크립트가 실행 프로세스 소유인지 검증한 뒤 캡처합니다.

```powershell
.\scripts\test-visual.ps1
```

결과는 `artifacts\wgc`에 생성됩니다. 세부 구조와 단일 상태 캡처 방법은 [화면 테스트 문서](docs/VISUAL-TESTING.md)를 참고하세요.

릴리스 후보는 로컬에서 `./scripts/verify-release.ps1 -Version 0.3.0`으로 공통 검증과 WGC 14개 상태를 모두 통과시킨 뒤 GitHub의 수동 `Draft release` 워크플로로 생성합니다. 자동 공개는 하지 않으며 자산과 체크섬을 사람이 확인한 후 공개합니다. 전체 순서는 [릴리스 절차](docs/RELEASING.md)에 정리했습니다.

WGC 캡처 도구는 저장소 안에서 화면 테스트를 빌드·실행하기 위한 개발 도구이며 `build-release.ps1`이 만드는 앱 배포 폴더에는 포함되지 않습니다. 이 도구의 바이너리를 별도로 재배포하려면 생성된 출력물에 포함되는 런타임 구성 요소와 각 라이선스를 다시 확인해야 합니다.

## 데이터 위치

사용자 설정과 프로필은 실행 파일 옆이 아니라 다음 위치에 보관됩니다.

```text
%LocalAppData%\IndianaExpedition\
├─ Data\
│  ├─ settings.json
│  ├─ favorites.json
│  ├─ history.json
│  ├─ downloads.json
│  └─ session.json
└─ WebView2\
```

JSON 저장은 임시 파일 교체 방식이며 이전 파일은 `.bak`으로 남습니다. 읽을 수 없는 파일은 `.corrupt-날짜.bak`으로 보존한 후 기본값으로 복구합니다.

검색 기록 삭제의 기본 선택은 방문 기록, 다운로드 기록, 디스크 캐시, 쿠키 및 사이트 저장소입니다. 자동 완성 데이터, 저장된 암호와 사이트 권한은 기본 선택되지 않습니다. 다운로드 기록을 지우거나 다운로드 보기에서 항목을 제거해도 다운로드한 파일 자체는 삭제되지 않으며, 쿠키를 지우면 사이트에서 로그아웃될 수 있습니다. 열린 페이지는 자동으로 새로 고치지 않으므로 쿠키·사이트 데이터 변경은 다음 요청 또는 새로 고침부터 반영됩니다.

다운로드는 기본적으로 설정된 다운로드 폴더에 자동 저장하며 같은 이름이 있으면 충돌하지 않는 이름을 만듭니다. 인터넷 옵션에서 다운로드마다 저장 위치를 묻도록 바꿀 수 있습니다. 진행 중인 다운로드가 있는 원본 브라우저 창을 닫으면 취소 여부를 먼저 확인하고, Windows 종료 시에는 종료를 막지 않고 다운로드를 취소합니다.

## 주요 단축키

| 키 | 동작 |
|---|---|
| `Ctrl+L` | 주소 표시줄 선택 |
| `Ctrl+N` | 새 창 |
| `Ctrl+F` | 이 페이지에서 찾기 |
| `F3`, `Shift+F3` | 다음 / 이전 찾기 결과 |
| `Ctrl+P` | 시스템 인쇄 |
| `Ctrl+J` | 최근 다운로드 보기 |
| `Ctrl++`, `Ctrl+-`, `Ctrl+0` | 페이지 확대 / 축소 / 100% 복원 |
| `Ctrl+D` | 현재 페이지를 즐겨찾기에 추가 |
| `Ctrl+I` | 즐겨찾기 탐색창 |
| `Ctrl+H` | 방문 기록 탐색창 |
| `Alt+Left / Right` | 뒤로 / 앞으로 |
| `Alt+Home` | 홈 |
| `F5`, `Ctrl+R` | 새로 고침 |
| `Esc` | 탐색 중지 |
| `F11` | 전체 화면 |

## 의도적인 한계

Indiana Expedition은 IE6 렌더링 엔진 에뮬레이터가 아닙니다. Trident, ActiveX, VBScript, IE 문서 모드 및 과거 TLS 동작은 지원하지 않으며 WebView2의 현대 보안 모델을 유지합니다. 비활성 메뉴는 당시 IE6 구조를 보여 주기 위한 시각 요소입니다.

## 참고 프로젝트

초기화, 탐색 이벤트 동기화, 브라우저 상태 갱신 및 기록 필터링 구조는 Microsoft의 [WebView2Browser](https://github.com/MicrosoftEdge/WebView2Browser) 샘플을 참고했습니다. Luna의 치수와 시각 구조는 [XP.css](https://github.com/botoxparty/XP.css), 비클라이언트 영역의 역할 분리는 [ReactOS](https://github.com/reactos/reactos)를 교차 참고했습니다. 각 프로젝트의 코드나 이미지 자산은 복사하지 않았으며 Indiana Expedition의 UI와 데이터 계층은 별도로 구현했습니다.

## 라이선스

이 저장소에서 독자적으로 작성한 코드와 문서는 [MIT License](LICENSE)로 배포합니다. 제3자 구성 요소와 참고·차용 코드에는 각각의 원 라이선스가 계속 적용됩니다.

## 제3자 고지와 상표

배포되는 구성 요소, 빌드 전용 패키지 및 참고·차용 코드의 라이선스는 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)에 정리했습니다. 시각 비교용 `target.png`는 권리 관계가 확인된 로컬 파일만 사용하며 공개 저장소와 릴리스 산출물에는 포함하지 않습니다.

Indiana Expedition은 Microsoft와 제휴 관계가 없는 독립 프로젝트이며 Microsoft가 이 프로젝트를 허가·후원·승인하지 않았습니다. Windows, Internet Explorer, Microsoft Edge, WebView2를 비롯한 제품명과 상표는 출처 식별 및 호환성 설명 목적으로만 사용하며 해당 권리자에게 귀속됩니다. 프로젝트에는 Microsoft의 공식 로고, 아이콘 또는 이미지 자산을 포함하지 않습니다.
