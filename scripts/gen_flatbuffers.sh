#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="${SCRIPT_DIR}/.."
SCHEMA_DIR="${ROOT_DIR}/Common/FlatBuffers"
SCHEMA_FILE="${SCHEMA_DIR}/game.fbs"
HELPER_FILE="${SCHEMA_DIR}/FlatMessageHelper.cs"

SERVER_OUT="${ROOT_DIR}/Server/Generated"
CLIENT_OUT="${ROOT_DIR}/Client/Assets/Generated"
COMMON_OUT="${SCHEMA_DIR}/Generated"

mkdir -p "${SERVER_OUT}" "${CLIENT_OUT}" "${COMMON_OUT}"

if ! command -v flatc >/dev/null 2>&1; then
  echo "[ERROR] flatc not installed. brew install flatbuffers" >&2
  exit 1
fi

echo "[INFO] Generate FlatBuffers C# (common)"
flatc --csharp -o "${COMMON_OUT}" "${SCHEMA_FILE}" || { echo "[ERROR] flatc generation failed" >&2; exit 1; }

echo "[INFO] Sync to server/client"
rsync -a --delete "${COMMON_OUT}/" "${SERVER_OUT}/"
rsync -a --delete "${COMMON_OUT}/" "${CLIENT_OUT}/"

if [ -f "${HELPER_FILE}" ]; then
  echo "[INFO] Copy FlatMessageHelper"
  cp "${HELPER_FILE}" "${SERVER_OUT}/"
  cp "${HELPER_FILE}" "${CLIENT_OUT}/"
fi

echo "[INFO] Done"