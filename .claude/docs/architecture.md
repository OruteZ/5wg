# Architecture

## 네임스페이스와 씬 서비스

모든 런타임 코드는 폴더와 같은 이름의 `FiveWG.*` 네임스페이스에 있다
(`FiveWG.Core` `Combat` `Enemies` `Player` `Progression` `Stage` `UI` `Weapons`).
`Beat/`만 외부 템플릿 유래라 기존 `BeatTemplate`을 유지한다.

asmdef는 두지 않았다. 의존 방향은 문서와 리뷰로 지킨다(→ 각 절의 "아는 것/모르는 것" 표).

**씬에 하나뿐인 것은 `SceneServices`가 찾는다.** 예전에는 컴포넌트마다
"인스펙터가 비어 있으면 `FindFirstObjectByType`"을 각자 들고 있었고 9개 파일 14곳까지 늘었다.
문제는 배선 실수가 조용히 성공한다는 것이었다 — 실제로 리네임으로 참조가 끊겼는데 폴백이
받아내 검증 중에야 발견했다. 이제 탐색은 `SceneServices` 한 곳에서만 일어나고,
각 참조는 처음 요청될 때 채워지므로 컴포넌트끼리의 `Awake` 순서에 기대지 않는다.

씬에 `SceneServices` 오브젝트를 두면 인스펙터로 명시 배선할 수 있다. 없으면 요청 시점에
임시 창구를 만들어 자동 탐색한다.

**씬 소유 서비스와 주체 소유 설정을 가른다.**

| | 소유 | 이유 |
| --- | --- | --- |
| `ProjectilePool` | 씬 (`SceneServices` 오브젝트) | 여러 주체가 같은 탄을 공유하고, 주체가 죽어도 날아가던 탄은 회수돼야 한다 |
| `RegistryTargetProvider` | 쏘는 주체 (플레이어) | 겨눌 편이 주체마다 다르다 |

예전에는 `WeaponHandler`가 둘 다 플레이어에 `AddComponent` 했다. 그러면 투사체 풀이 플레이어와
함께 사라지고, 씬만 봐서는 구성이 보이지 않았다.

## 플레이어 프리팹

`Prefabs/Player/Player.prefab`. 적·투사체·경험치 오브처럼 플레이어도 프리팹이다.

씬마다 인라인으로 두던 시절에는 설정이 갈라졌다(한 씬은 클록·시작 무기가 비어 발사가 안 됐다).
씬 참조가 필요한 값(`_clock` 등)은 프리팹 에셋에서 비어 있고, 런타임에 `SceneServices`가 채운다.
씬별로 다르게 하고 싶으면 인스턴스 오버라이드로 덮는다.

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

## 풀링

`Scripts/Runtime/Core/PrefabPool.cs`

프리팹 하나를 재사용하는 풀. 세 곳(적·경험치 오브·투사체)이 각자 들고 있던 보일러플레이트를 합쳤다.

```csharp
_pool = new PrefabPool<Enemy2D>(_enemyPrefab, "EnemyPool", capacity, maxSize,
    onCreate: enemy => enemy.OnDiedWithReward += HandleEnemyDied);
```

| 담당 | 위치 |
| --- | --- |
| 인스턴스 수명, 활성 목록, 중복 반납 차단 | `PrefabPool<T>` |
| 언제 꺼내고 언제 반납하는가 | 소유자 (스포너·오브 풀·투사체 풀) |

### 결정 사항

- **상속이 아니라 합성.** `EnemySpawner2D`는 풀이 아니라 풀을 *가진* 스포너다. is-a로 묶으면 깨진다.
  `ProjectilePool`도 프리팹별로 풀을 여러 개 들기 때문에 상속으로는 표현되지 않는다.
- MonoBehaviour가 아니라 순수 C# 클래스다. 무기 런타임을 순수 클래스로 둔 결정과 같은 이유.
- 유틸은 **"프리팹 하나 → 인스턴스 목록"만** 담당한다. 스폰 주기·회수 정책을 파라미터로 빨아들이면
  세 곳의 차이가 유틸로 새어나와 원래대로 돌아간다.
- 개체당 한 번만 할 일(이벤트 구독)은 `onCreate` 훅으로 넘긴다.
- 중복 반납 차단이 세 곳에서 제각각이던 걸 `Release`의 `_active.Remove` 반환값 검사 한 곳으로 모았다.

### 이 통합으로 같이 고쳐진 것

`ProjectilePool`만 활성 목록이 없어 `ReleaseAll`을 만들 수 없었다. 스테이지가 끝나면 적과 오브는
회수되는데 **날아가던 탄만 계속 날아갔다.** 이제 투사체 풀도 디렉터 상태를 구독해 회수한다.

