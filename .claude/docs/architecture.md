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

`ProjectilePool`은 이름과 달리 **투사체와 영역(장판·충격파) 두 부류를 함께 소유한다.**
부류가 늘 때마다 씬에 풀 컴포넌트를 하나씩 늘리지 않으려고 한 곳에 모았다. 프리팹별 풀을
고르는 로직은 `PrefabPoolSet<T>`(순수 C#)로 빠져 있어 부류마다 같은 코드를 다시 쓰지 않는다.
이름은 투사체만 담당하던 시절 그대로다 — 씬·프리팹이 GUID로 참조하고 있어 리네임은 따로 한다.

예전에는 `WeaponHandler`가 둘 다 플레이어에 `AddComponent` 했다. 그러면 투사체 풀이 플레이어와
함께 사라지고, 씬만 봐서는 구성이 보이지 않았다.

## 플레이어 프리팹

`Prefabs/Player/Player.prefab`. 적·투사체·경험치 오브처럼 플레이어도 프리팹이다.

씬마다 인라인으로 두던 시절에는 설정이 갈라졌다(한 씬은 클록·시작 무기가 비어 발사가 안 됐다).
씬 참조가 필요한 값(`_clock` 등)은 프리팹 에셋에서 비어 있고, 런타임에 `SceneServices`가 채운다.
씬별로 다르게 하고 싶으면 인스턴스 오버라이드로 덮는다.

---

여기부터는 축별 구조다. 아래 순서로 읽으면 된다.

| 절 | 다루는 것 |
| --- | --- |
| 스테이지 플레이 루프 | 시작 → 진행 → 종료 → 다음 씬. 무엇이 무엇을 켜고 끄는가 |
| 사망 판정 | 접촉 데미지가 오가는 경로 |
| 성장 | 경험치 오브 → 레벨업 → 카드 선택 |
| 픽업 | 티켓·아이템의 드랍 → 습득 → 정리 |
| 풀링 / 조준 대상 탐색 / 물리 레이어 | 개체 수명과 탐색. 측정 결과 포함 |
| 무기 스탯 | 15축의 계약, 무엇이 실제로 소비되는가 |
| 카메라 / 빌드에서만 드러나는 설정 | Cinemachine 구성, 보간·VSync |
| 알려진 제약 | 아직 안 고친 것 |

## 스테이지 플레이 루프

`StageDirector`는 **상태만** 소유한다. "언제 끝나는가"는 `StageEndSource`가, "무엇을 보여주는가"는 UI가,
"어디로 가는가"는 `GameFlow`가 안다. 넷은 서로의 내부를 모른다.

### 레이어

| 레이어 | 타입 | 아는 것 | 모르는 것 |
| --- | --- | --- | --- |
| 상태 | `StageDirector` | 클록·스포너·플레이어·종료 소스 목록 | 종료 조건, UI, 씬 이름 |
| 종료 판단 | `StageEndSource` 파생 | 디렉터의 `RequestClear`/`RequestFail` | 종료 시 무엇이 꺼지는지 |
| 표시 | `StageHud` `StageResultView` | 디렉터의 상태·진행도(읽기 전용) | 게임플레이 규칙 |
| 이동 | `GameFlow` | 씬 이름 | 그 외 전부 |

의존은 한 방향으로만 흐른다. **전투 코드(무기·적·플레이어)는 스테이지의 존재를 모른다.**
디렉터가 기존에 뚫려 있던 스위치(`WeaponHandler.AttackEnabled`, `PlayerController.ControlEnabled`,
`EnemySpawner.SetSpawning`)를 누를 뿐이다.

### 파일

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

### 시작 시퀀스

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

### 종료 시퀀스

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

### 씬 흐름

```
MainMenu ──START──> Stage01 ──클리어/사망──> 결과 오버레이 ──RETRY──> Stage01 (씬 재로드)
    ^                                                        └──MENU───> MainMenu
    └────────────────────────────────────────────────────────────────────┘
```

재도전은 상태를 되돌리지 않고 **씬을 통째로 다시 로드**한다. 풀·인벤토리·체력을 개별로 되감는
리셋 경로를 만들지 않기 위해서다. 씬 이름 문자열은 `GameFlow`만 안다. Build Settings 등록이
곧 계약이므로, 씬 이름을 바꾸면 `GameFlow`의 상수와 Build Settings를 함께 고쳐야 한다.

### 확장 지점

#### 종료 조건을 바꾸려면

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

#### 진행도의 의미

`StageDirector.Progress`는 소스들이 보고한 값 중 **최댓값**이다. 진행도 개념이 없는 소스
(`PlayerDeathEndSource`)는 0을 반환해 계산에 영향을 주지 않는다.

#### 전투를 잠깐 멈추려면

상점·컷신처럼 스테이지를 끝내지 않고 전투만 멈추는 경우는 `WeaponHandler.AttackEnabled`나
`IFireGate` 등록으로 처리한다. 디렉터의 상태를 건드리지 않는다.

## 사망 판정

`Enemy`가 트리거 접촉으로 `IDamageable.TakeDamage`를 호출한다. 상대의 구체 타입은 보지 않는다.

- 플레이어 콜라이더는 **트리거**, 적 콜라이더는 아니다 → 둘이 겹칠 때만 `OnTrigger*2D`가 온다.
- 적끼리는 둘 다 트리거가 아니라 이 경로로 오지 않는다.
- 투사체는 트리거지만 `IDamageable`이 아니라 걸러진다.
- 겹친 채 머무르면 `Enter`가 다시 오지 않으므로 `Stay`도 받고 **재타격 간격**으로 조절한다.

## 성장 (경험치·레벨)

`Scripts/Runtime/Progression/`

적이 죽은 자리에 경험치 오브가 떨어지고, 플레이어가 가까이 가면 끌려와 수집된다.
누적이 요구량을 넘으면 레벨이 오르고, 레벨업마다 카드 3장 중 하나를 고른다.

| 타입 | 역할 |
| --- | --- |
| `PlayerExp` | 누적·레벨 계산. `OnLeveledUp` / `OnExpChanged`만 발행. 오브가 직접 참조한다 |
| `ExpOrb` | 픽업. 자석 반경에 들어오면 가속하며 끌려온다 |
| `ExpOrbPool` | 오브 소유자. 드랍 요청을 받고, 스테이지 종료 시 전부 회수 |
| `LevelUpDraft` | 카드 후보를 뽑는 규칙. 순수 static |
| `LevelUpOption` | 카드 한 장. 무기와 오르기 전 레벨. **적용까지 자기가 안다** |
| `LevelUpCardView` | 큐와 화면. 후보가 비었을 때의 보상도 여기 규칙이다 |

`PlayerExp`는 여전히 레벨업 이벤트만 쏜다. 무엇을 강화할지는 `LevelUpDraft`가 정하고
적용은 `LevelUpOption.Apply`가 한다.

**소모성 보상(힐)은 카드가 아니다.** 고를 것이 없는 회차는 카드를 아예 띄우지 않으므로
"카드 한 장"으로 만들 이유가 없다. 예전에는 `LevelUpOption`에 `Heal` 종류를 두고 뷰가
"힐이면 안 띄우고 바로 적용" 분기를 들었는데, 화면에 도달하지 못하는 선택지가 타입에만
존재하는 꼴이었다. 지금은 `Build`가 빈 목록을 돌려주고 뷰가 회복을 준다.

덕분에 `LevelUpOption.Apply`는 `WeaponInventory` 하나만 받는다 — Progression이 Player를
알 필요가 없어졌다.

### 드랍 경로

```
Enemy.Die()
  → OnDiedWithReward(위치, 보상)      ← 사망일 때만. 일괄 회수는 여기로 오지 않는다
      → EnemySpawner.HandleEnemyDied  ← 적을 만드는 곳이 스포너라 구독도 여기서 한 번만
          → ExpOrbPool.Drop(위치, 값)
```

스포너가 중계하는 이유는 그곳이 적의 **팩토리**이기 때문이다. 풀에서 꺼낸 개체에 콜백을 다는
기존 `SetReleaseCallback` 패턴과 같은 자리다. 적은 경험치 오브의 존재를 모른다.

### 결정 사항

- 오브는 **콜라이더를 쓰지 않는다.** 수십 개가 동시에 떠 있는데 그만큼 트리거를 물리 엔진에 얹을
  이유가 없다. 거리 계산으로 자석·수집을 판정한다.
- 한 번 끌리기 시작하면 반경을 벗어나도 계속 따라온다. 경계에서 붙었다 떨어졌다 하지 않게.
- 레벨 곡선은 `5 + 3 × (레벨-1)` **선형**이고 적 1마리 = 경험치 1이다. 지수(VS식)를 쓰지 않는
  이유는 선택이 실시간이기 때문 — 초반 1~2분에 카드가 10번 넘게 뜨면 화면 아래를 계속 가린다.
  **단 이건 현재 코드의 이야기이고 기획은 이미 갈아엎혔다** — `progression.md`의 음표 4종
  확률 지급과 구간별 필요치 테이블이 새 기준이다(→ `progress.md`의 미해결).
- 한 번의 획득으로 여러 레벨이 오를 수 있다(while 루프). 후반 오브 폭식·보스 보상 대비.
- 종료 후 남은 오브가 끌려와 경험치가 더 들어가지 않도록 **풀이 디렉터 상태를 구독**해 회수한다.
  의존 방향은 Progression → Stage 한 방향이고, 디렉터는 경험치를 모른다.

### 카드는 아무것도 멈추지 않는다

카드가 떠 있어도 음악·적·발사가 계속 돈다. `Time.timeScale`은 애초에 쓸 수 없다 —
클록이 dspTime 기준이라 timeScale로는 박이 멈추지 않고 화면과 어긋나기만 한다.
고르지 않으면 무한히 기다리고, 그동안 또 레벨업하면 큐에 쌓아 순서대로 소진한다.

**카드를 띄울 때마다 `EventSystem`의 선택을 비운다.** `EventSystem`이 카드 하나를 자동
선택해 두면 Submit(Enter·Space·패드 A) 한 번에 **고르지도 않은 카드가 조용히 먹힌다** —
검증 중에 실제로 이걸로 카드 한 장이 저절로 소진됐다.

선택을 비우는 것만으로는 완전히 막히지 않는다. 기본 UI Navigate에 WASD가 물려 있어서
이동하는 것만으로 선택이 다시 카드로 옮겨간다. 완전히 막으려면 카드 버튼의
`Navigation.Mode`를 `None`으로 둬야 한다. 의도한 입력은 마우스 클릭과 숫자키 1·2·3뿐이다.

### 후보 규칙은 뷰 밖에 있다

`LevelUpDraft`는 MonoBehaviour가 아닌 static이다. 신규 무기의 가중치가 **빈 슬롯 수**라
슬롯이 찰수록 자연히 강화로 기우는데, 이런 규칙은 씬 없이 검증할 수 있어야 한다.
가중 추첨은 범위를 한 칸 흘리면 마지막 후보가 영영 안 뜨는 식으로 조용히 틀어지므로
`SelfCheck`이 에디터 로드마다 확인한다.

## 픽업 (티켓·아이템)

`Scripts/Runtime/Pickup/`

경험치를 뺀 나머지 드랍 — 티켓 2종과 아이템 3종(회복·자석·리롤). 기획은 `pickup.md`.

**경험치 오브와 셋이 다르다**: 끌려오지 않고, 저절로 사라지지 않고, 확률표로 나온다.
그래서 코드를 공유하지 않고 옆에 따로 세웠다.

| 타입 | 역할 |
| --- | --- |
| `PickupDefinition` | 픽업 한 종류의 정의(SO). 프리팹·효과·수치 |
| `PickupEffect` | 효과 종류 enum. 분기가 있는 유일한 자리 |
| `PickupDropTable` | 확률표(SO). 묶음(`DropGroup`)마다 "하나만 뽑기" 여부가 다르다 |
| `PickupDropper` | 적이 죽은 자리에서 표를 뽑는 창구. **스포너가 아는 픽업 타입은 이것뿐** |
| `PickupPool` | 종류별 `PrefabPool<Pickup>`. 동시 상한과 스테이지 종료 회수를 소유 |
| `Pickup` | 필드 개체. 거리로 접촉 판정 |
| `PickupCollector` | 주운 것을 효과로 바꾼다. `PickupEffect` switch 하나 |
| `PlayerInventory` | 판 안에서만 유지되는 티켓·리롤 보유량 |

### 드랍 경로

```
EnemySpawner.HandleEnemyDied(위치, 보상)   ← 경험치와 같은 자리
  → ExpOrbPool.Drop(위치, 값)
  → PickupDropper.RollDrops(위치)
       PickupDropTable.Roll(재사용 List)   ← 후반 초당 수십 번이라 목록을 새로 만들지 않는다
       → PickupPool.Drop(위치, 정의)
```

**뽑기와 풀을 나눈 이유**: 풀은 무엇이 왜 나왔는지 몰라야 한다. 확률 규칙이 아무리 늘어도
스포너와 풀은 그대로다.

### 결정 사항

- **픽업은 스스로 사라지지 않는다.** 그래서 **동시 상한이 유일한 정리 수단**이다(기본 200).
  상한에 닿으면 가장 오래된 것부터 지운다 — 방금 죽인 적의 드랍이 없어지는 것보다
  한참 전에 지나친 것이 없어지는 쪽이 덜 억울하다.
- 풀이 종류별로 갈라져 있어 "전체에서 가장 오래된 것"을 풀에 물어볼 수 없다. 떨어뜨린 순서대로
  `_alive` 목록을 따로 들고, `Pickup.OnDespawned`가 그 목록을 맞춘다.
- 오브와 마찬가지로 **콜라이더를 쓰지 않는다.** 거리 제곱 비교로 접촉을 판정한다.
- 종료 후 남은 픽업을 줍지 못하도록 **풀이 디렉터 상태를 구독**해 회수한다.
  의존 방향은 Pickup → Stage 한 방향이고, 디렉터는 픽업을 모른다(성장 쪽과 같은 구조).
- `PlayerInventory`와 `PickupCollector`는 **Player 프리팹에 붙어 있다.** 씬마다 다시 붙이지
  않으려는 것이고, 그래서 새 스테이지 씬은 `Pickups` 오브젝트 하나만 두면 된다.

## 풀링

`Scripts/Runtime/Core/PrefabPool.cs`

프리팹 하나를 재사용하는 풀. 세 곳(적·경험치 오브·투사체)이 각자 들고 있던 보일러플레이트를 합쳤다.

```csharp
_pool = new PrefabPool<Enemy>(_enemyPrefab, "EnemyPool", capacity, maxSize,
    onCreate: enemy => enemy.OnDiedWithReward += HandleEnemyDied);
```

| 담당 | 위치 |
| --- | --- |
| 인스턴스 수명, 활성 목록, 중복 반납 차단 | `PrefabPool<T>` |
| 언제 꺼내고 언제 반납하는가 | 소유자 (스포너·오브 풀·투사체 풀) |

### 결정 사항

- **상속이 아니라 합성.** `EnemySpawner`는 풀이 아니라 풀을 *가진* 스포너다. is-a로 묶으면 깨진다.
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
발사마다 `Physics2D.OverlapCircle`을 돌던 예전 방식(`PhysicsTargetProvider`)은 지웠다.

바꾼 이유는 쿼리 횟수다. 자동공격이라 발사가 서브비트마다 일어나고 무기가 6개까지 늘어나므로,
발사 한 번에 오버랩 한 번이면 **(무기 수 × 서브비트)**로 불어난다. 대상이 수십 규모인 지금은
목록을 직접 도는 쪽이 싸고 예측 가능하다.

- 등록·해제는 `OnEnable`/`OnDisable`에서 한다. **풀이 개체를 껐다 켜는 것만으로 목록이 맞춰진다.**
- 등록하는 쪽은 반드시 `IDamageable`이어야 한다. 조회 측이 그렇게 가정한다.
  새로운 피격 대상(파괴 가능한 오브젝트 등)을 만들면 등록 두 줄을 잊지 말아야 한다.
- 정적 목록이라 도메인 리로드를 끈 경우 플레이 모드를 나가도 남는다. `RuntimeInitializeOnLoadMethod`로 비운다.

**`DamageField`는 예외로 오버랩 쿼리를 쓴다.** 조준과 달리 발사마다가 아니라 영역이 타격할 때만
돌고, 동시에 떠 있는 영역이 한 자릿수라 횟수가 불어나지 않는다. 화면에 수십 개가 깔리면
이 목록 순회로 바꾼다.

### 편(Faction)

아군 배제를 **계층(`transform.root`) 비교가 아니라 `Faction`으로** 한다. 계층 비교는 소환수·아군
NPC처럼 서로 다른 계층에 있는 같은 편이 생기는 순간 무너진다.

- `IDamageable.Faction` — 모든 피격 대상이 편을 밝힌다. `Enemy`는 Enemy, `PlayerController`는 Player.
- `RegistryTargetProvider._targetFaction` — 겨눌 편을 인스펙터에서 정한다(기본 Enemy).
  적이 무기를 들면 이 값만 Player로 바꾸면 된다.
- `WeaponContext.Faction` — 쏘는 쪽의 편. `Projectile`가 같은 편을 그냥 통과시킨다.
  발사구 자해 방지(owner 계층 통과)는 편 설정과 무관하게 별개로 남겨둔다.
- 플레이어도 레지스트리에 등록된다. 이름이 "적 목록"이 아니라 대상 목록인 이유다.

## 무기 스탯

`Weapons/Data/WeaponLevelData.cs` — 축 목록은 `weapons.md`의 "스탯 축"과 1:1이다(15축).

### 계약

- **레벨 표는 절대값**이다. 증분이 아니라 "3레벨일 때 데미지 19" 식으로 적는다.
- **카드 강화와 영구 강화는 증분**이고, `operator +`로 더한다.
  적용 순서는 `레벨 표 → 카드 강화 → 영구 강화`(합연산).
- **보정(`Sanitized`)은 `WeaponDefinition.OnValidate`에서만 돈다.** 예전엔 `GetLevelData`가
  호출할 때마다 15번 클램프했다 — 발사마다. 런타임 조회 경로에는 이제 없다.
- 필드를 늘리면 `operator +`도 같이 늘려야 한다. 빠뜨리면 에디터 진입 시 셀프체크가 에러를 뱉는다
  (`WeaponLevelData.SelfCheck`, 리플렉션으로 전 필드 대조).
- **투사체 수치의 출처는 레벨 표 하나다.** `Projectile`의 `_speed`/`_damage` 필드는 제거했다.
  프리팹과 표 두 곳에 같은 값이 있으면 어느 쪽이 이기는지 알 수 없다.

### 소비처 없는 필드가 5개다

발사 형태를 만들면서 그 형태가 쓰는 축을 연결했다. 지금 실제로 코드가 읽는 것은 10축이다.

| 축 | 읽는 곳 |
| --- | --- |
| 데미지 | 전부 |
| 발사 수 / 투사체 속도 / 관통 수 / 투사체 크기 / 유도 강도 | `ProjectileWeapon` → `Projectile` |
| 효과 반경 / 지속시간 | `AreaWeapon` → `DamageField` |
| 넉백 | 둘 다 → `IDamageable.ApplyKnockback` |
| 사거리 | 조준 대상 탐색(`WeaponBase.ResolveAimDirection`), 영역의 낙하 지점 |

**아직 선언만 된 것은 5개다** — 치명타 확률, 치명타 배수, 연주 밀도 배수, 쿨다운 감소, 반사 횟수.
인스펙터에서 값을 넣어도 아무 일도 일어나지 않는다. "치명타를 50%로 넣었는데 안 터진다"는 버그가 아니다.

앞의 셋은 발사 형태가 아니라 **성장·채보 쪽에서 소비될 축**이라 여기서 연결하지 않았다.
반사는 그 형태를 쓰는 악기가 아직 없다.

### 넉백은 IDamageable의 기본 구현이다

`ApplyKnockback`은 인터페이스에 기본 구현(빈 메서드)으로 있다. 넉백을 받을 이유가 없는 대상
(플레이어)이 빈 메서드를 억지로 채우지 않게 하려는 것이고, 별도 인터페이스로 쪼개면 때리는 쪽이
`is IKnockbackable` 분기를 들게 되어 "대상의 구체 타입을 모른다"는 전제가 깨진다.

`Enemy`의 구현은 힘이 아니라 **추격을 잠깐 밀어내기로 갈아끼우는** 방식이다. `FixedUpdate`가
추격 속도를 매 프레임 통째로 덮어쓰기 때문에 `AddForce`로는 한 프레임도 밀리지 않는다.

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
- **`CinemachineBrain.UpdateMethod`는 `LateUpdate`다.** 기본값 `SmartUpdate`가 아니다 — 이유는 아래.
- `Prototype` 씬은 손대지 않았다. 무기 검증용이라 고정 카메라가 더 편하다.

## 빌드에서만 드러나는 설정

**에디터 Game View는 이 증상들을 가린다.** 프레임이 낮고 에디터가 합성해서 띄우기 때문이다.
빌드로만 재현되므로, 아래 셋은 "코드가 멀쩡한데 화면이 이상한" 부류로 다시 물리기 쉽다.

| 설정 | 값 | 없으면 |
| --- | --- | --- |
| `Rigidbody2D.interpolation` | `Interpolate` (Player·Enemy·Projectile) | 이동이 잘게 떨린다 |
| `CinemachineBrain.UpdateMethod` | `LateUpdate` | 카메라만 계단을 밟는다 |
| `QualitySettings` PC 레벨 `vSyncCount` | `1` | 화면이 가로로 갈라진다(테어링) |

물리는 `Fixed Timestep 0.02`로 50Hz 고정인데 렌더는 주사율대로 돈다. 144Hz면 144÷50=2.88로
나누어떨어지지 않아 **프레임당 물리 스텝 수가 `1-0-0-1-0-1`처럼 흔들린다.** 등속으로 이동할 때
이게 규칙적인 맥놀이로 보인다. 보간은 직전 두 물리 위치 사이를 렌더 프레임마다 채워 이를 없앤다.

**앞의 둘은 세트로 간다.** 보간된 위치는 렌더 직전(Update 단계)에 트랜스폼에 써지는데,
`SmartUpdate`는 물리로 움직이는 타겟을 감지해 카메라를 `FixedUpdate`에서 갱신한다 —
그 시점엔 **보간 전 원본 위치밖에 없다.** 프리팹 보간만 켜면 플레이어는 부드러워지고
카메라가 떠는 형태로 증상이 남는다. `Damping 0.35`가 그걸 더 도드라지게 한다.

`Extrapolate`는 쓰지 않는다. 속도로 앞을 예측하는 방식이라, WASD로 방향을 계속 꺾고 적·벽에
부딪히는 이 게임에서는 예측이 빗나갈 때마다 되튕김이 보인다. `Interpolate`가 만드는 최대 20ms
지연은 탑다운에서 체감되지 않는다.

`targetFrameRate`는 세우지 않는다. VSync와 같이 걸면 상한이 이중이 되어 고주사율 모니터에서
의도치 않게 낮은 프레임으로 묶인다.

## 알려진 제약

- **박은 프레임과 무관하게 간다.** 클록이 dspTime 기준이라 창이 비활성이거나 긴 히치가 나면
  프레임이 멈춘 동안에도 박이 흐르고, 복귀 순간 진행도가 뛴다.
- `BeatTimelineEndSource`는 임시다. 마디 수는 기획 확정 시 사라질 값.
- 카운트다운·일시정지 UI 없음. 레벨업 카드는 있다(→ "성장").
- HUD는 매 프레임 폴링한다. 위젯이 늘면 이벤트 기반으로 바꾸는 게 낫다.
- 고르지 않은 카드가 먹힐 수 있다 — `Navigation.Mode` 미설정. 전말은 "카드는 아무것도 멈추지 않는다"에.
- 픽업 드랍표의 `_dropUntilMinutes`는 아무도 안 읽는다. 스테이지 경과 시간을 알려주는 것이
  없어서, 회차 시스템이 생길 때까지 값만 받아 둔다(→ "픽업").
