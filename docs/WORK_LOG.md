# BS2 Cooler 작업 보고서

- 기록 기준 시각: 2026-10-16 00:50:35 KST
- 시간대: 한국 표준시, Asia/Seoul (UTC+09:00)
- 대상 저장소: `D:\kimyongrok\Frytigi_BS2_Cooler_Controler`
- 현재 결과물: BS2 Cooler 0.1.0 — BS2 Pro 전용 WPF USB 수동 제어 앱

## 기록 원칙

앞으로 이 파일에 날짜·시간과 함께 작업 내역을 누적한다. 각 기록에는 작업 내용,
변경 파일, 검증 결과, 문제와 처리, 남은 항목을 작성한다.

아래의 최초 개발 이력은 대화의 실행 결과와 파일 생성·수정 시각을 바탕으로 사후 정리했다.
파일 시각은 관련 산출물이 기록된 시각이며 작업 전체의 시작·종료 시각을 뜻하지 않는다.
정확한 단일 시각을 확인하지 못한 항목은 `약`으로 표시했다. 이후 신규 작업은 기록 당시의 실제 KST 시각을 사용한다.

## 2026-10-16 — 설치본 분석 및 독립 앱 개발

### 작업 목표

설치된 Flydigi Space Station의 BS2 Pro 쿨러 통신을 분석하고, 제조사 실행 파일·DLL·서비스에
의존하지 않는 WPF 앱을 구현한다. 사용자가 제기한 제조사 프로그램의 보안 우려를 반영해
새 앱의 실행 경로에서 제조사 코드를 제거한다.

### 시간별 작업

| 날짜·시간 (KST) | 수행 내용 | 결과 및 근거 |
| --- | --- | --- |
| 2026-10-16 약 00:10–00:15 | Electron ASAR와 .NET 단일 파일 번들의 분석용 사본 추출, 쿨러 통신 코드 추적 | 화면/서비스 분리 구조와 쿨러 SDK 확인. 설치 폴더는 수정하지 않음. 추출 스크립트 생성 00:11:07, 초기 검증 프로젝트 생성 00:15:15 |
| 2026-10-16 약 00:15–00:22 | 명령 프레임·체크섬·단계/RPM·조명 명령 분석 및 오프라인 비교 | 자체 생성 패킷 46개가 설치본 SDK 출력과 일치. 초기 비교 단계에는 제조사 SDK를 사용했으며 배포 앱과 분리 |
| 2026-10-16 00:22:04 | 실물 USB 조회 결과 기록 | `37D7:1002`, 입력 33/출력 32바이트 확인. 별도 검증 프로세스에서 현재 2300/목표 2400 RPM 조회 성공. `.analysis/probe-results.txt` 수정 시각 기준 |
| 2026-10-16 00:23:22 | 장치 연결 진단 결과 기록 | USB/HID PnP 노드 정상 인식. `.analysis/device-status.json` 수정 시각 기준 |
| 2026-10-16 00:27:23 | 독립 Core/Windows 통신 코드 작성 | 명령 생성·응답 검증, Windows SetupAPI/HID 직접 통신 구현. 해당 소스 생성 시각 기준 |
| 2026-10-16 00:29:05 | 프로토콜 테스트와 실기 진단 도구 작성 | 패킷, 잘린 응답, 손상 체크섬, 범위 제한, 거부 응답 테스트 작성. 테스트/진단 소스 생성 시각 기준 |
| 2026-10-16 00:31:56 | 한국어 WPF 화면 작성 | 현재/목표 RPM, 전원별 상한, 1~4단계, 수동 RPM, RGB/표시등 제어. 화면 소스 생성 시각 기준 |
| 2026-10-16 약 00:33–00:35 | 실물 쓰기 명령 검증 및 복원 절차 수정 | 수동 모드에서 `0x21` 거부 확인. 실제 앱은 성공한 `0x26`으로 수동 제어하도록 확정. 초기 중단된 조명 복원은 즉시 완료하고 각 복원 작업을 독립 실행하도록 수정 |
| 2026-10-16 00:36:44 | 속도·조명 변경/복원 시험 결과 기록 | 목표 2400→2600 RPM, 실제 회전수 상승, 두 조명의 상태 플래그 변경/복원, 저장 속도 4개 일치 확인. `.analysis/native-exercise.txt` 수정 시각 기준 |
| 2026-10-16 약 00:36–00:38 | 화면 렌더링과 레이아웃 수정 | 초기 창 높이에서 하단 제어부 잘림 발견. 기본 810×730/최소 710×710으로 조정하고 검증 도구에 잘림 검사 추가 |
| 2026-10-16 00:40:41 | 사용자 관리자 작업으로 원본 서비스 중지 | 서비스 중지 로그 확인. 에이전트 권한으로 중지/잔류 프로세스 종료는 거부되어 사용자가 수행. 잔류 프로세스는 이후 종료 확인 |
| 2026-10-16 00:44:40 | 원본 프로그램·서비스 프로세스가 모두 없는 상태에서 최종 실기 시험 결과 기록 | 자체 코드만으로 조회·속도·조명 변경 및 원상 복원 통과. `.analysis/standalone-exercise.txt` 수정 시각 기준 |
| 2026-10-16 00:44:46 | 최종 WPF 연결/화면 검증 결과 기록 | USB 연결, 실제 RPM, 펌웨어, 제어 버튼 활성화, 기본/최소 크기 레이아웃 통과. `.analysis/ui-preview.png` 수정 시각 기준 |
| 2026-10-16 00:45:46 | 단일 EXE와 ZIP 배포 패키지 생성 | EXE 239,080바이트, 사용 방법 및 SHA-256 포함. `artifacts/Bs2Cooler-win-x64/Bs2Cooler.exe` 수정 시각 기준 |
| 2026-10-16 00:46:48 | 배포 EXE 실행 | `BS2 Cooler` 창 실행 및 응답 확인. 앱 프로세스 시작 시각 기준. 제조사 모듈과 원본 프로세스가 없음을 후속 확인 |

