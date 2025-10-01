# Unity 클라이언트 코드 컨벤션 적용 보고서

## 작업 개요
Unity 클라이언트 프로젝트(`Game.Unity`)에 **Unity 공식 C# 코딩 컨벤션**을 적용했습니다.
일반 C# 프로젝트(Game.Server, DummyClient)와는 달리 Unity 특화 스타일을 적용했습니다.

**참고**: [Unity 공식 C# 스타일 가이드](https://unity.com/how-to/naming-and-code-style-tips-c-scripting-unity)

## 적용된 변경 사항

### 1. EditorConfig 파일
- **위치**: `Game.Unity/.editorconfig`
- **내용**: Unity 공식 가이드라인 기반의 C# 코딩 컨벤션
- **주요 규칙**:
  - 명명 규칙: PascalCase for types/methods
  - **SerializeField**: `camelCase` (언더스코어 없음) - Unity 권장
  - **Private 필드**: `_camelCase` (언더스코어 있음) - 프로젝트 표준
  - 포매팅: 모든 제어문에 중괄호 필수
  - 접근 제한자: 명시 권장 (suggestion 수준)

### 2. C# 컴파일러 설정 (csc.rsp)
- **위치**: `Game.Unity/Assets/csc.rsp`
- **내용**:
  ```
  -analyzerconfig:.editorconfig  # EditorConfig 규칙 활성화
  -nullable:enable               # Nullable 참조 타입 활성화
  -langversion:9.0               # C# 9.0 언어 기능
  -analysislevel:latest          # 최신 분석 수준
  -nowarn:IDE0044                # Unity SerializeField를 위해 readonly 경고 제외
  ```

### 3. 코드 수정

#### NetworkManager.cs
- ✅ SerializeField 변수명을 Unity 스타일로 변경:
  - `_host` → `host`, `_port` → `port`
  - `_reconnectDelaySeconds` → `reconnectDelaySeconds`
  - `_playerPrefab` → `playerPrefab`
- ✅ 모든 private 필드에 명시적 `private` 키워드 추가
- ✅ 모든 Unity 라이프사이클 메서드에 `private` 추가 (Awake, OnDestroy, OnApplicationQuit)
- ✅ 모든 helper 메서드에 `private` 추가 (ApplyJoinResponse, SpawnOrUpdate)
- ✅ 단일행 if/while 문에 중괄호 추가 (12개 위치 수정)
- ✅ 모든 변수 참조 업데이트 (6개 위치)

#### MyPlayer.cs
- ✅ SerializeField 변수명을 Unity 스타일로 변경:
  - `_moveSpeed` → `moveSpeed`
  - `_packetInterval` → `packetInterval`
- ✅ 모든 private 필드에 명시적 `private` 키워드 추가
- ✅ 모든 Unity 라이프사이클 메서드에 `private` 추가 (Awake, OnEnable, OnDisable, Update)
- ✅ 모든 helper 메서드에 `private` 추가 (EnsureNetworkManager, HandleMovementInput, MovementLoopAsync, PickRandomTarget)
- ✅ 단일행 if 문에 중괄호 추가 (4개 위치 수정)
- ✅ 모든 변수 참조 업데이트 (3개 위치)

#### Player.cs
- ✅ ApplyInfo 메서드의 null 체크에 중괄호 추가

#### InitialSettings.cs
- ✅ 이미 컨벤션 준수 (수정 불필요)

### 4. Unity 특수 케이스 처리

#### SerializeField 명명 규칙
- **Unity 권장**: 언더스코어 없는 `camelCase` (예: `moveSpeed`, `playerPrefab`)
- **일반 private 필드**: 언더스코어 있는 `_camelCase` (예: `_networkManager`, `_isConnected`)
- 이유: Unity Inspector에서 자연스러운 표시, Unity 공식 예제와 일관성

#### EditorConfig 설정
- `dotnet_style_require_accessibility_modifiers`: `suggestion` (Unity는 생략 가능)
- `dotnet_style_readonly_field`: `suggestion` (SerializeField는 readonly 불가)
- Private 필드 명명 규칙: `suggestion` (언더스코어 선택적)

#### csc.rsp 설정
- IDE0044 (readonly 제안) 경고 제외 → SerializeField를 위해 필요
- `-warnaserror` 미사용 → Unity 엔진/패키지 경고 때문

## 검증

### VSCode에서의 검증
- ✅ Roslyn 분석기가 .editorconfig 규칙을 감지
- ✅ 코드 편집 시 실시간으로 스타일 제안 표시 (warning이 아닌 suggestion)
- ✅ Format Document 시 자동으로 컨벤션 적용
- ✅ SerializeField 변수에 경고 없음

### Unity 에디터에서의 검증
- Unity 에디터는 `csc.rsp` 파일을 자동으로 인식하여 컴파일 시 적용
- Unity 프로젝트를 다시 열면 설정이 활성화됨

## 현재 상태

### 컨벤션 적용 현황
- ✅ **Game.Server** - Microsoft C# 컨벤션 (빌드 시 강제)
- ✅ **DummyClient** - Microsoft C# 컨벤션 (빌드 시 강제)
- ✅ **Game.Shared** - Microsoft C# 컨벤션 (빌드 시 강제)
- ✅ **Game.Unity** - **Unity 공식 컨벤션** (경고 수준)

### Unity vs 일반 C# 차이점

| 항목 | Game.Server/DummyClient | Game.Unity |
|------|-------------------------|------------|
| Private 필드 | `_camelCase` 필수 | `_camelCase` 권장 |
| SerializeField | 해당없음 | `camelCase` (언더스코어 없음) |
| 접근 제한자 | 명시 필수 (warning) | 명시 권장 (suggestion) |
| Readonly | 필수 (warning) | 선택적 (suggestion) |
| 빌드 오류 | 경고를 오류로 처리 | 경고로만 표시 |

### Unity 프로젝트의 특이사항
Unity 프로젝트는 다음 이유로 일반 C# 프로젝트와 다르게 설정합니다:
1. Unity 엔진 자체 코드에서 발생하는 경고를 제어할 수 없음
2. SerializeField와 같은 Unity 특수 기능과 C# 컨벤션 간의 충돌
3. Unity 패키지/플러그인에서 발생할 수 있는 경고
4. Unity Inspector에서 값을 수정할 수 있어야 함 (readonly 불가)

대신 **제안(suggestion)을 통해 컨벤션 위반을 표시**하되, **빌드는 성공**하도록 설정했습니다.

## 개발자 가이드

### VSCode에서 Unity 코드 편집 시
1. C# Dev Kit 확장 설치 필요
2. EditorConfig for VS Code 확장 설치 필요
3. 파일 저장 시 자동 포맷팅 활성화 (`.vscode/settings.json`에 설정됨)
4. SerializeField는 `camelCase`로 작성 (경고 없음)
5. 일반 private 필드는 `_camelCase`로 작성

### Unity 에디터에서 작업 시
1. Unity 에디터가 자동으로 `csc.rsp` 인식
2. 콘솔 창에서 컨벤션 위반 경고 확인 가능
3. 컴파일 에러는 발생하지 않으므로 Play 모드 실행 가능

### 코딩 예시

```csharp
public class ExampleScript : MonoBehaviour
{
    // SerializeField - camelCase (언더스코어 없음)
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private GameObject targetPrefab;

    // 일반 private 필드 - _camelCase (언더스코어 있음)
    private Transform _cachedTransform;
    private bool _isInitialized;

    private void Awake()
    {
        _cachedTransform = transform;
        _isInitialized = true;
    }
}
```

## 다음 단계

### 권장 사항
1. Unity 프로젝트를 다시 열어 `csc.rsp` 설정 적용 확인
2. 기존 코드 리뷰 후 추가적인 컨벤션 위반 수정
3. 팀원들에게 Unity 코딩 컨벤션 가이드 공유 (`UNITY_CODING_CONVENTIONS.md` 참조)

### 참고 문서
- `/UNITY_CODING_CONVENTIONS.md` - **Unity 전용 코딩 컨벤션 가이드** ⭐
- `/CODING_CONVENTIONS.md` - 일반 C# 코딩 컨벤션 가이드 (Server/DummyClient용)
- `/CONVENTION_REPORT.md` - 서버/DummyClient 컨벤션 적용 보고서
- `/.editorconfig` - 프로젝트 루트 EditorConfig 설정
- `/Game.Unity/.editorconfig` - Unity 전용 EditorConfig 설정

### 주요 변경 사항 요약
1. SerializeField는 언더스코어 없는 camelCase 사용 (`moveSpeed`, `playerPrefab`)
2. 일반 private 필드는 언더스코어 있는 _camelCase 사용 (`_networkManager`)
3. EditorConfig 규칙을 suggestion 수준으로 완화 (Unity 특성 반영)
4. 모든 제어문에 중괄호 필수 유지
5. 접근 제한자 명시 권장 (일관성 향상)

## 작업 완료 일시
2025년 10월 1일 (Unity 공식 가이드라인 기반으로 재작업)
