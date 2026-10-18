#!/usr/bin/env python3
"""
初始化 BetWin 的 SimpX RAG baseline。

脚本只写 SimpX 目录；业务仓只读，且只生成代码摘要，不复制完整业务源码。
全局 knowledge/indexes/rag-manifest.yaml 不由本脚本改写，重跑后按 knowledge-index-maintenance 更新。

用法：python tools/rag_bootstrap_betwin.py <BetWin 仓库根目录>
"""

from __future__ import annotations

import hashlib
import json
import re
import sqlite3
import sys
from datetime import datetime, timezone
from pathlib import Path


SOLUTION_FILE = "BetWin5.0.sln"


def resolve_repo_root() -> Path:
    """从命令行拿业务仓根目录，并用解决方案文件确认没传错仓库。

    不写死盘符，换电脑后只要传新的仓库路径。

    Returns:
        BetWin 仓库根目录。
    """
    if len(sys.argv) < 2:
        raise SystemExit(f"用法：python tools/rag_bootstrap_betwin.py <包含 {SOLUTION_FILE} 的仓库根目录>")
    root = Path(sys.argv[1]).resolve()
    if not (root / SOLUTION_FILE).is_file():
        raise SystemExit(f"{root} 下没有 {SOLUTION_FILE}，不是 BetWin 仓库根目录。")
    return root


SIMPX_ROOT = Path(__file__).resolve().parents[1]
REPO_ROOT = resolve_repo_root()
PROJECT_ID = "betwin"
BASELINE_VERSION = "rag-baseline-betwin-0.1.0"
SQLITE_SCHEMA_VERSION = "rag-data-model-v1"
QDRANT_COLLECTION_VERSION = "local-file-collection-v1"
EMBEDDING_MODEL = "not-configured-local-placeholder"
CHUNK_STRATEGY = "markdown-heading-yaml-top-key-code-summary"
ROUTING_TABLE_VERSION = "rag-x-routing-v1"
ROLE_MATRIX_VERSION = "rag-role-matrix-v1"

NOW = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
STAMP = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%SZ")
RUN_ID = f"{STAMP}-baseline-{PROJECT_ID}"

RAG_ROOT = SIMPX_ROOT / "rag"
DB_PATH = RAG_ROOT / "index.db"
QDRANT_ROOT = RAG_ROOT / "qdrant"
REPORT_DIR = RAG_ROOT / "init-reports"
SUMMARY_ROOT = SIMPX_ROOT / "knowledge" / "code-summaries" / PROJECT_ID

FORBIDDEN_PARTS = {".git", "bin", "obj", "node_modules", "dist", "logs", "packages"}
FORBIDDEN_NAMES = {"envs.secrets.yaml", "envs.secrets.example.yaml"}
FORBIDDEN_SUFFIXES = {".pfx", ".key", ".pem"}
SOURCE_SUFFIXES = {".cs", ".config", ".csproj"}

GROUPS = [
    ("core-agents", "业务 Agent", ["Library/BetWin"], "business-agent", ["funds", "order-state", "cache-consistency", "gateway"]),
    ("common-models", "公共模型", ["Library/BetWin.Common"], "domain-model", ["user-data", "database-schema", "elasticsearch"]),
    ("caching", "业务缓存", ["Library/BetWin.Caching"], "cache", ["cache-consistency", "user-data"]),
    ("handlers", "请求 Handler", [
        "Library/BetWin.Handler.Request",
        "Library/BetWin.Handler.Admin",
        "Library/BetWin.Handler.System",
        "Library/Handler/BetWin.Handler.Api",
    ], "http-handler", ["funds", "permission", "login", "callback", "user-data"]),
    ("gateways", "第三方网关", [
        "Library/GateWay/BetWin.Gateway.Game",
        "Library/GateWay/BetWin.Gateway.Payment",
        "Library/GateWay/BetWin.Gateway.Withdraw",
        "Library/GateWay/BetWin.Gateway.Payment.Special",
        "Library/GateWay/BetWin.Gateway.Withdraw.Special",
    ], "external-gateway", ["gateway", "callback", "funds", "idempotency"]),
    ("background-services", "后台服务", [
        "Services/JobSchedule",
        "Services/DataAnalysis",
        "Services/GameLog",
        "Services/MqServer",
    ], "background-service", ["mq", "elasticsearch", "async", "retry", "funds"]),
]