### 주요 변경 파일

| 파일/폴더 | 역할 |
| --- | --- |
| [Bs2Cooler.sln](../Bs2Cooler.sln) | WPF 앱·통신·진단·테스트 솔루션 |
| [Bs2Protocol.cs](../src/Bs2Cooler.Core/Bs2Protocol.cs) | 명령 생성, 길이/체크섬 확인, 응답 해석 |
| [NativeHid.cs](../src/Bs2Cooler.Windows/NativeHid.cs) | Windows 기본 HID API, 장치/인터페이스 검증 |
| [Bs2Connection.cs](../src/Bs2Cooler.Windows/Bs2Connection.cs) | 비동기 읽기/쓰기, 명령 직렬화, 응답 제한, 연결 종료 |
| [MainWindow.xaml](../src/Bs2Cooler.App/MainWindow.xaml) | 한국어 제어 화면 |
| [MainWindow.xaml.cs](../src/Bs2Cooler.App/MainWindow.xaml.cs) | 조회, 수동 적용, 조명, 자동 재검색 |
| [프로토콜 테스트](../tests/Bs2Cooler.Tests/Program.cs) | 실제 캡처와 경계/오류 조건 검증 |
| [실기 진단](../tools/Bs2Cooler.Diagnostics/Program.cs) | 읽기 전용 조회 및 변경/복원 시험 |
| [WPF 화면 검증](../tools/Bs2Cooler.UiSmoke/Program.cs) | 실제 연결 상태와 화면 레이아웃 검사 |
| [Publish-App.ps1](../tools/Publish-App.ps1) | EXE·ZIP·SHA-256 생성 |

### 확인된 결과

- 실물: BS2 Pro, USB `37D7:1002`, 펌웨어 `0.0.3.3`.
- 원본 프로그램과 서비스 프로세스가 모두 없는 상태에서 직접 제어 성공.
- 일반 전원에서 목표 RPM 변경, 실제 RPM 상승, 조명 상태 변경 확인 후 원래 상태 복원.
- 복원값: 2단, 목표 2400 RPM, RGB/단계 표시등 켜짐, 저장 속도 `1700,2400,3000,4000`.
- Release 빌드 경고 0개/오류 0개, 프로토콜 테스트 11개 통과.
- 배포 앱은 제조사 EXE/DLL/서비스 및 외부 HID 라이브러리를 사용하지 않음.
- 명시적으로 추가한 외부 NuGet 패키지 참조 없음. 단일 파일 게시 시 Microsoft SDK의 빌드 도구 복원은 수행됨.
- 앱에 인터넷 통신, 드라이버/서비스 설치, 관리자 권한 요청 기능 없음.
- 네이티브 DLL은 System32에서 로드. 제조사 프로그램의 악성 여부와 기존 PC 감염 유무는 이번 작업으로 판정하지 않음.

### 배포 결과물

- [실행 파일](../artifacts/Bs2Cooler-win-x64/Bs2Cooler.exe)
- [ZIP 패키지](../artifacts/Bs2Cooler-win-x64.zip)
- [사용 방법](User-Guide.ko.txt)
- [상세 구현 및 검증](IMPLEMENTATION.ko.md)
- 실행 파일 SHA-256: `AC77409CA4AC7EC44DA510F479F7461E60B1BCE34B51452078064A6F8BCDCAD2`

`.analysis/`와 `artifacts/`는 로컬에만 보관하고 Git 추적에서 제외한다.
이 보고서·소스·사용 문서는 Git 관리 대상 파일로 작성했으며, 아직 커밋하지 않았다.

### 남은 항목 및 범위

QC/PD 전원의 고속 회전, 스마트 모드 해제, 케이블 분리/절전 복귀, 장치 전원을 완전히 껐다 켠 뒤의
냉간 시작은 아직 실기 검증하지 않았다. 자동 재검색과 오류 처리는 구현했으며 물리적 재연결 시험과 구분한다.

현재 버전은 수동 속도/단계와 조명 켜기·끄기를 지원한다.
온도 기반 자동 곡선, Bluetooth, RGB 색상/효과 편집, 펌웨어 업데이트는 포함하지 않는다.

## 2026-10-16 00:50:35 KST — 작업 보고서 작성 체계 추가

- 요청: 작업 내역을 날짜·시간이 있는 특정 마크다운 보고서로 확인할 수 있게 작성.
- 변경: 이 파일에 기존 작업과 검증 근거를 정리하고 README에 링크 추가.
- 유지 규칙: 저장소 `AGENTS.md`에 향후 작업도 이 파일에 KST로 누적하도록 명시.
- 검증: 문서의 날짜·시간을 실행 기록/파일 시각과 대조하고 UTF-8, 로컬 링크, diff 공백 오류 확인.
- 영향: 문서와 기록 지침만 변경. 앱 코드·배포 EXE·쿨러 설정은 변경하지 않음.
- 다음 기록: 실제 추가 작업을 수행한 후 이 파일 하단에 같은 형식으로 추가.

## 2026-10-16 00:52:59 KST — 완료 시 빌드·검증·Git 업로드 절차 적용

