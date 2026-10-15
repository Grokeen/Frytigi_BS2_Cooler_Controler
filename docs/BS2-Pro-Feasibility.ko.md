# Flydigi BS2 Pro 쿨러 전용 WPF 앱 구현 가능성

후속 작업으로 독립 WPF 앱 0.1.0을 구현했다. 현재 기능과 실물 제어 검증 결과는
[구현 기록](IMPLEMENTATION.ko.md)과 [README](../README.md)를 따른다.
아래 내용은 실제 앱을 구현하기 전의 초기 분석 기록이다.

분석일: 2026-10-16 (한국 시간)

## 판단

**구현 가능하다.** 설치된 프로그램에서 BS2 Pro 식별, HID 통신, 팬 속도/단계,
스마트 모드, 조명 설정을 담당하는 코드를 확인했다. WPF에서 필요한 통신 로직을
독립적으로 구현할 수 있으며 Electron 화면이나 원본 Windows 서비스가 필수인
구조는 아니다.

설치본의 정적 분석, 오프라인 패킷 비교에 더해 **실물 BS2 Pro의 USB 연결과 별도 WPF
검증 프로세스에서 직접 보낸 RPM 조회 명령의 정상 응답까지 확인**했다.
마지막 조회 당시 현재 속도는 `2300 RPM`, 목표 속도는 `2400 RPM`이었다.
팬 속도/모드/조명 설정 변경은 실행하지 않았으므로, 쓰기 명령의 실제 효과와
원본 서비스가 없는 상태에서의 전체 초기화는 다음 구현 단계에서 검증해야 한다.

## 조사 대상과 구조

분석 대상은 `C:\Program Files\Flydigi Space Station`의 설치 파일이다.

| 구성 | 확인 내용 |
| --- | --- |
| `Flydigi Space Station.exe`, `resources/app.asar` | Electron + React 화면. `package.json` 버전 `4.2.0.9` |
| `SpaceStationService.exe` | .NET 단일 파일 번들. 파일 버전 `4.1.2`, 번들 내부 항목 250개 |
| `Flydigi.CoolerSdk.dll` | 쿨러 발견, 연결, 명령 생성, 상태 처리. 번들 내부 크기 72,704바이트 |
| `Flydigi.Hid.dll`, `HidApi.Net.dll`, `hidapi.dll` | HID 열거, Report Descriptor 확인, 읽기/쓰기 |
| `Flydigi.Basic.dll` | 공통 명령 프레임과 체크섬 |
| `Flydigi.SharedResources.dll`, `Google.Protobuf.dll` | 화면과 서비스 사이의 명령/데이터 모델 |
| `IntelligentCoolingService.dll`, `LibreHardwareMonitorLib.dll` | CPU/GPU 온도 수집과 온도 기반 팬 속도 계산 |

기존 앱의 기본 흐름:

```text
Electron/React 화면
  → 이름 있는 파이프 fcs.sock / Protobuf
  → SpaceStationService의 CoolerBusinessService / CoolerRepository
  → CoolerSdk / Hid
  → Windows HID 장치
```

새 앱은 `WPF → 자체 쿨러 서비스 → Windows HID → BS2 Pro`로 구성할 수 있다.
원본 서비스의 파이프에 연결하는 방법도 있지만, 그 방식은 원본 설치와 서비스 실행에
의존하므로 완전한 독립 앱과는 구분된다.

## 장치 식별 및 연결

`CoolerFactory.CreateCooler()`에서 확인한 매핑:

- Vendor ID: `0x37D7` (14295)
- BS2 Pro Product ID: `0x1002` (4098)
- 내부 코드: `bs2pro`
- 표시 이름: `Flydigi BS2 Pro`

이 값은 설치본의 매핑이며, **현재 PC에 연결된 실물에서도 같은 VID/PID를 확인**했다.
Windows PnP에서 USB 노드와 HID 노드 모두 정상 상태였고, HID 인터페이스는 1개 검색됐다.

원본은 USB와 Bluetooth HID를 지원한다. Bluetooth도 이 코드 경로에서는 Windows가
제공하는 HID 장치로 접근한다. 별도 BLE GATT 프로토콜을 반드시 구현해야 한다는
근거는 발견하지 못했다.

