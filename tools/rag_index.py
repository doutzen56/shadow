#!/usr/bin/env python3
"""
SimpX RAG 索引工具：重建文档层、导入业务项目代码摘要、按关键词检索。

只写 SimpX 目录；业务仓只读，只生成路径、符号、哈希和风险标签摘要，不复制源码。
全局 knowledge/indexes/rag-manifest.yaml 不由本脚本改写，每次运行会打印要回填的值，
按 workflows/knowledge-index-maintenance 手工更新。

用法：
  python tools/rag_index.py bootstrap
      重建 SimpX 文档层（规则、配置、角色、工作流、模板、人工索引、正式知识、候选、项目档案）、
      knowledge_item 和所有种子项目登记；写 Qdrant 集合占位。不扫业务仓。
  python tools/rag_index.py project <project_id> <业务仓根目录>
      扫一个业务仓，刷新该项目的 source/chunk/symbol、代码摘要 Markdown 和摘要 manifest；
      knowledge/code-summaries/<project_id>/cards/ 下手写的深挖卡片只登记不覆盖，源码哈希变了标 stale。
  python tools/rag_index.py search <关键词> [--project <project_id>] [--role <角色>] [--task <任务说明>]
      用 SQLite FTS 查 chunk，结果写入 retrieval_trace，作为“查过 SQLite”的证据。

全量重建：删掉 rag/index.db，先跑 bootstrap，再对每个种子项目各跑一次 project。
"""

from __future__ import annotations

import hashlib
import json
import os
import re
import sqlite3
import sys
from datetime import datetime, timezone
from pathlib import Path

SIMPX_ROOT = Path(__file__).resolve().parents[1]
RAG_ROOT = SIMPX_ROOT / "rag"
DB_PATH = RAG_ROOT / "index.db"
QDRANT_ROOT = RAG_ROOT / "qdrant"
REPORT_DIR = RAG_ROOT / "init-reports"
SUMMARY_ROOT = SIMPX_ROOT / "knowledge" / "code-summaries"

SQLITE_SCHEMA_VERSION = "rag-data-model-v2"
QDRANT_COLLECTION_VERSION = "local-file-collection-v1"
EMBEDDING_MODEL = "not-configured-local-placeholder"
CHUNK_STRATEGY = "markdown-heading-yaml-top-key-code-summary"
QDRANT_COLLECTIONS = ["rag_formal_knowledge", "rag_project_profiles", "rag_code_summaries", "rag_candidates"]

NOW = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
STAMP = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")

ALL_ROLES = ["@@X", "@@TL", "@@BA", "@@BE", "@@DEVOPS", "reviewer", "researcher"]
CODE_ROLES = ["@@BA", "@@BE", "@@DEVOPS", "reviewer"]
AGENT_DIR_TO_ROLE = {
    "orchestrator": "@@X",
    "technical-leader": "@@TL",
    "architect": "@@BA",
    "backend": "@@BE",
    "devops": "@@DEVOPS",
    "reviewer": "reviewer",
    "researcher": "researcher",
}

# 和 knowledge/indexes/rag-manifest.yaml 的 sources 一一对应；改这里要同步 manifest。
DOC_SOURCES = [
    ("hard_rules", ".cursor/rules", ["*.mdc"]),
    ("configs", "config", ["*.yaml", "README.md"]),
    ("agents", "agents", ["*/agent.yaml", "*/system.md", "*/workflows/README.md"]),
    ("workflows", "workflows", ["**/*.md"]),
    ("templates", "templates", ["**/*.md"]),
    ("knowledge_indexes", "knowledge/indexes", ["*.md"]),
    ("formal_knowledge", "knowledge/patterns", ["**/*.md"]),
    ("formal_knowledge", "knowledge/decisions", ["**/*.md"]),
    ("formal_knowledge", "knowledge/incidents", ["**/*.md"]),
    ("formal_knowledge", "knowledge/glossary", ["**/*.md"]),
    ("candidates", "knowledge/candidates", ["*.md"]),
    ("project_profiles", "projects/examples", ["*/README.md", "*/profile.yaml", "*/boundaries.md", "*/architecture-map.md", "*/envs.yaml"]),
]
KNOWLEDGE_DOC_TYPES = {
    "knowledge/patterns": "pattern",
    "knowledge/decisions": "decision",
    "knowledge/incidents": "incident",
    "knowledge/glossary": "glossary",
    "knowledge/candidates": "candidate",
}

PRUNED_DIRS = {".git", "bin", "obj", "node_modules", "dist", "packages", ".vs"}
SENSITIVE_NAMES = {"envs.secrets.yaml", "envs.secrets.example.yaml"}
SENSITIVE_SUFFIXES = {".pfx", ".key", ".pem", ".config"}
SENSITIVE_PATTERNS = [re.compile(r"^appsettings.*\.json$", re.IGNORECASE)]
SOURCE_SUFFIXES = {".cs", ".csproj"}
MAX_LISTED_FILES = 50
MAX_SKIP_SAMPLES = 30

HIGH_RISK_TAGS = {"funds", "order-state", "permission", "login", "user-data", "callback", "idempotency", "public-contract"}
RISK_WORDS = {
    "funds": ["money", "fund", "wallet", "payment", "withdraw", "recharge", "pay"],
    "order-state": ["order", "status", "ticket"],
    "permission": ["admin", "permission", "auth", "role"],
    "login": ["login", "token", "session"],
    "user-data": ["user", "member", "mobile", "email"],
    "callback": ["callback", "notify"],
    "idempotency": ["repeat", "duplicate", "idempotent"],
    "cache-consistency": ["cache", "redis"],
    "mq": ["mq", "queue", "consumer", "producer", "rabbit"],
    "elasticsearch": ["elastic", "gamelog"],
    "gateway": ["gateway", "channel"],
}

