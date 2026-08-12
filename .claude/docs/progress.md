# Progress

Current work, milestones, and decisions. Keep this updated as the project moves forward.

## 현재 상태 (2026-08-12)

리듬 기반 자동공격 프로토타입. 플레이어가 6개 무기를 보유하고 박자에 맞춰 자동 발사하며,
레벨업마다 카드 3장 중 하나를 골라 무기를 얻거나 강화한다. 적을 잡으면 경험치 외에
티켓·아이템도 떨어진다.

악기 10종과 4개 발사 형태가 전부 있고 **채보(곡별 악기 트랙)만 기다리고 있다.** 그게 들어오면
지금의 임시 타이밍(`EveryBeatTiming`·`BurstTiming`)이 통째로 교체된다.

작업 씬은 `Assets/_Project/Scenes/Stage01.unity`. 씬 목록은 `overview.md`에 있다.

아래는 **브랜치 순서대로 쌓인다. 위가 오래된 것, 아래가 최근**이고 "다음 작업"에서 끝난다.
그 뒤에 "해결한 문제"와 "미해결"이 붙는다.

구조 설명은 여기 두지 않는다 — `architecture.md`에 있고, 여기는 그때 왜 그렇게 정했는지만 남긴다.

## 플레이어 구성

한 컴포넌트가 입력·이동·발사를 다 하던 걸 셋으로 쪼갰다. 셋 다 같은 GameObject에 붙는다.

| 컴포넌트 | 역할 |
| --- | --- |
| `PlayerController` | 입력을 한 곳에서 읽어 하위에 주입. 체력·사망 관리(`IDamageable`) |
| `PlayerMovement` | 물리 이동만. 입력은 `SetMoveInput`으로 주입받음 |
| `WeaponHandler` | 박자 틱 생성 → 게이트 판정 → 무기에 발사 위임 |

`PlayerMovement`/`WeaponHandler`는 입력 장치를 모른다. `PlayerController`만 `InputActionAsset`을 든다.

## 무기 시스템

`Assets/_Project/Scripts/Runtime/Weapons/`

### 축 분리

각 축이 독립적으로 확장되도록 나눴다. 한 축을 바꿔도 나머지는 건드리지 않는다.

| 축 | 담당 | 비고 |
| --- | --- | --- |
| 언제 쏘는가 | `IFireTiming` | **채보 대기 중.** 임시로 `EveryBeatTiming`(N정박마다), `BurstTiming`(연타) |
| 쏴도 되는가 | `IFireGate` | 등록된 게이트를 전부 AND. 사망 게이트는 `PlayerController`가 등록 |
| 무엇을 갖고 있는가 | `WeaponInventory` | 6슬롯 고정. 중복 획득은 레벨업으로 흡수 |
| 어떻게 쏘는가 | `IWeapon` / `WeaponBase` | 순수 C# 클래스. `WeaponDefinition`(SO)이 팩토리 |
| 어디를 겨누는가 | `WeaponBase.ResolveAimDirection` | 무기가 재정의. 질의 유틸은 `AimHelper` |

### 폴더

```
Weapons/
  Core/       IWeapon  WeaponBase  WeaponContext(장착 시 1회)  FireContext(틱마다)
  Timing/     IFireTiming  BeatTick  EveryBeatTiming(임시)  BurstTiming(임시)
  Gates/      IFireGate  DelegateFireGate
  Aiming/     AimHelper
  Data/       WeaponDefinition  WeaponLevelData
  Services/   ProjectilePool  ITargetProvider  RegistryTargetProvider
  Projectile/ ProjectileWeapon  ProjectileWeaponDefinition
  Area/       AreaWeapon  AreaWeaponDefinition
  WeaponInventory.cs
```

무기가 씬에 뿌리는 개체는 `Combat/`에 있다 — `Projectile`, `DamageField`.

### 결정 사항

