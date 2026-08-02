---
name: unity-scene-check
description: Diagnose Unity scene/prefab wiring by reading YAML — which components are attached, which serialized fields are empty, whether script GUIDs resolve. Use when something does not work at runtime and the cause may be editor wiring rather than code. 씬 연결 확인, 인스펙터 비어있음, GUID 추적.
---

# unity-scene-check

씬은 **진단용으로만 읽는다.** 직접 편집하면 fileID 정합성이 깨지므로, 배선이 필요하면
사용자에게 에디터 작업으로 요청한다.

## 1. 어떤 컴포넌트가 붙어 있나

```bash
grep -n "m_Script:\|m_Name:\|m_EditorClassIdentifier" Assets/_Proejct/Scenes/<씬>.unity
```

`m_EditorClassIdentifier`에 옛 클래스명이 남아 있는 건 정상이다. Unity가 다음 저장 때 정리한다.
**신뢰할 것은 `m_Script`의 GUID뿐이다.**

## 2. GUID가 어느 스크립트인가

```bash
grep -rl "<guid>" Assets --include=*.meta
```

반대 방향(스크립트 → GUID):

```bash
grep guid Assets/_Proejct/Scripts/Runtime/<경로>/<파일>.cs.meta
```

**씬에 GUID가 아예 안 나타나면 그 컴포넌트는 씬에 없는 것이다.** 코드를 더 파기 전에 이걸 먼저 본다.

## 3. 직렬화 필드가 비었나

`m_Script` 줄 아래에 필드 값이 나열된다. `{fileID: 0}`은 **연결 안 됨**이고, 빈 배열은 `[]`다.

```
_clock: {fileID: 0}        ← 참조 없음
_startingWeapons: []       ← 비어 있음
```

## 4. 에셋 참조가 유효한가

`{fileID: N, guid: G, type: 2}`(에셋) / `type: 3`(프리팹 내 컴포넌트)의 `guid`를 해당
`.asset.meta` / `.prefab.meta`와 대조한다. 어긋나면 인스펙터에서 `Missing`으로 보인다.

## `.meta` 무결성

- 파일 이름·위치 변경은 **반드시 `git mv`**. `.meta`가 따라가지 않으면 씬 연결이 전부 끊긴다.
- 새 `.cs`는 **Unity가 열려 있어야** `.meta`가 생성된다. 없으면 씬에 배치할 수 없다.
- 커밋 시 `.cs`와 `.meta`를 같이 넣는다. 확인:

```bash
git status --porcelain | grep '\.cs$'
```
