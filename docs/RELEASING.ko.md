# GitHub 빌드와 Release 게시

PR/main 변경은 `Windows build`에서 전체 Release 빌드, 40개 테스트와 ZIP 게시를 수행합니다.
GitHub Actions 실행 화면의 Artifacts는 14일간 보관하며 실제 USB 장치가 없어도 검증합니다.
센서 드라이버를 설치하거나 실제 팬을 조작하는 진단은 원격 runner에서 실행하지 않습니다.

배포하려면 앱 csproj/manifest 버전, 사용 설명서와 `docs/RELEASE-NOTES.ko.md`를 갱신한 뒤
PR 빌드 성공과 변경 내용을 확인하고 main에 병합합니다. 앱 버전이 `0.4.0`이면 다음 태그를 사용합니다.

```powershell
git tag v0.4.0
git push origin v0.4.0
```

`Windows release` 작업은 태그와 앱 버전의 일치를 확인하고 다시 빌드/테스트합니다.
`tools/Publish-App.ps1`은 사용 설명서/상세 문서/아이콘/외부 고지를 ZIP에 포함하고
`tools/Test-Package.ps1`로 필수 파일, 중복/상위 경로, ZIP 내부 EXE와 해시의 일치를 확인합니다.
성공하면 ZIP과 `RELEASE-SHA256SUMS.txt`를 GitHub Release에 게시합니다.
태그 작업에만 contents 쓰기 권한을 부여하며 개인 토큰을 저장하지 않습니다.

기존 태그/Release 자산을 자동 덮어쓰지 않습니다. 게시 재실행은 이미 Release가 있으면 실패하므로
기존 결과를 확인한 뒤 새로운 버전으로 배포합니다. 사용자 PC의 앱을 자동 업데이트하지 않습니다.
설치된 앱과 원격 빌드의 소스 커밋/SDK가 다르면 EXE 해시도 달라질 수 있습니다.
항상 다운로드한 Release와 함께 제공된 해시 파일로 해당 배포본을 확인합니다.
