"""
只认契约用得到的 YAML 写法的小解析器，只用 Python 标准库。

支持：块状映射和列表、`- key: value` 形式的列表项、`key:` 下面不缩进的列表、
`[]` / `[a, "b"]` 行内列表、`{}`、单双引号字符串、整数、小数、true/false、null/~、# 注释。
不支持：多行字符串（| >）、锚点和引用（& *）、标签（!）、Tab 缩进、嵌套的行内结构。
遇到不支持的写法直接报错，不猜，免得把契约读错了还以为校验过了。
"""

from __future__ import annotations

import json
import re
from pathlib import Path

_RESERVED_START = set("&*!|>%@`")
_INT = re.compile(r"^[-+]?[0-9]+$")
_FLOAT = re.compile(r"^[-+]?([0-9]+\.[0-9]*|\.[0-9]+)([eE][-+]?[0-9]+)?$")


class YamlSubsetError(ValueError):
    """YAML 写法超出支持范围，或者缩进、格式不对。消息里带文件名和行号。"""


def load_file(path: str | Path) -> object:
    """按 UTF-8 读一个 YAML 文件并解析；文件带 BOM 也能读。

    Args:
        path: 文件路径。

    Returns:
        解析出的字典、列表或标量；空文件返回 None。

    Raises:
        YamlSubsetError: 写法不支持或格式不对。
    """
    text = Path(path).read_text(encoding="utf-8-sig")
    return loads(text, str(path))


def loads(text: str, source: str = "<text>") -> object:
    """解析一段 YAML 文本。

    Args:
        text: YAML 文本。
        source: 报错时显示的来源名，一般是文件路径。

    Returns:
        解析出的字典、列表或标量；没有内容时返回 None。

    Raises:
        YamlSubsetError: 写法不支持或格式不对。
    """
    lines: list[tuple[int, int, str]] = []
    for number, raw in enumerate(text.splitlines(), 1):
        leading = raw[: len(raw) - len(raw.lstrip(" \t"))]
        if "\t" in leading:
            raise YamlSubsetError(f"{source}:{number}：缩进里有 Tab，只能用空格")
        content = _strip_comment(raw).rstrip()
        if not content.strip() or content.strip() in {"---", "..."}:
            continue
        indent = len(content) - len(content.lstrip(" "))
        lines.append((number, indent, content.strip()))
    if not lines:
        return None
    value, index = _parse_block(lines, 0, lines[0][1], source)
    if index != len(lines):
        number = lines[index][0]
        raise YamlSubsetError(f"{source}:{number}：缩进不对，这一行接不上前面的结构")
    return value


def _strip_comment(raw: str) -> str:
    """去掉行尾注释；引号里的 # 不算注释，紧贴在字符后面的 # 也不算。

    只有出现在值开头位置（行首、空格、[ 或 , 后面）的引号才算字符串开始，
    普通文字里的撇号（如 it's）不会把后面的注释吞进去。

    Args:
        raw: 原始一行。

    Returns:
        去掉注释后的内容。
    """
    quote = None
    position = 0
    while position < len(raw):
        char = raw[position]
        previous = raw[position - 1] if position else " "
        if quote:
            if quote == '"' and char == "\\":
                position += 2
                continue
            if char == quote:
                quote = None
        elif char in {'"', "'"} and previous in " \t[,":
            quote = char
        elif char == "#" and previous in " \t":
            return raw[:position]
        position += 1
    return raw


def _is_sequence_line(content: str) -> bool:
    """判断一行是不是列表项（以 "- " 开头，或者只有一个 "-"）。"""
    return content == "-" or content.startswith("- ")


