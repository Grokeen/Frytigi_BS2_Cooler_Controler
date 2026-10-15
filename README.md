# BS2 Cooler

[날짜·시간별 작업 보고서](docs/WORK_LOG.md)

조이트론/플라이디지 BS2 Pro의 USB 쿨러 제어 기능만 구현한 독립 WPF 앱입니다.
제조사 EXE·DLL·서비스와 외부 NuGet 패키지를 사용하지 않고 Windows HID API로 직접 통신합니다.

## 실행

이 PC에서는 [Bs2Cooler.exe](artifacts/Bs2Cooler-win-x64/Bs2Cooler.exe)를 실행하면 됩니다.
이동/보관에는 [ZIP 패키지](artifacts/Bs2Cooler-win-x64.zip)를 사용합니다.
Windows와 Microsoft .NET Desktop Runtime 9 (x64)가 필요합니다. 현재 PC에는 설치되어 있습니다.

1. BS2 Pro의 전원을 켜고 데이터 USB 케이블로 연결합니다.
2. 앱이 장치를 찾으면 단계/RPM을 고르고 **속도 적용**을 누릅니다.
3. 오른쪽 버튼으로 RGB 조명과 단계 표시등을 켜고 끕니다.

단계 버튼은 해당 단계의 저장 속도를 화면에 불러옵니다. 실제 변경은 적용 버튼을 누를 때 수행합니다.
적용하면 해당 단계의 저장 속도도 바뀝니다. 앱 시작/재연결 시 설정을 자동 적용하지 않습니다.
전원에 따라 최대 2700/3300/4000 RPM으로 제한하며, 최신 전원 상태가 없으면 제어를 허용하지 않습니다.

## 현재 기능

- 자동 장치 검색, 현재/목표 RPM 및 펌웨어 표시
- 1~4단계 선택과 수동 RPM 설정
- RGB 조명/단계 표시등 켜기·끄기
- USB 연결 오류 처리와 자동 재검색
- 일반 사용자 권한 실행, 로컬 USB 통신

온도 기반 자동 제어, Bluetooth, RGB 색상/효과 편집은 이 버전에 포함하지 않았습니다.
관리자 권한 요청, 서비스/드라이버 설치, 자동 업데이트, 인터넷 통신 코드가 없습니다.

## 검증

실물 `37D7:1002`, 펌웨어 `0.0.3.3`에서 버전/RPM/저장 속도 조회,
목표 2400→2600 RPM 변경과 실제 회전수 상승, 조명 변경 및 기존 설정 복원을 확인했습니다.
원본 프로그램과 서비스 프로세스가 모두 없는 상태에서도 같은 시험을 통과했습니다.
빌드 경고/오류 0개, 프로토콜 테스트 11개와 WPF 연결/화면 검증을 통과했습니다.

[구현 및 검증 기록](docs/IMPLEMENTATION.ko.md) · [사용 방법](docs/User-Guide.ko.txt) · [초기 설치본 분석](docs/BS2-Pro-Feasibility.ko.md)

## 소스 빌드

.NET SDK 9가 필요합니다.

```powershell
dotnet build Bs2Cooler.sln -c Release
dotnet run --project tests/Bs2Cooler.Tests -c Release
dotnet run --project src/Bs2Cooler.App -c Release
```

EXE와 SHA-256 및 ZIP 패키지 생성:

```powershell
.\tools\Publish-App.ps1
```

읽기 전용 조회:

```powershell
dotnet run --project tools/Bs2Cooler.Diagnostics -c Release -- query
.\tools\Get-Bs2DeviceStatus.ps1 | ConvertTo-Json -Depth 5
```

`exercise` 진단은 팬과 조명을 잠시 변경한 뒤 복원하는 실기 시험입니다.
수동 모드이며 목표 속도와 해당 단계의 저장 속도가 같은 장치에서 실행합니다.

```powershell
dotnet run --project tools/Bs2Cooler.Diagnostics -c Release -- exercise
```

설치본 추출/디컴파일 자료와 이전 SDK 비교 코드는 `.analysis/`에 보관하고 Git 추적과 배포에서 제외합니다.
새 앱/테스트/진단 도구는 이 폴더에 의존하지 않습니다. 배포 패키지는 `artifacts/`에 있으며 Git 추적에서 제외합니다.
