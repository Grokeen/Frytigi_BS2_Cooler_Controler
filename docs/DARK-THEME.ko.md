# 다크 테마와 화면 반응

0.4.3부터 메인과 RGB 설정 창이 같은 네이비 배경, 밝은 글자, 민트 강조색을 사용합니다.
콤보박스는 닫혀 있을 때와 목록을 펼쳤을 때 모두 어두운 색상입니다.
입력창·탭·체크박스·슬라이더·스크롤바·도움말과 트레이 우클릭 메뉴도 같은 색상 체계를 사용합니다.
키보드로 이동한 항목에는 강조 테두리가 표시되며 기존 선택/입력/명령 동작을 유지합니다.

버튼 가리킴과 누름에는 100~120ms, 창/탭/목록 열기에는 180ms, 연결/작업 상태 변경에는 220ms 효과를 사용합니다.
온도와 RPM의 주기적 숫자 갱신에는 애니메이션을 붙이지 않습니다. 숨긴 창에서는 표시 효과를 시작하지 않습니다.
Windows **설정 → 접근성 → 시각 효과 → 애니메이션 효과**를 끄면 이동·페이드 효과를 생략하고
선택/가리킴 상태를 즉시 표시합니다. 앱이 Windows 설정을 변경하지는 않습니다.

지원되는 Windows에서는 기본 창 테두리/최소화/닫기 동작을 유지한 채 다크 제목 표시줄을 요청합니다.
Windows UAC나 기본 메시지 상자는 운영체제의 표시 설정을 따릅니다.

현재 Windows 11 개발 PC에서 실제 WPF 창/펼친 목록을 렌더링해 확인했습니다. 네 콤보박스의
키보드 선택, RGB 편집/전송/복원, 탭 전환과 상태 효과 완료/생략, 기본·최소 창 크기 배치도 검증했습니다.
고대비 테마와 여러 모니터의 서로 다른 DPI 조합은 별도로 검증하지 않았습니다.

구현은 Microsoft의 [ComboBox 템플릿 규칙](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/combobox-styles-and-templates),
[ClientAreaAnimation 설정](https://learn.microsoft.com/en-us/dotnet/api/system.windows.systemparameters.clientareaanimation),
[DWM 창 속성](https://learn.microsoft.com/en-us/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute)을 사용합니다.
날짜·시간별 결과와 배포 정보는 [작업 보고서](WORK_LOG.md)에서 확인합니다.
