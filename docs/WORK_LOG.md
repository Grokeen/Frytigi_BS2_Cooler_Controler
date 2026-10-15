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