# 新项目接入时在这里登记摘要分组；responsibility 会写进摘要 Markdown 的“职责摘要”。
PROJECTS = {
    "betwin": {
        "display_name": "BetWin",
        "solution_file": "BetWin5.0.sln",
        "primary_language": "C#/.NET Framework 4.8",
        "groups": [
            ("core-agents", "业务 Agent", ["Library/BetWin"], "business-agent",
             ["funds", "order-state", "cache-consistency", "gateway"],
             "站点、用户、资金、游戏、代理、风控的业务 Agent 和定时作业实现，资金和状态规则主要在这里。"),
            ("common-models", "公共模型", ["Library/BetWin.Common"], "domain-model",
             ["user-data", "database-schema", "elasticsearch"],
             "实体、枚举、报表模型和 Elasticsearch 索引注册，读写作业和查询共用。"),
            ("caching", "业务缓存", ["Library/BetWin.Caching"], "cache",
             ["cache-consistency", "user-data"],
             "Redis 业务缓存：用户态、资金点位、活动次数、站点配置等热点数据。"),
            ("handlers", "请求 Handler", [
                "Library/BetWin.Handler.Request",
                "Library/BetWin.Handler.Admin",
                "Library/BetWin.Handler.System",
                "Library/Handler/BetWin.Handler.Api",
            ], "http-handler", ["funds", "permission", "login", "callback", "user-data"],
             "四个请求面：会员与代理前台、站点管理端、平台系统端、游戏回调与运维接口。"),
            ("gateways", "第三方网关", [
                "Library/GateWay/BetWin.Gateway.Game",
                "Library/GateWay/BetWin.Gateway.Payment",
                "Library/GateWay/BetWin.Gateway.Withdraw",
                "Library/GateWay/BetWin.Gateway.Payment.Special",
                "Library/GateWay/BetWin.Gateway.Withdraw.Special",
            ], "external-gateway", ["gateway", "callback", "funds", "idempotency"],
             "第三方游戏、代收、代付插件；各供应商的签名、状态码、金额精度和回调时机都不一样。"),
            ("background-services", "后台服务", [
                "Services/JobSchedule",
                "Services/DataAnalysis",
                "Services/GameLog",
                "Services/MqServer",
            ], "background-service", ["mq", "elasticsearch", "async", "retry", "funds"],
             "Quartz 调度、数据分析与对账、注单采集、RabbitMQ 消费等后台进程。"),
        ],
    },
    "hx-matching": {
        "display_name": "HX.Matching",
        "solution_file": "HX.Matching.sln",
        "primary_language": "C#/.NET 9",
        "groups": [
            ("platform-hosts", "平台与商户后台宿主", [
                "HX.Matching.Admin.WebApi",
                "HX.Matching.AdminV2.WebApi",
                "HX.Matching.MerchantAdmin.WebApi",
            ], "web-host", ["permission", "login", "user-data"],
             "管理端和商户后台入口，承载运营、权限、报表和后台管理动作。"),
            ("open-apis", "商户与合作方开放接口", [
                "HX.Matching.MerchantApi",
                "HX.Matching.PartnerApi",
            ], "public-api", ["public-contract", "funds", "order-state", "gateway"],
             "商户和合作方开放接口热路径，涉及验签、订单创建、查单和外部契约。"),
            ("background-jobs", "后台任务与异步处理", [
                "HX.Matching.Quartz.Job",
                "HX.Matching.Tasks",
            ], "background-job", ["mq", "callback", "async", "retry", "order-state"],
             "定时任务、消息消费、回调、统计、超时处理和异步补偿入口。"),
            ("service-core", "业务核心服务", [
                "HX.Matching.ServiceCore",
                "HX.Matching.Service",
            ], "domain-service", ["funds", "order-state", "idempotency", "cache-consistency", "elasticsearch"],
             "撮合、渠道、统计、缓存、搜索和业务规则主要承载层。"),
            ("data-model-repository", "模型与仓储", [
                "HX.Matching.Model",
                "HX.Matching.Repository",
            ], "data-access", ["database-schema", "order-state", "user-data"],
             "实体、枚举、请求响应模型和 SqlSugar 仓储基础能力。"),
            ("infrastructure-common", "基础设施与公共能力", [
                "HX.Matching.Infrastructure",
                "HX.Matching.Common",
            ], "infrastructure", ["cache-consistency", "mq", "permission", "login"],
             "缓存、消息、认证、限流、异常处理、帮助类和横切能力。"),
            ("channels-integrations", "渠道与第三方集成", [
                "HX.Matching.Channels",
            ], "external-gateway", ["gateway", "funds", "callback", "retry"],
             "渠道适配、第三方网关、回调和通道相关扩展能力。"),
            ("migration-build-tools", "迁移与构建工具", [
                "src/EsMigrate.Console",
                "HX.BuildService",
            ], "tooling", ["elasticsearch", "production-config"],
             "ES 迁移、构建辅助和工程工具入口。"),
            ("tests", "测试项目", [
                "HX.Matching.Channels.Tests",
                "HX.Matching.V2.Tests",
                "src/TestIntegration",
            ], "test", [],
             "渠道、V2 和集成测试入口，用于后续验证定位。"),
        ],
    },
}


