# VS Code Run and Debug 설정 가이드

## 🎉 Run and Debug 순차 실행 설정 완료!

### 🚀 사용 방법

#### 1. Run and Debug 패널 열기
- **`Ctrl+Shift+D`** (Windows/Linux) 또는 **`Cmd+Shift+D`** (Mac)
- 또는 왼쪽 사이드바에서 "Run and Debug" 아이콘 클릭

#### 2. 실행 구성 선택

##### 🔹 서버만 실행
- **"🚀 Launch Server Only"** 선택
- **F5** 키 또는 ▶️ 버튼 클릭
- 서버가 7777 포트에서 실행됩니다

##### 🔹 서버와 클라이언트 순차 실행 (추천)
- **"🎯 Launch Server + DummyClient (Sequential)"** 선택
- **F5** 키 또는 ▶️ 버튼 클릭
- 서버가 먼저 실행되고 "Listening..." 메시지가 나타나면
- 자동으로 DummyClient가 실행됩니다

## 🎯 대안 실행 방법

### 방법 1: VS Code Task Runner 사용

#### 1. Command Palette 열기
- `Ctrl+Shift+P` (Windows/Linux) 또는 `Cmd+Shift+P` (Mac)

#### 2. Task 실행
1. **"Tasks: Run Task"** 입력하고 선택
2. 실행할 작업 선택:
   - **🔹 서버만 실행**: `서버 실행 (Run Server)` 선택
   - **🔹 클라이언트만 실행**: `클라이언트 실행 (Run DummyClient)` 선택
   - **🔹 전체 빌드**: `빌드 (Build)` 선택

#### 3. 서버와 클라이언트 동시 실행 (추천)
1. 먼저 `서버 실행 (Run Server)` 실행
2. 서버가 "Listening..." 메시지를 출력하면
3. 새로운 터미널에서 `클라이언트 실행 (Run DummyClient)` 실행

### 방법 2: 터미널 명령어 사용

#### 서버 실행
```bash
dotnet run --project Server
```

#### 클라이언트 실행 (별도 터미널)
```bash
dotnet run --project DummyClient
```

## 🔧 C# 디버거 문제 해결 시도

### 1. VS Code 재시작
1. VS Code 완전 종료
2. 다시 실행
3. C# 확장이 완전히 로드될 때까지 대기

### 2. C# 확장 재설치
1. `Ctrl+Shift+X`로 확장 패널 열기
2. "C#" 검색
3. **C# Dev Kit** 확장 비활성화 후 재활성화
4. 또는 제거 후 재설치

### 3. .NET SDK 확인
```bash
dotnet --version
```
- .NET 9.0 이상이 설치되어 있는지 확인

### 4. OmniSharp 로그 확인
1. `Ctrl+Shift+P` → "C#: Show Output"
2. 오류 메시지 확인

## 🎯 Task Runner 단축키

| 작업 | 단축키 |
|------|--------|
| Task 실행 | `Ctrl+Shift+P` → "Tasks: Run Task" |
| 빌드 | `Ctrl+Shift+B` |
| 터미널 열기 | `Ctrl+\`` |

## 💡 추천 워크플로우

1. VS Code에서 프로젝트 열기
2. `Ctrl+Shift+P` → "Tasks: Run Task" → "빌드 (Build)"
3. `Ctrl+Shift+P` → "Tasks: Run Task" → "서버 실행 (Run Server)"
4. 새 터미널: `Ctrl+Shift+\`` 
5. `Ctrl+Shift+P` → "Tasks: Run Task" → "클라이언트 실행 (Run DummyClient)"

## 🔍 브레이크포인트 없이 디버깅

현재는 `Console.WriteLine()` 또는 로깅을 사용하여 디버깅하세요:

```csharp
// 예시
Console.WriteLine($"클라이언트 연결: {endPoint}");
Console.WriteLine($"패킷 수신: {packet.GetType().Name}");
```

## 🎉 C# 디버거가 복구되면

C# 확장이 정상 작동하면 기존 디버그 설정이 자동으로 복원됩니다.

### 3. 디버깅 기능

#### 🔍 브레이크포인트 설정
1. 코드 라인 번호 왼쪽을 클릭하여 브레이크포인트 설정
2. 디버그 모드에서 실행하면 해당 지점에서 멈춤
3. 변수 값 확인 및 단계별 실행 가능

#### 📊 디버그 정보 확인
- **Variables**: 현재 변수 값들
- **Watch**: 감시할 표현식 추가
- **Call Stack**: 호출 스택 확인
- **Debug Console**: 실시간 명령 실행

#### ⏯️ 디버그 제어
- **Continue (F5)**: 다음 브레이크포인트까지 실행
- **Step Over (F10)**: 현재 라인 실행 후 다음 라인
- **Step Into (F11)**: 함수 내부로 들어가기
- **Step Out (Shift+F11)**: 현재 함수에서 나가기
- **Restart (Ctrl+Shift+F5)**: 디버거 재시작
- **Stop (Shift+F5)**: 디버깅 중지

### 4. 추천 디버깅 포인트

#### Server 프로젝트
```csharp
// ClientSession.cs - 클라이언트 연결 시
public override void OnConnected(EndPoint endPoint)
{
    // 여기에 브레이크포인트 설정
}

// PacketHandler.cs - 패킷 처리 시
public static void C_MoveHandler(PacketSession session, IPacket packet)
{
    // 여기에 브레이크포인트 설정
}
```

#### DummyClient 프로젝트
```csharp
// ServerSession.cs - 서버 연결 시
public override void OnConnected(EndPoint endPoint)
{
    // 여기에 브레이크포인트 설정
}

// Program.cs - 패킷 전송 시
// 패킷 전송 부분에 브레이크포인트 설정
```

### 5. 트러블슈팅

#### 문제: "프로그램을 찾을 수 없습니다"
- 해결: `Ctrl+Shift+P` → "Developer: Reload Window" 실행

#### 문제: 디버거가 연결되지 않음
- 해결: 프로젝트를 다시 빌드 (`Ctrl+Shift+P` → "Tasks: Run Task" → "빌드 (Build)")

#### 문제: 브레이크포인트가 작동하지 않음
- 해결: Debug 모드로 빌드되었는지 확인
- `dotnet build --configuration Debug` 실행

### 6. 키보드 단축키

| 기능 | Windows/Linux | Mac |
|------|---------------|-----|
| 디버그 시작/계속 | F5 | F5 |
| 디버그 중지 | Shift+F5 | Shift+F5 |
| 디버그 재시작 | Ctrl+Shift+F5 | Cmd+Shift+F5 |
| Step Over | F10 | F10 |
| Step Into | F11 | F11 |
| Step Out | Shift+F11 | Shift+F11 |
| 브레이크포인트 토글 | F9 | F9 |

## 🎉 이제 VS Code에서 완전한 디버깅 환경이 구축되었습니다!

Run and Debug 패널에서 원하는 구성을 선택하고 F5를 눌러 실행해보세요.