## 조준 대상 탐색

`Scripts/Runtime/Core/TargetRegistry.cs`, `Core/Faction.cs`, `Weapons/Services/RegistryTargetProvider.cs`

살아 있는 피격 대상을 **편(`Faction`)별 정적 목록**으로 들고, 조준은 그 목록을 훑어 고른다.
기존 `PhysicsTargetProvider`(발사마다 `Physics2D.OverlapCircle`)를 대체한다.

바꾼 이유는 쿼리 횟수다. 자동공격이라 발사가 서브비트마다 일어나고 무기가 6개까지 늘어나므로,
발사 한 번에 오버랩 한 번이면 **(무기 수 × 서브비트)**로 불어난다. 대상이 수십 규모인 지금은
목록을 직접 도는 쪽이 싸고 예측 가능하다.

- 등록·해제는 `OnEnable`/`OnDisable`에서 한다. **풀이 개체를 껐다 켜는 것만으로 목록이 맞춰진다.**
- 등록하는 쪽은 반드시 `IDamageable`이어야 한다. 조회 측이 그렇게 가정한다.
  새로운 피격 대상(파괴 가능한 오브젝트 등)을 만들면 등록 두 줄을 잊지 말아야 한다.
- 정적 목록이라 도메인 리로드를 끈 경우 플레이 모드를 나가도 남는다. `RuntimeInitializeOnLoadMethod`로 비운다.

### 편(Faction)

아군 배제를 **계층(`transform.root`) 비교가 아니라 `Faction`으로** 한다. 계층 비교는 소환수·아군
NPC처럼 서로 다른 계층에 있는 같은 편이 생기는 순간 무너진다.

- `IDamageable.Faction` — 모든 피격 대상이 편을 밝힌다. `Enemy2D`는 Enemy, `PlayerController`는 Player.
- `RegistryTargetProvider._targetFaction` — 겨눌 편을 인스펙터에서 정한다(기본 Enemy).
  적이 무기를 들면 이 값만 Player로 바꾸면 된다.
- `WeaponContext.Faction` — 쏘는 쪽의 편. `Projectile2D`가 같은 편을 그냥 통과시킨다.
  발사구 자해 방지(owner 계층 통과)는 편 설정과 무관하게 별개로 남겨둔다.
- 플레이어도 레지스트리에 등록된다. 이름이 "적 목록"이 아니라 대상 목록인 이유다.

## 물리 레이어

| 레이어 | 대상 | 무시하는 충돌 |
| --- | --- | --- |
| `Enemy` (3) | 적 프리팹 | 없음 — **적끼리는 충돌한다** |
| `Projectile` (8) | 투사체 프리팹 | 탄 ↔ 탄 |

- 적끼리 밀어내는 건 유지한다. 안 그러면 전부 한 점에 겹쳐 한 마리처럼 보인다.
- 탄끼리는 둘 다 트리거+리지드바디라 겹칠 때마다 `OnTriggerEnter2D`가 헛돌았다. 껐다.
- 투사체의 자기 주인 통과는 레이어가 아니라 코드로 막는다(기존 방침 유지).

### 측정으로 확인한 것 (적 200마리, 180프레임 평균)

**적끼리의 충돌은 병목이 아니다.** `Physics2D.Simulate`가 0.09~0.17ms로, 메인스레드
1.1~1.4ms 중 10% 남짓이고 반복 측정 간 편차보다 크지 않다.

콜라이더를 줄이면 접촉이 줄 것 같지만 **반대다.**

| 반지름(월드) | 접촉 수 | Physics2D.Simulate |
| --- | --- | --- |
| 0.40 (스프라이트 반폭) | 792 | 0.091ms |
| 0.325 | 894 | 0.090ms |
| 0.275 | 907 | 0.112ms |
| 0.25 | 920 | 0.166ms |

원이 빽빽하게 쌓이면 **개당 접촉 수는 크기와 무관하게 6 근처로 수렴한다**(원 패킹).
반지름을 줄이면 같은 공간에 더 촘촘히 packing되어 접촉이 오히려 늘어난다.
그래서 콜라이더는 스프라이트 반폭(0.16 × 스케일 2.5 = 0.40)을 유지한다. 피격 판정이 겉보기와
일치한다는 이점도 같이 지킨다.

측정할 때는 **단일 프레임 샘플을 믿으면 안 된다.** 프레임당 값은 배 이상 흔들려서,
처음엔 이 항목이 메인스레드의 절반을 먹는 것처럼 보였다. 180프레임 평균을 내자 사라졌다.

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