- 요청: 작업 완료 시 빌드/컴파일을 수행하고 Git 원격 저장소에 업로드.
- 변경: `AGENTS.md`에 Release 빌드, 필요한 검증, 작업 보고서 기록, 커밋 및 기존 origin 푸시를 완료 절차로 명시.
- 이번 업로드 범위: 이전 작업에서 작성한 독립 WPF 앱, Core/Windows 통신, 테스트·진단·게시 도구, 사용 문서 및 보고서.
- 빌드: `dotnet build Bs2Cooler.sln -c Release` 성공, 경고 0개/오류 0개.
- 테스트: `dotnet run --project tests/Bs2Cooler.Tests -c Release --no-build` 실행, 11개 통과.
- 저장소 확인: `origin/main`과 로컬 HEAD가 동일한 상태에서 시작, 사용자와 무관한 별도 변경 없음.
- 제외 확인: `.analysis/`, `artifacts/`, `bin/`, `obj/`가 Git에서 제외됨을 확인. 제조사 추출 파일과 로컬 실행 파일은 업로드하지 않음.
- 업로드 대상: `https://github.com/Grokeen/Frytigi_BS2_Cooler_Controler.git`, 브랜치 `main`.
- Git 상태: 변경 내용 검토 및 커밋/푸시 준비 완료. 실제 업로드 결과는 실행 후 후속 기록에 추가.
- 영향: 이번 요청에 따른 추가 변경은 작업 지침과 보고서이며, 쿨러 설정은 변경하지 않음.

## 2026-10-16 00:54:19 KST — 소스 및 보고서 Git 업로드 결과 확인

- 코드/문서 커밋: `e02d55db66f526c99eeb2dff75024fb98dd33563`.
- 커밋 제목: `Add standalone BS2 Pro WPF cooler controller and verified work reports`.
- 반영 범위: 독립 WPF 앱과 통신 코드, 테스트/진단/게시 도구, 작업 지침과 보고서를 포함한 29개 파일.
- 업로드: `git push origin main` 성공. 원격 `refs/heads/main`이 위 커밋과 일치함을 `git ls-remote`로 확인.
- 검증: Release 빌드 경고/오류 0개, 프로토콜 테스트 11개 통과, staged diff 검사 통과.
- 제외: 제조사 추출 자료, 원시 진단 로그, 로컬 EXE/ZIP, `bin/obj`는 커밋에 포함하지 않음.
- 기록 정정: 앞선 '아직 커밋하지 않았다' 및 '업로드 준비'는 당시 상태다. 이 기록 시점에는 최초 소스/보고서 업로드가 완료됨.
- 이 업로드 결과 기록은 후속 문서 커밋으로 원격에 추가 반영한다. 동일 작업 내 보고서 보충이며 앱 코드는 바뀌지 않아 추가 빌드/실기 시험은 반복하지 않음.

## 2026-10-16 01:24:17 KST — 온도 기반 자동 제어 0.2.0 구현 및 검증

- 요청: 원본에 있던 PC 발열 감지 및 쿨러 풍량 자동 조절 기능도 분석해 독립 앱에 구현.
- 결과: BS2 Cooler **0.2.0**에 온도 자동 탭, CPU/GPU 온도 표시, 성능/저소음 곡선, 자동 시작/중지와 기존 설정 복원을 추가. 앞부분의 0.1.0 기능/제한 기록은 당시 이력이며 현재 버전은 이 기록을 따른다.
- 원본 분석: 기존 분석 사본의 `IntelligentCoolingService.Services.TemperatureMonitor`, `IntelligentCoolingService`, `TemperatureData`, 쿨러 저장소 및 전원별 곡선 설정을 읽음. CPU/GPU 중 높은 온도에 단계형 곡선을 적용하고 PC에서 목표 RPM을 보내는 구조를 확인. 원본의 LibreHardwareMonitor/PawnIO 의존성과 CPU Package/GPU Core 센서 선택도 확인. 이번 분석에서 제조사 EXE/DLL/드라이버를 실행하지 않음.
- 통신 확인: 자동 진입 `0x23` → 임시 RPM `0x21` → 자동 종료 `0x24`. 수동 모드에서 거부됐던 `0x21`이 자동 모드에서는 성공함을 실물로 확인. 자동 제어 루프는 수동 저장 명령 `0x26`을 반복 사용하지 않음.

### 변경 파일과 동작

| 파일/영역 | 변경 내용 |
| --- | --- |
| `src/Bs2Cooler.Core/ThermalControl.cs` | 유효 온도 확인, 전원별 곡선, 높은 CPU/GPU 선택, 하강 시 2°C 여유, 90°C 고온 최대 풍량 및 87°C 미만 해제 |
| `src/Bs2Cooler.Core/Bs2Protocol.cs`, `src/Bs2Cooler.Windows/Bs2Connection.cs` | 자동 모드 진입 확인, 전원 상한 내 임시 RPM 전송, 자동 종료 |
| `src/Bs2Cooler.Windows/*Temperature*.cs` | 기존 NVIDIA NVML에서 GPU 온도 수집, 별도 구성된 PawnIO가 있을 때만 Intel CPU Package 읽기 |
| `src/Bs2Cooler.App/MainWindow.Automatic.cs`, 화면/스타일 | 약 2초 온도 수집, 자동 제어 상태와 센서 표시, 수동 속도 조작 차단, 중지/정상 종료 시 복원 |
| `src/Bs2Cooler.App/MainWindow.xaml.cs` | 자동 시작/재연결 작업 중 종료 요청이 오면 해당 작업이 끝난 뒤 복원/종료하도록 처리 |
| `tests/Bs2Cooler.Tests`, 진단/UI 도구 | 온도 정책·경계값 테스트 및 실물 자동 시작/중지/복원 검증 추가 |
| `third_party/PawnIO.Modules/0.2.11`, `THIRD_PARTY_NOTICES.txt` | 공식 서명 IntelMSR 모듈과 대응 소스·LGPL 고지 포함 |
| `tools/Publish-App.ps1`, README, 사용 설명서, 자동 제어 문서 | EXE/ZIP 생성에 고지와 모듈 소스 포함, 사용 방법 및 현재 제한 명시 |