def read_text(path: Path) -> str:
    """按 UTF-8 优先读文本；老项目的 GBK 文件降级读取，只用来提取符号和算哈希。"""
    try:
        return path.read_text(encoding="utf-8-sig")
    except UnicodeDecodeError:
        return path.read_text(encoding="gb18030", errors="ignore")
    except OSError:
        return ""


def write_text(path: Path, text: str) -> None:
    """按 UTF-8 无 BOM 写文本，保证中文报告和 YAML 不乱码。"""
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8", newline="\n")


def digest(text: str) -> str:
    """生成内容哈希，用来追踪来源文件和这次导入的来源集合有没有变化。"""
    return hashlib.sha256(text.encode("utf-8")).hexdigest()


def rel(path: Path, root: Path) -> str:
    """把绝对路径转成正斜杠相对路径，库里和报告里都不出现盘符。"""
    return path.relative_to(root).as_posix()


def to_json(value: object) -> str:
    """把列表或字典转成 SQLite 里存的 JSON 字符串，中文不转义。"""
    return json.dumps(value, ensure_ascii=False)


def connect() -> sqlite3.Connection:
    """打开 rag/index.db，没有表就按数据模型建表。"""
    RAG_ROOT.mkdir(parents=True, exist_ok=True)
    conn = sqlite3.connect(DB_PATH)
    create_schema(conn)
    return conn


def create_schema(conn: sqlite3.Connection) -> None:
    """按 config/rag-data-model.yaml 建控制面表；向量内容不进 SQLite。"""
    conn.executescript(
        """
        create table if not exists project (
          project_id text primary key, display_name text not null, identify_by text not null,
          simpx_path text, repo_root text, primary_language text,
          status text not null, created_at text not null, updated_at text not null
        );
        create table if not exists source (
          source_id text primary key, project_id text, path text not null,
          source_type text not null, layer text not null, content_hash text not null,
          lifecycle_status text not null, updated_at text not null
        );
        create index if not exists ix_source_project on source(project_id, source_type);
        create table if not exists chunk (
          chunk_id text primary key, source_id text not null, project_id text,
          chunk_type text not null, heading text, summary text not null, text_ref text not null,
          role_scope text, task_scope text, risk_tags text,
          qdrant_point_id text, lifecycle_status text not null
        );
        create index if not exists ix_chunk_source on chunk(source_id);
        create table if not exists symbol (
          symbol_id text primary key, project_id text not null, source_id text not null,
          file_path text not null, symbol text not null, symbol_kind text not null,
          signature_hash text, file_hash text not null, lifecycle_status text not null
        );
        create index if not exists ix_symbol_project on symbol(project_id, symbol);
        create table if not exists knowledge_item (
          knowledge_id text primary key, source_id text not null, title text not null,
          doc_type text not null, maturity text not null, lifecycle_status text not null,
          confirmed_by text, user_reviewed integer,
          project_scope text, role_scope text, task_scope text, risk_tags text,
          evidence_refs text, superseded_by text
        );
        create table if not exists ingestion_run (
          run_id text primary key, run_type text not null, project_id text,
          manifest_hash text not null, started_at text not null, finished_at text,
          status text not null, error_summary text
        );
        create table if not exists retrieval_trace (
          trace_id text primary key, task_id text, project_id text,
          role_id text, query text not null, filters text, hits text not null,
          selected_hits text, rejected_hits text, created_at text not null
        );
        create table if not exists repair_log (
          repair_id text primary key, repair_type text not null, risk_level text not null,
          target_ref text not null, before_state text, after_state text,
          status text not null, needs_confirmation integer not null, created_at text not null
        );
        create virtual table if not exists chunk_fts using fts5(heading, summary, text_ref, content='chunk', content_rowid='rowid');
        create virtual table if not exists knowledge_item_fts using fts5(title, content='knowledge_item', content_rowid='rowid');
        """
    )


def table_counts(conn: sqlite3.Connection) -> dict:
    """统计主要表的行数，导入前后各拍一次，写进报告当证据。"""
    tables = ["project", "source", "chunk", "symbol", "knowledge_item", "ingestion_run", "retrieval_trace"]
    return {name: conn.execute(f"select count(*) from {name}").fetchone()[0] for name in tables}


def refresh_fts(conn: sqlite3.Connection) -> None:
    """按 chunk 和 knowledge_item 重建全文索引，导入后关键词检索才能查到新内容。"""
    conn.execute("insert into chunk_fts(chunk_fts) values ('rebuild')")
    conn.execute("insert into knowledge_item_fts(knowledge_item_fts) values ('rebuild')")


def parse_frontmatter(text: str) -> dict:
    """解析文件开头 --- 包住的简单 YAML：只认顶层的 key: value 和 key 下面的 - 列表。"""
    if not text.startswith("---"):
        return {}
    end = text.find("\n---", 3)
    if end < 0:
        return {}
    data: dict = {}
    current = None
    for line in text[3:end].splitlines():
        if not line.strip():
            continue
        item = re.match(r"^\s+-\s+(.*)$", line)
        if item and current:
            if not isinstance(data.get(current), list):
                data[current] = []
            data[current].append(item.group(1).strip().strip('"'))
            continue
        pair = re.match(r"^([A-Za-z_][\w-]*):\s*(.*)$", line)
        if pair:
            current = pair.group(1)
            value = pair.group(2).strip().strip('"')
            data[current] = value if value else []
    return data


