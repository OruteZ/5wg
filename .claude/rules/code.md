# Code

`Assets/_Proejct/Scripts/Runtime/`에 적용된다. 여기 적힌 것은 취향이 아니라 **이미 53개 파일이
전부 따르고 있는 것**이므로, 새 파일도 맞춰야 한다. 어기면 그 파일만 튄다.

## 네임스페이스

폴더 이름과 같은 `FiveWG.*`를 쓴다 (`FiveWG.Core` `Combat` `Enemies` `Player` `Progression`
`Stage` `UI` `Weapons`).

**예외는 `Beat/` 하나다.** 외부 템플릿 유래라 `BeatTemplate` 네임스페이스를 유지하고,
`BeatMath.cs`는 네임스페이스가 아예 없다. 여기 손대지 마라 — 다른 규칙도 이 폴더엔 적용되지 않는다.

asmdef는 두지 않는다. 의존 방향은 문서와 리뷰로 지킨다(→ `docs/architecture.md`).

## 인스펙터

`SerializeField`가 있으면 Alchemy 어트리뷰트를 같이 쓴다.

- `[Title("묶음")]`으로 필드를 묶고, `[LabelText("한글 라벨")]`로 이름을 단다.
  단위와 범위를 라벨에 적는다 — `[LabelText("수명 (sec)")]`, `[LabelText("치명타 확률 (0~1)")]`.
- 비면 안 되는 참조는 `[Required("...")]`.
- 런타임에 확인하고 싶은 값은 `[ShowInInspector, ReadOnly]` 계산 프로퍼티로 노출한다.
  **단, 플레이 중에는 갱신되지 않는다** — 이유와 회피는 `docs/progress.md`의 "해결한 문제"에.

## 주석

**무엇을 하는지가 아니라 왜 그렇게 했는지를 적는다.** 특히 "당연해 보이는 다른 방법을 왜 안 썼는가".
이 저장소의 주석 대부분이 그 형태이고, 그게 이 문서들의 실제 가치다.

```csharp
// 겹친 채 머무르면 Enter가 다시 오지 않으므로 Stay도 받고 재타격 간격으로 조절한다.
```

의도적으로 지른 지름길에는 `ponytail:` 주석으로 한계와 올릴 방법을 같이 적는다.

```csharp
// ponytail: 타격마다 오버랩 쿼리 하나. 동시에 떠 있는 영역이 한 자릿수라 문제가 안 된다.
```

## 검증

테스트 어셈블리가 없다. 대신 **`[InitializeOnLoadMethod]` 셀프체크**를 쓴다 — 에디터가 로드될
때마다 돌고, 어긋나면 `Debug.LogError`를 뱉는다.

조용히 틀어지는 로직(리플렉션으로 필드를 훑는 코드, 가중 추첨, 인덱스 계산)에는 붙인다.
`WeaponLevelData` `BurstTiming` `LevelUpDraft`에 예시가 있다.

## 조용한 실패를 만들지 않는다

참조가 비었거나 연결이 끊겨 **아무 일도 안 일어나는 상태**가 이 프로젝트에서 두 번이나
오래 걸리는 디버깅을 만들었다. 시작 시점에 로그로 알린다.

```csharp
if (_clock == null)
    Debug.LogError($"[{nameof(WeaponHandler)}] BpmClock을 찾지 못했다. 박자 틱이 없어 발사가 일어나지 않는다.", this);
```

`Debug.Log` 계열에는 `[{nameof(타입)}]` 접두사와 두 번째 인자 `this`(클릭하면 그 오브젝트로
가도록)를 붙인다.

## 씬에 로직을 숨기지 않는다

버튼 배선은 인스펙터 UnityEvent가 아니라 코드로 건다 (`onClick.AddListener`).
씬 파일을 열어봐야만 알 수 있는 동작을 만들지 않기 위해서다.
