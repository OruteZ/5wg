# Unity C# 규약

모든 런타임 C# 코드에 적용한다. Unity 6 전용이므로 구버전 호환 우회 코드를 쓰지 않는다.

런타임 구조(씬 서비스, 풀링, 조준, 무기 스탯)의 상세는 `.claude/docs/architecture.md`에 있다.
여기에는 코드를 쓸 때 지킬 것만 둔다.

## 1. 차원 — 게임플레이는 2D

렌더 파이프라인은 URP 3D 템플릿에서 시작했지만 **게임플레이는 전부 2D**다.
3D API는 스타일 문제가 아니라 버그다. 호출 자체가 오지 않아 발견이 늦다.

- 물리: `Rigidbody2D` `Collider2D` `Physics2D.Raycast` / `OverlapCircle` / `ContactFilter2D`.
- 콜백: `OnTriggerEnter2D` `OnCollisionEnter2D` `OnTriggerStay2D`.
  "트리거가 안 먹는다" 싶으면 여기부터 본다.
- 위치·방향·속도는 `Vector2`. `Transform`과 만나는 경계에서만 `Vector3`으로 바꾼다.
  속도는 `Rigidbody2D.linearVelocity` (구식 `velocity` 아님).
- 회전은 Z축 각도 `float` 하나. 조준은 `Mathf.Atan2`, 대입 시점에만 `Quaternion.Euler(0, 0, angle)`.
- 그리는 순서는 `SpriteRenderer`의 정렬 레이어와 `sortingOrder`. 깊이 정렬은 해당 없다.
- 충돌 규칙은 Project Settings의 Physics 2D에서 정한다. `Update`에서 수동으로 걸러내지 않는다.
- 한 스크립트에 2D와 3D가 섞여 있으면 3D 샘플을 붙여넣은 흔적이다. 지적한다.

## 2. 입력 — New Input System만

- `UnityEngine.InputSystem`을 쓴다. 레거시 `Input.GetAxis/GetKey/mousePosition`은 쓰지 않는다.
- `Assets/InputSystem_Actions.inputactions`의 액션 맵을 쓴다. 생성된 C# 래퍼는 손으로 고치지 않는다.
- Input Actions는 Disposable이다. `OnEnable`에서 켜고 `OnDisable`에서 끄고 버린다.
- 조준의 스크린→월드는 직교 카메라에 `Camera.ScreenToWorldPoint` 후 `z`를 버린다.

## 3. 시간

**박에 맞춰야 하는 것은 `AudioSettings.dspTime` 기준이다.** `Time.time` `Time.deltaTime`
프레임 카운트를 쓰지 않는다. 박은 프레임과 무관하게 가므로, 긴 히치 뒤에는 진행도가 뛴다.

**`Time.timeScale`은 박에 영향을 주지 않는다.** timeScale로 멈춰도 박은 계속 가고 화면과 어긋난다.
전투를 멈춰야 하면 축별 스위치를 쓴다
(`WeaponHandler.AttackEnabled` `PlayerController.ControlEnabled` `EnemySpawner2D.SetSpawning`).

박과 무관한 대기는 코루틴이 아니라 `Awaitable`을 쓴다
(`await Awaitable.WaitForSecondsAsync(t)` `NextFrameAsync()` `FixedUpdateAsync()`).
취소는 `CancellationToken`으로 하고, `CancellationTokenSource`는 `OnDisable`/`OnDestroy`에서
취소하고 Dispose한다.

게임 로직에 스레드나 락을 쓰지 않는다. 필요해 보이면 먼저 물어라.

## 4. 네이밍

- 클래스·메서드·프로퍼티·이벤트 `PascalCase`. private 필드 `_camelCase`. 지역·매개변수 `camelCase`.
- `[SerializeField] private Type _fieldName;`. **public 필드는 쓰지 않고** 프로퍼티로 노출한다.
- 파일 하나에 public 클래스 하나. 파일명 == 클래스명.
- 네임스페이스는 폴더명과 같은 `FiveWG.*`. `Beat/`만 `BeatTemplate`을 유지한다.
- 차원에 묶인 클래스만 `2D` 접미사(`Projectile2D` `Enemy2D`).
  풀·상태 머신·데이터 컨테이너는 붙이지 않는다.