- 무기 런타임은 MonoBehaviour가 아닌 순수 C# 클래스 — 슬롯 교체에 컴포넌트 생성·파괴가 끼지 않게.
- 무기 식별자는 `WeaponDefinition` 에셋 참조. 세이브가 필요해지면 별도 문자열 ID를 추가한다.
- 레벨 데이터는 증분이 아닌 레벨별 절대값 표(`WeaponLevelData[]`). 레벨업이 비트 패턴 자체를 바꿀 수 있어서다.
- 자동공격 전환으로 Attack 입력 경로를 제거했다. Look 입력은 스틱 조준 단서로만 남아 있다.
- 조준 방향은 핸들러가 정하지 않는다. `FireContext`에는 원본 단서(스틱 입력·이동 방향)만 싣고
  최종 방향은 무기가 `ResolveAimDirection`에서 뽑는다. 기본 순서는 최근접 적 → 마우스 → 스틱 → 이동 방향.
- 탄·장판 풀은 무기가 아닌 `ProjectilePool`이 프리팹별로 소유한다. 무기마다 다른 개체를 쓰기 때문.
- `WeaponHandler`는 `BpmClock`을 폴링해 서브비트 경계를 감지하고, 프레임이 밀리면 최대 N틱까지 보정 발사한다.

## 스테이지 흐름 (버티컬 슬라이스)

`Assets/_Project/Scripts/Runtime/Stage/`, `.../UI/`

시작·종료 규칙을 갖춘 최소 플레이 루프. 수치는 전부 임시다.
레이어 분리와 시퀀스는 `architecture.md`가 전부 갖고 있다 — 여기는 그때 내린 결정만 남긴다.

- 종료 판정을 초가 아니라 **박 기준**으로 잰다. 실제 곡을 물릴 때 "몇 마디"가 그대로 이식된다.
- 버튼 배선은 인스펙터 UnityEvent가 아니라 코드(`onClick.AddListener`). 씬 파일에 로직이 숨지 않게.
- `Stage01`의 `BpmClock`은 `playOnStart`를 껐다. 시작 시점은 디렉터가 소유한다(`Stop()`→`Play()`로 재앵커).
- 적 접촉 데미지는 트리거 1회 타격 + 재타격 간격(기본 0.8초).

UI는 uGUI 레거시 `Text`/`Slider`/`Button` 그레이박스다. TMP 에센셜을 안 받아도 되게.
씬 목록은 `overview.md`에 있다.

## 성장·카메라 (빌드 피드백 폴리싱)

`Assets/_Project/Scripts/Runtime/Progression/`

- 적이 죽으면 경험치 오브를 떨어뜨리고, 플레이어가 가까이 가면 끌려와 수집된다.
  누적이 요구량을 넘으면 레벨업한다. (레벨업 카드는 아래 `feat/weapon-first-type` 절에서 붙었다.)
- `Stage01` 카메라를 Cinemachine으로 교체. Main Camera는 `CinemachineBrain`만 들고,
  `CM Player Camera`가 플레이어를 따라간다(데드존 0.12 / 감쇠 0.35).

구조와 결정 사항은 `architecture.md`의 "성장", "카메라" 절에 있다.

## 풀링·조준 정리 (최적화)

- 세 곳에 흩어져 있던 오브젝트 풀 보일러플레이트를 `PrefabPool<T>`(합성)로 합쳤다.
  이 과정에서 `ProjectilePool`만 없던 일괄 회수가 생겨, 스테이지 종료 후에도 날아가던 탄이 정리된다.
- 조준 대상 탐색을 물리 오버랩에서 `TargetRegistry` 기반으로 바꿨다.
  발사마다 쿼리를 돌던 걸 목록 순회로 대체 — 무기가 6개로 늘면 차이가 커진다.

- HUD가 매 프레임 만들던 문자열 2개를 값이 바뀔 때만 만들도록 고쳤다.
- 적 200마리로 프로파일링했고 **뚜렷한 병목이 없었다.** 그래서 **매니저 루프 합치기와
  적끼리 충돌 끄기를 둘 다 하지 않았다** — 재검토할 사람이 같은 측정을 다시 하지 않도록 적어둔다.
  수치와 콜라이더 크기 실험은 `architecture.md`의 "물리 레이어" 절에 표로 있다.

구조는 `architecture.md`의 "풀링", "조준 대상 탐색", "물리 레이어" 절에 있다.

## 무기 스탯 축 (브랜치 `weapon-stat`)

무기 스탯 축을 기획 문서(15축)에 맞춰 넓혔다. 계약은 `architecture.md`의 "무기 스탯" 절.

