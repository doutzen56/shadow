"""
只认少量 JSON Schema 关键字的校验器，只用 Python 标准库。

支持的关键字见 SUPPORTED_KEYWORDS。Schema 里出现别的关键字会直接报错：
不认识的约束如果悄悄跳过，校验看起来通过了，其实那条约束根本没检查。
"""

from __future__ import annotations

import json
import re
from pathlib import Path

SUPPORTED_KEYWORDS = {
    "$schema", "$id", "title", "description",
    "type", "const", "enum",
    "minLength", "pattern",
    "minItems", "uniqueItems", "items",
    "required", "properties", "additionalProperties",
}

_TYPE_NAMES = {
    "object": "映射", "array": "列表", "string": "字符串", "integer": "整数",
    "number": "数字", "boolean": "布尔值", "null": "空值",
}


class SchemaError(ValueError):
    """Schema 文件本身写错了，或者用了校验器不支持的关键字。"""


def load_schema(path: str | Path) -> dict:
    """按 UTF-8 读一个 Schema 文件，并检查里面只用了支持的关键字。

    Args:
        path: Schema 文件路径。

    Returns:
        Schema 字典。

    Raises:
        SchemaError: 不是 JSON 对象，或用了不支持的关键字。
    """
    try:
        schema = json.loads(Path(path).read_text(encoding="utf-8-sig"))
    except json.JSONDecodeError as error:
        raise SchemaError(f"{path}：不是合法 JSON（第 {error.lineno} 行：{error.msg}）") from None
    if not isinstance(schema, dict):
        raise SchemaError(f"{path}：Schema 顶层要是对象")
    _check_keywords(schema, "$", str(path))
    return schema


def _check_keywords(schema: dict, where: str, source: str) -> None:
    """递归检查 Schema 里每一层都只用了支持的关键字。

    Args:
        schema: 当前这一层 Schema。
        where: 当前位置，报错用。
        source: Schema 文件名，报错用。

    Raises:
        SchemaError: 出现不支持的关键字。
    """
    unknown = set(schema) - SUPPORTED_KEYWORDS
    if unknown:
        raise SchemaError(f"{source} {where}：校验器不支持这些关键字 {sorted(unknown)}，要么去掉，要么先扩展 schema_check.py")
    for key, child in (schema.get("properties") or {}).items():
        _check_keywords(child, f"{where}.properties.{key}", source)
    if isinstance(schema.get("items"), dict):
        _check_keywords(schema["items"], f"{where}.items", source)


def validate(value: object, schema: dict, where: str = "$") -> list[str]:
    """按 Schema 校验一个值，返回全部错误，不在第一条错误就停。

    Args:
        value: 要校验的值。
        schema: Schema 字典（先用 load_schema 读，保证关键字都支持）。
        where: 当前位置，错误消息里用，如 $.parallel.class。

    Returns:
        错误消息列表；空列表表示通过。
    """
    expected = schema.get("type")
    if expected is not None:
        types = expected if isinstance(expected, list) else [expected]
        if not any(_is_type(value, name) for name in types):
            wanted = " 或 ".join(_TYPE_NAMES.get(name, name) for name in types)
            return [f"{where}：应该是{wanted}，实际是{_type_name(value)}"]

    errors: list[str] = []
    if "const" in schema and value != schema["const"]:
        errors.append(f"{where}：只能是 {schema['const']!r}，实际是 {value!r}")
    if "enum" in schema and value not in schema["enum"]:
        errors.append(f"{where}：只能是 {schema['enum']} 之一，实际是 {value!r}")

    if isinstance(value, str):
        if len(value) < schema.get("minLength", 0):
            errors.append(f"{where}：不能为空")
        if "pattern" in schema and not re.search(schema["pattern"], value):
            errors.append(f"{where}：格式不对，要符合 {schema['pattern']}，实际是 {value!r}")

    if isinstance(value, list):
        if len(value) < schema.get("minItems", 0):
            errors.append(f"{where}：至少要有 {schema['minItems']} 项")
        if schema.get("uniqueItems"):
            seen: list = []
            for item in value:
                if item in seen:
                    errors.append(f"{where}：有重复项 {item!r}")
                seen.append(item)
        if isinstance(schema.get("items"), dict):
            for position, item in enumerate(value):
                errors.extend(validate(item, schema["items"], f"{where}[{position}]"))

    if isinstance(value, dict):
        for key in schema.get("required", []):
            if key not in value:
                errors.append(f"{where}：缺少必填字段 {key}")
        properties = schema.get("properties", {})
        for key, item in value.items():
            if key in properties:
                errors.extend(validate(item, properties[key], f"{where}.{key}"))
            elif schema.get("additionalProperties") is False:
                errors.append(f"{where}：不认识的字段 {key}")
    return errors


def _is_type(value: object, name: str) -> bool:
    """判断值是不是 Schema 里说的类型；布尔值不算整数，也不算数字。

    Args:
        value: 要判断的值。
        name: Schema 类型名。

    Returns:
        是这个类型时返回 True。
    """
    if name == "object":
        return isinstance(value, dict)
    if name == "array":
        return isinstance(value, list)
    if name == "string":
        return isinstance(value, str)
    if name == "boolean":
        return isinstance(value, bool)
    if name == "null":
        return value is None
    if name == "integer":
        return isinstance(value, int) and not isinstance(value, bool)
    if name == "number":
        return isinstance(value, (int, float)) and not isinstance(value, bool)
    return False


def _type_name(value: object) -> str:
    """给错误消息用的中文类型名。

    Args:
        value: 任意值。

    Returns:
        中文类型名。
    """
    for name in ("null", "boolean", "integer", "number", "string", "array", "object"):
        if _is_type(value, name):
            return _TYPE_NAMES[name]
    return type(value).__name__