def split_sections(path: Path, text: str) -> list[tuple[str, str]]:
    """按 rag-corpus.yaml 的 chunking 规则切块：Markdown/mdc 按标题，YAML 按顶层键。"""
    body = text
    if path.suffix in {".md", ".mdc"} and text.startswith("---"):
        end = text.find("\n---", 3)
        if end > 0:
            body = text[end + 4:]
    if path.suffix == ".yaml":
        pattern = re.compile(r"^([A-Za-z_][\w-]*):", re.MULTILINE)
    else:
        pattern = re.compile(r"^#{1,4}\s+(.+)$", re.MULTILINE)
    matches = list(pattern.finditer(body))
    if not matches:
        return [(path.stem, body)]
    sections = []
    head = body[:matches[0].start()].strip()
    if head:
        sections.append((path.stem, head))
    for index, match in enumerate(matches):
        end = matches[index + 1].start() if index + 1 < len(matches) else len(body)
        sections.append((match.group(1).strip(), body[match.start():end]))
    return sections


def insert_source(conn: sqlite3.Connection, project_id: str | None, path: str, source_type: str, layer: str, content_hash: str) -> str:
    """登记一个来源文件并返回 source_id；同一路径同一内容总得到同一个 id。"""
    source_id = digest(f"{path}:{content_hash}")[:32]
    conn.execute(
        "insert or replace into source values (?, ?, ?, ?, ?, ?, 'active', ?)",
        (source_id, project_id, path, source_type, layer, content_hash, NOW),
    )
    return source_id


def insert_chunk(conn: sqlite3.Connection, source_id: str, project_id: str | None, chunk_type: str, heading: str, summary: str, text_ref: str, roles: list[str], tags: list[str]) -> None:
    """登记一个可检索文本块；摘要只截前几百字，定位用，不当正文。"""
    chunk_id = digest(f"{source_id}:{text_ref}:{heading}")[:32]
    conn.execute(
        "insert or replace into chunk values (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 'active')",
        (chunk_id, source_id, project_id, chunk_type, heading, summary, text_ref,
         to_json(roles), to_json([]), to_json(tags), None),
    )


def doc_project_id(relative: str) -> str | None:
    """项目档案按 projects/examples/<project_id>/ 归属到项目，其余文档不属于任何项目。"""
    parts = relative.split("/")
    if len(parts) > 2 and parts[0] == "projects" and parts[1] == "examples":
        return parts[2]
    return None


def doc_roles(relative: str) -> list[str]:
    """角色文件只给对应角色看，其余文档所有角色都能看。"""
    parts = relative.split("/")
    if parts[0] == "agents" and len(parts) > 1 and parts[1] in AGENT_DIR_TO_ROLE:
        return [AGENT_DIR_TO_ROLE[parts[1]]]
    return ALL_ROLES


def insert_knowledge_item(conn: sqlite3.Connection, source_id: str, relative: str, text: str) -> bool:
    """有 frontmatter id 的正式知识或候选，登记成 knowledge_item，周审和重排序靠它。"""
    meta = parse_frontmatter(text)
    if not meta.get("id"):
        return False
    base = next((key for key in KNOWLEDGE_DOC_TYPES if relative.startswith(key + "/")), None)
    doc_type = meta.get("doc_type") or KNOWLEDGE_DOC_TYPES.get(base, "knowledge")
    lifecycle = meta.get("lifecycle_status") or ("candidate" if doc_type == "candidate" else "unknown")
    reviewed = meta.get("user_reviewed")
    conn.execute(
        "insert or replace into knowledge_item values (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
        (
            meta["id"], source_id, meta.get("title") or Path(relative).stem, doc_type,
            meta.get("maturity") or "n/a", lifecycle, meta.get("confirmed_by") or None,
            None if reviewed in (None, []) else int(str(reviewed).lower() == "true"),
            to_json(meta.get("projects") or meta.get("project_scope") or []),
            to_json(meta.get("role_scope") or []), to_json(meta.get("task_scope") or []),
            to_json(meta.get("risk_tags") or meta.get("tags") or []),
            to_json(meta.get("evidence_refs") or []), meta.get("superseded_by") or None,
        ),
    )
    return True


def is_sensitive(name: str) -> bool:
    """判断文件名是不是密钥、证书或可能带连接串的配置，这类文件一律不读。"""
    lower = name.lower()
    if lower in SENSITIVE_NAMES or Path(lower).suffix in SENSITIVE_SUFFIXES:
        return True
    return any(pattern.match(name) for pattern in SENSITIVE_PATTERNS)