- `WeaponLevelData` 3축 → 15축, `operator +`(합연산), 보정을 `OnValidate`로 이동
- `Projectile._speed`/`_damage` 제거 — 수치 출처를 레벨 표 하나로

## 악기 10종 1차 (브랜치 `feat/weapon-first-type`, PR #10)

악기 10종의 **1차 버전**. 4축을 전부 만들고 정의 에셋 10개를 붙였다.

만든 것은 클래스 셋과 타이밍 하나뿐이다. 형태가 같은 것을 클래스로 나누지 않았다.

| | 담당 | 악기 |
| --- | --- | --- |
| `DamageField` | 반경 안을 주기적으로 때리는 영역 | 킥·크래시·베이스·신스 |
| `AreaWeapon` / `AreaWeaponDefinition` | 그 영역을 배치 | 〃 |
| `BurstTiming` | 정박에서 시작해 서브비트 N개 연타 | 탐·건반 |
| `PrefabPoolSet<T>` | 프리팹별 풀 고르기 (기존 `ProjectilePool`에서 추출) | — |

기존 `Projectile`에 관통·유도·크기·넉백을 붙였다. 인자가 아홉 개가 되어 `ProjectileSpawnData`로 묶었다.

- **즉발 범위와 지속 영역은 한 클래스다.** 충격파 = 지속시간 0인 장판, 오라 = 추종하는 장판.
- **순차 다단은 무기가 아니라 타이밍이다.** 그래서 탐은 투사체, 건반은 영역이면서 둘 다 연타다.
- 대상 탐색에 트리거 콜라이더를 쓰지 않는다 — 풀에서 꺼낸 순간 이미 겹쳐 있던 적에게는 `Enter`가
  다음 물리 스텝에나 와서, 지속 0인 충격파가 **아무도 못 때린다.** 타격마다 오버랩 쿼리를 돈다.
- 넉백은 `IDamageable`의 기본 구현으로 뚫었다. `Enemy`는 힘이 아니라 추격 속도를 잠깐
  갈아끼우는 방식이다 — `FixedUpdate`가 속도를 매 프레임 덮어써서 `AddForce`가 먹지 않는다.

### 테스트용으로 바꾼 임시 값

밸런스 결정이 아니라 플레이 확인을 위해 넣은 값이다. 기획 수치가 나오면 갈아엎는다.

| 대상 | 값 | 이유 |
| --- | --- | --- |
| 악기 10종 에셋 | 5레벨 표 전부 | 형태가 눈에 구분되는 정도로만 잡았다 |
| `Player.prefab`의 시작 무기 | 스네어·기타·킥·베이스·신스·탐 | 6슬롯으로 4축을 전부 덮으려고 |
| `Stage01`의 `_barsToClear` | 8 → 64마디(약 2분) | 16초마다 끝나면 테스트가 안 됨 |

레벨 표는 **TSV 왕복**으로도 편집한다. `WeaponDefinition`의 버튼 두 개가 표를 클립보드로
복사하고 되받는다. 스프레드시트에서 10종을 한 화면에 놓고 밸런싱하는 흐름이다.

### 레벨업 카드

레벨이 오르면 화면 중앙 하단에 카드 3장이 뜨고 하나를 고른다. 규칙은 `progression.md`의
"카드 선택" 절 그대로다. 구조는 `architecture.md`의 "성장" 절.

- `LevelUpDraft` (static) — 후보를 모아 중복 없이 3장. 신규 무기 가중치 = 빈 슬롯 수, 강화 = 1
- `LevelUpOption` — 카드 한 장. 적용까지 자기가 안다
- `LevelUpCardView` — 큐와 화면. 마우스 클릭 + 숫자키 1·2·3
- 고를 강화가 하나도 없으면 `Build`가 **빈 목록**을 주고, 뷰가 힐을 바로 적용한다(기본 25).
  힐은 카드 종류가 아니다 — 화면에 도달하지 않는 선택지를 타입에 두지 않으려고 (→ `architecture.md`)
- 경험치 곡선을 기획서대로 `5 + 3 × (레벨-1)` **선형**으로 바꿨다 (지수 1.25배였음)

`PlayerExp`의 `_growth`(배수) 필드가 `_requirementStep`(증가량)으로 바뀌었다.
씬·프리팹에 남은 옛 값은 무시된다.

### 확인한 것

