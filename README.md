# BS2 Cooler

[날짜·시간별 작업 보고서](docs/WORK_LOG.md)

조이트론/플라이디지 BS2 Pro의 USB 쿨러 제어 기능만 구현한 독립 WPF 앱입니다.
제조사 EXE·DLL·서비스와 외부 NuGet 패키지를 사용하지 않고 Windows HID API로 직접 통신합니다.
0.2.0부터 온도 기반 자동 제어를 지원합니다. GPU는 기존 NVIDIA 드라이버에서 읽고,
Intel CPU는 별도 설정된 공식 PawnIO 드라이버가 있을 때 공개 서명 모듈로 읽습니다.
앱은 CPU 드라이버를 설치하거나 시작하지 않습니다. [외부 구성요소 고지](THIRD_PARTY_NOTICES.txt)
0.2.1에서는 CPU 센서 권한이 부족할 때 관리자 권한으로 다시 실행하는 버튼을 제공합니다.
0.3.0에서는 RGB 색상·효과·밝기·속도와 LED별 색상을 편집할 수 있습니다.

## 실행

이 PC에서는 [Bs2Cooler.exe](artifacts/Bs2Cooler-win-x64/Bs2Cooler.exe)를 실행하면 됩니다.
이동/보관에는 [ZIP 패키지](artifacts/Bs2Cooler-win-x64.zip)를 사용합니다.
Windows와 Microsoft .NET Desktop Runtime 9 (x64)가 필요합니다. 현재 PC에는 설치되어 있습니다.

1. BS2 Pro의 전원을 켜고 데이터 USB 케이블로 연결합니다.
2. 앱이 장치를 찾으면 단계/RPM을 고르고 **속도 적용**을 누릅니다.
3. 오른쪽 버튼으로 RGB 조명과 단계 표시등을 켜고 끕니다.
4. **RGB 조명 → 세부 설정**에서 색상/효과/밝기/속도를 고른 뒤 적용합니다.
   기존 장치 색상표 조회는 지원하지 않으며 이 PC의 마지막 적용값을 저장합니다. [RGB 사용 방법](docs/RGB-LIGHTING.ko.md)

자동 제어는 **온도 자동** 탭에서 제어 기준과 성능/저소음 곡선을 선택한 뒤 시작합니다.
현재 PC에는 사용자 허용에 따라 공식 PawnIO 2.2.0을 설정했습니다. CPU 온도를 읽으려면
**CPU 온도 권한으로 다시 실행**을 눌러 Windows 관리자 승인을 허용합니다.
CPU/GPU 기준은 두 센서가 모두 읽힐 때 사용할 수 있습니다. [온도 제어와 CPU 센서 설정](docs/AUTOMATIC-COOLING.ko.md)

단계 버튼은 해당 단계의 저장 속도를 화면에 불러옵니다. 실제 변경은 적용 버튼을 누를 때 수행합니다.
적용하면 해당 단계의 저장 속도도 바뀝니다. 앱 시작/재연결 시 설정을 자동 적용하지 않습니다.
전원에 따라 최대 2700/3300/4000 RPM으로 제한하며, 최신 전원 상태가 없으면 제어를 허용하지 않습니다.

## 현재 기능

- 자동 장치 검색, 현재/목표 RPM 및 펌웨어 표시
- 1~4단계 선택과 수동 RPM 설정
- RGB 조명/단계 표시등 켜기·끄기
- RGB 8가지 효과, 밝기 0~100%, 효과 속도 1~10, HEX 색상 6개와 미리보기
- LED별 고정 색상, 호흡 1~5색, 마지막 적용값 저장/불러오기
- USB 연결 오류 처리와 자동 재검색
- 일반 사용자 권한 실행, 로컬 USB 통신
- CPU/GPU 온도 표시 및 온도 기반 자동 RPM 제어
- 성능/저소음 곡선, 온도 하강 시 2°C 여유, 90°C 고온 보호
- 센서 누락/8초 초과 시 최대 풍량, 중지/정상 종료 시 기존 설정 복원

Bluetooth는 이 버전에 포함하지 않았습니다.
CPU 온도 권한 버튼을 누를 때만 Windows 관리자 승인을 요청합니다.
서비스/드라이버 설치, 자동 업데이트, 인터넷 통신 코드는 앱에 없습니다.

## 검증

실물 `37D7:1002`, 펌웨어 `0.0.3.3`에서 버전/RPM/저장 속도 조회,
목표 2400→2600 RPM 변경과 실제 회전수 상승, 조명 변경 및 기존 설정 복원을 확인했습니다.
원본 프로그램과 서비스 프로세스가 모두 없는 상태에서도 같은 시험을 통과했습니다.
0.2.0은 GPU 실측 약 53~54°C에서 자동 목표 2100 RPM 적용과 기존 1400 RPM 복원을 확인했습니다.
자동 제어가 4단계 저장 속도를 바꾸지 않는 것도 확인했습니다.
빌드 경고/오류 0개, 프로토콜/온도/RGB 테스트 34개와 WPF 자동 시작/중지/복원 검증을 통과했습니다.
0.2.1은 공식 PawnIO를 통해 현재 PC의 Intel CPU Package 온도도 실제로 읽었습니다.
CPU 78°C / GPU 50°C → 높은 CPU 기준으로 목표 2700 RPM, 실제 2600 RPM까지 상승 후
기존 1400 RPM 복원을 확인했습니다. WPF에서도 CPU/GPU 기준 자동 시작/중지 검증을 통과했습니다.
0.3.0은 실물에서 RGB 8가지 효과 업로드의 성공 응답, 팬 설정 유지, WPF 색상 입력/적용/저장을 확인했습니다.
기존 색상표 읽기와 실제 발광 모양 확인은 검증 범위와 구분합니다.

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
dotnet run --project tools/Bs2Cooler.Diagnostics -c Release -- thermal
.\tools\Get-Bs2DeviceStatus.ps1 | ConvertTo-Json -Depth 5
```

`exercise` 진단은 팬과 조명을 잠시 변경한 뒤 복원하는 실기 시험입니다.
수동 모드이며 목표 속도와 해당 단계의 저장 속도가 같은 장치에서 실행합니다.

```powershell
dotnet run --project tools/Bs2Cooler.Diagnostics -c Release -- exercise
```

`auto-exercise`는 GPU 실측 온도로 자동 모드에 진입해 RPM을 적용한 뒤 기존 모드/속도를 복원합니다.
실제 장치 설정에 영향을 주는 진단이므로 읽기 전용 `thermal`과 구분합니다.

설치본 추출/디컴파일 자료와 이전 SDK 비교 코드는 `.analysis/`에 보관하고 Git 추적과 배포에서 제외합니다.
새 앱/테스트/진단 도구는 이 폴더에 의존하지 않습니다. 배포 패키지는 `artifacts/`에 있으며 Git 추적에서 제외합니다.
