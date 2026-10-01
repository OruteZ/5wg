---
name: docs-audit
description: Check .claude/docs and rules against the actual code, then fix what drifted — dead type names, stale counts, claims that contradict each other, content duplicated across docs. Use before a PR that touched docs, after a rename or refactor, or when asked to review/optimize the docs. 문서 점검, 문서 최신화, 문서 최적화.
---

# docs-audit

문서는 **틀린 채로도 잘 읽힌다.** 그래서 눈으로 훑는 것으로는 안 잡히고, 기계적으로 대조해야 한다.
아래 다섯 개를 순서대로 돌린 뒤 고친다.

가장 흔한 원인은 **리네임·삭제 커밋이 코드만 바꾸고 문서를 두고 간 것**이다. 1번이 그걸 잡는다.

## 1. 문서가 부르는 이름이 코드에 있는가

제일 값어치 있는 검사다. 문서를 읽고 코드를 보러 갔을 때 바로 막히는 종류를 잡는다.

```bash
for n in $(grep -oh '`[A-Z][A-Za-z0-9_]*`' .claude/docs/*.md .claude/rules/*.md CLAUDE.md | tr -d '`' | sort -u); do
  grep -rq "\b$n\b" Assets/_Project/Scripts/ 2>/dev/null || echo "코드에 없음: $n"
done
```

**전부 문제인 것은 아니다.** 이런 것들이 정상적으로 걸린다.

| 걸리지만 정상 | 예 |
| --- | --- |
| 에셋·씬·오브젝트 이름 | `Snare` `Kick` `Prototype` `Card2` |
| 커밋 태그 (`rules/git.md`) | `Feat` `Fix` `Chore` |
| Unity·패키지 API | `CinemachineBrain` `AddForce` `Enter` |
| 컴파일러·빌드 코드 | `CS0618` `CS2002` `MSB3277` |
| **의도적으로 "지웠다"고 적은 옛 타입** | 문장이 과거형인지 확인하고 남긴다 |

마지막 줄이 판단이 필요한 유일한 곳이다. 나머지는 그냥 고친다.

`git log --diff-filter=RD --name-status --format="%h %s" -15 -- '*.cs'`로 최근 리네임·삭제를 보면
왜 어긋났는지가 바로 나온다.

## 2. 같은 내용이 두 문서에 있는가

```bash
cd .claude/docs && grep -h "^| " *.md | grep -v "^| --" | sed 's/ *$//' | sort | uniq -d
```

표가 두 곳에 복사돼 있으면 **한 곳만 갱신되어 서로 모순되는 것이 시간 문제**다.
한 곳으로 모으고 나머지는 포인터(`→ overview.md`)로 바꾼다.

어디를 남길지는 문서의 역할로 정한다 — 구조는 `architecture.md`, 목록·환경은 `overview.md`,
"그때 왜 그렇게 정했나"는 `progress.md`.

## 3. 수치 주장

```bash
grep -rn "[0-9]\+개\|[0-9]\+종\|[0-9]\+축\|[0-9]\+줄\|[0-9]\+마디" .claude/docs/*.md
```

각각 세어본다. 스크립트 수는 `find Assets/_Project/Scripts -name "*.cs" | wc -l`.
임시 수치(클리어 마디 수 등)는 **씬·프리팹에서 실제 값을 확인**한다 — 바꿔놓고 문서를 안 고친 게 흔하다.

## 4. 유예·부재 주장이 아직 참인가

```bash
grep -rn "다음 브랜치\|범위 밖\|대기 중\|하나뿐\|미구현\|UI 없음\| 예정" .claude/docs/*.md
```

"다음 브랜치에서 한다"가 이미 된 일이면 문서가 거짓말을 한다. 10줄 안팎이라 눈으로 다 본다.

## 5. 제목 계층이 실제 구조와 맞는가

```bash
grep -n "^## \|^### \|^#### " .claude/docs/architecture.md
```

하위 절이 상위 절과 같은 `##` 층에 있으면 **어디까지가 그 절인지 알 수 없다.**
문서가 길어질수록 이렇게 무너진다. 목차를 붙였다면 그 목차가 실제 `##`과 일치하는지도 본다.

## 고칠 때

- **기획 문서(`weapons.md` `progression.md`)의 "미정"은 채우지 않는다.** 결정되지 않은 것이지
  빠진 것이 아니다. 구현이 끝난 항목만 "구현 상태"로 따로 적는다.
- **코드와 문서가 다르면 코드가 옳다.** 문서를 코드에 맞춘다. 코드가 기획을 어긴 것 같으면
  고치지 말고 물어본다.
- 지나간 브랜치 절은 지우지 말고 **줄인다.** 구조 설명은 `architecture.md`로 넘기고,
  그때 왜 그렇게 정했는지와 측정 결과만 남긴다. 재검토할 사람이 같은 실험을 다시 하지 않도록.
- 알면서 안 고친 것은 `미해결`에 올린다. 조용히 두면 다음 사람이 버그로 다시 발견한다.

## 마지막

1번을 다시 돌려서 새로 쓴 문장이 없는 이름을 부르지 않는지 확인한다.