무기 축 — `Stage01` 플레이 40초에서 47킬. 6무기 전부 발사되고, 관통(남은 타격 2),
유도(대상 잡힘), 추종 오라와 고정 장판이 동시에 살아 있는 것을 런타임에서 확인했다.

카드 축 — 레벨업 3연발로 큐가 쌓이고 순서대로 소진되는 것, 고른 카드가 실제로 무기 레벨을
올리는 것(스네어 Lv1→Lv3), 슬롯을 비우면 신규 무기 카드가 뜨는 것, 전부 만렙이면 카드 없이
HP가 40→65로 회복되는 것까지 확인. 콘솔 에러 없음.

### 검증 중에 잡은 것

**카드가 저절로 소진됐다.** `EventSystem`이 `Card2`를 자동 선택해 둔 상태라 Submit 한 번에
고르지도 않은 카드가 먹혔다(신스가 혼자 Lv2가 돼 있었다). 카드를 띄울 때마다 선택을 비우도록
했다. **다만 이것만으로는 완전히 막히지 않는다** — 자세한 건 아래 "미해결"에.

**적이 하나도 안 나와서 한참 헤맸는데 코드 문제가 아니었다.** 에디터가 포커스 밖이면 시간이
아예 안 흐른다. 검증 절차로 `overview.md`의 "Working in this repo"에 옮겨 적었다.

## 픽업 시스템 (브랜치 `feat/pickup-system`, PR #11)

경험치를 뺀 나머지 드랍 — 티켓 2종, 아이템 3종(회복·자석·리롤). 기획은 `pickup.md`,
구조는 `architecture.md`의 "픽업" 절.

- **뽑기(`PickupDropper`)와 풀(`PickupPool`)을 나눴다.** 스포너가 아는 픽업 타입이 드로퍼
  하나뿐이라, 확률 규칙이 늘어도 스포너와 풀은 그대로다.
- 확률은 전부 `PickupDropTable`(SO)에 있고 코드에 없다. 티켓은 "묶음에서 하나만",
  아이템은 "항목마다 따로" — 뽑는 방식이 달라서 묶음 단위로 갈랐다.
- **픽업은 저절로 사라지지 않는다.** 시간 소멸을 넣지 않은 대신 동시 상한(200)을 유일한
  정리 수단으로 뒀다. 안 주운 것이 필드에 남아 있는 게 이 게임의 의도다.
- `_dropUntilMinutes`(티켓 드랍 종료)는 **값만 받아 두고 아무도 안 읽는다.** 스테이지 경과
  시간을 알려주는 것이 없어서, 회차 시스템이 생길 때까지 미룬다.

## 픽업을 본편 씬으로 이관 (브랜치 `feat/pause-and-optimization`)

픽업 배선이 검증용 씬(`Stage01_Pickup`)에만 있어서 본편에서는 아무것도 안 떨어졌다.

- `PlayerInventory`·`PickupCollector`를 씬 오버라이드가 아니라 **Player 프리팹에** 넣었다.
  새 스테이지 씬마다 다시 붙이지 않으려는 것. 씬에는 `Pickups` 오브젝트 하나만 두면 된다.
- 검증용 씬은 역할이 겹쳐 삭제했다.

## 죽은 추상 정리 (브랜치 `build_refactor`)

구현체가 하나뿐인 인터페이스를 걷어냈다. 두 번째 구현이 생기면 그때 다시 만든다.

- `IExpReceiver` → `PlayerExp` 직접 참조, `IPickupReceiver` → `PickupCollector` 직접 참조.
  둘 다 풀이 이미 구체 타입을 들고 있어 인터페이스가 한 곳에서만 살아 있었다.
- `IWeapon.IsReady` 제거 — `WeaponBase`가 항상 `true`를 돌려주고 override가 없었다.
  재장전·차지 무기가 생기면 되살린다.
- 이관이 끝난 `[FormerlySerializedAs]` 두 개 제거, `AimHelper`의 외부 미사용 public 둘을 private로.

**남겨둔 것**: `IFireGate`/`DelegateFireGate`(게이트가 `() => IsAlive` 하나뿐이라 후보였으나,
컷신·상점용 확장 지점으로 유지), `IWeapon`(사용처 21곳이라 교체 비용이 이득보다 컸다).

