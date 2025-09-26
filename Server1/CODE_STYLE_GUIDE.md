# C# 코드 스타일 자동 수정 가이드

## 빠른 해결 방법

### 1단계: 즉시 모든 스타일 문제 해결
```bash
cd /Users/eohjin/GameProject/C#Server_UnityClient_Sample/Server1
dotnet-format --verbosity diagnostic
```

### 2단계: VS Code에서 자동화
- **Ctrl+Shift+P** → "Tasks: Run Task" → "코드 스타일 수정" 선택

### 3단계: 향후 자동 방지
- 파일 저장 시 자동 포맷팅 활성화됨
- `.editorconfig`로 팀 규칙 적용됨

## 해결된 문제 유형들

✅ **매개변수 줄바꿈 문제**
```csharp
// 이전 (노란색 밑줄)
new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

// 수정 후
new Socket(
    endPoint.AddressFamily,
    SocketType.Stream,
    ProtocolType.Tcp
);
```

✅ **Lambda 표현식 포맷팅 문제**
```csharp
// 이전 (노란색 밑줄)
_listener.Init(endPoint, () => { return SessionManager.Instance.Generate(); });

// 수정 후
_listener.Init(
    endPoint,
    () =>
    {
        return SessionManager.Instance.Generate();
    }
);
```

## 자주 사용할 명령어들

```bash
# 전체 프로젝트 정리
./fix_csharp_issues.sh

# 스타일만 수정
dotnet-format

# 빌드 + 포맷팅
dotnet build && dotnet-format
```

## VS Code 단축키

- **Shift+Alt+F**: 현재 파일 포맷팅
- **Ctrl+Shift+P**: 명령 팔레트
- **F1**: 빠른 작업

---

# 🚀 프로젝트 실행 가이드

## 📋 프로젝트 구조 분석

### 각 프로젝트의 역할:
- **ServerCore**: 공통 네트워킹 라이브러리 (실행 X)
- **Server**: 게임 서버 (포트 7777 Listen)
- **DummyClient**: 테스트 클라이언트 (500개 연결 시뮬레이션)

## ⚡ 올바른 실행 순서

### 1단계: 서버 먼저 실행
```bash
# 터미널 1
cd /Users/eohjin/GameProject/C#Server_UnityClient_Sample/Server1
dotnet run --project Server

# 출력 예상:
# Listening...
```

### 2단계: 클라이언트 실행 (서버 실행 후)
```bash
# 터미널 2 (새 터미널)
cd /Users/eohjin/GameProject/C#Server_UnityClient_Sample/Server1
dotnet run --project DummyClient

# 출력 예상:
# OnConnected : [IP주소]:7777
# (500개 연결 시도)
```

## 🔧 VS Code에서 실행하기

### Task 실행 방법:
1. **Ctrl+Shift+P** → `Tasks: Run Task`
2. `Server 실행` 선택 (먼저)
3. `DummyClient 실행` 선택 (나중에)

## 📊 실행 시 확인사항

### 서버 실행 시:
- ✅ "Listening..." 메시지 출력
- ✅ 포트 7777 대기 상태

### 클라이언트 실행 시:
- ✅ "OnConnected" 메시지들 출력
- ✅ 서버에서 "Connected : 1, 2, 3..." 메시지들
- ✅ 움직임 패킷 전송 시작

## ⚠️ 주의사항

- **ServerCore는 실행하지 마세요** (라이브러리입니다)
- **Server를 먼저 실행**해야 DummyClient가 연결됩니다
- **같은 네트워크**에 있어야 연결 가능합니다
