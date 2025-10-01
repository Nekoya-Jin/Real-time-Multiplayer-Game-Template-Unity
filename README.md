# Real-time Multiplayer Game Template - Unity & C# Server

MagicOnion을 사용한 Unity 클라이언트와 C# 서버 프로젝트입니다.

## 📁 프로젝트 구조

```
UnityClient_C#Server/
├── Game.sln                    # 루트 솔루션 파일
├── Game.Server/               # ASP.NET Core gRPC 서버
├── Game.Shared/               # 클라이언트-서버 간 공유 라이브러리 (Unity Local Package)
├── Game.Unity/                # Unity 클라이언트 프로젝트
└── DummyClient/              # 테스트용 .NET 클라이언트
```

## 🔧 주요 설정 사항

### 1. Game.Shared 프로젝트 (Unity Local Package)

`Game.Shared` 프로젝트는 서버와 Unity 클라이언트 간에 공유되는 코드를 포함합니다.
Unity에서 로컬 패키지로 사용할 수 있도록 다음 파일들이 설정되어 있습니다:

- **package.json**: Unity 패키지 메타데이터
- **Game.Shared.Unity.asmdef**: Unity Assembly Definition
- **Directory.Build.props**: 빌드 아티팩트를 `.artifacts` 폴더로 출력
- **Directory.Build.targets**: Unity 관련 파일(.meta)과 빌드 아티팩트를 IDE에서 숨김

### 2. Unity 프로젝트 설정

`Game.Unity/Packages/manifest.json`에 로컬 패키지 참조가 추가되어 있습니다:

```json
"com.game.shared.unity": "file:../Game.Shared"
```

이를 통해 Unity 프로젝트가 자동으로 Game.Shared 프로젝트의 코드를 참조합니다.

### 3. 필수 패키지

Unity 프로젝트에는 다음 패키지들이 설치되어 있습니다:
- MagicOnion.Client.Unity
- YetAnotherHttpHandler
- MemoryPack
- UniTask
- NuGetForUnity

## 🚀 시작하기

### 서버 실행

```bash
cd Game.Server
dotnet run
```

### Unity 클라이언트 실행

1. Unity Hub에서 `Game.Unity` 프로젝트를 엽니다
2. Play 버튼을 눌러 실행합니다

### 전체 빌드

```bash
dotnet build Game.sln
```

## � 코딩 컨벤션

이 프로젝트는 Microsoft C# 공식 코딩 컨벤션을 따릅니다.
자세한 내용은 [CODING_CONVENTIONS.md](./CODING_CONVENTIONS.md)를 참조하세요.

- `.editorconfig` 파일을 통해 자동으로 코드 스타일이 적용됩니다
- 빌드 시 코드 스타일을 검사하여 컨벤션 위반 시 빌드 에러가 발생합니다
- VSCode에서 C# Dev Kit 확장을 설치하면 실시간으로 컨벤션 위반을 확인할 수 있습니다

## �📚 참고 문서

- [MagicOnion Quickstart with Unity](https://cysharp.github.io/MagicOnion/quickstart-unity)
- [MagicOnion Project Structure](https://cysharp.github.io/MagicOnion/fundamentals/project-structure)
- [C# 코딩 컨벤션 가이드](./CODING_CONVENTIONS.md)

## ⚠️ 주의사항

- `Game.Shared` 프로젝트의 `bin`과 `obj` 폴더는 삭제되었으며, 빌드 출력은 `.artifacts` 폴더로 이동합니다
- Unity에서 C# 파일을 열 때 자동으로 Game.sln이 열립니다 (SlnMerge 설정 시)
- macOS에서는 `.artifacts` 폴더가 기본적으로 숨겨질 수 있습니다 (Cmd+Shift+. 로 표시)