실물 USB Report Descriptor의 Usage Page는 `0xFFA0`, Usage는 `1`이다.
HID Report ID 항목은 없으며, HidSharp에서 확인한 입력 버퍼 길이는 `33`, 출력은 `32`,
Feature는 `0`바이트다. HidSharp는 이 장치의 보고서 버퍼 맨 앞에 `00`을 포함한다.

원본은 USB 입력을 정규화할 때 `03`을 제거하도록 설정한다. 실제 HidSharp 수신 데이터는
`00 03 5A A5 ...`였으므로 **HidSharp 구현에서는 앞의 2바이트를 제거**해야 했다.
출력에서는 `00 5A A5 ...` 형식으로 조회 요청을 보내 정상 응답을 확인했다.
공통 명령 생성 단계의 `01`을 그대로 전송하면 이 장치의 실제 형식과 다르다.
입출력의 비대칭과 라이브러리별 버퍼 규칙을 구분해야 한다.

## 분리 구현할 수 있는 기능

아래 명령은 서비스 IPC 번호가 아닌 **장치 프로토콜의 명령 바이트**다.

| 기능 | 명령 | 판단 및 조건 |
| --- | --- | --- |
| 펌웨어 버전 읽기 | `0x01` | 초기 연결 확인에 사용 가능 |
| 현재/목표 RPM 읽기 | `0x22` | 응답의 RPM 값은 16비트 little-endian |
| 목표 RPM 설정 | `0x21` | 수동 속도 제어 및 PC에서 계산한 자동 속도 적용에 사용 |
| 단계와 RPM 함께 설정 | `0x26` | 4개 단계. 전송 단계 값은 `0..3` |
| 단계별 저장 속도 읽기 | `0x27` | 각 단계 RPM 조회 |
| 스마트 모드 진입/해제 | `0x23` / `0x24` | 온도 곡선 계산은 별도 PC 로직 필요 |
| 전원 공급 시 자동 시작 | `0x0C` | 사용 `1`, 해제 `2` |
| 스마트 시작/정지 설정 | `0x0D` | 비활성/즉시 정지/지연 정지의 모드 값 확인 |
| RGB 켜기/끄기 | `0x46` | 사용 `1`, 해제 `0` |
| 단계 표시 조명 | `0x48` | 사용 `1`, 해제 `0` |
| RGB 효과 데이터 전송 | `0x41` → `0x47` → `0x43` | 준비 → 인덱스와 10바이트 단위 데이터 → 완료 |
| 비동기 장치 상태 수신 | `0xEF` | 현재/목표 RPM, 전원 공급 상태, 스마트 모드/조명 플래그 등 |

펌웨어 업데이트, 공장 초기화 명령도 설치본에 있으나, 쿨러 전용 제어 앱의 첫 구현 범위에
포함할 필요는 없다. 이번 조사에서는 어떤 장치 설정도 변경하지 않았다.

원본의 BS2 Pro 최대 속도 매핑은 일반 전원 `2700`, QC 상태 `3300`, PD 상태 `4000 RPM`이다.
새 앱은 수신한 전원 상태와 장치 정보를 기준으로 입력 범위를 정해야 한다.
오프라인 패킷 비교에 사용한 RPM 값이 모두 현재 장치에서 허용된다는 의미는 아니다.

## 확인한 패킷 형식

설치된 SDK가 생성하는 신규 프로토콜 명령 배열은 32바이트다.

```text
[Report ID] 5A A5 [명령] [payload 길이 + 2] [payload ...] [합계 체크섬] [0 패딩 ...]
```

체크섬은 명령 바이트부터 payload 마지막까지의 합계 하위 8비트다.
원본 함수 이름은 `Crc`지만 실제 계산은 단순 가산 체크섬이다.

예: 2단계, 1700 RPM을 지정하는 원본 명령 생성 결과:

```text
01 5A A5 26 05 01 A4 06 D6 00 ...
```

