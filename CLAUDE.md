# CLAUDE.md

you are Unity Engine Code Agent.



This file only routes to where the real project context lives. Read the referenced files before acting, and keep this index in sync if new categories are added — don't add project details directly here.

- **Rules** — `.claude/rules/`: coding and workflow rules for this repo. Follow them.
  - `unity-csharp.md` — **모든 런타임 C# 코드의 규약.** 2D 차원 규칙, 입력, Awaitable, 메모리, 동시성.
  - `git.md` — 커밋·브랜치 규칙.
- **Skills** — `.claude/skills/`: reusable skills for this project. Check for a matching one before improvising a workflow.
- **Docs** — `.claude/docs/`: project design and status.
  - `overview.md` — 기술 스택, 엔진 버전, 패키지, 폴더·씬 구조, 이 저장소에서 일하는 법.
  - `progress.md` — 브랜치별로 무엇을 왜 했는가. 미해결 목록.
  - `architecture.md` — **런타임 구조 전부.** 스테이지 루프, 성장, 무기 스탯, 풀링, 조준, 물리.
  - `weapons.md` — 무기 기획. 악기 10종, 발사 형태, 채보 기반 타이밍, 스탯 축.
  - `progression.md` — 성장 기획. 경험치 곡선, 레벨업 카드, 박자 동기화.
  - `pickup.md` — 재화·아이템 기획. 티켓, 상점, 회복·자석·리롤. 경험치는 `progression.md`.

기획(`weapons` `progression` `pickup`)과 구현(`architecture` `progress`)을 나눠 둔다.
기획 문서의 **"미정"은 아직 결정되지 않은 것이지 생략된 것이 아니다** — 채우지 말고 물어라.
