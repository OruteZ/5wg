# Architecture

이 프로젝트가 어떻게 조립되어 있는지, 그리고 **왜 그렇게 되어 있는지**를 적는다.
"무엇이 있는가"는 코드를 읽으면 알 수 있으니, 여기에는 코드만 봐서는 알 수 없는 판단 근거를 남긴다.

관련 문서: [overview.md](overview.md) — 엔진 버전·패키지·폴더. [progress.md](progress.md) — 진행 중인 작업과 미해결 항목.

---

## 전체 그림

리듬 기반 자동공격 게임. 플레이어는 조준도 발사도 하지 않고, 무기 6개가 음악의 박자에 맞춰 알아서 쏜다.

```
BpmClock ──(dspTime 기반 박자)──> WeaponHandler ──> 무기 6개 ──> ProjectilePool ──> Projectile2D
   │                                   │                 │
   │                                   │                 └─> EnemyTargetProvider ──> EnemyRegistry
   │                                   │
PauseController                  IFireGate (발사 허용)
   │
PauseState (전역) ──> Time.timeScale
```

---

## 레이어

### `Runtime/Core/`
프로젝트 어디서나 참조하는 최소 계약. `IDamageable`, `PauseState`, `PauseController`.
여기에는 게임 규칙을 넣지 않는다. 늘어나기 시작하면 잘못 가고 있는 신호다.

### `Runtime/Beat/`
`BpmClock`은 **순수 클록**이다. `AudioSettings.dspTime`을 앵커로 "지금 몇 박인지"만 계산하고,
누가 그걸 어디에 쓰는지는 모른다. 게임 쪽 개념(일시정지, 전투)이 여기 들어가면 안 된다.
일시정지도 `PauseController`가 밖에서 `Pause()`를 불러주는 방식이다.

### `Runtime/Player/`
`PlayerController` / `PlayerMovement` / `WeaponHandler` 셋으로 나뉜다.
**입력 장치를 아는 건 `PlayerController` 하나뿐**이고, 나머지는 주입받은 값만 본다.
그래서 AI가 조종하는 유닛을 만들 때 `PlayerController`만 갈아끼우면 된다.

### `Runtime/Weapons/`
가장 큰 덩어리. 아래 "무기 시스템" 참고.

### `Runtime/Enemies/`, `Runtime/Combat/`
`Enemy2D`, `EnemySpawner2D`, `EnemyRegistry`, `Projectile2D`.

---

## 무기 시스템

무기 하나를 **다섯 축**으로 쪼갰다. 축이 독립적이라 한쪽을 바꿔도 나머지를 건드리지 않는다.

| 축 | 담당 | 확장 방법 |
| --- | --- | --- |
| 언제 쏘는가 | `IFireTiming` | 구현체 추가 |
| 쏴도 되는가 | `IFireGate` | 게이트 추가 후 `WeaponHandler.AddGate` |
| 무엇을 갖고 있는가 | `WeaponInventory` | 6슬롯 고정 |
| 어떻게 쏘는가 | `IWeapon` / `WeaponBase` | `WeaponBase` 상속 후 `OnFire` 구현 |
| 어디를 겨누는가 | `WeaponBase.ResolveAimDirection` | `override` |

### 왜 이렇게 나눴나

**타이밍을 무기 밖으로 뺀 이유.** 무기별 발사 리듬이 기획서에서 정해질 예정이라, 그 부분만
비워두고 나머지를 먼저 세워야 했다. `IFireTiming`은 그 빈자리다. 지금은 `EveryBeatTiming`
하나뿐이고, `WeaponDefinition._intervalBeats`/`_offsetBeats`는 기획 확정 시 사라질 임시 필드다.

**발사 허용을 게이트로 뺀 이유.** "쏠 수 없는 상황"은 계속 늘어난다 — 사망, 일시정지, 컷신,
상점, 침묵 디버프, 슬롯 봉인. 이걸 `WeaponHandler`의 `if`로 쌓으면 금방 썩는다.
조건 하나 = 객체 하나로 두고 전부 AND로 묶는다. 새 조건은 `AddGate`로 런타임에 붙인다.
그래서 `WeaponHandler`는 플레이어가 죽었는지도, 게임이 멈췄는지도 직접 알 필요가 없다.

**조준을 무기에 맡긴 이유.** 핸들러가 방향을 미리 계산해 넘기면 무기가 다르게 조준할 방법이
없어진다. `FireContext`에는 **원본 단서**(스틱 입력, 마지막 이동 방향)만 싣고, 최종 방향은
무기가 `ResolveAimDirection`에서 뽑는다. 질의 자체(최근접 적, 마우스 월드 좌표)는 `AimHelper`에 모았다.

**무기를 MonoBehaviour로 만들지 않은 이유.** 슬롯 교체가 잦은데 그때마다 컴포넌트를
생성·파괴하면 비용도 들고 수명 관리도 꼬인다. 순수 C# 클래스라 `WeaponDefinition`(SO)이
팩토리 역할만 하면 된다. 대신 무기는 `Transform`이나 풀 같은 걸 직접 못 잡으니 `WeaponContext`로 주입받는다.

**레벨 데이터를 증분이 아니라 절대값 표로 둔 이유.** 리듬 게임이라 레벨업이 데미지뿐 아니라
**발사 패턴 자체**를 바꿀 가능성이 높다. 증분으로는 그걸 표현할 수 없다.

**투사체 풀을 무기 밖에 둔 이유.** 6개 무기가 서로 다른 탄을 쓴다. 무기마다 풀을 들면
같은 프리팹에 대해 풀이 중복 생성된다. `ProjectilePool`이 프리팹별로 하나씩 소유한다.

### 두 종류의 컨텍스트