`01`은 생성 단계의 Report ID 자리 표시자다. 이번 실물 USB에서는 `00`으로 교체한다.
이 속도 설정 예시는 장치에 전송하지 않았다.

수신 데이터는 앞 절의 접두부를 제거한 뒤 `5A A5 [명령] ...` 형식으로 처리한다.
장치 설정 명령의 성공 응답은 원본에서 주로 정규화된 `data[4] == 1`을 확인한다.
새 구현은 길이, 명령 ID, 응답 상태를 함께 확인하고 명령 큐와 시간 초과 처리가 필요하다.

원본의 사용되지 않는 상태 파서와 실제 서비스의 상태 처리 코드 사이에 일부 비트 해석 차이도
존재한다. 실제 heartbeat 캡처로 전원/단계/모드 해석을 검증해야 하며, 디컴파일 코드를
그대로 복사하는 방식으로 완성을 판단하면 안 된다.

## 온도 기반 자동 제어

수동 RPM 제어와 CPU/GPU 온도 기반 자동 제어는 구현 난도가 다르다.

원본은 LibreHardwareMonitor로 CPU/GPU 센서를 읽고 PC에서 곡선을 계산한 뒤
`SetFanSpeed` 명령을 보낸다. `SetSpeedCurve`는 서비스 IPC 기능이며, 분석한 코드에서는
온도 곡선을 쿨러에 저장하는 단일 장치 명령으로 확인되지 않았다.

원본의 온도 모니터 초기화에는 PawnIO 드라이버 로드 로직이 있다. 같은 방식을 사용할 경우
센서 호환성, 드라이버/권한 처리, 센서 값이 없을 때의 동작을 별도로 구현·검증해야 한다.
이번 조사에서는 드라이버를 설치하거나 시작하지 않았고 온도 수집도 실행하지 않았다.

첫 버전은 장치 검색, 현재 RPM 표시, 4단계/수동 RPM, RGB 켜기/끄기로 구성하고,
그다음 온도 곡선과 트레이/자동 실행을 추가하는 순서가 적절하다.

## 실제 수행한 검증

1. 설치 파일을 읽기 전용으로 열고 Electron ASAR 및 .NET 번들의 분석용 사본을 로컬
   `.analysis/`에 추출했다. 설치 폴더의 파일은 수정하지 않았다.
2. ILSpy CLI `9.1.0.7988`로 쿨러 SDK, 공통 명령, HID, 서비스의 호출 경로를 확인했다.
3. 검증용 `net9.0-windows` / `UseWPF=true` 프로젝트를 빌드·실행해 WPF Slider를 생성했다.
4. 독립 패킷 생성 코드와 설치본 SDK의 패킷을 비교했다. 7개 RPM × 5개 속도/단계 조합,
   조회/모드 전환/조명/자동 시작을 합해 **46개가 바이트 단위로 일치**했다.
5. 사용자가 같은 PC에서 USB로 정상 제어한다고 확인한 후 다시 검색했다.
   전체 HID 인터페이스는 11개, 그중 BS2 Pro `37D7:1002`는 1개였다. 원본 앱과
   `Flydigi Space Station Service`의 실행 및 서비스의 `bs2pro` 연결 로그도 확인했다.
6. 별도 WPF 검증 프로세스에서 장치를 열어 Report Descriptor를 읽고 비동기 상태
   `0xEF`를 수신했다. HidSharp 입력 접두부 `00 03`을 정규화한 뒤 RPM을 해석했다.
7. **자체 패킷 생성 코드로 읽기 전용 `GetFanSpeed (0x22)` 요청을 보내고 실제 응답을
   받았다.** 원본 SDK는 이 요청의 생성·전송에 사용하지 않았다. 물리적 통신에는 HidSharp를 사용했다.

```text
요청: 00 5A A5 22 02 24 00 ...      (총 32바이트)
응답: 00 03 5A A5 22 06 FC 08 60 09 95 00 ...
해석: 현재 0x08FC = 2300 RPM / 목표 0x0960 = 2400 RPM
```

