# Project Overview

Unity project built on the URP Empty Template. `Assets/TutorialInfo/` is stock template onboarding content (a `Readme` asset shown in the editor) — not project code, can be deleted once no longer needed.

- **Unity Editor version:** `6000.3.11f1` (Unity 6) — pinned in `ProjectSettings/ProjectVersion.txt`. Use this exact editor version when opening the project or invoking the Unity CLI, since Unity will attempt an in-place project upgrade if a different version opens it.
- **Render pipeline:** Universal Render Pipeline (URP) 17.3.0. Pipeline assets live in `Assets/Settings/` — `PC_RPAsset.asset`/`PC_Renderer.asset` and `Mobile_RPAsset.asset`/`Mobile_Renderer.asset` are separate quality tiers, each with its own Renderer asset. `UniversalRenderPipelineGlobalSettings.asset` holds the project-wide URP config.
- **Input:** New Input System package. `Assets/InputSystem_Actions.inputactions` defines the input action map/bindings; regenerate the C# wrapper class from this asset (via its importer) rather than hand-editing generated code.
- **Notable packages** (full list in `Packages/manifest.json`): Alchemy (`com.annulusgames.alchemy`, git dependency — 인스펙터 어트리뷰트를 전 코드가 쓴다), Cinemachine 3.1.7, 2D Feature, AI Navigation, Timeline, Visual Scripting, Multiplayer Center, Test Framework (`com.unity.test-framework` — 테스트 어셈블리 없음. Test Runner가 인식하려면 `.asmdef`부터 필요).

## 프로젝트 코드 (`Assets/_Proejct/`)

스크립트 53개. **asmdef는 없다** — 전부 `Assembly-CSharp`에 들어가므로 스크립트 하나를 고치면
전체가 재컴파일된다(3초 + 도메인 리로드). 대신 네임스페이스로 나눠뒀다.

| 폴더 | 네임스페이스 | 내용 |
| --- | --- | --- |
| `Scripts/Runtime/Core/` | `FiveWG.Core` | `IDamageable` `Faction` `TargetRegistry` `PrefabPool<T>` `PrefabPoolSet<T>` `SceneServices` |
| `Scripts/Runtime/Beat/` | `BeatTemplate` | `BpmClock` `BeatEvent` `BeatMath` `Quantizer` (외부 템플릿 유래라 이름 유지) |
| `Scripts/Runtime/Weapons/` | `FiveWG.Weapons` | 무기 축 전부 + 개체 풀·타겟 탐색 |
| `Scripts/Runtime/Combat/` | `FiveWG.Combat` | 무기가 씬에 뿌리는 개체 — `Projectile` `DamageField` |
| `Scripts/Runtime/Player/` | `FiveWG.Player` | `PlayerController` `PlayerMovement` `WeaponHandler` |
| `Scripts/Runtime/Progression/` | `FiveWG.Progression` | 경험치 오브 + 레벨업 카드 규칙 |
| `Scripts/Runtime/Enemies/` `Stage/` `UI/` | 각 폴더명 | 적·스테이지·HUD·카드 화면 |

| 씬 | 용도 |
| --- | --- |
| `Scenes/MainMenu.unity` | START / QUIT. Build Settings 0번 |
| `Scenes/Stage01.unity` | **작업 씬.** 스테이지 본편. Build Settings 1번 |
| `Scenes/Prototype.unity` | 무기 검증용. 스테이지 UI 없음 |
| `Scenes/Prototype/Prototype_PlayerMovement.unity` | 이동만. 클록·무기 미연결이라 발사 안 됨 |
| `Scenes/SampleScene.unity` | URP 템플릿 잔재. 안 씀 |

프리팹 `Prefabs/`: `Player` `Enemies/Enemy` `Combat/Projectile` `Combat/DamageField` `Progression/ExpOrb`.
무기 데이터 `ScriptableObjects/Weapon/`에 악기 10종뿐(`Kick` `Snare` `HiHat` `Crash` `Tom`
`Bass` `ElectricGuitar` `AcousticGuitar` `Keys` `Synth`).

- **레이어**: `Enemy`(3)와 `Projectile`(8)을 쓴다. 탄↔탄 충돌은 꺼져 있다(적↔적은 켜 둠).
- **빌드**: `Builds/Windows/5wg.exe`로 뽑는다. `.gitignore`에 걸려 커밋되지 않는다.

## Working in this repo

- Git repo is initialized, remote is `https://github.com/OruteZ/5wg.git`.
- package.json도 Makefile도 없다. **컴파일 확인은 `unity-build` 스킬로 한다** — 에디터를 열지 않고
  MSBuild로 `Assembly-CSharp.csproj`를 빌드한다. 스크립트를 고쳤으면 보고 전에 돌린다.
- 테스트 러너는 못 쓴다(테스트 어셈블리가 없다). 대신 이 저장소는 **`[InitializeOnLoadMethod]` 셀프체크**를
  쓴다 — 에디터가 로드될 때마다 돌고 어긋나면 `Debug.LogError`를 뱉는다.
  `WeaponLevelData`(스탯 합연산·TSV 왕복), `BurstTiming`(연타 위치), `LevelUpDraft`(가중 추첨)에 있다.
- **플레이 모드를 MCP로 검증할 때는 `Application.runInBackground = true`를 먼저 켠다.**
  에디터 창이 포커스 밖이면 시간이 아예 안 흘러서(`Time.timeSinceLevelLoad`가 0에 멈춤)
  스폰도 이동도 일어나지 않는다. 코드 버그로 오해하기 쉽다.
- `Assembly-CSharp.csproj` / `Assembly-CSharp-Editor.csproj`, other `*.csproj` files, and the `.sln` are Unity/Rider-generated IDE projects — regenerated automatically, should not be hand-maintained.
  새로 만든 `.cs`는 Unity가 csproj를 다시 만들기 전까지 빌드에서 빠진다(→ `unity-build` 스킬).