센서 값이 없거나 비정상/8초 초과이면 0°C로 계산하지 않고 현재 전원에서 허용하는 최대 RPM을 선택한다.
CPU/GPU 기준은 두 센서가 모두 유효해야 시작하며, GPU만 사용할 때는 화면에 별도 기준으로 표시한다.
USB 연결이 끊기면 자동 제어를 중지하고 재연결 후에는 사용자가 다시 시작한다.

### 실제 검증과 결과

- 환경: Dell G15 5530, Intel i7-13650HX, NVIDIA RTX 4060 Laptop, BS2 Pro 펌웨어 `0.0.3.3`, 일반 전원 최대 2700 RPM.
- 원본 서비스는 `Stopped`, 원본 Space Station 프로세스는 없는 상태에서 검증함.
- GPU 온도는 기존 Windows System32의 `nvml.dll`로 실측. 해당 DLL의 Microsoft WHQL 서명 유효 및 `nvidia-smi` 조회도 확인. 시험 중 약 49~54°C.
- `auto-exercise`: GPU 실측 53°C → 자동 목표 **2100 RPM** 적용 성공. 중지 후 시험 직전 목표 **1400 RPM**으로 복원. 저장 속도 `1400,2400,2700,2700` 유지. RGB/단계 표시등 꺼짐 유지.
- WPF 시험: GPU 54°C 표시 → 자동 목표 2100 RPM → 수동 적용 비활성화 → 자동 중지 → 기존 목표 복원, 최소 창 크기 화면 검사 통과. 최신 화면은 로컬 `.analysis/automatic-ui-preview.png`에 보관.
- `dotnet build Bs2Cooler.sln -c Release`: 성공, 경고 **0개**, 오류 **0개**.
- `dotnet run --project tests/Bs2Cooler.Tests -c Release --no-build`: **24개 통과**. 온도 누락/만료/비정상값, CPU/GPU 최댓값, 전원 상한, 하강 여유, 고온 해제, Intel 레지스터 디코딩 포함.
- `dotnet run --project tools/Bs2Cooler.UiSmoke -c Release --no-build -- .analysis/automatic-ui-preview.png --automatic`: 성공.
- `tools/Publish-App.ps1`: 성공. EXE 272,360바이트, ZIP 177,833바이트. EXE SHA-256: `AFAB0C65B549B33FCBC69D45DC4F389CCD9B5FBD76AD8112F9B933304E76D5BA`.
- 테스트/진단 도구도 제조사 런타임에 의존하지 않음. 앱의 드라이버/서비스 설치·시작, 관리자 권한 요청, 인터넷 통신 기능은 추가하지 않음.

### CPU 센서 상태와 남은 검증

- 현재 PC의 Windows ACPI/WMI CPU 온도 경로는 미지원 또는 유효 값 없음. 기존 PawnIO 장치 열기도 Windows 오류 2로 실패했고 등록/실행 중 드라이버 서비스가 없었음.
- 따라서 **현재 실물 검증 완료 범위는 GPU 온도 기반 자동 제어**다. CPU 패키지 읽기 코드는 구현했으나 CPU 실측과 CPU/GPU 기준 실기 시험은 아직 수행하지 못함.
- CPU까지 사용하려면 공식 서명 PawnIO를 관리자 권한으로 별도 설정해야 함. 추가 설치 여부에 관한 사용자 답변은 이 기록 시점까지 없으며 설치/드라이버 시작은 수행하지 않음. GPU 기준은 추가 설치 없이 사용 가능.
- 모듈 출처: 공식 `namazso/PawnIO.Modules` 0.2.11 릴리스. `IntelMSR.bin` SHA-256: `D6ED85D65AB17A22F813EF98207D6D537155EE2DED5976A21CB48413C9B92E5F`. 제조사 폴더에서 모듈을 복사하지 않음. 앱은 온도 관련 MSR 2개의 읽기만 호출하며 쓰기는 노출하지 않음.
- QC/PD 전원에서의 자동 속도 변경, USB 물리 분리/재연결 중 자동 모드 동작, CPU 드라이버 구성 후 실측은 이번 실기 검증 범위에 포함하지 않음. 전원 상한과 센서 오류 처리는 코드 테스트로 검증함.
- CPU 설정 안내와 상세 동작: [AUTOMATIC-COOLING.ko.md](AUTOMATIC-COOLING.ko.md).
- Git: 기존 `origin/main`과 로컬 HEAD 일치 확인. 변경 검토 및 커밋/푸시 결과는 실행 후 다음 기록에 추가함. `.analysis/`, `artifacts/`, `bin/obj`는 업로드에서 제외.

## 2026-10-16 01:27:38 KST — 종료 중 복원 검증 및 최종 패키지 갱신

