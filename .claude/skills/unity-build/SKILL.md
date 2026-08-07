---
name: unity-build
description: Verify C# compiles without opening the Unity Editor (MSBuild on Assembly-CSharp.csproj). Use after any script edit, before reporting code work as done, or when diagnosing compile errors. 컴파일 확인, 빌드 검증.
---

# unity-build

Unity 에디터를 열지 않고 컴파일을 확인한다. **스크립트를 고쳤으면 보고 전에 반드시 돌린다.**

## 실행

```powershell
$msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
& $msbuild "Assembly-CSharp.csproj" /t:Build /nologo /verbosity:minimal /clp:NoSummary 2>&1 | Select-String -Pattern ": error|: warning CS|Assembly-CSharp ->" | Select-Object -First 20
```

`Assembly-CSharp -> ...dll`만 나오면 성공.

## 무시해도 되는 잡음

- `MSB3277` — 어셈블리 버전 충돌. 패키지들 사이의 기존 문제다.
- Alchemy 패키지의 `CS0618` — 사용되지 않는 API 경고.

## "이름이 현재 컨텍스트에 없습니다" (CS0103)

`Assembly-CSharp.csproj`는 **Unity가 생성한다.** 에디터가 꺼져 있으면 새로 만든 `.cs`가
등록되지 않아 빌드에서 빠진다. 임시로 직접 넣어 검증한다.

```xml
<Compile Include="Assets\_Proejct\Scripts\Runtime\경로\새파일.cs" />
```

**Unity가 다시 csproj를 만들면 중복 등록으로 `CS2002` 경고가 뜬다. 그때 직접 넣은 줄을 지운다.**
csproj는 `.gitignore`에 있으니 커밋 걱정은 없다.

## "소스 파일을 찾을 수 없습니다" (CS2001)

브랜치를 옮긴 직후에 잘 난다. csproj는 `.gitignore`에 있어 체크아웃으로 바뀌지 않으므로,
**다른 브랜치에만 있는 파일을 계속 참조한다.** 코드 문제가 아니다.

없다고 나온 파일이 현재 브랜치에 실제로 없는지 확인한다. 그렇다면 Unity에 프로젝트를 다시
읽히거나(에디터 포커스), 그 브랜치에서 작업할 때 재생성되길 기다린다.

## 빌드 통과 ≠ 동작

플레이 모드 확인이 필요한 변경은 **"빌드는 통과했지만 런타임 확인은 못 했다"고 명시**한다.
이 프로젝트에서 발사가 안 되던 두 건 모두 컴파일은 멀쩡했다.
