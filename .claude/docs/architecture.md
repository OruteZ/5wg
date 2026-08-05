# Architecture

이 문서는 **스테이지 플레이 루프**(시작 → 진행 → 종료 → 다음 씬)의 구조를 다룬다.
무기·박자 시스템의 내부 구조는 `progress.md`에 있고, 여기서는 스테이지가 그것들을 어떻게 켜고 끄는지만 다룬다.

## 한 줄 요약

`StageDirector`는 **상태만** 소유한다. "언제 끝나는가"는 `StageEndSource`가, "무엇을 보여주는가"는 UI가,
"어디로 가는가"는 `GameFlow`가 안다. 넷은 서로의 내부를 모른다.

## 레이어

| 레이어 | 타입 | 아는 것 | 모르는 것 |
| --- | --- | --- | --- |
| 상태 | `StageDirector` | 클록·스포너·플레이어·종료 소스 목록 | 종료 조건, UI, 씬 이름 |
| 종료 판단 | `StageEndSource` 파생 | 디렉터의 `RequestClear`/`RequestFail` | 종료 시 무엇이 꺼지는지 |
| 표시 | `StageHud` `StageResultView` | 디렉터의 상태·진행도(읽기 전용) | 게임플레이 규칙 |
| 이동 | `GameFlow` | 씬 이름 | 그 외 전부 |

의존은 한 방향으로만 흐른다. **전투 코드(무기·적·플레이어)는 스테이지의 존재를 모른다.**
디렉터가 기존에 뚫려 있던 스위치(`WeaponHandler.AttackEnabled`, `PlayerController.ControlEnabled`,
`EnemySpawner2D.SetSpawning`)를 누를 뿐이다.

## 파일

```
Scripts/Runtime/
  Stage/
    StageState.cs            Ready / Playing / Cleared / Failed
    StageDirector.cs         상태 소유 + 종료 창구
    StageEndSource.cs        종료를 알리는 주체의 추상 베이스
    BeatTimelineEndSource.cs N마디 생존 → 클리어 (임시)
    PlayerDeathEndSource.cs  사망 → 실패
    GameFlow.cs              씬 이동 (static)
  UI/
    StageHud.cs              체력·진행도 (상시)
    StageResultView.cs       결과 오버레이 (종료 시)
    MainMenuView.cs          START / QUIT
```

## 시작 시퀀스

```
StageDirector.Awake
  참조 해결 (인스펙터 → 비어 있으면 씬에서 탐색)
  SetCombatActive(false)          ← 시작 선언 전까지 아무것도 돌지 않는다
StageDirector.Start
  BindEndSources()                ← 각 소스에 자기 자신을 넘긴다
  Begin()
    state = Playing
    clock.Stop() → clock.Play()   ← 이 순간을 비트 0으로 재앵커
    SetCombatActive(true)         ← 스폰·발사·입력 개방
    각 소스.OnStageBegin()         ← 계측 시작
```

`Awake`가 아니라 `Start`에서 소스를 묶는 이유는, 다른 컴포넌트의 `Awake` 초기화가 끝난 뒤여야
`PlayerController.OnDied` 같은 이벤트 구독이 안전하기 때문이다.

## 종료 시퀀스

```
어떤 소스든 RequestClear() / RequestFail()
  → StageDirector.End(result)
       if (state != Playing) return   ← 같은 프레임에 둘이 동시에 끝내도 먼저 온 쪽만
       state = Cleared | Failed        → OnStateChanged 발행 (UI가 여기서 반응)
       clock.Stop()
       SetCombatActive(false)          ← 스폰·발사·입력 차단
       spawner.ClearAll()              ← 남은 적 회수
       각 소스.OnStageEnd()
```

**`Time.timeScale`을 쓰지 않는다.** 클록이 `AudioSettings.dspTime` 기준이라 timeScale을 건드려도
박은 계속 가고, 오히려 비트 그리드와 화면이 어긋난다. 그래서 축별로 개별 차단한다.

## 씬 흐름

```
MainMenu ──START──> Stage01 ──클리어/사망──> 결과 오버레이 ──RETRY──> Stage01 (씬 재로드)
    ^                                                        └──MENU───> MainMenu
    └────────────────────────────────────────────────────────────────────┘
```

재도전은 상태를 되돌리지 않고 **씬을 통째로 다시 로드**한다. 풀·인벤토리·체력을 개별로 되감는
리셋 경로를 만들지 않기 위해서다. 씬 이름 문자열은 `GameFlow`만 안다. Build Settings 등록이
곧 계약이므로, 씬 이름을 바꾸면 `GameFlow`의 상수와 Build Settings를 함께 고쳐야 한다.

## 확장 지점

### 종료 조건을 바꾸려면

`StageEndSource`를 상속해 씬의 컴포넌트만 갈아끼운다. 디렉터·UI·스포너는 건드리지 않는다.

```csharp
public sealed class MusicOrchestrator : StageEndSource
{
    public override float Progress => _playedBars / (float)_totalBars;   // HUD가 그대로 그린다
    public override void OnStageBegin() { /* 곡 재생 시작 */ }
    // 곡이 끝나면 RequestClear();
}
```