def _parse_block(lines: list, index: int, indent: int, source: str) -> tuple[object, int]:
    """从 index 开始按缩进解析一段块状结构，是列表还是映射看第一行。

    Args:
        lines: 预处理后的 (行号, 缩进, 内容) 列表。
        index: 起始行下标。
        indent: 这一段的缩进。
        source: 报错用的来源名。

    Returns:
        (解析结果, 下一个没处理的行下标)。
    """
    if _is_sequence_line(lines[index][2]):
        return _parse_sequence(lines, index, indent, source)
    return _parse_mapping(lines, index, indent, source)


def _parse_sequence(lines: list, index: int, indent: int, source: str) -> tuple[list, int]:
    """解析同一缩进下连续的列表项。

    Args:
        lines: 预处理后的行。
        index: 第一个列表项的行下标。
        indent: 列表项的缩进。
        source: 报错用的来源名。

    Returns:
        (列表, 下一个没处理的行下标)。
    """
    items: list = []
    while index < len(lines) and lines[index][1] == indent and _is_sequence_line(lines[index][2]):
        number, _, content = lines[index]
        rest = content[1:].strip()
        if not rest:
            if index + 1 < len(lines) and lines[index + 1][1] > indent:
                value, index = _parse_block(lines, index + 1, lines[index + 1][1], source)
            else:
                value, index = None, index + 1
        elif _split_key(rest, source, number) is not None:
            # "- key: value" 是一个映射项，映射的缩进等于 key 所在的列。
            child_indent = indent + (len(content) - len(rest))
            lines = lines[:index] + [(number, child_indent, rest)] + lines[index + 1:]
            value, index = _parse_mapping(lines, index, child_indent, source)
        else:
            value, index = _scalar(rest, source, number), index + 1
        items.append(value)
    if index < len(lines) and lines[index][1] > indent:
        raise YamlSubsetError(f"{source}:{lines[index][0]}：缩进不对，列表项下面多缩进了一行")
    return items, index


def _parse_mapping(lines: list, index: int, indent: int, source: str) -> tuple[dict, int]:
    """解析同一缩进下连续的 key: value。

    Args:
        lines: 预处理后的行。
        index: 第一个键所在的行下标。
        indent: 键的缩进。
        source: 报错用的来源名。

    Returns:
        (字典, 下一个没处理的行下标)。
    """
    result: dict = {}
    while index < len(lines) and lines[index][1] >= indent:
        number, current, content = lines[index]
        if current > indent:
            raise YamlSubsetError(f"{source}:{number}：缩进不对，多缩进了")
        if _is_sequence_line(content):
            raise YamlSubsetError(f"{source}:{number}：这里应该是 key: value，不能直接写列表项")
        pair = _split_key(content, source, number)
        if pair is None:
            raise YamlSubsetError(f"{source}:{number}：这一行不是 key: value")
        key, rest = pair
        if key in result:
            raise YamlSubsetError(f"{source}:{number}：键 {key} 重复了")
        if rest:
            value, index = _scalar(rest, source, number), index + 1
        elif index + 1 < len(lines) and lines[index + 1][1] > indent:
            value, index = _parse_block(lines, index + 1, lines[index + 1][1], source)
        elif index + 1 < len(lines) and lines[index + 1][1] == indent and _is_sequence_line(lines[index + 1][2]):
            value, index = _parse_sequence(lines, index + 1, indent, source)
        else:
            value, index = None, index + 1
        result[key] = value
    return result, index


def _split_key(content: str, source: str, number: int) -> tuple[str, str] | None:
    """把 "key: value" 拆成键和值文本；不是这种形式时返回 None。

    键可以带引号；不带引号时，键里不能出现 ": "。

    Args:
        content: 去掉缩进的一行。
        source: 报错用的来源名。
        number: 行号。

    Returns:
        (键, 值文本)，值为空时值文本是空串；不是键值对时返回 None。
    """
    if content[0] in {'"', "'"}:
        end = _closing_quote(content)
        if end < 0:
            raise YamlSubsetError(f"{source}:{number}：引号没有闭合")
        after = content[end + 1:]
        if not after.startswith(":"):
            return None
        if len(after) > 1 and after[1] != " ":
            return None
        key = _scalar(content[: end + 1], source, number)
        return str(key), after[1:].strip()
    match = re.match(r"^([^\s:][^:]*?)\s*:(?:\s+(.*)|$)", content)
    if not match:
        return None
    return match.group(1), (match.group(2) or "").strip()