RISK_WORDS = {
    "funds": ["money", "fund", "wallet", "payment", "withdraw", "recharge", "pay"],
    "order-state": ["order", "status", "ticket"],
    "permission": ["admin", "permission", "auth", "role"],
    "login": ["login", "token", "session"],
    "user-data": ["user", "member", "mobile", "email"],
    "callback": ["callback", "notify", "api"],
    "idempotency": ["repeat", "duplicate", "idempotent"],
    "cache-consistency": ["cache", "redis"],
    "mq": ["mq", "queue", "consumer", "producer"],
    "elasticsearch": ["elastic", "gamelog", "index"],
    "gateway": ["gateway", "payment", "withdraw", "game"],
}


def read_text(path: Path) -> str:
    """按 UTF-8 优先读取文本，遇到旧编码文件时降级为只读摘要输入。"""
    try:
        return path.read_text(encoding="utf-8-sig")
    except UnicodeDecodeError:
        return path.read_text(encoding="gb18030", errors="ignore")
    except OSError:
        return ""


def write_text(path: Path, text: str) -> None:
    """按 UTF-8 无 BOM 写入文本，保证中文报告和 YAML 不乱码。"""
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8", newline="\n")


def digest(text: str) -> str:
    """生成内容哈希，用来记录 source、chunk 和 manifest 的可追溯状态。"""
    return hashlib.sha256(text.encode("utf-8")).hexdigest()


def rel(path: Path, root: Path) -> str:
    """把绝对路径转成稳定的正斜杠相对路径，便于跨环境比较。"""
    return path.relative_to(root).as_posix()


def forbidden(path: Path) -> str | None:
    """判断文件是否落入禁止索引范围，并返回跳过原因。"""
    parts = {part.lower() for part in path.parts}
    if parts & FORBIDDEN_PARTS:
        return "forbidden_directory"
    if path.name.lower() in FORBIDDEN_NAMES:
        return "forbidden_secret_file"
    if path.suffix.lower() in FORBIDDEN_SUFFIXES:
        return "forbidden_sensitive_suffix"
    return None


def risk_tags(path: str, defaults: list[str]) -> list[str]:
    """根据路径和文件名补充风险标签，摘要只用于定位，行为仍要回读源码。"""
    lower = path.lower()
    tags = set(defaults)
    for tag, words in RISK_WORDS.items():
        if any(word in lower for word in words):
            tags.add(tag)
    return sorted(tags)


def extract_symbols(text: str) -> list[str]:
    """提取少量 C# 类和方法名，避免把完整源码写进知识库。"""
    symbols: list[str] = []
    patterns = [
        r"\b(?:public|internal|private|protected)?\s*(?:partial\s+)?(?:class|interface|enum)\s+([A-Za-z_][A-Za-z0-9_]*)",
        r"\b(?:public|internal|private|protected)\s+(?:static\s+)?(?:async\s+)?[A-Za-z0-9_<>,\[\]\.]+\s+([A-Za-z_][A-Za-z0-9_]*)\s*\(",
    ]
    for pattern in patterns:
        for match in re.finditer(pattern, text):
            name = match.group(1)
            if name not in symbols:
                symbols.append(name)
            if len(symbols) >= 12:
                return symbols
    return symbols


