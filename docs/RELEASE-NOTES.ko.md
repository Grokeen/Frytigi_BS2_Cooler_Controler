BS2 Pro를 제조사 앱/서비스 없이 USB로 직접 제어하는 Windows WPF 앱입니다.

0.4.1 변경: 창 상단에 CPU/GPU 온도 요약을 추가하고 트레이 우클릭 메뉴에서도 온도를 확인할 수 있습니다.
약 2초마다 갱신하며 센서가 없거나 데이터가 만료되면 —로 표시합니다.

- 수동 RPM/4단계 팬 설정, CPU/GPU 온도에 따른 자동 풍량 조절
- 고온/센서 만료 보호와 자동 제어 중지 시 기존 팬 설정 복원
- RGB 8가지 효과, 밝기/속도/HEX 색상/LED별 색상 편집
- 로그인 시 자동 실행, 트레이 백그라운드 제어와 팬 아이콘

`Bs2Cooler-win-x64.zip`을 내려받아 압축을 풀고 `Bs2Cooler.exe`를 실행합니다.
Windows x64와 **Microsoft .NET Desktop Runtime 9 (x64)**가 필요합니다.
로그인 자동 실행 설치는 동봉한 `Install-Desktop.ps1`을 관리자 PowerShell에서 실행합니다.
기본값은 앱만 자동 실행하며 팬 자동 제어 동반 시작은 트레이 메뉴에서 선택합니다.

GPU 온도는 기존 NVIDIA 드라이버를 사용합니다. 현재 CPU 센서는 공식 PawnIO가 별도로 설정된 지원 Intel CPU에서
관리자 권한으로 사용합니다. 드라이버 설치 파일은 이 ZIP에 포함하지 않습니다.

GitHub Windows runner에서 Release 빌드, 장치 없는 프로토콜/온도/RGB 테스트 34개와 ZIP 해시/구성 검사를 수행합니다.
BS2 Pro USB 실물, CPU/GPU 자동 제어, RGB 전송과 트레이 복원은 개발 PC에서 별도로 검증했습니다.
실제 재부팅/절전 복귀와 RGB 발광 모양 확인은 수행하지 않았습니다.

다운로드 ZIP 및 내부 EXE의 해시는 `RELEASE-SHA256SUMS.txt`에서 확인할 수 있습니다.
ZIP에는 한국어 사용 설명서, 자동 실행/온도/RGB 상세 문서, 아이콘, 외부 구성요소의 소스와 라이선스가 포함됩니다.
