# Progress

Current work, milestones, and decisions. Keep this updated as the project moves forward.

## 현재 상태 (2026-08-02)

리듬 기반 자동공격 프로토타입. 플레이어가 6개 무기를 보유하고 박자에 맞춰 자동 발사한다.
무기 시스템의 뼈대는 세워졌고, **무기별 발사 타이밍만 기획서 대기 중**이다.

작업 씬은 `Assets/_Proejct/Scenes/Prototype.unity`.

## 플레이어 구성

한 컴포넌트가 입력·이동·발사를 다 하던 걸 셋으로 쪼갰다. 셋 다 같은 GameObject에 붙는다.

| 컴포넌트 | 역할 |
| --- | --- |
| `PlayerController` | 입력을 한 곳에서 읽어 하위에 주입. 체력·사망 관리(`IDamageable`) |
| `PlayerMovement` | 물리 이동만. 입력은 `SetMoveInput`으로 주입받음 |
| `WeaponHandler` | 박자 틱 생성 → 게이트 판정 → 무기에 발사 위임 |

`PlayerMovement`/`WeaponHandler`는 입력 장치를 모른다. `PlayerController`만 `InputActionAsset`을 든다.

## 무기 시스템

`Assets/_Proejct/Scripts/Runtime/Weapons/`

### 축 분리

각 축이 독립적으로 확장되도록 나눴다. 한 축을 바꿔도 나머지는 건드리지 않는다.

| 축 | 담당 | 비고 |
| --- | --- | --- |
| 언제 쏘는가 | `IFireTiming` | **기획서 대기 중**. 현재 `EveryBeatTiming`(N정박마다) 하나뿐 |
| 쏴도 되는가 | `IFireGate` | 등록된 게이트를 전부 AND. 사망 게이트는 `PlayerController`가 등록 |
| 무엇을 갖고 있는가 | `WeaponInventory` | 6슬롯 고정. 중복 획득은 레벨업으로 흡수 |
| 어떻게 쏘는가 | `IWeapon` / `WeaponBase` | 순수 C# 클래스. `WeaponDefinition`(SO)이 팩토리 |
| 어디를 겨누는가 | `WeaponBase.ResolveAimDirection` | 무기가 재정의. 질의 유틸은 `AimHelper` |

### 폴더

```
Weapons/
  Core/       IWeapon  WeaponBase  WeaponContext(장착 시 1회)  FireContext(틱마다)
  Timing/     IFireTiming  BeatTick  EveryBeatTiming(임시)
  Gates/      IFireGate  DelegateFireGate
  Aiming/     AimHelper
  Data/       WeaponDefinition  WeaponLevelData
  Services/   ProjectilePool  ITargetProvider  PhysicsTargetProvider
  Projectile/ ProjectileWeapon  ProjectileWeaponDefinition
  WeaponInventory.cs
```

### 결정 사항

- 무기 런타임은 MonoBehaviour가 아닌 순수 C# 클래스 — 슬롯 교체에 컴포넌트 생성·파괴가 끼지 않게.
- 무기 식별자는 `WeaponDefinition` 에셋 참조. 세이브가 필요해지면 별도 문자열 ID를 추가한다.
- 레벨 데이터는 증분이 아닌 레벨별 절대값 표(`WeaponLevelData[]`). 레벨업이 비트 패턴 자체를 바꿀 수 있어서다.
- 자동공격 전환으로 Attack 입력 경로를 제거했다. Look 입력은 스틱 조준 단서로만 남아 있다.
- 조준 방향은 핸들러가 정하지 않는다. `FireContext`에는 원본 단서(스틱 입력·이동 방향)만 싣고
  최종 방향은 무기가 `ResolveAimDirection`에서 뽑는다. 기본 순서는 최근접 적 → 마우스 → 스틱 → 이동 방향.
- 투사체 풀은 무기가 아닌 `ProjectilePool`이 프리팹별로 소유한다. 6개 무기가 서로 다른 탄을 쓰기 때문.
- `WeaponHandler`는 `BpmClock`을 폴링해 서브비트 경계를 감지하고, 프레임이 밀리면 최대 N틱까지 보정 발사한다.

## 해결한 문제

**클록이 돌지 않아 발사가 아예 안 됨** — `BpmClock`이 `Idle`로 시작하는데 아무도 `Play()`를 호출하지
않았다. `playOnStart`(기본 켬)를 추가했다. `Awake`가 아니라 `Start`에서 앵커를 잡는다.

**투사체가 자기 주인을 때리고 사라짐** — 발사구가 플레이어 콜라이더(트리거) 안이라, `PlayerController`에
`IDamageable`이 붙은 뒤로 스폰 즉시 자해하고 회수됐다. 화면상 발사가 안 되는 것처럼 보이고 5초 뒤
플레이어가 죽어 생존 게이트가 잠겼다. 레이어 설정에 기대지 않도록 코드로 막았다.

- `Projectile2D.Launch(..., Transform owner)` — owner 계층에 속한 대상은 통과시킨다.
- `PhysicsTargetProvider` — 자기 계층 제외 + `IDamageable`인 대상만 타겟으로 인정(아군 투사체 조준 방지).

**조용한 실패** — 위 두 건 모두 로그 없이 아무 일도 안 일어나 원인 추적이 오래 걸렸다. `WeaponHandler`가
시작 시 클록 미연결·무기 미지급을 로그로 알리고, 인스펙터에 클록 상태와 마지막 서브비트 인덱스를 띄운다.

## 미해결

- **무기별 비트 패턴(기획서 필요)** — 확정되면 `IFireTiming` 구현체와 `WeaponLevelData` 필드를 채운다.
  `WeaponDefinition`의 `_intervalBeats`/`_offsetBeats`는 그때 제거될 임시 필드.
- 무기가 아직 `ProjectileWeapon` 1종뿐. 나머지 5종의 형태(장판·오라·근접 등)가 정해져야 한다.
- 레이어 마스크는 아직 `~0` 기본값. 코드로 막아뒀지만 성능을 위해 적 레이어는 지정하는 게 좋다.
- `PhysicsTargetProvider`는 매 발사마다 오버랩을 돈다. 적이 많아지면 등록 기반 레지스트리로 교체.
- `Camera.main`을 `WeaponHandler.Awake`에서 한 번만 잡는다. 런타임에 카메라를 바꾸면 참조가 낡는다.
- `Prototype_PlayerMovement.unity`는 `BpmClock`·시작 무기가 비어 있어 발사되지 않는다.