def schema(conn: sqlite3.Connection) -> None:
    """创建 RAG 控制面表；向量内容不进 SQLite。"""
    conn.executescript(
        """
        create table if not exists project (
          project_id text primary key, display_name text, identify_by text,
          simpx_path text, repo_root text, primary_language text,
          status text, created_at text, updated_at text
        );
        create table if not exists source (
          source_id text primary key, project_id text, path text,
          source_type text, layer text, content_hash text,
          lifecycle_status text, updated_at text
        );
        create table if not exists chunk (
          chunk_id text primary key, source_id text, project_id text,
          chunk_type text, heading text, summary text, text_ref text,
          role_scope text, task_scope text, risk_tags text,
          qdrant_point_id text, lifecycle_status text
        );
        create table if not exists symbol (
          symbol_id text primary key, project_id text, source_id text,
          file_path text, symbol text, symbol_kind text,
          signature_hash text, file_hash text, lifecycle_status text
        );
        create table if not exists knowledge_item (
          knowledge_id text primary key, source_id text, title text,
          doc_type text, maturity text, project_scope text,
          role_scope text, task_scope text, risk_tags text,
          evidence_refs text, superseded_by text
        );
        create table if not exists ingestion_run (
          run_id text primary key, run_type text, project_id text,
          manifest_hash text, started_at text, finished_at text,
          status text, error_summary text
        );
        create table if not exists retrieval_trace (
          trace_id text primary key, task_id text, project_id text,
          role_id text, query text, filters text, hits text,
          selected_hits text, rejected_hits text, created_at text
        );
        create table if not exists repair_log (
          repair_id text primary key, repair_type text, risk_level text,
          target_ref text, before_state text, after_state text,
          status text, needs_confirmation integer, created_at text
        );
        create virtual table if not exists chunk_fts using fts5(heading, summary, text_ref, content='chunk', content_rowid='rowid');
        create virtual table if not exists knowledge_item_fts using fts5(title, content='knowledge_item', content_rowid='rowid');
        """
    )


def upsert_source(conn: sqlite3.Connection, path: Path, root: Path, source_type: str, layer: str, project_id: str | None = None) -> str:
    """注册一个可追溯来源文件，并返回 source_id。"""
    text = read_text(path)
    relative = rel(path, root)
    content_hash = digest(text)
    source_id = digest(f"{relative}:{content_hash}")[:32]
    conn.execute(
        """
        insert or replace into source
        (source_id, project_id, path, source_type, layer, content_hash, lifecycle_status, updated_at)
        values (?, ?, ?, ?, ?, ?, 'active', ?)
        """,
        (source_id, project_id, relative, source_type, layer, content_hash, NOW),
    )
    return source_id


def upsert_chunk(conn: sqlite3.Connection, source_id: str, project_id: str | None, chunk_type: str, heading: str, summary: str, text_ref: str, tags: list[str]) -> str:
    """注册一个可检索文本单元，FTS 用摘要和路径定位，不替代源码判断。"""
    chunk_id = digest(f"{source_id}:{heading}:{text_ref}")[:32]
    conn.execute(
        """
        insert or replace into chunk
        (chunk_id, source_id, project_id, chunk_type, heading, summary, text_ref, role_scope, task_scope, risk_tags, qdrant_point_id, lifecycle_status)
        values (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 'active')
        """,
        (
            chunk_id,
            source_id,
            project_id,
            chunk_type,
            heading,
            summary,
            text_ref,
            json.dumps(["@@X", "@@TL", "@@BA", "@@BE", "@@DEVOPS"], ensure_ascii=False),
            json.dumps(["知识库/RAG运维", "项目接入", "代码实现", "缺陷定位"], ensure_ascii=False),
            json.dumps(tags, ensure_ascii=False),
            chunk_id,
        ),
    )
    return chunk_id


