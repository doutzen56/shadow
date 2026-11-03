#!/usr/bin/env bash
# 向 Telegram 群发一条部署结果。token / chat_id 只从环境变量读，不写进仓库。
# 用法：./docker/notify-telegram.sh success|failure [可选正文]
set -euo pipefail

STATUS="${1:-}"
CUSTOM_TEXT="${2:-}"

if [[ -z "${STATUS}" ]]; then
  echo "[notify-telegram] 用法: $0 success|failure [text]" >&2
  exit 2
fi

if [[ -z "${TELEGRAM_BOT_TOKEN:-}" || -z "${TELEGRAM_CHAT_ID:-}" ]]; then
  echo "[notify-telegram] 未配置 TELEGRAM_BOT_TOKEN / TELEGRAM_CHAT_ID，跳过通知。" >&2
  exit 0
fi

if [[ -n "${CUSTOM_TEXT}" ]]; then
  TEXT="${CUSTOM_TEXT}"
else
  SHORT_SHA="${CI_COMMIT_SHORT_SHA:-unknown}"
  BRANCH="${CI_COMMIT_BRANCH:-unknown}"
  PIPE_URL="${CI_PIPELINE_URL:-}"
  TEXT="SimpX.Net 部署 ${STATUS}
分支: ${BRANCH}
提交: ${SHORT_SHA}
${PIPE_URL}"
fi

# Telegram 对 Markdown 特殊字符挑剔，这里用纯文本。
RESP="$(curl -fsS -X POST "https://api.telegram.org/bot${TELEGRAM_BOT_TOKEN}/sendMessage" \
  --data-urlencode "chat_id=${TELEGRAM_CHAT_ID}" \
  --data-urlencode "text=${TEXT}" \
  --data-urlencode "disable_web_page_preview=true" \
  || true)"

if [[ -z "${RESP}" ]]; then
  echo "[notify-telegram] 发送失败（网络或 token/chat_id 不对）。" >&2
  exit 1
fi

echo "[notify-telegram] 已发送。"
