using System.Text.Json;
using System.Text.RegularExpressions;
using SqlSugar;

namespace SimpX.Net.Test;

/// <summary>
/// 从 Host.Admin 的 Database.json 只读取出 BetWin30 连接，供 ORM 冒烟用；不改宿主配置、不开建表。
/// </summary>
internal static class OrmConnection
{
    /// <summary>
    /// 解析 Admin Database.json 里第一条 SqlServer 连接串（主业务库 BetWin30）。
    /// </summary>
    /// <returns>SqlServer 连接串</returns>
    /// <exception cref="FileNotFoundException">找不到 Database.json</exception>
    /// <exception cref="InvalidOperationException">文件里没有可用连接串</exception>
    public static string LoadBetWin30ConnectionString()
    {
        var path = ResolveAdminDatabaseJsonPath();
        if (!File.Exists(path))
            throw new FileNotFoundException("找不到 Host.Admin Database.json，无法取 BetWin30 连接串。", path);

        var raw = File.ReadAllText(path);
        // Database.json 带 // 注释，先去掉再按 JSON 读
        var json = Regex.Replace(raw, @"^\s*//.*$", string.Empty, RegexOptions.Multiline);
        using var doc = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            CommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        });

        if (!doc.RootElement.TryGetProperty("DbConnection", out var dbConnection) ||
            !dbConnection.TryGetProperty("ConnectionConfigs", out var configs) ||
            configs.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException($"Database.json 缺少 DbConnection.ConnectionConfigs：{path}");

        // 只认库名 BetWin30 的 SqlServer，避免本地 Sqlite（常无 ConfigId）被误当成业务库
        foreach (var item in configs.EnumerateArray())
        {
            if (!item.TryGetProperty("ConnectionString", out var csEl))
                continue;
            var cs = csEl.GetString();
            if (string.IsNullOrWhiteSpace(cs))
                continue;
            if (item.TryGetProperty("DbType", out var dbTypeEl) &&
                !string.Equals(dbTypeEl.GetString(), "SqlServer", StringComparison.OrdinalIgnoreCase))
                continue;
            var dbName = ExtractDatabaseName(cs);
            if (string.Equals(dbName, "BetWin30", StringComparison.OrdinalIgnoreCase))
                return cs;
        }

        var fromUnitTest = TryLoadFromBetWinUnitTestAppConfig();
        if (!string.IsNullOrWhiteSpace(fromUnitTest))
            return fromUnitTest;

        throw new InvalidOperationException(
            $"未找到 BetWin30 SqlServer 连接串。请在 Database.json 配置 Database=BetWin30，或保证 BetWin.UnitTest/App.config 生效段可用。已查：{path}");
    }

    /// <summary>
    /// 从 BetWin 单测 App.config 生效段读取 DbConnection（与迁移硬约束 B13 一致）。
    /// </summary>
    /// <returns>连接串；找不到返回 null</returns>
    private static string TryLoadFromBetWinUnitTestAppConfig()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidates = new[]
            {
                Path.Combine(dir.FullName, "UnitTests", "BetWin.UnitTest", "App.config"),
                Path.Combine(dir.FullName, "betwin5.0", "UnitTests", "BetWin.UnitTest", "App.config"),
                Path.Combine(dir.FullName, "..", "betwin5.0", "UnitTests", "BetWin.UnitTest", "App.config"),
            };
            foreach (var relative in candidates)
            {
                var full = Path.GetFullPath(relative);
                if (!File.Exists(full))
                    continue;
                var text = File.ReadAllText(full);
                // 取未注释 connectionStrings 里 name=DbConnection 且 database=BetWin30 的串
                var match = Regex.Match(
                    text,
                    @"<connectionStrings>\s*<add\s+name=""DbConnection""\s+connectionString=""([^""]+)""",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (!match.Success)
                    continue;
                var cs = match.Groups[1].Value;
                if (ExtractDatabaseName(cs).Equals("BetWin30", StringComparison.OrdinalIgnoreCase))
                    return NormalizeSqlClientConnectionString(cs);
            }
            dir = dir.Parent;
        }
        return null;
    }

    /// <summary>
    /// 把旧版 server=;uid=;pwd= 写成 SqlClient 常用键名。
    /// </summary>
    /// <param name="legacy">旧连接串</param>
    /// <returns>规范化连接串</returns>
    private static string NormalizeSqlClientConnectionString(string legacy)
    {
        if (string.IsNullOrWhiteSpace(legacy))
            return legacy;
        var cs = legacy.Trim();
        cs = Regex.Replace(cs, @"\bserver\s*=", "Server=", RegexOptions.IgnoreCase);
        cs = Regex.Replace(cs, @"\buid\s*=", "User Id=", RegexOptions.IgnoreCase);
        cs = Regex.Replace(cs, @"\bpwd\s*=", "Password=", RegexOptions.IgnoreCase);
        cs = Regex.Replace(cs, @"\bdatabase\s*=", "Database=", RegexOptions.IgnoreCase);
        if (cs.IndexOf("TrustServerCertificate", StringComparison.OrdinalIgnoreCase) < 0)
            cs += ";Encrypt=True;TrustServerCertificate=True";
        return cs;
    }

    /// <summary>
    /// 创建只连业务库、关闭自动建表的 SqlSugar 客户端。
    /// </summary>
    /// <returns>已配置的 SqlSugarClient</returns>
    public static SqlSugarClient CreateClient()
    {
        var connectionString = LoadBetWin30ConnectionString();
        return new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = connectionString,
            DbType = DbType.SqlServer,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute
        });
    }

    /// <summary>
    /// 从测试输出目录向上定位 Host.Admin/Configuration/Database.json。
    /// </summary>
    /// <returns>Database.json 绝对路径</returns>
    private static string ResolveAdminDatabaseJsonPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "SimpX.Net.Host.Admin", "Configuration", "Database.json");
            if (File.Exists(candidate))
                return candidate;

            // 在 SimpX.Net 解决方案目录下
            var underSolution = Path.Combine(dir.FullName, "SimpX.Net", "SimpX.Net.Host.Admin", "Configuration", "Database.json");
            if (File.Exists(underSolution))
                return underSolution;

            dir = dir.Parent;
        }

        throw new FileNotFoundException("从测试目录向上未找到 SimpX.Net.Host.Admin/Configuration/Database.json。");
    }

    /// <summary>
    /// 从连接串里取出 Database= 的值，便于区分主库与日志库。
    /// </summary>
    /// <param name="connectionString">完整连接串</param>
    /// <returns>库名；解析不到则空串</returns>
    private static string ExtractDatabaseName(string connectionString)
    {
        var match = Regex.Match(connectionString, @"Database\s*=\s*([^;]+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }
}
