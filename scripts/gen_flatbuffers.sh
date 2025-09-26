#!/usr/bin/env bash
set -euo pipefail

# 루트 기준 경로 계산 (스크립트 위치 기준)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="${SCRIPT_DIR}/.."
SCHEMA_DIR="${ROOT_DIR}/Common/FlatBuffers"
SCHEMA_FILE="${SCHEMA_DIR}/game.fbs"
HELPER_FILE="${SCHEMA_DIR}/FlatMessageHelper.cs"

# 출력 경로
SERVER_OUT="${ROOT_DIR}/Server/Generated"
CLIENT_OUT="${ROOT_DIR}/Client/Assets/Generated"
COMMON_OUT="${SCHEMA_DIR}/Generated" # 기존 경로 (옵션, 유지)

mkdir -p "${SERVER_OUT}" "${CLIENT_OUT}" "${COMMON_OUT}"

if ! command -v flatc >/dev/null 2>&1; then
  echo "[ERROR] flatc 가 설치되어 있지 않습니다. brew install flatbuffers 등으로 설치하세요." >&2
  exit 1
fi

echo "[INFO] FlatBuffers C# 코드 생성 (공용)"
flatc --csharp -o "${COMMON_OUT}" "${SCHEMA_FILE}" || {
  echo "[ERROR] 공용 코드 생성 실패" >&2; exit 1; }

# 공용 생성물 소스 목록
GEN_SRC_LIST=$(find "${COMMON_OUT}" -maxdepth 1 -name '*.cs' -print)

# 서버/클라이언트로 복사 (동기화)
echo "[INFO] 서버 / 클라이언트 동기화"
rsync -a --delete "${COMMON_OUT}/" "${SERVER_OUT}/"
rsync -a --delete "${COMMON_OUT}/" "${CLIENT_OUT}/"

# FlatMessageHelper 동기화 (수동 작성 헬퍼)
if [ -f "${HELPER_FILE}" ]; then
  echo "[INFO] FlatMessageHelper 복사"
  cp "${HELPER_FILE}" "${SERVER_OUT}/"  # 서버
  cp "${HELPER_FILE}" "${CLIENT_OUT}/"  # 클라이언트 (Unity Assets 내부)
fi

echo "[INFO] 생성 완료"
