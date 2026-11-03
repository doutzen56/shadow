using System.Net;
using System.Text;
using System.Text.Json;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 站点 Setting / PCSetting 的 querystring 编解码（对齐旧版 SettingBase）。
/// </summary>
public static class SiteSettingQuery
{
    /// <summary>
    /// 把库里的 Setting 原文解析成键值表。
    /// </summary>
    /// <param name="raw">Setting / PCSetting 原文</param>
    /// <returns>键值（已 URL 解码）</returns>
    public static Dictionary<string, string> Parse(string raw) =>
        RequestSiteService.ParseSettingMap(raw);

    /// <summary>
    /// 读字符串；没有键返回空串。
    /// </summary>
    /// <param name="map">键值表</param>
    /// <param name="name">键名</param>
    /// <returns>值</returns>
    public static string Get(Dictionary<string, string> map, string name) =>
        map != null && map.TryGetValue(name, out var v) ? (v ?? string.Empty) : string.Empty;

    /// <summary>
    /// 读布尔；对齐旧版 SettingBase（1 / true）。
    /// </summary>
    /// <param name="map">键值表</param>
    /// <param name="name">键名</param>
    /// <returns>布尔</returns>
    public static bool GetBool(IDictionary<string, string> map, string name)
    {
        if (map == null || !map.TryGetValue(name, out var v) || string.IsNullOrWhiteSpace(v))
            return false;
        if (bool.TryParse(v, out var b))
            return b;
        return v == "1";
    }

    /// <summary>
    /// 把键值表写成 querystring（对齐 SettingBase.ToString：值 URL 编码）。
    /// </summary>
    /// <param name="map">键值表</param>
    /// <returns>原文</returns>
    public static string ToQueryString(IDictionary<string, string> map)
    {
        if (map == null || map.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        var first = true;
        foreach (var kv in map)
        {
            if (string.IsNullOrEmpty(kv.Key))
                continue;
            if (!first)
                sb.Append('&');
            first = false;
            sb.Append(kv.Key);
            sb.Append('=');
            sb.Append(WebUtility.UrlEncode(kv.Value ?? string.Empty));
        }
        return sb.ToString();
    }

    /// <summary>
    /// 把 JSON 对象合并进键值表；布尔写成 True/False，其它 ToString。
    /// </summary>
    /// <param name="target">目标表</param>
    /// <param name="source">前端提交的 Setting</param>
    public static void MergeJson(IDictionary<string, string> target, Dictionary<string, JsonElement> source)
    {
        if (source == null)
            return;
        foreach (var kv in source)
        {
            if (string.IsNullOrEmpty(kv.Key))
                continue;
            target[kv.Key] = JsonElementToSettingValue(kv.Value);
        }
    }

    /// <summary>
    /// 把键值表转成 get 用的 Setting 对象（字符串原样；能解析成 bool/number 的尽量转，方便前端表单）。
    /// </summary>
    /// <param name="map">键值表</param>
    /// <returns>可序列化字典</returns>
    public static Dictionary<string, object> ToSettingObject(Dictionary<string, string> map)
    {
        var result = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        if (map == null)
            return result;
        foreach (var kv in map)
        {
            var raw = kv.Value ?? string.Empty;
            if (bool.TryParse(raw, out var b))
                result[kv.Key] = b;
            else if (int.TryParse(raw, out var i) && !LooksLikeLeadingZeroId(raw))
                result[kv.Key] = i;
            else if (decimal.TryParse(raw, out var d) && raw.Contains('.') )
                result[kv.Key] = d;
            else
                result[kv.Key] = raw;
        }
        return result;
    }

    /// <summary>
    /// 单段 JsonElement 转 Setting 字符串值。
    /// </summary>
    private static string JsonElementToSettingValue(JsonElement el) =>
        el.ValueKind switch
        {
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            JsonValueKind.Number => el.ToString(),
            JsonValueKind.Null => string.Empty,
            JsonValueKind.String => el.GetString() ?? string.Empty,
            _ => el.ToString()
        };

    /// <summary>
    /// 避免把 "01" 这类编号误收成整数。
    /// </summary>
    private static bool LooksLikeLeadingZeroId(string raw) =>
        raw.Length > 1 && raw.StartsWith('0') && raw.All(char.IsDigit);
}
