#!/bin/bash

# C# 프로젝트 문제 한 번에 해결 스크립트
echo "🔧 C# 프로젝트 문제 해결을 시작합니다..."

# 1. 모든 bin, obj 폴더 정리
echo "📁 빌드 캐시 정리 중..."
find . -name "bin" -type d -exec rm -rf {} + 2>/dev/null || true
find . -name "obj" -type d -exec rm -rf {} + 2>/dev/null || true

# 2. NuGet 패키지 캐시 정리
echo "📦 NuGet 캐시 정리 중..."
dotnet nuget locals all --clear

# 3. 솔루션 복원
echo "🔄 패키지 복원 중..."
dotnet restore

# 4. 전체 빌드 (경고 포함)
echo "🔨 프로젝트 빌드 중..."
dotnet build --no-restore

# 5. 코드 포맷팅 (dotnet format 사용)
echo "✨ 코드 포맷팅 중..."
if command -v dotnet-format &> /dev/null; then
    # 모든 스타일 문제를 한 번에 해결
    dotnet-format --verbosity diagnostic
    dotnet-format style --verbosity diagnostic
    dotnet-format analyzers --verbosity diagnostic
else
    echo "⚠️  dotnet format이 설치되지 않았습니다."
    echo "설치 명령어: dotnet tool install -g dotnet-format"
fi

# 6. 테스트 실행 (테스트 프로젝트가 있는 경우)
echo "🧪 테스트 실행 중..."
dotnet test --no-build 2>/dev/null || echo "ℹ️  테스트 프로젝트가 없습니다."

echo "✅ 프로젝트 정리 완료!"
echo "💡 다음 단계:"
echo "   - .editorconfig 파일로 코딩 스타일 통일"
echo "   - nullable 경고 점진적 해결"
echo "   - 코드 리뷰 및 리팩토링"