def run_bootstrap() -> None:
    """重建 SimpX 文档层、knowledge_item 和种子项目登记；业务代码层不动。"""
    conn = connect()
    try:
        before = table_counts(conn)
        conn.execute("delete from chunk where source_id in (select source_id from source where source_type = 'doc')")
        conn.execute("delete from source where source_type = 'doc'")
        conn.execute("delete from knowledge_item")

        imported: list[tuple[str, str]] = []
        skipped: list[dict] = []
        knowledge_items = 0
        for layer, base, patterns in DOC_SOURCES:
            root = SIMPX_ROOT / base
            if not root.exists():
                continue
            files = sorted({path for pattern in patterns for path in root.glob(pattern) if path.is_file()})
            for path in files:
                relative = rel(path, SIMPX_ROOT)
                if is_sensitive(path.name):
                    skipped.append({"path": relative, "reason": "sensitive_file"})
                    continue
                text = read_text(path)
                content_hash = digest(text)
                project_id = doc_project_id(relative)
                source_id = insert_source(conn, project_id, relative, "doc", layer, content_hash)
                for heading, section in split_sections(path, text):
                    summary = re.sub(r"\s+", " ", section).strip()[:400]
                    insert_chunk(conn, source_id, project_id, layer, heading, summary,
                                 f"{relative}#{heading}", doc_roles(relative), [])
                if layer in {"formal_knowledge", "candidates"} and insert_knowledge_item(conn, source_id, relative, text):
                    knowledge_items += 1
                imported.append((relative, content_hash))

        for project_id, project in PROJECTS.items():
            exists = conn.execute("select created_at from project where project_id = ?", (project_id,)).fetchone()
            conn.execute(
                "insert or replace into project values (?, ?, ?, ?, ?, ?, 'active', ?, ?)",
                # 仓库位置随电脑变，repo_root 不落库；运行时按 identify_by 里的 .sln 定位。
                (project_id, project["display_name"], project["solution_file"], f"projects/examples/{project_id}",
                 None, project["primary_language"], exists[0] if exists else NOW, NOW),
            )

        refresh_fts(conn)
        source_set_hash = digest("\n".join(f"{path}:{content_hash}" for path, content_hash in imported))
        run_id = f"{STAMP}-baseline"
        conn.execute(
            "insert or replace into ingestion_run values (?, 'baseline', null, ?, ?, ?, 'completed', '')",
            (run_id, source_set_hash, NOW, NOW),
        )
        conn.commit()
        after = table_counts(conn)
    finally:
        conn.close()

    for collection in QDRANT_COLLECTIONS:
        meta = {
            "collection": collection,
            "version": QDRANT_COLLECTION_VERSION,
            "embedding_model": EMBEDDING_MODEL,
            "updated_at": NOW,
            "note": "本地集合占位，没有真实向量；接入 embedding 后按 source/chunk 重建 point。",
        }
        write_text(QDRANT_ROOT / collection / "collection.json", json.dumps(meta, ensure_ascii=False, indent=2) + "\n")

    report = {
        "run_id": run_id,
        "run_type": "baseline",
        "baseline_version": f"rag-baseline-{STAMP}",
        "generated_at": NOW,
        "generator": "tools/rag_index.py bootstrap",
        "manifest_hash": source_set_hash,
        "manifest_hash_meaning": "本次导入的全部来源（路径 + 内容哈希）拼起来的哈希；来源不变，哈希就不变。",
        "sqlite": rel(DB_PATH, SIMPX_ROOT),
        "sqlite_schema_version": SQLITE_SCHEMA_VERSION,
        "qdrant_collection_version": QDRANT_COLLECTION_VERSION,
        "embedding_model": EMBEDDING_MODEL,
        "chunk_strategy": CHUNK_STRATEGY,
        "doc_sources": len(imported),
        "knowledge_items": knowledge_items,
        "counts_before": before,
        "counts_after": after,
        "skipped": skipped,
        "global_manifest_updated": False,
        "next_step": "按 workflows/knowledge-index-maintenance 回填 rag-manifest.yaml 的 baseline 段。",
    }
    write_text(REPORT_DIR / f"{run_id}.json", json.dumps(report, ensure_ascii=False, indent=2) + "\n")
    print(json.dumps({key: report[key] for key in [
        "run_id", "baseline_version", "manifest_hash", "doc_sources", "knowledge_items", "counts_before", "counts_after", "next_step",
    ]}, ensure_ascii=False, indent=2))


def risk_tags_for(path: str, defaults: list[str]) -> tuple[list[str], int]:
    """按路径关键词补风险标签，返回全部标签和关键词新增的高风险标签个数（用于挑代表文件）。"""
    lower = path.lower()
    found = {tag for tag, words in RISK_WORDS.items() if any(word in lower for word in words)}
    return sorted(found | set(defaults)), len(found & HIGH_RISK_TAGS)


def extract_symbols(text: str) -> list[str]:
    """提取少量 C# 类型名和方法名，只做定位，不把源码写进知识库。"""
    symbols: list[str] = []
    patterns = [
        r"\b(?:public|internal|private|protected)?\s*(?:static\s+|sealed\s+|abstract\s+)*(?:partial\s+)?(?:class|interface|enum|record|struct)\s+([A-Za-z_][A-Za-z0-9_]*)",
        r"\b(?:public|internal|private|protected)\s+(?:static\s+|override\s+|virtual\s+)*(?:async\s+)?[A-Za-z0-9_<>,\[\]\.\?]+\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(",
    ]
    for pattern in patterns:
        for match in re.finditer(pattern, text):
            name = match.group(1)
            if name not in symbols:
                symbols.append(name)
            if len(symbols) >= 12:
                return symbols
    return symbols