## 폴더명 오타 수정 (`243edc1`)

`Assets/_Proejct` → `Assets/_Project`. `git mv`로 처리해 `.meta`가 따라갔고 씬 참조는
하나도 안 끊겼다. **"고치면 메타가 흔들린다"던 기존 판단이 틀렸다** — 규칙에서 지웠다.

## 빌드 떨림·테어링 (브랜치 `build_refactor`, PR #12)

빌드로 실행해 한쪽으로 계속 이동하면 플레이어가 떨리고 화면이 갈라졌다. **에디터에서는
재현되지 않는 종류**라 원인 특정에 시간이 걸린다. 셋을 고쳤다.

- `Rigidbody2D.interpolation`을 Player·Enemy·Projectile 셋 다 `Interpolate`로
- `CinemachineBrain.UpdateMethod`를 `SmartUpdate` → `LateUpdate`로
- PC 품질 레벨의 `vSyncCount`를 0 → 1로

**앞의 둘은 세트로 가야 한다** — 보간만 켜면 카메라가 대신 떤다. 이유와 수치는
`architecture.md`의 "빌드에서만 드러나는 설정"에 표로 있다.

덤으로 `EditorBuildSettings`가 폴더 리네임 뒤에도 옛 경로를 들고 있던 것을 바로잡았다.

## 다음 작업

- 소비처 없는 5축(치명타 확률·배수, 연주 밀도, 쿨다운 감소, 반사) 중 성장 쪽에서 쓸 것 연결
- 채보 기반 타이밍. `EveryBeatTiming`·`BurstTiming` 둘 다 그때 교체된다
- 티켓을 쓸 곳이 없다. 상점이 생겨야 한다
- 리롤을 얻기만 하고 쓰는 경로가 없다. 레벨업 카드와 상점 양쪽에 붙어야 한다

## 해결한 문제

**인스펙터의 `[ShowInInspector]` 값이 플레이 중 갱신되지 않음** — 디버그 버튼을 눌러 무기를
레벨업시켜도 인스펙터 표시가 그대로였다. 로직 버그로 오해하기 쉽다.

- **로직은 정상이다.** 플레이 중 같은 메서드를 리플렉션으로 호출해 확인하면 값이 바뀌어 있다.
  `Lv 1 → 2`로 오르고 계산 프로퍼티도 즉시 새 값을 반환한다.
- **원인**: Alchemy는 `[ShowInInspector]`가 붙은 **계산 프로퍼티를 UI 트리를 만들 때 한 번만 읽는다.**
  `Alchemy.Editor` 어셈블리에 Update/Track/Refresh 계열 메서드가 하나도 없다(리플렉션으로 확인).
  직렬화 필드가 아니라 UI Toolkit 바인딩도 걸리지 않는다.
- **효과 없는 시도**: `EditorApplication.update`에서 `Editor.Repaint()`를 매 프레임 걸어도 안 된다.
  리페인트는 다시 그릴 뿐 getter를 다시 호출하지 않는다.
  `Alchemy.Editor.MonoBehaviourEditor`를 상속해 `RequiresConstantRepaint`를 켜는 것도 막혀 있다 —
  internal이라 접근 불가(CS0122).
- **회피**: 값을 바꾸는 디버그 버튼이 결과를 `Debug.Log`로 뱉는다. 콘솔은 항상 갱신된다.
  인스펙터에서 다른 오브젝트를 선택했다 돌아와도 값은 맞게 나온다(UI 트리가 다시 만들어지므로).
- **판별법**: 인스펙터 표시가 의심되면 먼저 플레이 모드에서 값을 직접 읽어라.
  표시가 안 바뀐다고 로직을 고치기 시작하면 멀쩡한 코드를 뜯게 된다.

**클록이 돌지 않아 발사가 아예 안 됨** — `BpmClock`이 `Idle`로 시작하는데 아무도 `Play()`를 호출하지
않았다. `playOnStart`(기본 켬)를 추가했다. `Awake`가 아니라 `Start`에서 앵커를 잡는다.

**투사체가 자기 주인을 때리고 사라짐** — 발사구가 플레이어 콜라이더(트리거) 안이라, `PlayerController`에
`IDamageable`이 붙은 뒤로 스폰 즉시 자해하고 회수됐다. 화면상 발사가 안 되는 것처럼 보이고 5초 뒤
플레이어가 죽어 생존 게이트가 잠겼다. 레이어 설정에 기대지 않도록 코드로 막았다.

