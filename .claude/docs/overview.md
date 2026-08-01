# Project Overview

> **관련 문서** — `CLAUDE.md`의 인덱스에는 아직 없지만 `.claude/docs/`에 함께 있다.
> - [architecture.md](architecture.md) — 시스템이 왜 그렇게 조립됐는지. 설계를 건드리기 전에 읽는다.
> - [session-guide.md](session-guide.md) — 새 세션 시작 안내. 컴파일 검증 방법과 Unity 특유의 함정.
> - [progress.md](progress.md) — 진행 중인 작업과 미해결 항목.


Unity project built on the URP Empty Template. `Assets/TutorialInfo/` is stock template onboarding content (a `Readme` asset shown in the editor) — not project code, can be deleted once no longer needed.

- **Unity Editor version:** `6000.3.11f1` (Unity 6) — pinned in `ProjectSettings/ProjectVersion.txt`. Use this exact editor version when opening the project or invoking the Unity CLI, since Unity will attempt an in-place project upgrade if a different version opens it.
- **Render pipeline:** Universal Render Pipeline (URP) 17.3.0. Pipeline assets live in `Assets/Settings/` — `PC_RPAsset.asset`/`PC_Renderer.asset` and `Mobile_RPAsset.asset`/`Mobile_Renderer.asset` are separate quality tiers, each with its own Renderer asset. `UniversalRenderPipelineGlobalSettings.asset` holds the project-wide URP config.
- **Input:** New Input System package. `Assets/InputSystem_Actions.inputactions` defines the input action map/bindings; regenerate the C# wrapper class from this asset (via its importer) rather than hand-editing generated code.
- **Project code root:** `Assets/_Proejct/` — `Scenes/` currently holds `SampleScene.unity`; `Scripts/` exists but is empty so far.
- **Notable packages** (full list in `Packages/manifest.json`): Alchemy (`com.annulusgames.alchemy`, git dependency), AI Navigation, Timeline, Visual Scripting, Multiplayer Center, Test Framework (`com.unity.test-framework` — no test assemblies/files exist yet; requires an `.asmdef` with a Test Assembly reference before Test Runner will discover tests).

## Working in this repo

- Git repo is initialized, remote is `https://github.com/OruteZ/5wg.git`.
- No build/lint/test scripts, package.json, or Makefile — all builds and tests are driven through the Unity Editor (or `Unity.exe -batchmode` / Unity Test Runner from the command line).
- `Assembly-CSharp.csproj` / `Assembly-CSharp-Editor.csproj`, other `*.csproj` files, and the `.sln` are Unity/Rider-generated IDE projects — regenerated automatically, should not be hand-maintained.
