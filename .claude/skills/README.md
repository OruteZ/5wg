# Skills

Project-specific skills live here as `.claude/skills/<name>/SKILL.md`, following Claude Code's skill format.

| Skill | 언제 |
| --- | --- |
| `unity-build` | 스크립트를 고친 뒤 컴파일 확인 |
| `unity-scene-check` | 런타임 문제의 원인이 코드가 아니라 씬 배선일 때 |
| `split-commits` | 섞인 변경을 종류별 커밋으로 나눌 때 |
| `git-commit-push` | 그냥 커밋하고 푸시할 때 |

## 새 스킬을 추가하기 전에

**스킬은 공짜가 아니다.** 이름과 설명이 매 세션 컨텍스트에 상주하므로,
`(빈도 × 회당 절감) > (설명 비용 × 모든 세션)`을 넘지 못하면 오히려 손해다.

- 절차가 명확하고 비자명한 것만 넣는다. 판단이 필요한 일은 `.claude/docs/`에 문서로 남긴다.
- 기존 스킬과 설명이 겹치면 잘못 발동한다. 트리거 문구를 명확히 갈라둔다.
- 명령을 적을 때는 **실제로 실행해 검증한 것**만 적는다.
