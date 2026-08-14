# Indiana Expedition 릴리스 절차

릴리스는 로컬 대화형 Windows 데스크톱에서 WGC 화면 게이트를 먼저 통과한 뒤, GitHub Actions가 동일한 소스를 다시 검증해 Draft Release를 만드는 방식으로 진행한다. 자동 공개는 하지 않는다.

## 1. 버전과 작업 트리 확인

`Directory.Build.props`의 `IndianaExpeditionVersion`, `README.md`의 현재 버전, `docs/RELEASE-README.txt` 첫 줄이 모두 같아야 한다. App과 Core 프로젝트는 이 공통 속성을 참조한다.

```powershell
git status --short
git pull --ff-only
./scripts/verify-version.ps1 -ExpectedVersion 0.3.0
```

릴리스할 변경을 커밋하고 `main`에 반영한 후 다음 단계로 진행한다.

## 2. 로컬 WGC 릴리스 게이트

대화형 데스크톱이 있는 로컬 Windows 환경에서 다음 명령을 실행한다.

```powershell
./scripts/verify-release.ps1 -Version 0.3.0
```

이 스크립트는 공통 검증을 마친 뒤 14개 시각 상태를 직접 WGC로 캡처한다. 모든 결과가 `CaptureMode=wgc`, `ForegroundUntouched=True`일 때만 ZIP과 SHA-256 파일을 만든다. 입력 합성, 포그라운드 전환, `PrintWindow` 및 대체 캡처 방식은 사용하지 않는다.

생성 파일은 다음과 같다.

```text
artifacts\IndianaExpedition-0.3.0-win-x64.zip
artifacts\IndianaExpedition-0.3.0-win-x64.zip.sha256
```

체크섬은 한 번 더 직접 비교할 수 있다.

```powershell
$zip = "artifacts\IndianaExpedition-0.3.0-win-x64.zip"
$expected = ((Get-Content "$zip.sha256" -Raw).Trim() -split '\s+')[0]
$actual = (Get-FileHash $zip -Algorithm SHA256).Hash
if ($actual -ne $expected) { throw "SHA-256 불일치" }
```

## 3. GitHub Draft Release 생성

GitHub 저장소의 Actions에서 `Draft release` 워크플로를 선택하고 `Run workflow`를 누른 뒤 소스와 같은 버전(예: `0.3.0`)을 입력한다. 워크플로는 `main`에서만 실행되며 다음을 수행한다.

- 요청 버전과 공통 소스 버전 일치 확인
- 같은 `v0.3.0` 태그 또는 Release가 없는지 확인
- 공통 Release/x64 빌드와 Core/App 테스트 재실행
- 배포 구성·라이선스·버전 및 `git diff --check` 검사
- 현재 커밋에 태그를 만들고 생성 릴리스 노트가 포함된 Draft Release 생성
- ZIP과 `.sha256` 파일 첨부

GitHub 호스팅 러너는 대화형 데스크톱을 보장하지 않으므로 WGC를 다시 실행하지 않는다.

## 4. 사람의 최종 공개

Draft Release 화면에서 다음을 직접 확인한다.

- 대상 태그와 커밋이 의도한 `main` 커밋인지
- ZIP과 `.sha256` 두 자산의 이름이 정확한지
- 내려받은 ZIP의 SHA-256이 첨부 체크섬과 일치하는지
- ZIP 루트에 실행 파일, WebView2 DLL, `LICENSE`, `THIRD-PARTY-NOTICES.md`, `licenses` 폴더가 있는지
- 생성 릴리스 노트에 민감 정보나 부정확한 설명이 없는지

모든 항목을 확인한 사람만 GitHub UI에서 Release를 공개한다. 실행 파일은 코드 서명되지 않은 프리뷰라는 안내를 유지한다.