def collect_docs(conn: sqlite3.Connection) -> int:
    """导入 SimpX 规则、配置、工作流、角色、正式知识和 BetWin 项目档案。"""
    doc_sets = [
        (SIMPX_ROOT / ".cursor" / "rules", "*.mdc", "rule", "hard_rules", None),
        (SIMPX_ROOT / "config", "*.yaml", "config", "simpx_config", None),
        (SIMPX_ROOT / "workflows", "README.md", "workflow", "workflows", None),
        (SIMPX_ROOT / "agents", "system.md", "agent", "agents", None),
        (SIMPX_ROOT / "knowledge" / "patterns", "*.md", "knowledge", "formal_knowledge", None),
        (SIMPX_ROOT / "knowledge" / "decisions", "*.md", "knowledge", "formal_knowledge", None),
        (SIMPX_ROOT / "knowledge" / "incidents", "*.md", "knowledge", "formal_knowledge", None),
        (SIMPX_ROOT / "knowledge" / "glossary", "*.md", "knowledge", "formal_knowledge", None),
        (SIMPX_ROOT / "projects" / "examples" / PROJECT_ID, "*", "project_profile", "project_profiles", PROJECT_ID),
    ]
    count = 0
    for base, pattern, source_type, layer, project_id in doc_sets:
        if not base.exists():
            continue
        for path in sorted(base.rglob(pattern) if pattern not in {"README.md", "system.md"} else base.rglob(pattern)):
            if not path.is_file() or forbidden(path):
                continue
            source_id = upsert_source(conn, path, SIMPX_ROOT, source_type, layer, project_id)
            text = read_text(path)
            title = path.stem if path.name != "README.md" else path.parent.name
            upsert_chunk(conn, source_id, project_id, source_type, title, text[:500].replace("\n", " "), rel(path, SIMPX_ROOT), [])
            count += 1
    return count


def scan_group(conn: sqlite3.Connection, group_id: str, title: str, roots: list[str], host: str, defaults: list[str]) -> dict:
    """扫描一个 BetWin 摘要分组，生成文件级和符号级定位信息。"""
    files: list[dict] = []
    skipped: list[dict] = []
    for root in roots:
        base = REPO_ROOT / root
        if not base.exists():
            skipped.append({"path": root, "reason": "missing_root"})
            continue
        for path in sorted(base.rglob("*")):
            if not path.is_file():
                continue
            reason = forbidden(path)
            if reason:
                skipped.append({"path": rel(path, REPO_ROOT), "reason": reason})
                continue
            if path.suffix.lower() not in SOURCE_SUFFIXES:
                continue
            text = read_text(path)
            relative = rel(path, REPO_ROOT)
            tags = risk_tags(relative, defaults)
            symbols = extract_symbols(text)
            file_hash = digest(text)
            files.append({
                "path": relative,
                "hash": file_hash,
                "symbols": symbols,
                "risk_tags": tags,
            })
            source_id = digest(f"{relative}:{file_hash}")[:32]
            conn.execute(
                """
                insert or replace into source
                (source_id, project_id, path, source_type, layer, content_hash, lifecycle_status, updated_at)
                values (?, ?, ?, 'code_summary', ?, ?, 'active', ?)
                """,
                (source_id, PROJECT_ID, relative, host, file_hash, NOW),
            )
            summary = f"{title} 文件摘要；宿主或层次：{host}；符号：{', '.join(symbols[:6]) if symbols else '未提取到公开符号'}。"
            upsert_chunk(conn, source_id, PROJECT_ID, "code_summary", relative, summary, relative, tags)
            for symbol in symbols:
                symbol_id = digest(f"{relative}:{symbol}:{file_hash}")[:32]
                conn.execute(
                    """
                    insert or replace into symbol
                    (symbol_id, project_id, source_id, file_path, symbol, symbol_kind, signature_hash, file_hash, lifecycle_status)
                    values (?, ?, ?, ?, ?, 'csharp-symbol', ?, ?, 'active')
                    """,
                    (symbol_id, PROJECT_ID, source_id, relative, symbol, digest(symbol)[:16], file_hash),
                )
    group_file = SUMMARY_ROOT / f"{group_id}.md"
    group_tags = sorted({tag for item in files for tag in item["risk_tags"]} | set(defaults))
    lines = [
        f"# {title}",
        "",
        f"- project_id: `{PROJECT_ID}`",
        f"- host_or_layer: `{host}`",
        f"- roots: {', '.join(f'`{root}`' for root in roots)}",
        f"- file_count: {len(files)}",
        f"- generated_at: {NOW}",
        "",
        "## 风险标签",
        "",
        ", ".join(f"`{tag}`" for tag in group_tags) or "无",
        "",
        "## 代表文件",
        "",
    ]
    for item in files[:80]:
        symbol_text = ", ".join(item["symbols"][:8]) if item["symbols"] else "未提取到公开符号"
        tag_text = ", ".join(item["risk_tags"])
        lines.append(f"- `{item['path']}`：符号 `{symbol_text}`；风险 `{tag_text}`。")
    if len(files) > 80:
        lines.append(f"- 还有 {len(files) - 80} 个文件仅写入 SQLite 控制面，未在 Markdown 摘要里展开。")
    if skipped:
        # 跳过明细可能带证书、密钥这类敏感路径，只进初始化报告，不进可召回的摘要。
        reasons = sorted({item["reason"] for item in skipped})
        lines.extend([
            "",
            "## 跳过项",
            "",
            f"- 共跳过 {len(skipped)} 个文件，原因：{', '.join(reasons)}。明细只写入 `rag/init-reports/{RUN_ID}.json`，不进入可召回摘要。",
        ])
    write_text(group_file, "\n".join(lines) + "\n")
    return {
        "group_id": group_id,
        "title": title,
        "host_or_layer": host,
        "roots": roots,
        "file_count": len(files),
        "summary_file": rel(group_file, SIMPX_ROOT),
        "risk_tags": group_tags,
        "skipped": skipped,
    }