- `Projectile.Launch`는 발사 주체를 받아 그 계층에 속한 대상을 통과시킨다.
- 조준도 `IDamageable`인 대상만 인정한다(아군 투사체를 겨누지 않게). 지금은 편(`Faction`)으로 거른다.

**조용한 실패** — 위 두 건 모두 로그 없이 아무 일도 안 일어나 원인 추적이 오래 걸렸다. `WeaponHandler`가
시작 시 클록 미연결·무기 미지급을 로그로 알리고, 인스펙터에 클록 상태와 마지막 서브비트 인덱스를 띄운다.

## 미해결

- **경험치 곡선이 기획과 갈라졌다.** 지금 코드는 `5 + 3 × (레벨-1)` 선형 곡선에 적 1마리 = 경험치 1로
  40회 목표를 구현하고 있다. 기획(`progression.md`)은 음표 4종 확률 지급 + 레벨 구간별 필요치
  테이블 + 45회 목표 + 상자(엘리트 드랍 강화) 시스템으로 대체됐다. 구현은 아직 옛 값 그대로다.
- **무기별 비트 패턴(기획서 필요)** — 확정되면 `IFireTiming` 구현체와 `WeaponLevelData` 필드를 채운다.
  `WeaponDefinition`의 `_intervalBeats`/`_offsetBeats`/`_burstCount`/`_burstSubStep`은 그때 제거될 임시 필드.
- **발사 형태는 초안이다.** 10종의 그림이 악기 소리와 맞는지는 곡이 나와야 정해진다(`weapons.md`).
- 10종이 전부 같은 프리팹을 쓴다(탄 하나, 영역 하나). 화면에서 무기끼리 구분되는 건 크기·수·유도뿐이다.
- 카드 UI는 uGUI 그레이박스다. 연출·아이콘 없음. 등장 방식을 곡의 마디와 맞출지는 미정(`progression.md`).
- **카드가 의도치 않게 선택될 수 있다.** 카드 버튼의 `Navigation.Mode`를 `None`으로 두면 막힌다.
  전말은 `architecture.md`의 "카드는 아무것도 멈추지 않는다"에.
- **오브 흡수의 박자 동기화가 아직 없다.** `progression.md`는 오브가 다음 정박에 도착하도록
  속도를 맞추라고 하는데, 지금은 그냥 가속해서 끌려온다.
- **티켓·리롤에 쓰는 곳이 없다.** 벌어서 들고 있는 데까지만 됐다. 티켓은 상점이, 리롤은
  레벨업 카드와 상점이 생겨야 소비된다(`pickup.md`).
- **픽업 드랍표의 `_dropUntilMinutes`가 동작하지 않는다.** 스테이지 경과 시간을 알려주는 것이
  없어서 값만 받아 둔다. 회차 시스템이 생길 때 연결한다.
- `Camera.main`을 `WeaponHandler.Awake`에서 한 번만 잡는다. 런타임에 카메라를 바꾸면 참조가 낡는다.
- `Prototype_PlayerMovement.unity`는 `BpmClock`·시작 무기가 비어 있어 발사되지 않는다.
  플레이어를 프리팹으로 올렸으니(→ `architecture.md`), 이 씬도 프리팹 인스턴스로 교체하면 해소된다.
- **스테이지 종료 조건이 임시**(`BeatTimelineEndSource`). 기획이 정해지면 실제 곡을 소유하는
  오케스트레이터로 교체한다. `_barsToClear`/`_beatsPerBar`는 그때 사라질 값.
- 클록이 dspTime 기준이라 프레임이 멈춰도(창 비활성·긴 히치) 박은 계속 간다. 복귀 순간 진행도가
  한 번에 뛴다. `runInBackground`가 꺼져 있으면 빌드에서 알트탭 시 드러난다.
- 카운트다운·일시정지 UI 없음.
- 스테이지 밸런스 미조정. 스폰 간격 0.8초 / 접촉 데미지 10 / 체력 100 / 클리어 64마디(120BPM에서 약 2분).
  이동으로 카이팅하는 걸 전제한 값이다.
