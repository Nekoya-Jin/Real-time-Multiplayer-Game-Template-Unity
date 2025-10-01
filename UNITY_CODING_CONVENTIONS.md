# Unity C# 코딩 컨벤션 가이드

## 개요
이 문서는 Unity 프로젝트(`Game.Unity`)에 적용되는 C# 코딩 컨벤션을 설명합니다.
Unity 공식 가이드라인을 기반으로 하며, 일반 C# 프로젝트와는 일부 차이가 있습니다.

**참고 문서**: [Unity 공식 C# 스타일 가이드](https://unity.com/how-to/naming-and-code-style-tips-c-scripting-unity)

---

## Unity vs 일반 C# 프로젝트 차이점

| 항목 | 일반 C# (Game.Server, DummyClient) | Unity (Game.Unity) |
|------|-------------------------------------|---------------------|
| Private 필드 | `_camelCase` (필수) | `camelCase` 또는 `_camelCase` (선택) |
| SerializeField | 해당없음 | `camelCase` (권장) |
| 접근 제한자 | 명시 필수 (warning) | 명시 권장 (suggestion) |
| Readonly 필드 | 가능한 경우 필수 (warning) | Inspector 수정 위해 선택적 (suggestion) |
| 빌드 오류 처리 | 경고를 오류로 처리 | 경고로만 표시 |

---

## 명명 규칙

### 클래스 및 인터페이스
```csharp
// ✅ 올바른 예시
public class PlayerController { }
public class GameManager { }
public interface IMovable { }          // 인터페이스는 'I' 접두사

// ❌ 잘못된 예시
public class playerController { }     // 첫 글자는 대문자
public class Player_Controller { }    // 언더스코어 사용 안 함
```

### 메서드
```csharp
// ✅ 올바른 예시
public void MovePlayer() { }
private void HandleInput() { }
public bool IsGameOver() { }           // bool 반환은 질문 형태

// ❌ 잘못된 예시
public void move_player() { }          // camelCase 아닌 PascalCase
private void handle_input() { }
```

### 필드 및 변수

#### Public 필드 (사용 최소화)
```csharp
// ✅ 올바른 예시
public int MaxHealth;
public string PlayerName;

// ❌ 잘못된 예시
public int maxHealth;                  // PascalCase 사용
public string player_name;
```

#### SerializeField (Unity Inspector에 노출)
```csharp
// ✅ Unity 권장 스타일
[Header("Movement")]
[SerializeField] private float moveSpeed = 5f;
[SerializeField] private float jumpHeight = 2f;

[Header("References")]
[SerializeField] private GameObject playerPrefab;
[SerializeField] private Transform spawnPoint;

// ⚠️ 가능하지만 권장하지 않음
[SerializeField] private float _moveSpeed = 5f;  // 언더스코어는 선택적
```

**중요**:
- SerializeField는 **언더스코어 없는 camelCase** 사용 권장
- Unity Inspector에서 수정 가능해야 하므로 `readonly` 사용 안 함
- `private` 명시는 선택적이지만 명시하는 것을 권장

#### Private 필드 (일반)
```csharp
// ✅ 올바른 예시 (언더스코어 없음 - Unity 스타일)
private int healthPoints;
private Transform targetTransform;
private List<Enemy> enemies;

// ✅ 올바른 예시 (언더스코어 있음 - Microsoft 스타일)
private int _healthPoints;
private Transform _targetTransform;
private List<Enemy> _enemies;

// 둘 다 가능하지만, 프로젝트 내에서 일관성 유지 필요
// 현재 프로젝트: 언더스코어 사용 (_로 시작)
```

#### Static Private 필드
```csharp
// ✅ 올바른 예시
private static int s_instanceCount;    // s_ 접두사 권장
private static readonly int MaxPlayers = 10;

// ⚠️ 가능하지만 권장하지 않음
private static int instanceCount;      // 접두사 없음
```

#### 로컬 변수 및 파라미터
```csharp
// ✅ 올바른 예시
private void CalculateDamage(int baseValue, float multiplier)
{
    int finalDamage = (int)(baseValue * multiplier);
    float healthPercentage = health / maxHealth;
}

// ❌ 잘못된 예시
private void CalculateDamage(int BaseValue, float Multiplier)  // PascalCase 아님
{
    int FinalDamage = (int)(BaseValue * Multiplier);
}
```

#### Boolean 변수
```csharp
// ✅ 올바른 예시 (질문 형태)
private bool isAlive;
private bool hasWeapon;
private bool canJump;
public bool IsGameOver { get; private set; }

// ❌ 잘못된 예시
private bool alive;                    // 질문 형태 아님
private bool weapon;
```

### 상수
```csharp
// ✅ 올바른 예시
private const int MaxRetries = 3;
private const float GravityScale = 9.81f;
public const string GameVersion = "1.0.0";

// ❌ 잘못된 예시
private const int MAX_RETRIES = 3;     // UPPER_CASE는 C#에서 사용 안 함
private const float gravity_scale = 9.81f;
```

### 열거형 (Enum)
```csharp
// ✅ 올바른 예시
public enum GameState                  // 단수형 명사
{
    MainMenu,
    Playing,
    Paused,
    GameOver
}

[Flags]
public enum DamageTypes                // Flags는 복수형 가능
{
    None = 0,
    Physical = 1,
    Magical = 2,
    Fire = 4
}

// ❌ 잘못된 예시
public enum GameStates { }             // 단수형 사용
public enum gameState { }              // PascalCase 사용
```

---

## Unity 특수 케이스

### MonoBehaviour 라이프사이클 메서드
```csharp
// ✅ 접근 제한자 명시 (권장)
private void Awake() { }
private void Start() { }
private void Update() { }
private void OnDestroy() { }

// ✅ 접근 제한자 생략 (Unity에서 허용)
void Awake() { }
void Start() { }
void Update() { }
void OnDestroy() { }

// 현재 프로젝트: private 명시 사용
```

### Coroutine
```csharp
// ✅ 올바른 예시
private IEnumerator SpawnEnemiesCoroutine()
{
    yield return new WaitForSeconds(1f);
}

private void StartSpawning()
{
    StartCoroutine(SpawnEnemiesCoroutine());
}
```

### 이벤트 및 델리게이트
```csharp
// ✅ 올바른 예시
public event System.Action OnPlayerDied;
public event System.Action<int> OnScoreChanged;

// 이벤트 발생 메서드는 'On' 접두사
private void OnTriggerEnter(Collider other)
{
    OnPlayerDied?.Invoke();
}
```

---

## 포맷팅 규칙

### 중괄호
```csharp
// ✅ 올바른 예시 (항상 중괄호 사용)
if (health <= 0)
{
    Die();
}

// ❌ 잘못된 예시
if (health <= 0)
    Die();                             // 단일문이라도 중괄호 필수
```

### 들여쓰기
```csharp
// ✅ 공백 4칸 사용
public class Example
{
    private void Method()
    {
        if (condition)
        {
            DoSomething();
        }
    }
}
```

### 네임스페이스
```csharp
// ✅ 올바른 예시
namespace MyGame.Player
{
    public class PlayerController : MonoBehaviour
    {
        // ...
    }
}

// 또는 파일 스코프 네임스페이스 (C# 10+)
namespace MyGame.Player;

public class PlayerController : MonoBehaviour
{
    // ...
}
```

---

## EditorConfig 설정

Unity 프로젝트의 `.editorconfig`는 다음과 같이 설정되어 있습니다:

```ini
# Unity 전용 설정
dotnet_style_require_accessibility_modifiers = for_non_interface_members:suggestion
dotnet_style_readonly_field = true:suggestion

# Private 필드 명명 규칙은 suggestion 수준
dotnet_naming_rule.private_or_internal_field_should_be_begins_with_underscore.severity = suggestion
```

---

## csc.rsp 설정

Unity 컴파일러 옵션 (`Game.Unity/Assets/csc.rsp`):

```
-analyzerconfig:.editorconfig    # EditorConfig 규칙 활성화
-nullable:enable                 # Nullable 참조 타입
-langversion:9.0                 # C# 9.0 언어 기능
-analysislevel:latest            # 최신 분석 수준
-nowarn:IDE0044                  # readonly 제안 제외 (SerializeField용)
```

**중요**:
- Unity는 경고를 오류로 처리하지 않음 (`-warnaserror` 미사용)
- Unity 엔진/패키지의 경고를 제어할 수 없기 때문

---

## 실전 예시

### 완전한 Unity MonoBehaviour 예시

```csharp
using UnityEngine;
using System.Collections;

namespace MyGame.Player
{
    /// <summary>
    /// 플레이어 이동을 관리하는 컴포넌트
    /// </summary>
    public class PlayerMovement : MonoBehaviour
    {
        // SerializeField - Inspector에서 수정 가능
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 10f;

        [Header("References")]
        [SerializeField] private Rigidbody playerRigidbody;

        // Private 필드 - 내부 상태
        private Vector3 _moveDirection;
        private bool _isGrounded;

        // Public 프로퍼티
        public bool IsMoving => _moveDirection.magnitude > 0.01f;

        // Unity 라이프사이클
        private void Awake()
        {
            if (playerRigidbody == null)
            {
                playerRigidbody = GetComponent<Rigidbody>();
            }
        }

        private void Update()
        {
            HandleInput();
        }

        private void FixedUpdate()
        {
            ApplyMovement();
        }

        // Private 메서드
        private void HandleInput()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            _moveDirection = new Vector3(horizontal, 0f, vertical).normalized;

            if (Input.GetKeyDown(KeyCode.Space) && _isGrounded)
            {
                Jump();
            }
        }

        private void ApplyMovement()
        {
            Vector3 velocity = _moveDirection * moveSpeed;
            playerRigidbody.velocity = new Vector3(velocity.x, playerRigidbody.velocity.y, velocity.z);
        }

        private void Jump()
        {
            playerRigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }
}
```

---

## 체크리스트

Unity 코드 작성 시 확인사항:

- [ ] 클래스명은 PascalCase
- [ ] 메서드명은 PascalCase, 동사로 시작
- [ ] SerializeField는 camelCase (언더스코어 없음)
- [ ] Private 필드는 _camelCase
- [ ] Boolean은 질문 형태 (is, has, can 등)
- [ ] 모든 제어문에 중괄호 사용
- [ ] 접근 제한자 명시 (MonoBehaviour 메서드 포함)
- [ ] 상수는 PascalCase
- [ ] 네임스페이스 사용

---

## 참고 자료

- [Unity 공식 C# 스타일 가이드](https://unity.com/how-to/naming-and-code-style-tips-c-scripting-unity)
- [Unity 코드 스타일 예시 (GitHub)](https://github.com/thomasjacobsen-unity/Unity-Code-Style-Guide)
- [Unity C# 스타일 가이드 전자책](https://resources.unity.com/games/create-code-style-guide-e-book)
- [Microsoft C# 코딩 컨벤션](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)

---

**작성일**: 2025년 10월 1일
**적용 프로젝트**: Game.Unity