- `WeaponContext` — **장착 시 1회**. 오래 사는 서비스(Owner, ProjectilePool, ITargetProvider, Camera).
- `FireContext` — **틱마다**. 그 순간의 값(Origin, AimInput, FacingDirection, BeatTick).

수명이 다른 걸 한 구조체에 섞지 않는다.

### 발사 판정 순서

`WeaponHandler.DispatchTick`에서 이 순서로 거른다. 순서에 의미가 있다.

1. `AttackEnabled` — 전역 스위치
2. `weapon.IsReady` — 무기 내부 사정(재장전·차지)
3. `AllGatesAllow(weapon)` — 외부 조건
4. `weapon.Timing.ShouldFire(tick)` — 박자
5. `weapon.Fire(context)`

싼 검사부터 한다. 타이밍 판정이 가장 마지막인 이유는, 못 쏘는 상황에서 타이밍 구현체의
내부 카운터가 헛돌면 안 되기 때문이다.

---

## 박자 처리

`BpmClock`은 `AudioSettings.dspTime`을 앵커로 쓴다. `Time.time`이 아니라 dspTime인 이유는
오디오와 어긋나면 리듬 게임이 성립하지 않기 때문이다. **그 대신 `Time.timeScale`의 영향을 받지 않는다.**

`WeaponHandler`는 클록을 폴링해 서브비트 경계를 감지한다. 이벤트 구독이 아니라 폴링인 이유는
`BeatEvent`가 `UnityEvent` 기반 에디터 배선용 컴포넌트라, 전투 시스템이 인스펙터 연결에 의존하게 되기 때문이다.

**밀린 틱 보정.** 프레임이 밀리면 서브비트를 건너뛸 수 있다. 그냥 넘기면 발사가 통째로 누락되므로
지난 프레임 이후의 서브비트를 전부 틱으로 흘린다. 다만 한 번에 몰아 쏘면 화면이 터지므로
`_maxCatchUpTicks`(기본 8)로 한도를 둔다. **이 값은 게임 느낌에 직접 영향을 준다.**

---

## 일시정지

`PauseState`는 정적 클래스다. 일시정지는 `Time.timeScale`처럼 본질적으로 전역이고,
아무 컴포넌트나 `OnEnable`에서 구독할 수 있어야 초기화 순서에 얽히지 않기 때문이다.

| 멈추는 방식 | 대상 |
| --- | --- |
| `Time.timeScale = 0`으로 자동 | 적 이동(FixedUpdate), 물리, `Time.deltaTime` 기반 전부 |
| 명시적으로 멈춰야 함 | `BpmClock` (dspTime 기반), `EnemySpawner2D` (Awaitable), `WeaponHandler` (게이트) |

**`timeScale`만 믿으면 안 되는 게 핵심이다.** 새 시스템을 붙일 때 dspTime이나 unscaled 시간을
쓴다면 `PauseState.Changed`를 구독해야 한다.

도메인 리로드를 끈 상태에서 일시정지 중 플레이를 멈추면 다음 실행이 멈춘 채로 시작하므로,
`RuntimeInitializeOnLoadMethod`로 정적 상태를 되돌린다. **정적 상태를 새로 만들면 이 처리를 같이 해야 한다.**

---

## 성능에 관한 판단

이미 적용한 것과, 일부러 하지 않은 것을 함께 남긴다.

### 적용

- **적 탐색은 레지스트리 순회.** 물리 오버랩은 발사마다 씬 콜라이더를 훑고 후보마다 인터페이스
  조회를 한다. 무기 6개 × 매 박자면 그대로 곱해진다. `Enemy2D`가 스스로 등록하는 목록을 훑는 게 훨씬 싸다.
- **투사체 수명은 풀에서 일괄 갱신.** 개체마다 `Update`를 두면 개체 수만큼 네이티브-매니지드
  전환이 생긴다. 탄막 게임에서는 그 개체 수가 곧 부하다.
- **목록 제거는 마지막 원소 당겨오기(O(1)).** `EnemyRegistry`, `ProjectilePool` 활성 목록 모두.
  순서가 의미 없는 목록에서만 쓴다.
- **박자 조회는 한 번에.** `CurrentBeat`/`CurrentSubBeat`를 따로 읽으면 `Quantize`가 두 번 돈다.

### 일부러 하지 않음

- **`EnemySpawner2D._activeEnemies.Remove`의 O(n).** 동시 최대 40마리라 실측 부하가 없다.
  적 수명 관리를 건드리는 위험이 이득보다 크다.
- **`ObjectPool(collectionCheck: true)`.** Get/Release마다 HashSet 조회가 있지만, 이중 반납은
  추적하기 어려운 버그라 방어를 유지한다.
- **`Enemy2D.Update`(피격 연출) 중앙화.** 40마리 규모에서는 이득이 미미하고, 상태가 개체별이라
  중앙화하면 코드가 더 복잡해진다.

---

## 알려진 취약점

- **`Camera.main`을 `WeaponHandler.Awake`에서 한 번만 잡는다.** 런타임에 카메라를 교체하면 참조가 낡는다.
- **`PhysicsTargetProvider`의 레이어 마스크 기본값이 `~0`.** 자기 계층 제외와 `IDamageable` 검사로
  막아뒀지만, 성능을 위해서는 적 레이어를 지정하는 게 맞다.
- **피아 식별이 레이어가 아니라 코드.** `Projectile2D`가 owner 계층을 통과시키는 방식이다.
  진영이 셋 이상으로 늘어나면 팀 개념이 따로 필요하다.
- **`Prototype_PlayerMovement.unity`는 배선이 비어 있어 발사되지 않는다.** 작업 씬은 `Prototype.unity`.
