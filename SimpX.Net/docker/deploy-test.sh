#!/usr/bin/env bash
# 测试环境一键：编译前后端 → 同步前端到 nginx 目录 → compose 重启 → 健康检查 → Telegram 通知。
# 由 GitLab CI（dev 分支）或本机在仓库根目录执行：./docker/deploy-test.sh
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
DOCKER_DIR="${SCRIPT_DIR}"
HEALTH_PATH="${DEPLOY_HEALTH_PATH:-/}"
BASE_URL="${DEPLOY_BASE_URL:-http://127.0.0.1:9100}"
HEALTH_URL="${BASE_URL%/}${HEALTH_PATH}"
NOTIFY="${SCRIPT_DIR}/notify-telegram.sh"

log() {
  printf '[deploy-test] %s\n' "$*"
}

fail() {
  log "ERROR: $*"
  exit 1
}

require_file() {
  local path="$1"
  local hint="$2"
  [[ -f "${path}" ]] || fail "缺少 ${path}。${hint}"
}

wait_http_ok() {
  local url="$1"
  local tries="${2:-60}"
  local i
  for ((i = 1; i <= tries; i++)); do
    if curl -fsS -o /dev/null --connect-timeout 3 --max-time 10 "${url}"; then
      log "健康检查通过：${url}"
      return 0
    fi
    sleep 5
  done
  fail "健康检查超时：${url}（已重试 ${tries} 次）"
}

[[ -x "${NOTIFY}" ]] || chmod +x "${NOTIFY}" || true

require_file "${DOCKER_DIR}/.env" "请从 .env.example 复制并填写 MSSQL_SA_PASSWORD / REDIS_PASSWORD。"
require_file "${DOCKER_DIR}/shared/Secrets.json" "请从 shared/Secrets.json.example 复制并填写；不要提交到 Git。"

command -v docker >/dev/null 2>&1 || fail "未找到 docker 命令。"
command -v curl >/dev/null 2>&1 || fail "未找到 curl（健康检查与 Telegram 通知需要）。"
docker compose version >/dev/null 2>&1 || fail "需要 Docker Compose V2（docker compose）。"

cd "${DOCKER_DIR}"

log "编译后端（Release）…"
docker compose -f docker-compose-builder.yml run --rm --no-deps net-builder

log "编译前端（admin）…"
docker compose -f docker-compose-builder.yml run --rm --no-deps web-builder

log "清理编译容器…"
docker compose -f docker-compose-builder.yml down --remove-orphans >/dev/null 2>&1 || true

# nginx 挂的是 Web/dist；vite 默认出到 apps/admin/dist，这里同步一份给网关用。
ADMIN_DIST="${ROOT}/Web/apps/admin/dist"
WEB_DIST="${ROOT}/Web/dist"
[[ -d "${ADMIN_DIST}" ]] || fail "前端产物不存在：${ADMIN_DIST}"
log "同步前端产物 → Web/dist …"
rm -rf "${WEB_DIST}"
mkdir -p "${WEB_DIST}"
cp -a "${ADMIN_DIST}/." "${WEB_DIST}/"

log "启动 / 更新运行中的栈…"
docker compose up -d --remove-orphans

log "等待 nginx 可访问…"
wait_http_ok "${HEALTH_URL}" 60

SHORT_SHA="${CI_COMMIT_SHORT_SHA:-$(git -C "${ROOT}" rev-parse --short HEAD 2>/dev/null || echo unknown)}"
BRANCH="${CI_COMMIT_BRANCH:-$(git -C "${ROOT}" rev-parse --abbrev-ref HEAD 2>/dev/null || echo unknown)}"
USER_NAME="${GITLAB_USER_NAME:-${GITLAB_USER_LOGIN:-local}}"
PIPE_URL="${CI_PIPELINE_URL:-本地执行}"

MSG="$(cat <<EOF
SimpX.Net 测试环境部署成功
分支: ${BRANCH}
提交: ${SHORT_SHA}
触发: ${USER_NAME}
访问: ${BASE_URL}
流水线: ${PIPE_URL}
EOF
)"

"${NOTIFY}" success "${MSG}" || log "Telegram 通知失败（部署本身已成功，请检查 CI Variables）。"
log "完成。"
