Indiana Expedition 0.1.0
============

프리뷰 및 코드 서명 안내
----------------------
이 버전은 기능과 UI를 검증하는 프리뷰입니다. 실행 파일에는 아직 코드 서명이
적용되지 않아 Windows가 SmartScreen 경고를 표시할 수 있습니다. 이 저장소의
공식 Releases에서 받은 파일인지 확인한 후 실행하세요.

1. IndianaExpedition.exe를 실행합니다.
2. 실행되지 않고 WebView2 안내가 표시되면 다음 공식 페이지에서
   Microsoft Edge WebView2 Evergreen Runtime을 설치합니다.
   https://developer.microsoft.com/microsoft-edge/webview2/

요구 사항
---------
- Windows 10/11 x64
- .NET Framework 4.8 Runtime
- Microsoft Edge WebView2 Evergreen Runtime

WebView2 사용 이유
-----------------
Indiana Expedition은 브라우저 렌더링 엔진을 자체적으로 내장하거나 포크하지 않습니다.
웹 표준 호환성과 엔진 보안 업데이트를 WebView2 Evergreen Runtime의
업데이트 경로에 맡겨 브라우저 엔진 유지보수 의존성과 패치 부담을 낮춥니다.
대신 실행 환경에는 WebView2 Evergreen Runtime이 필요합니다.

사용자 데이터
-----------
설정, 즐겨찾기, 방문 기록, 쿠키와 WebView2 프로필은 다음 경로에 저장됩니다.
%LocalAppData%\IndianaExpedition

주의
----
Indiana Expedition은 IE6의 외형과 주요 조작 경험을 재구성한 앱입니다.
ActiveX, VBScript, Trident 및 IE 문서 모드는 지원하지 않습니다.

제3자 고지와 상표
----------------
프로젝트 고유 코드와 문서는 동봉된 LICENSE의 MIT License로 배포합니다.
THIRD-PARTY-NOTICES.md 및 licenses 폴더에서 제3자 라이선스를 확인할 수
있습니다. Indiana Expedition은 Microsoft와 제휴 관계가 없는 독립 프로젝트이며
Microsoft가 이 프로젝트를 허가·후원·승인하지 않았습니다. 제품명과 상표는
출처 식별 및 호환성 설명에만 사용합니다.
