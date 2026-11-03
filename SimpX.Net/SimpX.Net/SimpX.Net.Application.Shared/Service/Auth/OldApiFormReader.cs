using System.Text.Json;
using Furion.FriendlyException;
using Microsoft.AspNetCore.Http;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 旧路径 Form/Query 读取助手。
/// </summary>
public static class OldApiFormReader
{
    /// <summary>读字符串。</summary>
    public static string ReadString(HttpContext http, params string[] keys)
    {
        if (http == null) return string.Empty;
        foreach (var key in keys)
        {
            if (http.Request.HasFormContentType && http.Request.Form.ContainsKey(key))
                return http.Request.Form[key].ToString();
            if (http.Request.Query.ContainsKey(key))
                return http.Request.Query[key].ToString();
        }

        return string.Empty;
    }

    /// <summary>读 int。</summary>
    public static int ReadInt(HttpContext http, params string[] keys)
    {
        _ = int.TryParse(ReadString(http, keys), out var n);
        return n;
    }

    /// <summary>读 decimal。</summary>
    public static decimal ReadDecimal(HttpContext http, params string[] keys)
    {
        _ = decimal.TryParse(
            ReadString(http, keys),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var n);
        if (n == 0m)
            _ = decimal.TryParse(ReadString(http, keys), out n);
        return n;
    }

    /// <summary>读 bool（1/true）。</summary>
    public static bool ReadBool(HttpContext http, params string[] keys)
    {
        var s = ReadString(http, keys);
        return s is "1" or "true" or "True" or "TRUE";
    }

    /// <summary>整表 Form→JsonElement 字典。</summary>
    public static Dictionary<string, JsonElement> ReadJsonMap(HttpContext http)
    {
        var map = new Dictionary<string, JsonElement>(StringComparer.OrdinalIgnoreCase);
        if (http?.Request.HasFormContentType == true)
        {
            foreach (var key in http.Request.Form.Keys)
                map[key] = JsonSerializer.SerializeToElement(http.Request.Form[key].ToString());
        }

        foreach (var key in http?.Request.Query.Keys ?? Enumerable.Empty<string>())
        {
            if (!map.ContainsKey(key))
                map[key] = JsonSerializer.SerializeToElement(http.Request.Query[key].ToString());
        }

        return map;
    }

    /// <summary>统一跑旧 API。</summary>
    public static Task<OldApiResult> RunAsync(Func<CancellationToken, Task<object?>> action) =>
        RunAsync<object?>(action);

    /// <summary>统一跑旧 API（任意返回类型）。</summary>
    public static async Task<OldApiResult> RunAsync<T>(Func<CancellationToken, Task<T>> action)
    {
        var ct = App.HttpContext?.RequestAborted ?? default;
        try
        {
            var info = await action(ct);
            return info is null ? OldApiResult.Ok("#操作成功#") : OldApiResult.Ok(string.Empty, info);
        }
        catch (AppFriendlyException ex)
        {
            return OldApiResult.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            return OldApiResult.Fail(ex.Message);
        }
    }
}