- 자동 시작이 진행 중일 때 창을 연속으로 닫아도 통신 작업 완료/설정 복원 전에 종료되지 않도록 종료 완료 상태를 구분함. 재연결 작업도 종료 전에 끝나도록 처리함.
- `Bs2Cooler.UiSmoke --close-during-start` 실물 검증: 자동 시작 직후 닫기 2회 요청 → 정상 종료 → 새 HID 연결로 기존 목표 1400 RPM과 수동 모드 복원 확인, 성공.
- 수정 후 Release 빌드 경고/오류 0개, 프로토콜/온도 테스트 24개 통과. 일반 WPF 자동 시작/2100 RPM 적용/중지/기존 목표 복원 및 레이아웃 시험도 재실행해 통과.
- 최종 게시를 다시 수행함. 앞선 01:24 기록의 패키지는 이 결과물로 갱신됨. EXE 272,360바이트, ZIP 177,859바이트.
- **최종 EXE SHA-256**: `77E7C56BB00B2FF298DAAADA2CDFD6D43D6124FBF34046C2E268DAC31F24B68F`.
- ZIP 내 실행 파일, 사용 설명서, 해시 파일, 외부 고지, IntelMSR 모듈/대응 소스/라이선스 포함을 확인함.
- Git staged diff 검토: 자체 코드/문서 공백 오류 없음, 비밀키/인증 토큰 패턴 검색 결과 없음. 공식 외부 소스의 기존 줄 끝 공백은 원문 보존을 위해 수정하지 않았으며 자체 코드 검사와 구분함.
- CPU 드라이버 설치/실측 검증은 여전히 미수행. 제조사 서비스는 중지 상태를 유지함.

## 2026-10-16 01:29:06 KST — 자동 제어 소스 업로드 및 배포 실행 확인

- 기능 커밋: `8621f0723a19c5985f8b3aeffb35657906c91fed` — `Add temperature-based automatic cooling with live GPU sensing`.
- `git push origin main` 성공. `git ls-remote origin refs/heads/main`이 같은 커밋임을 확인함. 기능/문서/테스트/공개 모듈 고지와 소스 등 34개 파일 반영.
- 커밋 식별자가 EXE 버전에도 반영되도록 동일 소스를 다시 게시함. ProductVersion: `0.2.0+8621f0723a19c5985f8b3aeffb35657906c91fed`. 앱 기능 코드는 바꾸지 않았으며 앞서 통과한 검증 결과가 적용됨.
- **현재 최종 배포 EXE SHA-256**: `BEECCE8EE78C1CDD4E3E55F32600A500DB254A2AA093D6E0E93FA999B7B39C8D`. 앞선 해시는 이전 게시본 이력이다. EXE 272,360바이트, ZIP 177,860바이트.
- 배포 EXE를 실행해 `BS2 Cooler` 창과 응답 상태 확인. 제조사 폴더 모듈 로드 0개, 원본 서비스 `Stopped`. 자동 제어는 사용자가 온도 자동 탭에서 시작하도록 둠.
- 업로드 제외: 제조사 추출 자료/원시 로그 `.analysis/`, 로컬 EXE/ZIP `artifacts/`, `bin/obj`.
- CPU 실제 측정은 공식 센서 드라이버가 미설정이므로 아직 미검증. 현재 바로 사용할 수 있는 자동 제어는 GPU 기준이며 CPU 설정 안내는 자동 제어 상세 문서에 기록함.
- 이 업로드 확인 기록은 후속 문서 커밋으로 동일 원격에 반영함. 보고서만 추가하며 이미 검증한 앱 소스/실행 파일은 변경하지 않음.

## 2026-10-16 01:49:40 KST — 사용자 허용에 따른 CPU 센서 설치와 CPU/GPU 자동 제어 검증

- 사용자 답변: **CPU/GPU 모두: 오픈소스 센서 설치·설정 허용**. 앞선 CPU 추가 설치 미허용/미검증 상태는 이 작업 전의 기록이다.
- 결과: 공식 서명 PawnIO 2.2.0을 설치하고 Intel CPU Package 및 NVIDIA GPU 온도를 실제로 읽음. CPU/GPU 중 높은 온도에 따른 자동 제어와 실제 회전수 상승/기존 목표 복원을 확인함. 앱 버전 **0.2.1**.

### 설치 출처와 변경 범위

