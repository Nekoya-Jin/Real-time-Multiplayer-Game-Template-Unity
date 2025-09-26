# Real-time Multiplayer Game Template Unity

Unity와 C# 서버를 사용한 실시간 멀티플레이어 게임 템플릿입니다.

## 프로젝트 구조

```
├── Client/              # Unity 클라이언트 프로젝트
│   ├── Assets/         # Unity 애셋 파일들
│   │   ├── Scripts/    # C# 스크립트 파일들
│   │   ├── Scenes/     # Unity 씬 파일들
│   │   └── Resources/  # 리소스 파일들
│   └── ProjectSettings/ # Unity 프로젝트 설정
├── Server/              # C# 서버 프로젝트
│   ├── Server/         # 메인 서버 코드
│   ├── ServerCore/     # 서버 코어 라이브러리
│   └── DummyClient/    # 테스트용 더미 클라이언트
├── Common/             # 공통 라이브러리
│   └── Packet/         # 패킷 정의
└── PacketGenerator/    # 패킷 생성기
```

## 주요 기능

- **실시간 네트워크 통신**: Unity 클라이언트와 C# 서버 간 TCP 소켓 통신
- **패킷 시스템**: 자동 패킷 생성 및 직렬화/역직렬화
- **플레이어 동기화**: 실시간 플레이어 위치 동기화
- **멀티플레이어 지원**: 여러 클라이언트 동시 접속 지원

## 시작하기

### 서버 실행
1. `Server/Server.sln` 솔루션을 Visual Studio에서 열기
2. Server 프로젝트를 시작 프로젝트로 설정
3. 빌드 후 실행

### 클라이언트 실행
1. Unity Hub에서 `Client` 폴더 열기
2. Unity Editor에서 프로젝트 로드
3. Play 버튼으로 실행

## 개발 환경

- **Unity**: 2022.3 LTS 이상
- **.NET**: .NET 6.0 이상
- **IDE**: Visual Studio, Unity Editor

## 네트워크 구조

클라이언트는 `NetworkManager`를 통해 서버와 통신하며, `MyPlayer` 클래스에서 주기적으로 움직임 패킷을 전송합니다.

```csharp
// 예시: 움직임 패킷 전송
C_Move movePacket = new C_Move();
movePacket.posX = Random.Range(-50, 50);
movePacket.posY = 0;
movePacket.posZ = Random.Range(-50, 50);
_network.Send(movePacket.Write());
```

## 라이선스

MIT License
