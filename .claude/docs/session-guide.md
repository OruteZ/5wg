# Session Guide

새 세션에서 이 저장소를 만졌을 때 빠르게 궤도에 오르기 위한 안내.
규칙(`.claude/rules/`)이 "반드시 지켜야 할 것"이라면, 이 문서는 "알고 있으면 시간을 버는 것"이다.

**규칙과 겹치는 내용은 여기 두지 않는다.** 컨벤션은 `rules/`, 절차는 `skills/`가 갖는다.

---

## 읽는 순서

1. `CLAUDE.md` — 라우팅만 있다. **수정 금지.**
2. `.claude/rules/` — 전부. `code.md`(어떻게 쓰는가), `unity-csharp.md`(무엇을 쓰는가), `git.md`(커밋 태그).
3. `.claude/docs/overview.md` — 엔진 버전, 패키지, 폴더·씬 구조.
4. `.claude/docs/architecture.md` — 시스템이 왜 그렇게 조립됐는지. **설계를 건드리기 전에 필수.**
5. `.claude/docs/progress.md` — 지금 무슨 작업 중이고 뭐가 미해결인지.

작업을 마치면 `progress.md`를 갱신한다. 설계 판단을 바꿨다면 `architecture.md`도 갱신한다.

---

## 스킬이 있는 일은 스킬로 한다

직접 절차를 지어내기 전에 `.claude/skills/`를 본다.

| 하려는 일 | 스킬 |
| --- | --- |
| C# 컴파일 확인 (에디터 없이) | `unity-build` |
| 씬·프리팹 배선 진단 | `unity-scene-check` |
| 커밋·푸시 | `git-commit-push`, 섞였으면 `split-commits` |
| 문서 점검 | `docs-audit` |

**컴파일 확인은 보고 전에 반드시 돌린다.** 다만 빌드가 통과해도 동작은 모른다 —
플레이 모드 확인이 필요한 변경은 **"빌드는 통과했지만 런타임 확인은 못 했다"고 명시**한다.
이 프로젝트에서 발사가 안 되던 두 건 모두 컴파일은 멀쩡했다.

---

## Unity 특유의 함정

### `.meta` 파일은 GUID다

씬과 프리팹은 파일 경로가 아니라 `.meta`의 GUID로 스크립트를 참조한다.

- 파일 이름·위치를 바꿀 때는 **반드시 `git mv`**. `.meta`가 따라가지 않으면 씬의 모든 연결이 끊긴다.
  (`_Proejct` → `_Project` 폴더 리네임이 그렇게 처리돼 참조가 하나도 안 끊겼다.)
- 새 `.cs`를 만들면 **Unity가 열려 있어야** `.meta`가 생성된다.
- 커밋할 때 `.cs`와 `.meta`를 **같이** 넣는다.

### 씬 YAML은 직접 고치지 않는다

읽어서 **진단하는 용도로만** 쓴다(→ `unity-scene-check`). 배선은 에디터나 MCP로 한다.
직접 편집하면 fileID 정합성이 깨지기 쉽다.

### 인스펙터에 남은 낡은 필드

`m_EditorClassIdentifier`에 옛 클래스명이 남아 있는 건 정상이다. Unity가 다음 저장 때 정리한다.

### 정적 상태와 도메인 리로드

Enter Play Mode Options에서 도메인 리로드를 끄면 정적 필드가 플레이 세션 사이에 살아남는다.
정적 상태를 만들면 `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`으로
초기화하는 코드를 **같이** 넣는다. `TargetRegistry`가 그렇게 되어 있다.

### 플레이 모드를 MCP로 검증할 때

`Application.runInBackground = true`를 먼저 켠다. 에디터 창이 포커스 밖이면 시간이 아예 안 흐른다.
**단 이 값은 `ProjectSettings.asset`에 새어 들어가므로**, 검증이 끝나면 되돌리거나 커밋에서 뺀다.

---

## 대화

- 사용자와의 대화는 한국어. 주석도 한국어.
- `gh` CLI가 설치돼 있다. PR은 `gh pr create`로 직접 만든다(→ `gh` 스킬).

---

## 건드리지 않을 것

- `CLAUDE.md` — 라우팅 인덱스. 수정 금지.
- `Assets/_Project/Scripts/Runtime/Beat/` — 외부 템플릿 유래. 네임스페이스도 규칙도 다르다.
- `Assets/TutorialInfo/` — URP 템플릿 잔재.
- `*.csproj`, `*.sln` — Unity 생성물.
- `Library/`, `Temp/` — 빌드 산출물.
- `.claude/skills/` — 사용자 소유. 고칠 곳을 찾으면 고치지 말고 알린다.