def write_qdrant_placeholders() -> None:
    """创建本地 Qdrant collection 占位目录，记录集合版本和用途。"""
    collections = ["rag_formal_knowledge", "rag_project_profiles", "rag_code_summaries", "rag_candidates"]
    for collection in collections:
        meta = {
            "collection": collection,
            "version": QDRANT_COLLECTION_VERSION,
            "created_at": NOW,
            "embedding_model": EMBEDDING_MODEL,
            "note": "本地初始化占位；接入真实 Qdrant 后按 source/chunk 重建 point。",
        }
        write_text(QDRANT_ROOT / collection / "collection.json", json.dumps(meta, ensure_ascii=False, indent=2) + "\n")


def refresh_fts(conn: sqlite3.Connection) -> None:
    """重建 FTS 表，保证初始化后能做关键词检索。"""
    conn.execute("insert into chunk_fts(chunk_fts) values ('rebuild')")
    conn.execute("insert into knowledge_item_fts(knowledge_item_fts) values ('rebuild')")


def write_manifests(groups: list[dict], doc_count: int, manifest_hash: str) -> None:
    """写入 BetWin 摘要 manifest 和初始化报告。

    全局 rag-manifest.yaml 不在这里改：重跑后要按 knowledge-index-maintenance
    只更新 latest_run 和 seed_projects.betwin，避免把已登记的来源覆盖回旧结构。
    """
    total_files = sum(group["file_count"] for group in groups)
    forbidden_scope = sorted(FORBIDDEN_PARTS | FORBIDDEN_NAMES | FORBIDDEN_SUFFIXES)
    summary = [
        "schema_version: v1",
        f"project_id: {PROJECT_ID}",
        "status: active",
        "source_profile: projects/examples/betwin/profile.yaml",
        f"solution_file: {SOLUTION_FILE}",
        f"last_ingestion_run: {RUN_ID}",
        f"generated_at: {NOW}",
        f"imported_docs: {doc_count}",
        f"summarized_files: {total_files}",
        "summary_groups:",
    ]
    for group in groups:
        summary.extend([
            f"  - group_id: {group['group_id']}",
            f"    title: {group['title']}",
            f"    host_or_layer: {group['host_or_layer']}",
            "    roots:",
            *[f"      - {root}" for root in group["roots"]],
            f"    file_count: {group['file_count']}",
            f"    skipped_count: {len(group['skipped'])}",
            f"    summary_file: {group['summary_file']}",
            "    risk_tags:",
            *[f"      - {tag}" for tag in group["risk_tags"]],
        ])
    summary.append("forbidden_scope:")
    summary.extend(f"  - {item}" for item in forbidden_scope)
    summary.extend([
        "notes:",
        "  - 未读取 envs.secrets.yaml。",
        "  - 业务源码没有复制进 SimpX，只记录摘要、路径、符号、哈希和风险标签。",
        "  - 跳过文件明细只在初始化报告里，摘要不列路径。",
        "  - 代码摘要只作为定位锚点，涉及行为结论必须回读当前源码。",
    ])
    write_text(SUMMARY_ROOT / "manifest.yaml", "\n".join(summary) + "\n")

    report = {
        "baseline_version": BASELINE_VERSION,
        "run_id": RUN_ID,
        "generated_at": NOW,
        "manifest_hash": manifest_hash,
        "sqlite": rel(DB_PATH, SIMPX_ROOT),
        "sqlite_schema_version": SQLITE_SCHEMA_VERSION,
        "qdrant_root": rel(QDRANT_ROOT, SIMPX_ROOT),
        "qdrant_collection_version": QDRANT_COLLECTION_VERSION,
        "doc_sources": doc_count,
        "summarized_files": total_files,
        "groups": groups,
        "forbidden_scope": forbidden_scope,
        "global_manifest_updated": False,
        "next_step": "按 workflows/knowledge-index-maintenance 更新 rag-manifest.yaml 的 latest_run 和 seed_projects.betwin。",
        "notes": [
            "未读取 envs.secrets.yaml。",
            "业务源码没有复制进 SimpX，只记录摘要、路径、符号、哈希和风险标签。",
            "本地 Qdrant 目录为集合占位；真实向量需要接入 embedding 后重建 point。",
        ],
    }
    write_text(REPORT_DIR / f"{RUN_ID}.json", json.dumps(report, ensure_ascii=False, indent=2) + "\n")


