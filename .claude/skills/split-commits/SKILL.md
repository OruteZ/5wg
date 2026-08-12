---
name: split-commits
description: Split a working tree containing mixed changes into several commits grouped by kind. Use when asked to commit separately, by type, or not all at once. 종류별로 나눠서 커밋, 분할 커밋.
---

# split-commits

태그는 `.claude/rules/git.md`를 따른다. 한 커밋에 기능·수정·리팩터링·문서를 섞지 않는다.

## 1. 인덱스를 비우고 시작

```bash
git reset --quiet
```

이전 작업의 스테이징이 남아 있으면 분할이 어긋난다.

## 2. 시간순이 아니라 의존 방향으로 정렬

**이게 핵심이다.** 작업한 순서가 아니라, **뒤 커밋이 앞 커밋의 API를 쓰도록** 배치한다.
그러면 각 커밋 시점에 컴파일이 깨지지 않는다.

예: 무기 시스템이 `Projectile.Launch(..., owner)`를 쓴다면, 나중에 발견한 버그 수정이라도
`Fix: Projectile` 커밋이 `Feat: 무기 시스템`보다 **앞에** 와야 한다.

파일이 여러 그룹에 걸치면(예: 한 파일에 기능 변경과 최적화가 함께 있음) 그 파일은 뒤 그룹에
넣는다. 앞 커밋은 그 파일의 이전 버전으로 남고, 이전 버전도 컴파일된다면 문제없다.

## 3. 그룹별로 스테이징 후 커밋

```bash
git add <그룹에 속한 경로들>
git status --porcelain | grep '^[ARMD]'   # 의도한 것만 올라갔는지 확인
```

Unity 프로젝트면 `.cs`와 `.meta`를 같이 넣는다.

## 4. 커밋 메시지

**본문에 "왜"를 적는다.** 무엇을 바꿨는지는 diff에 있다.

PowerShell에서 여러 줄 메시지를 넘길 때 **메시지에 큰따옴표가 있으면 here-string이 깨진다.**
파일로 넘기는 게 안전하다.

```bash
git commit -F <스크래치패드>/msg.txt
```

## 5. 마무리 확인

```bash
git log --oneline -<개수>
git status --porcelain
```

남은 변경이 의도적으로 제외한 것인지 확인하고, **제외했다면 사용자에게 이유와 함께 보고한다.**
관계없는 파일(사용자 소유 설정, 줄바꿈만 바뀐 파일)을 말없이 끼워 넣지 않는다.
