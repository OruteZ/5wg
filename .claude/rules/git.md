# Git

## 브랜치

- **`main`에 직접 커밋하지 않는다.** 브랜치를 파고 PR로 올린다.
- 종류별로 나눈다. 기능·수정·리팩터링·문서를 한 커밋에 섞지 않는다(→ `split-commits` 스킬).
- 순서는 시간순이 아니라 **의존 방향**. 뒤 커밋이 앞 커밋의 API를 쓰도록 배치하면
  각 커밋 시점에 컴파일이 깨지지 않는다.
- 본문에 **왜**를 적는다. 무엇을 바꿨는지는 diff에 있다.

## Commit naming

Prefix every commit message with one of the following tags.

| Tag | Description |
| --- | --- |
| `Feat` | 새로운 기능을 추가 |
| `Fix` | 버그 수정 |
| `Design` | CSS 등 사용자 UI 디자인 변경 |
| `!BREAKING CHANGE` | 커다란 API 변경의 경우 |
| `!HOTFIX` | 급하게 치명적인 버그를 고쳐야하는 경우 |
| `Style` | 코드 포맷 변경, 세미 콜론 누락, 코드 수정이 없는 경우 |
| `Refactor` | 프로덕션 코드 리팩토링 |
| `Comment` | 필요한 주석 추가 및 변경 |
| `Docs` | 문서 수정 |
| `Test` | 테스트 코드, 리팩토링 테스트 코드 추가, Production Code(실제로 사용하는 코드) 변경 없음 |
| `Chore` | 빌드 업무 수정, 패키지 매니저 수정, 패키지 관리자 구성 등 업데이트, Production Code 변경 없음 |
| `Rename` | 파일 혹은 폴더명을 수정하거나 옮기는 작업만인 경우 |
| `Remove` | 파일을 삭제하는 작업만 수행한 경우 |