def scan_root(repo_root: Path, root: str, skipped: dict) -> list[Path]:
    """遍历一个扫描根目录，跳过构建产物目录和敏感文件，只返回 .cs 和 .csproj。"""
    base = repo_root / root
    if not base.exists():
        skipped.setdefault("missing_root", []).append(root)
        return []
    found: list[Path] = []
    for current, dirs, files in os.walk(base):
        for name in list(dirs):
            if name.lower() in PRUNED_DIRS:
                dirs.remove(name)
                skipped.setdefault("pruned_directory", []).append(rel(Path(current) / name, repo_root))
        for name in files:
            path = Path(current) / name
            if is_sensitive(name):
                skipped.setdefault("sensitive_file", []).append(rel(path, repo_root))
            elif path.suffix.lower() in SOURCE_SUFFIXES:
                found.append(path)
    return sorted(found)


def render_group_markdown(project_id: str, project: dict, group: dict, run_id: str) -> str:
    """生成一组代码摘要 Markdown；格式所有项目统一，手改会在下次导入时被覆盖。"""
    lines = [
        f"# {group['title']}",
        "",
        "<!-- 由 tools/rag_index.py 生成，下次导入会覆盖；要改职责说明，改脚本里 PROJECTS 的分组定义。 -->",
        "",
        f"- project_id: `{project_id}`",
        f"- summary_group: `{group['group_id']}`",
        f"- host_or_layer: `{group['host_or_layer']}`",
        f"- solution_file: `{project['solution_file']}`",
        f"- last_ingestion_run: `{run_id}`",
        f"- file_count: {group['file_count']}",
        f"- project_count: {group['project_count']}",
        f"- risk_tags: {', '.join(f'`{tag}`' for tag in group['risk_tags']) or '无'}",
        "",
        "## 职责摘要",
        "",
        group["responsibility"],
        "",
        "## 扫描根目录",
        "",
        *[f"- `{root}`" for root in group["roots"]],
        "",
        "## 项目文件",
        "",
        *([f"- `{path}`" for path in group["project_files"]] or ["- 无"]),
        "",
        "## 定位样例",
        "",
        "按路径里的高风险关键词优先挑选，最多列 50 个。",
        "",
    ]
    for item in group["listed"]:
        symbols = ", ".join(item["symbols"][:8]) or "未提取到符号"
        lines.append(f"- `{item['path']}`：符号 `{symbols}`；风险 `{', '.join(item['risk_tags']) or '无'}`")
    hidden = group["file_count"] - len(group["listed"])
    if hidden > 0:
        lines.append(f"- 还有 {hidden} 个文件只写入 SQLite 控制面；本会话没用 `tools/rag_index.py search` 或 SQL 查过时，不能当作已读取。")
    lines.extend([
        "",
        "## 跳过项",
        "",
        f"- {group['skipped_summary']}；明细只写入 `rag/init-reports/{run_id}.json`。",
        "",
        "## 使用约束",
        "",
        "- 本摘要只用于定位和初筛，不替代当前源码。",
        "- 判断真实行为、接口契约、状态变更或兼容逻辑时，必须回读业务仓当前源码。",
        "- 风险标签按路径关键词自动打，可能多打或漏打；高风险判断以源码和项目档案为准。",
        "",
    ])
    return "\n".join(lines)


