# Progress

지금 무슨 작업 중이고 뭐가 안 끝났는지만 적는다.
설계 근거는 [architecture.md](architecture.md), 세션 시작 안내는 [session-guide.md](session-guide.md)에 있다.

---

## 현재 상태 (2026-08-02)

리듬 기반 자동공격 프로토타입. 플레이어가 6개 무기를 보유하고 박자에 맞춰 자동 발사한다.
무기 시스템 뼈대와 일시정지가 올라갔고, **무기별 발사 타이밍만 기획서 대기 중**이다.

작업 씬은 `Assets/_Proejct/Scenes/Prototype.unity`.

### 브랜치

| 브랜치 | 내용 | 상태 |
| --- | --- | --- |
| `feat/weaponSystem` | 플레이어 3분할, 무기 시스템, 발사 버그 2건 | 푸시됨, PR 미생성 |
| `feat/pause-and-optimization` | 일시정지, 최적화 3건, 문서 | 작업 중 |

`gh` CLI가 없어 PR은 수동 생성해야 한다.

---

## 에디터에서 해야 할 일

코드로는 끝났지만 씬 배선이 남은 것들.

1. **`Prototype.unity`에 `PauseController` 추가** — `InputActionAsset` 연결 필요.
   기본 액션 경로 `UI/Cancel`(키보드 Esc, 게임패드). `BpmClock`은 비우면 자동 탐색.
2. **`PhysicsTargetProvider`를 쓸 경우 적 레이어 지정** — 기본값이 `~0`이다.
   현재 기본 탐색은 `EnemyTargetProvider`(레지스트리 기반)라 해당 없음.

---

## 런타임 미검증

빌드는 통과했지만 **플레이 모드로 확인하지 못한** 변경들. 우선 여기부터 확인할 것.

- **일시정지 전반** — Esc로 멈추고 풀리는지, 재개 후 박자 위상이 맞는지.
- **`EnemyRegistry` 등록/해제가 풀 재사용과 맞물리는지** — 적이 안 잡히거나 죽은 적이 잡히면 이쪽.
- **투사체가 수명대로 사라지는지** — `ProjectilePool`이 수명을 대신 돌리도록 바뀌었다.
  누수(안 사라짐)나 조기 소멸이 있으면 활성 목록 인덱스 관리를 의심한다.

---

## 미해결

### 기획 대기

- **무기별 비트 패턴** — 확정되면 `IFireTiming` 구현체와 `WeaponLevelData` 필드를 채운다.
  `WeaponDefinition._intervalBeats`/`_offsetBeats`는 그때 제거될 임시 필드다.
- **무기 5종의 형태** — 현재 `ProjectileWeapon` 1종뿐. 장판·오라·근접 등이 정해져야
  `WeaponDefinition` 파생 클래스를 더 만들 수 있다.

### 기술 부채

- `Camera.main`을 `WeaponHandler.Awake`에서 한 번만 잡는다. 런타임 카메라 교체 시 참조가 낡는다.
- 피아 식별이 owner 계층 비교 방식이라 진영이 셋 이상이 되면 팀 개념이 필요하다.
- `WeaponHandler._maxCatchUpTicks`(기본 8)는 게임 느낌에 영향을 준다. 플레이 후 조정 필요.
- `Prototype_PlayerMovement.unity`는 `BpmClock`·시작 무기가 비어 있어 발사되지 않는다.
  더 쓸 계획이 없으면 지우는 게 낫다.

### 미착수

- 일시정지 UI가 없다. `PauseState.Changed`를 구독하면 붙일 수 있다.
- 무기 슬롯 HUD가 없다. `WeaponInventory`의 `Acquired`/`Removed`/`Upgraded` 이벤트로 붙인다.
- 테스트 어셈블리가 없다. `WeaponInventory`, `EveryBeatTiming`, `AimHelper`는 MonoBehaviour가
  아니라 순수 C#이므로 에디트 모드 테스트가 바로 가능하다.

---

## 해결한 문제 (기록)

같은 곳을 다시 밟지 않도록 남긴다. 자세한 배경은 [session-guide.md](session-guide.md#이-프로젝트에서-반복해서-물린-것들).

- **클록이 돌지 않아 발사가 아예 안 됨** — `BpmClock`이 `Idle`로 시작하는데 아무도 `Play()`를
  호출하지 않았다. `playOnStart`(기본 켬) 추가.
- **투사체가 자기 주인을 때리고 사라짐** — 발사구가 플레이어 트리거 안이라 스폰 즉시 자해했다.
  `Launch`에 owner를 받아 통과시킨다.
- **둘 다 로그 없이 조용히 실패했다** — `WeaponHandler`가 시작 시 클록 미연결·무기 미지급을
  경고하고, 인스펙터에 클록 상태를 노출한다.
