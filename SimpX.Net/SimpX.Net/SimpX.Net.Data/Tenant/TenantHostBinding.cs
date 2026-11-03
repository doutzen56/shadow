namespace SimpX.Net.Data;

/// <summary>
/// 租户绑定域名工具：一个 Host 字段可存多个域名（逗号/分号/空白分隔）。
/// </summary>
public static class TenantHostBinding
{
    private static readonly char[] Separators = [',', ';', ' ', '\t', '\r', '\n'];

    /// <summary>
    /// 拆开并规范化绑定域名列表（小写、去端口、去空、去重）。
    /// </summary>
    /// <param name="hostField">库里的 Host 字段或入参</param>
    /// <returns>规范化后的域名列表</returns>
    public static List<string> SplitHosts(string hostField)
    {
        if (string.IsNullOrWhiteSpace(hostField))
            return [];

        return hostField
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizeOne)
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// 把多个域名收成入库字符串（逗号分隔）。
    /// </summary>
    /// <param name="hosts">域名列表</param>
    /// <returns>入库字符串；没有域名时返回空串</returns>
    public static string NormalizeStore(IEnumerable<string> hosts)
    {
        var list = hosts?
            .Select(NormalizeOne)
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(u => u, StringComparer.Ordinal)
            .ToList() ?? [];
        return string.Join(',', list);
    }

    /// <summary>
    /// 规范化单个请求 Host 或绑定值（小写、去端口）。
    /// </summary>
    /// <param name="host">原始 Host</param>
    /// <returns>规范化后的 Host</returns>
    public static string NormalizeOne(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return null;

        var normalized = host.Trim().ToLowerInvariant();
        var colon = normalized.IndexOf(':');
        if (colon > 0)
            normalized = normalized[..colon];
        return normalized;
    }

    /// <summary>
    /// 请求 Host 在本机别名下的候选值。
    /// </summary>
    /// <param name="normalizedHost">已规范化的请求 Host</param>
    /// <returns>候选列表</returns>
    public static List<string> RequestCandidates(string normalizedHost)
    {
        var list = new List<string> { normalizedHost };
        if (normalizedHost == "localhost")
            list.Add("127.0.0.1");
        else if (normalizedHost == "127.0.0.1")
            list.Add("localhost");
        return list;
    }

    /// <summary>
    /// 租户绑定是否包含任一请求候选域名。
    /// </summary>
    /// <param name="hostField">租户 Host 字段</param>
    /// <param name="requestCandidates">请求候选</param>
    /// <returns>包含则返回 true</returns>
    public static bool MatchesAny(string hostField, IEnumerable<string> requestCandidates)
    {
        var bound = SplitHosts(hostField);
        if (bound.Count == 0) return false;
        var set = new HashSet<string>(bound, StringComparer.Ordinal);
        return requestCandidates.Any(set.Contains);
    }

    /// <summary>
    /// 两段 Host 字段是否有交集（用于唯一性校验）。
    /// </summary>
    /// <param name="left">左边</param>
    /// <param name="right">右边</param>
    /// <returns>有交集返回 true</returns>
    public static bool Overlaps(string left, string right)
    {
        var a = SplitHosts(left);
        if (a.Count == 0) return false;
        var b = new HashSet<string>(SplitHosts(right), StringComparer.Ordinal);
        return a.Any(b.Contains);
    }
}