def main() -> None:
    """执行 BetWin RAG baseline 初始化。"""
    RAG_ROOT.mkdir(parents=True, exist_ok=True)
    SUMMARY_ROOT.mkdir(parents=True, exist_ok=True)
    REPORT_DIR.mkdir(parents=True, exist_ok=True)

    conn = sqlite3.connect(DB_PATH)
    try:
        schema(conn)
        conn.execute(
            """
            insert or replace into project
            (project_id, display_name, identify_by, simpx_path, repo_root, primary_language, status, created_at, updated_at)
            values (?, 'BetWin', 'BetWin5.0.sln', 'projects/examples/betwin', ?, 'C#/.NET Framework 4.8', 'active', ?, ?)
            """,
            # 仓库位置换电脑就会变，不落库；运行时按 identify_by 里的 .sln 重新定位。
            (PROJECT_ID, None, NOW, NOW),
        )
        doc_count = collect_docs(conn)
        groups = [scan_group(conn, *group) for group in GROUPS]
        refresh_fts(conn)
        placeholder_hash = digest(f"{BASELINE_VERSION}:{RUN_ID}:{doc_count}:{sum(group['file_count'] for group in groups)}")
        conn.execute(
            """
            insert or replace into ingestion_run
            (run_id, run_type, project_id, manifest_hash, started_at, finished_at, status, error_summary)
            values (?, 'baseline', ?, ?, ?, ?, 'completed', '')
            """,
            (RUN_ID, PROJECT_ID, placeholder_hash, NOW, NOW),
        )
        conn.commit()
    finally:
        conn.close()

    write_qdrant_placeholders()
    manifest_material = f"{BASELINE_VERSION}:{RUN_ID}:{DB_PATH}:{QDRANT_ROOT}"
    write_manifests(groups, doc_count, digest(manifest_material))
    print(json.dumps({
        "status": "completed",
        "run_id": RUN_ID,
        "baseline_version": BASELINE_VERSION,
        "sqlite": rel(DB_PATH, SIMPX_ROOT),
        "report_dir": rel(REPORT_DIR, SIMPX_ROOT),
        "summary_manifest": rel(SUMMARY_ROOT / "manifest.yaml", SIMPX_ROOT),
        "global_manifest": "未修改，按 knowledge-index-maintenance 更新 latest_run 和 seed_projects.betwin",
    }, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