초기 조사 시에는 장치가 검색되지 않았으나 후속 검색에서 정상 연결을 확인했다.
연결 오류 원인을 단정하거나 드라이버/서비스 설정을 수정하지 않았다.
실기 검증은 원본 프로그램도 실행된 상태에서 수행했다. 원본 없이 시작하는 경우의
초기화, heartbeat 활성화 여부, 자동 재연결은 아직 검증하지 않았다.

검증용 프로젝트와 결과는 `.analysis/probe/`, `.analysis/probe-results.txt`에 있다.
이 프로젝트는 오프라인 대조를 위해 제조사 DLL을 참조하는 **분석용 실행 코드**이며,
완성된 제어 앱이나 독립 배포본은 아니다. 실제 조회 요청 생성과 HID 송수신은 자체 코드로
수행했지만, 제조사 DLL 참조를 제거한 최종 배포 프로젝트는 아직 만들지 않았다.

## 신규 WPF 프로젝트 제안

```text
Bs2Cooler.Wpf       장치 상태, 속도/단계 설정, 조명 화면
Bs2Cooler.Core      명령/응답 모델, 체크섬, 상태 파싱
Bs2Cooler.Hid       장치 발견, descriptor 기반 report 처리, 비동기 I/O
Bs2Cooler.Thermal   선택 기능: 온도 센서, 곡선 계산, 자동 제어
```

원본 설치 DLL에 의존하지 않는 통신 코드를 작성할 수 있다. 수동 제어만 필요하면
온도 모니터와 원본의 컨트롤러/키보드/네트워크/펌웨어 업데이트 기능을 포함할 이유가 없다.
현재 PC에는 .NET SDK `9.0.305` 및 WPF 실행 환경이 있어 검증 프로젝트 빌드가 가능했다.
위 프로젝트 구성은 설계 제안이며, 실제 앱 프로젝트는 아직 생성하지 않았다.

장치 ID/Report Descriptor 획득과 직접 RPM 조회 응답 수신은 완료했다.
남은 실기 확인은 수동 속도 변경 후 RPM 피드백 비교, 원본 프로그램 없이 시작하기,
재연결, 슬립 복귀, 설정을 변경할 때의 동시 실행 충돌 여부다.
이 검증 후에 실제 사용 가능한 앱으로 판단할 수 있다.

## 분석 근거 위치

다음 파일은 이번 실행 환경의 `.analysis/`에만 보관한다. 원본 소스/바이너리를 저장소에
포함하는 대신 분석 결과와 진단 스크립트만 Git 관리 대상으로 둔다.

| 근거 | 로컬 분석 파일 |
| --- | --- |
| 장치 ID 및 모델 매핑 | `cooler-sdk/Flydigi.CoolerSdk.hardware/CoolerFactory.cs` |
| USB/Bluetooth 분기 | `cooler-sdk/Flydigi.CoolerSdk.hardware/CoolerHidManager.cs` |
| Report ID 및 실제 쓰기 | `hid-sdk/Flydigi.Hid.data/HidCommunicationProtocol.cs` |
| 공통 프레임 | `basic-sdk/Flydigi.Common.data.command/AbstractCommand.cs` |
| 가산 체크섬 | `basic-sdk/Flydigi.Common.util/ByteExtension.cs` |
| 단계+RPM 명령 | `cooler-sdk/Flydigi.CoolerSdk.data.command.system/SetDeviceGearAndSpeedCommand.cs` |
| 전원별 최대 속도 | `cooler-sdk/Flydigi.CoolerSdk.data.model/VbusStatusExtensions.cs` |
| 자동 제어 호출 경로 | `decompiled/Flydigi.ControllerService.data.CoolerRepository.decompiled.cs` |
| 센서/드라이버 분리 | `intelligent-cooling/IntelligentCoolingService.Services/TemperatureMonitor.cs`, `DriverLoader.cs` |

.NET 번들 추출 형식은 [Microsoft의 번들 매니페스트 구현](https://github.com/dotnet/runtime/blob/main/src/installer/managed/Microsoft.NET.HostModel/Bundle/Manifest.cs)을 참고했다.
장치 제어에 대한 판단은 웹의 일반 설명이 아닌 이 PC에 설치된 파일과 위 실행 결과에 근거한다.
