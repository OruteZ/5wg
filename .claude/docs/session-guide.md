# Session Guide

새 세션에서 이 저장소를 만졌을 때 빠르게 궤도에 오르기 위한 안내.
규칙(`.claude/rules/`)이 "반드시 지켜야 할 것"이라면, 이 문서는 "알고 있으면 시간을 버는 것"이다.

---

## 읽는 순서

1. `CLAUDE.md` — 라우팅만 있다. **수정 금지.**
2. `.claude/rules/git.md` — 커밋 태그 규칙. 반드시 따른다.
3. `.claude/docs/overview.md` — 엔진 버전, 패키지, 폴더 구조.
4. `.claude/docs/architecture.md` — 시스템이 왜 그렇게 조립됐는지. **설계를 건드리기 전에 필수.**
5. `.claude/docs/progress.md` — 지금 무슨 작업 중이고 뭐가 미해결인지.

작업을 마치면 `progress.md`를 갱신한다. 설계 판단을 바꿨다면 `architecture.md`도 갱신한다.

---

## 컴파일 검증

**Unity를 열지 않고 C# 컴파일을 확인할 수 있다.** 이걸 모르면 코드를 쓰고도 맞는지 확인할 방법이 없다.

```bash
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "Assembly-CSharp.csproj" /t:Build /nologo /verbosity:minimal /clp:NoSummary
```

에러만 보려면 출력을 `": error"` / `": warning CS"`로 거른다. MSB3277(어셈블리 버전 충돌)과
Alchemy 패키지의 CS0618은 기존부터 있던 잡음이라 무시해도 된다.

### 새 파일을 만들었는데 "이름이 현재 컨텍스트에 없습니다"가 나올 때

`Assembly-CSharp.csproj`는 **Unity가 생성한다.** Unity 에디터가 꺼져 있으면 새 `.cs` 파일이
csproj에 등록되지 않아 빌드에서 빠진다. 임시로 `<Compile Include="..." />`를 직접 추가해 검증하되,
**Unity가 나중에 csproj를 다시 만들면 중복 등록(CS2002 경고)이 생기므로 그때 지운다.**
csproj는 `.gitignore`에 있으니 커밋 걱정은 없다.

### 런타임은 검증할 수 없다

빌드가 통과해도 실제로 동작하는지는 모른다. 플레이 모드 확인이 필요한 변경은
**"빌드는 통과했지만 런타임 확인은 못 했다"고 명시**할 것. 이번 프로젝트에서 발사가 안 되던 두 건 모두
컴파일은 멀쩡했다.

---

## Unity 특유의 함정

### `.meta` 파일은 GUID다

씬과 프리팹은 파일 경로가 아니라 `.meta`의 GUID로 스크립트를 참조한다.

- 파일 이름·위치를 바꿀 때는 **반드시 `git mv`**. `.meta`가 따라가지 않으면 씬의 모든 연결이 끊긴다.
- 새 `.cs`를 만들면 **Unity가 열려 있어야** `.meta`가 생성된다. Unity가 꺼진 상태에서 만든
  스크립트는 `.meta`가 없어 씬에 배치할 수 없다.
- 커밋할 때 `.cs`와 `.meta`를 **같이** 넣는다.

### 씬 YAML은 직접 고치지 않는다

`.unity` 파일은 읽어서 **진단하는 용도로만** 쓴다 (어떤 컴포넌트가 붙어 있는지, 필드가 비었는지).
직접 편집하면 fileID 정합성이 깨지기 쉽다. 배선이 필요하면 사용자에게 에디터 작업으로 요청한다.

씬에서 자주 확인하게 되는 것:
```
grep -n "m_Script:\|m_Name:" Assets/_Proejct/Scenes/Prototype.unity
```
그리고 GUID가 실제 파일과 맞는지: `grep guid <파일>.cs.meta`

### 인스펙터에 남은 낡은 필드

`m_EditorClassIdentifier`에 옛 클래스명(`FiveWG.Prototype.PlayerShooter2D` 같은)이 남아 있는 건
정상이다. Unity가 다음 저장 때 정리한다.

---

## 코드 컨벤션

읽어서 맞추는 게 원칙이지만, 놓치기 쉬운 것들:

- **네임스페이스 없음.** 게임플레이 코드는 전역 네임스페이스다 (커밋 `3786fea`에서 의도적으로 제거됨).
  예외는 `BeatTemplate` — 재사용 가능한 박자 템플릿이라 분리되어 있다.
- **주석은 한국어.** 설명체가 아니라 **판단 근거**를 적는다. "무엇을 하는지"가 아니라 "왜 이렇게 했는지".
- **Alchemy 인스펙터 속성**(`[Title]`, `[LabelText]`, `[Required]`, `[Button]`, `[ShowInInspector]`)을
  적극적으로 쓴다. 라벨은 한국어에 단위를 붙인다: `[LabelText("이동 속도 (units/sec)")]`.
- **직렬화 필드는 `_camelCase`**, 프로퍼티는 `PascalCase`.
- 사용자와의 대화는 한국어.

---

## 이 프로젝트에서 반복해서 물린 것들

### 조용한 실패

발사가 안 되던 문제 두 건 모두 **로그 한 줄 없이** 아무 일도 일어나지 않았다.
초기화 시 필수 참조가 비어 있거나 조건이 안 맞으면 **반드시 로그를 남긴다.**
`WeaponHandler.WarnIfCannotFire`가 그 예다.

### `Time.timeScale`이 안 먹는 것들

`AudioSettings.dspTime`(`BpmClock`)과 unscaled 시간을 쓰는 코루틴/`Awaitable`은
`timeScale = 0`으로 멈추지 않는다. 시간 기반 시스템을 새로 만들면
**`PauseState.Changed` 구독이 필요한지 먼저 따진다.**

### 정적 상태와 도메인 리로드

Unity의 Enter Play Mode Options에서 도메인 리로드를 끄면 정적 필드가 플레이 세션 사이에 살아남는다.
정적 상태를 만들면 `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`으로
초기화하는 코드를 **같이** 넣는다. `PauseState`, `EnemyRegistry`가 그렇게 되어 있다.

### 스폰 위치와 자기 콜라이더

투사체가 발사 주체의 콜라이더 안에서 생성되면 트리거가 즉시 겹친다.
`Projectile2D`는 owner 계층을 통과시켜 막는다. **새 발사체를 만들면 같은 처리가 필요하다.**

---

## 커밋

`.claude/rules/git.md`의 태그를 쓴다. 추가로:

- **종류별로 나눈다.** 기능/수정/리팩터링/문서를 한 커밋에 섞지 않는다.
- **순서는 시간순이 아니라 의존 방향.** 뒤 커밋이 앞 커밋의 API를 쓰도록 배치하면
  각 커밋 시점에 컴파일이 깨지지 않는다.
- **본문에 "왜"를 적는다.** 무엇을 바꿨는지는 diff에 있다.
- `main`에 직접 커밋하지 않는다. 브랜치를 판다.

`gh` CLI는 **설치되어 있지 않다.** PR은 만들 수 없으니 푸시 후 링크를 안내한다:
`https://github.com/OruteZ/5wg/pull/new/<branch>`

---

## 건드리지 않을 것

- `CLAUDE.md` — 라우팅 인덱스. 수정 금지.
- `Assets/TutorialInfo/` — URP 템플릿 잔재.
- `*.csproj`, `*.sln` — Unity 생성물.
- `Library/`, `Temp/` — 빌드 산출물.
- `.claude/skills/` — 사용자 소유.