- **`Assets/_Proejct/`는 오타지만 고치지 않는다.** 폴더를 옮기면 메타 파일 경로가 전부 흔들린다.

## 5. 메모리와 수명

- **이벤트가 1순위 누수원이다.** 모든 `+=`에 대응하는 `-=`가 있어야 한다.
  `OnEnable` 구독 → `OnDisable` 해제, 또는 `Awake`/`Start` → `OnDestroy`.
  static 이벤트에 살아 있는 구독은 구독자를 영원히 붙잡는다.
- `IDisposable`은 전부 Dispose한다. `CancellationTokenSource`를 버리지 않고 두지 않는다.
- **자주 스폰되는 것은 `Core/PrefabPool<T>`로 풀링한다.** 적·투사체·픽업은 수백 개가
  동시에 존재한다. 새 풀을 만들면 스테이지 종료 시 회수 경로를 같이 만든다.
- **핫패스에서 할당하지 않는다.** `Update`/`FixedUpdate`/발사 경로에 LINQ, `new` 컬렉션,
  문자열 결합을 넣지 않는다. `Physics2D` 쿼리는 `NonAlloc`이나 재사용 `List<T>` 오버로드를 쓴다.
- 스크립트에 구독·할당·스폰이 있으면 해제 지점을 같이 보인다.
  해제할 게 없으면 괜찮지만, 빠뜨린 게 아니라 의도한 것이어야 한다.

## 6. 구조

- 컴포넌트 참조는 `Awake()`에서 캐시한다. `Update()`에서 `GetComponent<T>()`를 호출하지 않는다.
- `Update()`는 가볍게, 물리는 `FixedUpdate()`. `Rigidbody2D`는 `linearVelocity`나 `MovePosition`으로
  움직이고 `transform.position`을 직접 쓰지 않는다.
- **씬에 하나뿐인 것은 `SceneServices`로 찾는다.** 컴포넌트마다 `FindFirstObjectByType` 폴백을
  각자 두지 않는다.
- **피격 대상을 새로 만들면 `IDamageable` 구현과 `TargetRegistry` 등록을 잊지 않는다.**
  등록·해제는 `OnEnable`/`OnDisable`. 아군 배제는 계층이 아니라 `Faction`으로 한다.
- 밸런스 수치는 `ScriptableObject`로 뺀다. 드랍 확률, 획득량, 스탯 표를 코드에 박지 않는다.
- **수치가 문서에 없으면 지어내지 말고 물어라.** 기획 문서의 "미정"은 생략이 아니라 미결정이다.

## 7. 인스펙터

이 프로젝트는 **Alchemy**(`com.annulusgames.alchemy`)를 쓴다. 전 코드가 이미 쓰고 있다.

- `[BoxGroup]` `[FoldoutGroup]` `[Button]` `[ShowInInspector]`를 UX가 실제로 나아지는 곳에만 쓴다.
- **Alchemy의 `[Button]`은 라벨 인자를 받지 않는다.** 라벨이 필요하면 `[LabelText]`를 따로 단다.
- 런타임 내부 상태가 밖에서 안 보이면 `[Button]`이나 `[ShowInInspector]` 읽기 전용 프로퍼티로
  노출한다(풀 점유율, 현재 박 인덱스, 활성 적 수). 핫패스에 `Debug.Log`를 뿌리지 않는다.

## 8. 주석

- public 클래스와 자명하지 않은 public 메서드에 XML `/// <summary>`.
- **주석은 자명하지 않은 의도만 설명한다.** 코드가 이미 말하는 것을 다시 쓰지 않는다.
  자기 정당화 주석, 구획 배너를 쓰지 않는다. 설명이 필요 없는 줄에는 주석을 달지 않는다.