def run_project(project_id: str, repo_arg: str) -> None:
    """扫一个业务仓，替换该项目的代码层索引，并重写它的代码摘要和摘要 manifest。"""
    if project_id not in PROJECTS:
        raise SystemExit(f"未登记的项目：{project_id}。先在 PROJECTS 里登记摘要分组，再按 workflows/project-init 接入。")
    project = PROJECTS[project_id]
    repo_root = Path(repo_arg).resolve()
    if not (repo_root / project["solution_file"]).is_file():
        raise SystemExit(f"{repo_root} 下没有 {project['solution_file']}，不是 {project_id} 的仓库根目录。")
    run_id = f"{STAMP}-project-{project_id}"
    out_dir = SUMMARY_ROOT / project_id

    conn = connect()
    try:
        if not conn.execute("select 1 from project where project_id = ?", (project_id,)).fetchone():
            raise SystemExit("rag/index.db 里没有这个项目，先跑 bootstrap。")
        before = table_counts(conn)
        conn.execute("delete from chunk where source_id in (select source_id from source where project_id = ? and source_type in ('code_file', 'code_summary'))", (project_id,))
        conn.execute("delete from source where project_id = ? and source_type in ('code_file', 'code_summary')", (project_id,))
        conn.execute("delete from symbol where project_id = ?", (project_id,))

        imported: list[tuple[str, str]] = []
        groups: list[dict] = []
        for group_id, title, roots, host, defaults, responsibility in project["groups"]:
            skipped: dict = {}
            items = []
            for root in roots:
                for path in scan_root(repo_root, root, skipped):
                    relative = rel(path, repo_root)
                    text = read_text(path)
                    file_hash = digest(text)
                    tags, score = risk_tags_for(relative, defaults)
                    symbols = extract_symbols(text) if path.suffix.lower() == ".cs" else []
                    source_id = insert_source(conn, project_id, relative, "code_file", group_id, file_hash)
                    summary = f"{title}；层次 {host}；符号 {', '.join(symbols[:6]) or '无'}"
                    insert_chunk(conn, source_id, project_id, "code_summary", relative, summary, relative, CODE_ROLES, tags)
                    for symbol in symbols:
                        conn.execute(
                            "insert or replace into symbol values (?, ?, ?, ?, ?, 'csharp-symbol', ?, ?, 'active')",
                            (digest(f"{relative}:{symbol}:{file_hash}")[:32], project_id, source_id, relative,
                             symbol, digest(symbol)[:16], file_hash),
                        )
                    items.append({"path": relative, "symbols": symbols, "risk_tags": tags, "score": score})
                    imported.append((relative, file_hash))
            listed = sorted(items, key=lambda item: (-item["score"], not item["symbols"], item["path"]))[:MAX_LISTED_FILES]
            listed.sort(key=lambda item: item["path"])
            skip_counts = {reason: len(paths) for reason, paths in skipped.items()}
            groups.append({
                "group_id": group_id,
                "title": title,
                "host_or_layer": host,
                "roots": roots,
                "responsibility": responsibility,
                "file_count": len(items),
                "project_count": sum(1 for item in items if item["path"].endswith(".csproj")),
                "project_files": [item["path"] for item in items if item["path"].endswith(".csproj")],
                "risk_tags": sorted({tag for item in items for tag in item["risk_tags"]} | set(defaults)),
                "listed": listed,
                "skip_counts": skip_counts,
                "skip_samples": {reason: paths[:MAX_SKIP_SAMPLES] for reason, paths in skipped.items()},
                "skipped_summary": "、".join(f"{reason} {count} 项" for reason, count in skip_counts.items()) or "没有跳过项",
                "summary_file": f"knowledge/code-summaries/{project_id}/{group_id}.md",
            })

        current_hashes = dict(imported)
        cards = []
        for card_path in sorted((out_dir / "cards").glob("*.md")) if (out_dir / "cards").exists() else []:
            card_text = read_text(card_path)
            meta = parse_frontmatter(card_text)
            target = meta.get("file_path") or ""
            # 卡片记的哈希和当前源码对不上，说明源码改过，卡片内容可能已经过时。
            status = "active" if target and current_hashes.get(target) == meta.get("file_hash") else "stale"
            card_rel = rel(card_path, SIMPX_ROOT)
            source_id = insert_source(conn, project_id, card_rel, "code_summary", "code_summary_cards", digest(card_text))
            conn.execute("update source set lifecycle_status = ? where source_id = ?", (status, source_id))
            for heading, section in split_sections(card_path, card_text):
                insert_chunk(conn, source_id, project_id, "code_summary_card", heading,
                             re.sub(r"\s+", " ", section).strip()[:400], f"{card_rel}#{heading}",
                             CODE_ROLES, meta.get("risk_tags") or [])
            conn.execute("update chunk set lifecycle_status = ? where source_id = ?", (status, source_id))
            cards.append({"card": card_rel, "file_path": target, "status": status})

        for group in groups:
            markdown = render_group_markdown(project_id, project, group, run_id)
            path = out_dir / f"{group['group_id']}.md"
            write_text(path, markdown)
            source_id = insert_source(conn, project_id, rel(path, SIMPX_ROOT), "code_summary", "code_summaries", digest(markdown))
            insert_chunk(conn, source_id, project_id, "code_summary_group", group["title"],
                         f"{group['title']}：{group['responsibility']}", group["summary_file"], CODE_ROLES, group["risk_tags"])

        total = sum(group["file_count"] for group in groups)
        forbidden_scope = sorted(PRUNED_DIRS | SENSITIVE_NAMES | SENSITIVE_SUFFIXES | {"appsettings*.json"})
        manifest_lines = [
            "schema_version: v1",
            f"project_id: {project_id}",
            "status: active",
            f"source_profile: projects/examples/{project_id}/profile.yaml",
            f"solution_file: {project['solution_file']}",
            f"last_ingestion_run: {run_id}",
            f"generated_at: {NOW}",
            "generator: tools/rag_index.py project",
            f"summarized_files: {total}",
            "summary_groups:",
        ]
        for group in groups:
            manifest_lines.extend([
                f"  - group_id: {group['group_id']}",
                f"    title: {group['title']}",
                f"    host_or_layer: {group['host_or_layer']}",
                "    roots:",
                *[f"      - {root}" for root in group["roots"]],
                f"    file_count: {group['file_count']}",
                f"    project_count: {group['project_count']}",
                f"    skipped: {to_json(group['skip_counts'])}",
                f"    summary_file: {group['summary_file']}",
                f"    risk_tags: {to_json(group['risk_tags'])}",
            ])
        manifest_lines.append("forbidden_scope:")
        manifest_lines.extend(f"  - \"{item}\"" for item in forbidden_scope)
        manifest_lines.extend([
            "notes:",
            "  - 只导入 .cs 和 .csproj；.config、appsettings*.json、密钥和证书文件不读。",
            "  - 业务源码没有复制进 SimpX，只记录路径、符号、哈希和风险标签。",
            "  - 每组 Markdown 最多列 50 个定位样例，其余只在 SQLite；没查过 SQLite 不能当作已读取。",
            "  - 代码摘要只作为定位锚点，涉及行为结论必须回读当前源码。",
        ])
        manifest_text = "\n".join(manifest_lines) + "\n"
        manifest_path = out_dir / "manifest.yaml"
        write_text(manifest_path, manifest_text)
        insert_source(conn, project_id, rel(manifest_path, SIMPX_ROOT), "code_summary", "code_summaries", digest(manifest_text))

        refresh_fts(conn)
        source_set_hash = digest("\n".join(f"{path}:{content_hash}" for path, content_hash in imported))
        conn.execute(
            "insert or replace into ingestion_run values (?, 'project_import', ?, ?, ?, ?, 'completed', '')",
            (run_id, project_id, source_set_hash, NOW, NOW),
        )
        conn.commit()
        after = table_counts(conn)
    finally:
        conn.close()

    point = {
        "collection": "rag_code_summaries",
        "project_id": project_id,
        "run_id": run_id,
        "embedding_model": EMBEDDING_MODEL,
        "summary_manifest": f"knowledge/code-summaries/{project_id}/manifest.yaml",
        "summarized_files": total,
        "note": "本地 point 占位，没有真实向量；接入 embedding 后按 source/chunk 重建。",
    }
    write_text(QDRANT_ROOT / "rag_code_summaries" / f"{project_id}.json", json.dumps(point, ensure_ascii=False, indent=2) + "\n")
    profile_point = dict(point, collection="rag_project_profiles", source=f"projects/examples/{project_id}/profile.yaml")
    profile_point.pop("summary_manifest")
    profile_point.pop("summarized_files")
    write_text(QDRANT_ROOT / "rag_project_profiles" / f"{project_id}.json", json.dumps(profile_point, ensure_ascii=False, indent=2) + "\n")

    report = {
        "run_id": run_id,
        "run_type": "project_import",
        "project_id": project_id,
        "solution_file": project["solution_file"],
        "generated_at": NOW,
        "generator": "tools/rag_index.py project",
        "manifest_hash": source_set_hash,
        "manifest_hash_meaning": "本次导入的全部业务文件（路径 + 内容哈希）拼起来的哈希；源码不变，哈希就不变。",
        "summarized_files": total,
        "counts_before": before,
        "counts_after": after,
        "groups": [{key: group[key] for key in [
            "group_id", "title", "file_count", "project_count", "risk_tags", "summary_file", "skip_counts", "skip_samples",
        ]} for group in groups],
        "cards": cards,
        "stale_cards": [card for card in cards if card["status"] == "stale"],
        "forbidden_scope": forbidden_scope,
        "global_manifest_updated": False,
        "next_step": f"按 workflows/knowledge-index-maintenance 回填 rag-manifest.yaml 的 latest_run 和 seed_projects.{project_id}。",
    }
    write_text(REPORT_DIR / f"{run_id}.json", json.dumps(report, ensure_ascii=False, indent=2) + "\n")
    print(json.dumps({key: report[key] for key in [
        "run_id", "project_id", "manifest_hash", "summarized_files", "counts_before", "counts_after", "stale_cards", "next_step",
    ]} | {"summary_groups": len(groups), "cards": len(cards)}, ensure_ascii=False, indent=2))