def _closing_quote(text: str) -> int:
    """找到开头引号对应的闭合引号位置；双引号支持反斜杠转义，单引号支持 '' 转义。

    Args:
        text: 以引号开头的文本。

    Returns:
        闭合引号的下标；没找到返回 -1。
    """
    quote = text[0]
    position = 1
    while position < len(text):
        char = text[position]
        if quote == '"' and char == "\\":
            position += 2
            continue
        if char == quote:
            if quote == "'" and position + 1 < len(text) and text[position + 1] == "'":
                position += 2
                continue
            return position
        position += 1
    return -1


def _scalar(text: str, source: str, number: int) -> object:
    """把一个值文本转成 Python 值。

    Args:
        text: 值文本，已去掉首尾空白和注释。
        source: 报错用的来源名。
        number: 行号。

    Returns:
        字符串、整数、小数、布尔、None、空字典或标量列表。

    Raises:
        YamlSubsetError: 引号没闭合、用了不支持的写法。
    """
    if text[0] == '"':
        if _closing_quote(text) != len(text) - 1:
            raise YamlSubsetError(f"{source}:{number}：双引号字符串格式不对")
        try:
            return json.loads(text)
        except json.JSONDecodeError as error:
            raise YamlSubsetError(f"{source}:{number}：双引号字符串转义不对（{error.msg}）") from None
    if text[0] == "'":
        if _closing_quote(text) != len(text) - 1:
            raise YamlSubsetError(f"{source}:{number}：单引号字符串格式不对")
        return text[1:-1].replace("''", "'")
    if text[0] == "[":
        if not text.endswith("]"):
            raise YamlSubsetError(f"{source}:{number}：行内列表要以 ] 结尾，不能跨行")
        inner = text[1:-1].strip()
        if not inner:
            return []
        parts = _split_flow(inner, source, number)
        return [_scalar(part, source, number) for part in parts]
    if text[0] == "{":
        if text.replace(" ", "") == "{}":
            return {}
        raise YamlSubsetError(f"{source}:{number}：行内映射只支持 {{}}，有内容请写成块状")
    if text[0] in _RESERVED_START:
        raise YamlSubsetError(
            f"{source}:{number}：以 {text[0]} 开头的写法不支持（多行字符串、锚点、标签等）；"
            "如果这是普通文字，请加引号"
        )
    lower = text.lower()
    if lower in {"null", "~"}:
        return None
    if lower == "true":
        return True
    if lower == "false":
        return False
    if _INT.match(text):
        return int(text)
    if _FLOAT.match(text):
        return float(text)
    return text


def _split_flow(inner: str, source: str, number: int) -> list[str]:
    """按逗号拆开行内列表的元素，引号里的逗号不拆；不支持嵌套。

    Args:
        inner: 去掉方括号后的内容。
        source: 报错用的来源名。
        number: 行号。

    Returns:
        去掉首尾空白的元素文本列表。
    """
    parts: list[str] = []
    current = ""
    quote = None
    for char in inner:
        if quote:
            current += char
            if char == quote:
                quote = None
            continue
        if char in {'"', "'"}:
            quote = char
            current += char
        elif char in "[]{}":
            raise YamlSubsetError(f"{source}:{number}：行内列表里不支持嵌套结构")
        elif char == ",":
            parts.append(current.strip())
            current = ""
        else:
            current += char
    if quote:
        raise YamlSubsetError(f"{source}:{number}：行内列表里的引号没有闭合")
    parts.append(current.strip())
    if any(not part for part in parts):
        raise YamlSubsetError(f"{source}:{number}：行内列表里有空元素")
    return parts