- 출처: [PawnIO 공식 사이트](https://pawnio.eu/), [공식 설치 릴리스 2.2.0](https://github.com/namazso/PawnIO.Setup/releases/tag/2.2.0).
- 설치 파일: 공식 `PawnIO_setup.exe`, 크기 3,410,960바이트. GitHub 릴리스 자산에 게시된 SHA-256과 다운로드 파일의 해시 일치 확인.
- 설치 파일 SHA-256: `1F519A22E47187F70A1379A48CA604981C4FCF694F4E65B734AAA74A9FBA3032`.
- Authenticode `Valid`, 서명자 `namazso.eu`, 인증서 지문 `F380DCC9F706E2756A5047B832FFE719E1BC35F5`.
- 사용자 허용에 따라 Windows 관리자 실행으로 `-install -silent` 실행, 종료 코드 **0**. 공식 기본판을 설치했으며 unrestricted 옵션은 사용하지 않음.
- 설치 위치 `C:\Program Files\PawnIO`, 표시 버전 `2.2.0.0`. 드라이버 `PawnIO` 실행 상태, 시작 형식 `Manual`(공식 기본 설정).
- 실제 드라이버는 Windows DriverStore의 `PawnIO.sys`. SHA-256 `FCA6E7D58B0CF38DBB913A2B9E532F48629145D395F454B16A9F58E97B8D3940`. Microsoft Windows Hardware Compatibility Publisher 서명 `Valid` 확인.
- 제조사 폴더의 센서/드라이버를 재사용하지 않았으며 LibreHardwareMonitor 전체 라이브러리를 추가 설치하지 않음. 기존의 공개 서명 IntelMSR 모듈로 필요한 온도 레지스터만 읽음.

### 앱 변경

- 공식 드라이버는 일반 사용자 CPU 접근을 거부함을 확인. 드라이버의 접근 권한은 변경하지 않고 기본 설정 유지.
- `IntelTemperatureReader.RequiresElevation` → 온도 스냅샷 → WPF에 권한 필요 상태를 전달함. CPU가 없는 경우와 권한 부족을 화면에서 구분.
- **CPU 온도 권한으로 다시 실행** 버튼 추가. 버튼을 직접 누를 때만 Windows 관리자 실행을 요청함. 자동 제어가 진행 중이면 먼저 중지/원래 설정 복원 후 재실행.
- 기본 온도 기준을 **CPU/GPU 중 더 높은 온도**로 설정. GPU 기준만 사용하는 선택지도 유지.
- 자동 제어는 약 1초마다 임시 RPM `0x21`을 갱신하도록 조정. 수동 저장 속도를 반복 변경하지 않음.
- 진단 도구에 `thermal --require-cpu`, `auto-exercise --cpu-gpu` 추가. 실제 회전수 수렴과 저장 속도/목표 복원도 확인.
- UI 도구에 `--cpu-gpu`, `--expect-elevation` 추가. 사용 설명서와 현재 동작 문서를 갱신함.

### 실물 검증 결과

| 검사 | 실제 결과 |
| --- | --- |
| 최초 관리자 센서 조회 | CPU Package 72°C, NVIDIA GPU 49°C 읽기 성공 |
| 일반 권한 WPF | CPU 값 없음/권한 버튼 표시, CPU/GPU 자동 시작 차단, GPU 49°C 표시, 레이아웃 통과 |
| CPU/GPU 자동 명령 | CPU 78°C / GPU 50°C → 높은 CPU 온도 78°C 기준 → 목표 2700 RPM 적용 성공 |
| 실제 회전수 | 약 1400 RPM에서 점진 상승해 **2600 RPM** 도달. 목표 2700 RPM의 진단 허용 오차 ±150 RPM 이내 확인 |
| 중지/복원 | 기존 목표 **1400 RPM** 복원, 저장 속도 **1400,2400,2700,2700** 유지 |
| CPU/GPU WPF | CPU 60°C / GPU 49°C → 목표 2400 RPM 표시, 자동 시작/수동 차단/중지/기존 목표 복원 및 최소 크기 레이아웃 통과 |
| 원본 의존성 | 최종 조회 시 Flydigi/Space Station 서비스 0개, 원본 폴더 프로세스 0개. 해당 제거 작업을 이번에 수행하지 않았으며 조회 결과만 기록 |

- 초기 진단은 수렴 제한이 7초여서 실제 회전수가 목표 범위에 들어오기 전에 실패함. 실패 시에도 `finally`에서 기존 목표와 저장 속도 복원을 확인함. 팬의 완만한 가속을 관찰하도록 최대 35초로 조정하고 1초마다 임시 목표를 갱신하는 검사로 재실행해 통과함. 이 기록은 목표 설정 ACK만으로 실제 회전수 도달을 단정하지 않음.
- 관리자 실기 검증 로그 `.analysis/cpu-gpu-verification.txt`, WPF 화면 `.analysis/cpu-gpu-ui-preview.png`, 일반 권한 화면 `.analysis/cpu-permission-ui.png`에 로컬 보관. 두 화면을 열어 표시/레이아웃을 확인함.
- `dotnet build Bs2Cooler.sln -c Release`: 성공, 경고 **0개**, 오류 **0개**.
- `dotnet run --project tests/Bs2Cooler.Tests -c Release --no-build`: **24개 통과**.
- QC/PD 전원 및 재부팅 후 드라이버 로드 상태는 이번 실물 검증에 포함하지 않음. 현재 PC의 일반 전원/USB와 설치 직후 실행 상태를 확인한 결과임.
- 앱/게시 스크립트에 드라이버 설치·시작이나 인터넷 통신을 추가하지 않음. 이번 설치는 사용자가 별도로 허용한 환경 구성 작업이며 설치 파일과 원시 로그는 Git/배포 ZIP에 포함하지 않음.
- 남은 마무리: 변경 검토, 커밋/원격 푸시, 커밋 식별자가 포함된 EXE/ZIP 게시와 실행 확인. 실제 결과는 다음 기록에 추가함.

## 2026-10-16 01:52:00 KST — 권한 안내 및 GPU 기준 회귀 검증

- CPU/GPU에서 GPU 기준으로 바꿀 때 이전 CPU 권한 안내가 남지 않도록 선택 변경 시 센서 준비 메시지를 다시 계산함.
- 최종 코드 Release 빌드 성공, 경고/오류 0개, 프로토콜/온도 테스트 24개 통과.
- 일반 권한 `Bs2Cooler.UiSmoke --expect-elevation --automatic`: CPU 권한 버튼/CPU 누락 시 시작 차단 확인 후 GPU 기준으로 변경 → 실측 51°C/목표 2100 RPM → 시작/중지/기존 목표 복원 통과.
- 앞서 통과한 관리자 CPU/GPU 실기 및 WPF 시험과 함께, CPU 드라이버 설치 후에도 일반 권한 GPU 제어가 유지됨을 확인함.
- Git 변경 파일/공백 검토 통과. 최종 배포본과 원격 업로드 결과는 다음 기록에 추가함.

## 2026-10-16 01:53:04 KST — 0.2.1 게시 및 Git 업로드 완료

- 기능 커밋: `88cba9e0d8d29db0b9399b4ba39839f6b7596cb2` — `Enable verified CPU and GPU cooling with explicit sensor elevation`.
- `git push origin main` 성공. `git ls-remote`로 원격 `refs/heads/main`이 같은 커밋임을 확인함. 소스/진단/UI 도구/문서/보고서 15개 파일 반영.
- staged diff 공백 검사 통과, 비밀키/인증 토큰 패턴 검색 결과 없음. 설치 파일, 원시 로그, 로컬 EXE/ZIP은 Git에 포함하지 않음.
- `tools/Publish-App.ps1` 성공. EXE 버전 `0.2.1+88cba9e0d8d29db0b9399b4ba39839f6b7596cb2`, 크기 276,968바이트. ZIP 크기 179,284바이트.
- **현재 최종 EXE SHA-256**: `53CB02B6C41908AE240050B0D9FD9FF4725EC6B0D61A666A85F65F99539E5853`.
- ZIP에 EXE/사용 설명서/해시/외부 고지와 IntelMSR 모듈 대응 소스·라이선스 포함 확인. PawnIO 설치 파일은 포함하지 않음.
- CPU 온도를 사용할 수 있도록 배포 EXE를 Windows 관리자 실행으로 시작했고 `BS2 Cooler` 창과 응답 상태를 확인함. 팬 제어는 사용자가 온도 자동 탭에서 시작하도록 둠.
- 이후 일반 실행에서 CPU 권한 안내가 표시되면 **CPU 온도 권한으로 다시 실행** 버튼을 사용하면 됨. 현재 PC의 공식 드라이버 설치는 완료됐으므로 매번 다시 설치할 필요는 없음.
- 이 최종 확인 기록은 후속 문서 커밋으로 동일 원격에 반영함. 검증 완료된 앱 소스와 배포 EXE는 변경하지 않음.

## 2026-10-16 02:10:19 KST — RGB 색상/효과 세부 설정 0.3.0 구현 및 실물 검증

- 요청: 조명/RGB의 세부 설정도 가능하게 추가.
- 구현: **RGB 조명 → 세부 설정** 창에서 8가지 효과, 밝기 0~100%(화면 5% 단위), 속도 1~10, HEX 색상 6개/색상 미리보기, LED별 고정 색상, 호흡 1~5색을 편집하고 적용하도록 추가함.
- 효과: 단색, 호흡, 무지개, 파도, 점멸, 흐르는 색상, LED별 고정 색상, 색상 회전. 고정 색상에는 효과 속도를 적용하지 않으며 무지개는 고정 7색을 사용.
- 편집/기본값/마지막 적용값 불러오기는 장치를 바꾸지 않음. **조명 설정 적용**을 눌렀을 때 색상표와 RGB 켜기 체크 상태를 전송. 창을 닫으면 미적용 편집은 폐기하고 이미 적용한 설정은 유지함.

### 분석 및 변경 파일

| 파일/영역 | 내용 |
| --- | --- |
| 기존 분석 사본 `SetRgbConfigCommandFactory`, `CoolerDataMapper` | 제조사 코드를 실행하지 않고 RGB 표 형식과 전송 절차 확인 |
| `src/Bs2Cooler.Core/RgbLighting.cs`, `Bs2Protocol.cs` | 독립 효과 생성, 범위/HEX 검사, 186바이트 직렬화, 준비/조각/완료 명령 생성 |
| `src/Bs2Cooler.Windows/Bs2Connection.cs` | RGB 전체 전송 동안 연결 명령 잠금 유지, 각 응답 성공 확인, 팬 명령과 조각 전송이 섞이지 않도록 처리 |
| `src/Bs2Cooler.Windows/RgbSettingsStore.cs` | 마지막 적용값 로컬 JSON 저장/검증/불러오기, 임시 파일 후 교체 |
| `src/Bs2Cooler.App/RgbWindow.xaml(.cs)` | RGB 편집 창, 색상 미리보기, 적용/오류 표시 및 적용 중 종료 방지 |
| 메인 화면/스타일/버전 | 세부 설정 버튼, 작은 창 배치 조정, 글자 색상 상속 수정, 버전 0.3.0 |
| 테스트 및 진단/UI 도구 | RGB 데이터/분할/체크섬 테스트 10개 추가, 8효과 실물 전송 및 WPF 적용/저장/복원 시험 |
| `docs/RGB-LIGHTING.ko.md`, 사용 설명서/README/구현 기록 | 기능 사용법, 조회/복원 제한과 실제 검증 범위 기록 |

- RGB 데이터는 버전 0.2, LED 6개 × 10프레임, 헤더 포함 **186바이트**. `0x41` 준비 → `0x47` 19조각(10바이트 × 18 + 마지막 6바이트) → `0x43` 완료 후 `0x46` 켜기 상태 적용.
- WPF는 적용 중 기존 폴링이 끝나기를 기다린 뒤 RGB 전송을 진행하고 자동 팬 제어를 재개함. RGB 적용으로 팬 모드/단계/저장 속도/단계 표시등을 쓰지 않음.
- 저장 위치 `%LocalAppData%\Bs2Cooler\rgb-settings.json`. 앱 재시작이나 USB 재연결 시 저장된 조명을 자동 전송하지 않음.

### 기존 색상 보존에 관한 확인 및 실제 변경

- 분석한 SDK에서 RGB 켜짐/꺼짐 조회는 확인했지만 **기존 색상표를 읽는 명령은 확인하지 못했으며 새 앱도 이를 지원하지 않음**. 따라서 최초 기존 색상표의 정확한 백업/복원은 수행하지 못함.
- 이 제한을 사용자에게 진행 상황으로 안내한 뒤 실물 전송을 시험함. 시험 중 RGB를 꺼서 점멸 효과가 실제 발광하지 않게 함.
- 최초 시험 종료 후 첫 앱 적용값으로 **청록 단색 `#55CBB0`, 밝기 50%**를 저장/적용. 원래 RGB 꺼짐과 단계 표시등 꺼짐은 유지함. 이것은 원본 색상표 복원과 구분함.
- 이후 WPF 시험에서는 이미 저장된 앱 설정이 있으므로 시험용 LED별 색상/밝기를 적용한 뒤 해당 앱 설정을 정확히 다시 불러와 적용/저장하고 RGB 켜짐 상태도 유지함.

### 검증 결과

- 실물 BS2 Pro `37D7:1002`, 펌웨어 `0.0.3.3`: **8효과 모두 준비/19조각/완료 명령 성공 응답** 확인. 시험 설정 밝기 35%, 속도 8, 호흡 색상 5개.
- 시험 후 RGB/단계 표시등 꺼짐, 팬 목표 **1400 RPM**, 저장 속도 **1400,2400,2700,2700** 유지 확인.
- 일반 권한 WPF: HEX 오류 거부 → LED별 고정 색상/색상 1 `#123456`/밝기 35% 적용 → 로컬 저장 확인 → 이전 앱 색상표/켜짐 상태 복원. RGB 권한에 관리자 실행이 필요하지 않음도 확인.
- GPU 자동 제어 중 GPU 53°C/목표 2100 RPM 상태에서 같은 RGB 편집/실물 적용/복원 성공. 자동 제어 중지 후 기존 목표 1400 RPM 복원 확인.
- 관리자 CPU/GPU 기준에서도 CPU 약 63~64°C / GPU 53°C/목표 2400 RPM 상태에서 RGB 적용/복원과 팬 자동 제어 중지/1400 RPM 복원 성공.
- UI 기본/최소 크기 검사 및 `.analysis/rgb-ui-preview.png`, `.analysis/rgb-main-ui-preview.png`를 열어 글자/색상표/버튼/레이아웃 확인.
- `dotnet build Bs2Cooler.sln -c Release`: 최종 성공, 경고 **0개**, 오류 **0개**.
- `dotnet run --project tests/Bs2Cooler.Tests -c Release --no-build`: **34개 통과**. 기존 24개와 RGB HEX/범위/효과 표/LED 순서/호흡 프레임/19조각/체크섬 테스트 10개.
- 실물 진단 `rgb-exercise`, 일반 UI `--automatic --rgb --expect-elevation`, 관리자 UI `--cpu-gpu --rgb` 최종 통과.
- 초기 빌드에서 WPF 기본 `Effect` 멤버와 이름 충돌 및 테스트의 의도한 Dispatcher 비동기 예약 경고를 수정함. 초기 최소 크기 검사에서 조명 정보가 잘린 문제는 버튼 가로 배치로 수정했고, 밝은 콤보박스의 흰 글자는 스타일 상속 수정 후 재검증함. 실패한 중간 결과를 최종 통과로 계산하지 않음.
- **검증 한계**: 장치 응답과 전송 데이터/켜짐 상태는 확인했으나 실제 발광 색상/방향/효과 모양은 카메라나 육안으로 확인하지 않음. 장치 색상표 읽기, 전원 재인가 후 RGB 유지도 미검증.
- 제조사 런타임, 외부 NuGet 패키지, 네트워크 통신을 추가하지 않음. 분석 사본/원시 로그/화면 이미지는 `.analysis/`에 로컬 보관하며 Git에서 제외.
- 마무리: 변경 검토/커밋/푸시와 EXE·ZIP 게시 결과는 실제 수행 후 다음 기록에 추가함.

## 2026-10-16 02:12:49 KST — RGB 0.3.0 배포 및 Git 업로드 확인

- 기능 커밋 `2deac07528beb8883b8730bf2bb32497dd1a807a` — `Add detailed RGB effects, per-LED colors, brightness and speed`.
- `git push origin main` 성공. `git ls-remote`로 원격 `refs/heads/main`이 같은 커밋임을 확인함. RGB 기능/화면/테스트/문서/보고서 20개 파일 반영.
- staged diff 공백 검사와 비밀키/인증 토큰 패턴 검사 통과. `.analysis/`, `artifacts/`, 사용자 로컬 RGB JSON, `bin/obj`는 Git에서 제외.
- `tools/Publish-App.ps1` 성공. EXE 버전 `0.3.0+2deac07528beb8883b8730bf2bb32497dd1a807a`, 크기 301,032바이트. ZIP 189,989바이트.
- **현재 최종 EXE SHA-256**: `197EE67E031FF431610A4785972577A4F3A1363F90866548F992BA6F043E08C2`.
- ZIP의 EXE/사용 설명서/해시/고지와 기존 IntelMSR 대응 소스·라이선스 포함 확인. 드라이버 설치 파일은 포함하지 않음.
- 배포 EXE를 기존 CPU/GPU 온도 읽기를 사용할 수 있도록 관리자 실행으로 시작하고 `BS2 Cooler` 창/응답 확인. 팬 자동 제어나 조명을 앱 시작만으로 켜지 않음.
- 사용 경로: **RGB 조명 → 세부 설정**. 현재 RGB는 기존처럼 꺼짐이며 발광을 원하면 편집 창의 **RGB 조명 켜기**를 체크하고 **조명 설정 적용**을 누름.
- 이 배포/업로드 확인 기록을 후속 문서 커밋으로 원격에 반영함. 이미 검증한 앱 코드/실행 파일은 변경하지 않음.