디렉터의 `_endSources` 배열에 넣기만 하면 된다. 비워두면 씬 전체에서 자동 수집한다.
여러 개를 동시에 두면 **먼저 끝을 알린 쪽이 이긴다**(OR 조건). AND 조건이 필요해지면
소스를 합성하는 소스를 하나 만드는 쪽이 디렉터를 고치는 것보다 싸다.

### 진행도의 의미

`StageDirector.Progress`는 소스들이 보고한 값 중 **최댓값**이다. 진행도 개념이 없는 소스
(`PlayerDeathEndSource`)는 0을 반환해 계산에 영향을 주지 않는다.

### 전투를 잠깐 멈추려면

상점·컷신처럼 스테이지를 끝내지 않고 전투만 멈추는 경우는 `WeaponHandler.AttackEnabled`나
`IFireGate` 등록으로 처리한다. 디렉터의 상태를 건드리지 않는다.

## 사망 판정

`Enemy2D`가 트리거 접촉으로 `IDamageable.TakeDamage`를 호출한다. 상대의 구체 타입은 보지 않는다.

- 플레이어 콜라이더는 **트리거**, 적 콜라이더는 아니다 → 둘이 겹칠 때만 `OnTrigger*2D`가 온다.
- 적끼리는 둘 다 트리거가 아니라 이 경로로 오지 않는다.
- 투사체는 트리거지만 `IDamageable`이 아니라 걸러진다.
- 겹친 채 머무르면 `Enter`가 다시 오지 않으므로 `Stay`도 받고 **재타격 간격**으로 조절한다.

## 성장 (경험치·레벨)

`Scripts/Runtime/Progression/`

적이 죽은 자리에 경험치 오브가 떨어지고, 플레이어가 가까이 가면 끌려와 수집된다.
누적이 요구량을 넘으면 레벨이 오른다. **레벨업 시 무엇을 강화할지는 아직 아무도 모른다** —
`PlayerExp`는 이벤트만 쏘고 끝난다(업그레이드 UI는 다음 브랜치).

| 타입 | 역할 |
| --- | --- |
| `IExpReceiver` | 경험치를 받는 대상. 오브가 수집자의 구체 타입을 모르게 한다 |
| `PlayerExp` | 누적·레벨 계산. `OnLeveledUp` / `OnExpChanged`만 발행 |
| `ExpOrb` | 픽업. 자석 반경에 들어오면 가속하며 끌려온다 |
| `ExpOrbPool` | 오브 소유자. 드랍 요청을 받고, 스테이지 종료 시 전부 회수 |

### 드랍 경로

```
Enemy2D.Die()
  → OnDiedWithReward(위치, 보상)      ← 사망일 때만. 일괄 회수는 여기로 오지 않는다
      → EnemySpawner2D.HandleEnemyDied  ← 적을 만드는 곳이 스포너라 구독도 여기서 한 번만
          → ExpOrbPool.Drop(위치, 값)
```

스포너가 중계하는 이유는 그곳이 적의 **팩토리**이기 때문이다. 풀에서 꺼낸 개체에 콜백을 다는
기존 `SetReleaseCallback` 패턴과 같은 자리다. 적은 경험치 오브의 존재를 모른다.

### 결정 사항

- 오브는 **콜라이더를 쓰지 않는다.** 수십 개가 동시에 떠 있는데 그만큼 트리거를 물리 엔진에 얹을
  이유가 없다. 거리 계산으로 자석·수집을 판정한다.
- 한 번 끌리기 시작하면 반경을 벗어나도 계속 따라온다. 경계에서 붙었다 떨어졌다 하지 않게.
- 레벨 곡선은 `기본요구량 × 배수^(레벨-1)` 한 줄. 곡선이 복잡해지면 그때 SO로 뺀다.
- 한 번의 획득으로 여러 레벨이 오를 수 있다(while 루프). 후반 오브 폭식·보스 보상 대비.
- 종료 후 남은 오브가 끌려와 경험치가 더 들어가지 않도록 **풀이 디렉터 상태를 구독**해 회수한다.
  의존 방향은 Progression → Stage 한 방향이고, 디렉터는 경험치를 모른다.

## 카메라

`Stage01`의 Main Camera는 `CinemachineBrain`만 들고, 실제 프레이밍은 `CM Player Camera`
(`CinemachineCamera` + `CinemachinePositionComposer`)가 한다. Follow 대상은 플레이어.

- 직교 투영 강제(`ModeOverride = Orthographic`), 크기는 기존 카메라와 동일한 5.
- 데드존 0.12 + 감쇠 0.35 — 미세 이동에 화면이 떨리지 않게.
- `Prototype` 씬은 손대지 않았다. 무기 검증용이라 고정 카메라가 더 편하다.

## 알려진 제약

- **박은 프레임과 무관하게 간다.** 클록이 dspTime 기준이라 창이 비활성이거나 긴 히치가 나면
  프레임이 멈춘 동안에도 박이 흐르고, 복귀 순간 진행도가 뛴다.
- `BeatTimelineEndSource`는 임시다. 마디 수는 기획 확정 시 사라질 값.
- 카운트다운·일시정지·성장 UI 없음.
- HUD는 매 프레임 폴링한다. 위젯이 늘면 이벤트 기반으로 바꾸는 게 낫다.