def run_search(args: list[str]) -> None:
    """用 FTS 查 chunk，打印命中并写一条 retrieval_trace，作为检索过 SQLite 的证据。"""
    options = {"--project": None, "--role": None, "--task": None}
    words: list[str] = []
    index = 0
    while index < len(args):
        if args[index] in options and index + 1 < len(args):
            options[args[index]] = args[index + 1]
            index += 2
        else:
            words.append(args[index])
            index += 1
    if not words:
        raise SystemExit("用法：python tools/rag_index.py search <关键词> [--project <id>] [--role <角色>] [--task <任务说明>]")
    query = " ".join(words)
    fts_query = " OR ".join('"' + word.replace('"', "") + '"' for word in words)
    sql = (
        "select c.chunk_id, c.project_id, c.chunk_type, c.heading, c.text_ref, c.summary, c.role_scope "
        "from chunk_fts f join chunk c on c.rowid = f.rowid where chunk_fts match ?"
    )
    params: list = [fts_query]
    if options["--project"]:
        sql += " and (c.project_id = ? or c.project_id is null)"
        params.append(options["--project"])
    sql += " order by rank limit 20"

    conn = connect()
    try:
        rows = conn.execute(sql, params).fetchall()
        role = options["--role"]
        hits = []
        rejected = []
        for chunk_id, project_id, chunk_type, heading, text_ref, summary, role_scope in rows:
            hit = {"chunk_id": chunk_id, "project_id": project_id, "chunk_type": chunk_type, "text_ref": text_ref, "summary": summary[:160]}
            if role and role not in json.loads(role_scope or "[]"):
                rejected.append(dict(hit, reason=f"role_scope 不含 {role}"))
            else:
                hits.append(hit)
        selected = hits[:3]
        trace_id = digest(f"{NOW}:{query}:{options}")[:32]
        conn.execute(
            "insert into retrieval_trace values (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
            (trace_id, options["--task"], options["--project"], role, query,
             to_json({"project": options["--project"], "role": role}), to_json(hits),
             to_json(selected), to_json(rejected), NOW),
        )
        conn.commit()
    finally:
        conn.close()
    print(json.dumps({
        "trace_id": trace_id,
        "query": query,
        "hit_count": len(hits),
        "selected_top3": selected,
        "rejected_count": len(rejected),
        "note": "这是关键词检索，不是语义向量召回；命中只做定位，行为结论要回读源码。",
    }, ensure_ascii=False, indent=2))


def main() -> None:
    """按子命令分发：bootstrap、project、search。"""
    if len(sys.argv) < 2 or sys.argv[1] not in {"bootstrap", "project", "search"}:
        raise SystemExit(__doc__)
    command = sys.argv[1]
    if command == "bootstrap":
        run_bootstrap()
    elif command == "project":
        if len(sys.argv) != 4:
            raise SystemExit("用法：python tools/rag_index.py project <project_id> <业务仓根目录>")
        run_project(sys.argv[2], sys.argv[3])
    else:
        run_search(sys.argv[2:])


if __name__ == "__main__":
    main()
