using Xunit;

namespace SimpX.Net.Test;

/// <summary>
/// 从 BetWin30 导出一比一 SQLite DDL，并校验列名集合。
/// </summary>
public class SqliteBizSchemaExportTests
{
    /// <summary>
    /// 导出 DDL 到 Fixtures/biz-schema，并在内存库上校验列名。
    /// </summary>
    [Fact]
    public void Export_And_Assert_ColumnNames_OneToOne()
    {
        var outDir = ResolveOutputDir();
        var count = SqliteBizSchema.ExportToDirectory(outDir);
        Assert.True(count >= 3, $"至少应导出若干表，实际 {count}");

        using var sqlServer = OrmConnection.CreateClient();
        using var sqlite = SqliteBizSchema.CreateMemoryDb(
            ["site_Domain", "usr_Group", "usr_IdCard"],
            []);

        foreach (var t in new[] { "site_Domain", "usr_Group", "usr_IdCard" })
            SqliteBizSchema.AssertColumnNamesMatch(sqlServer, sqlite, t);
    }

    /// <summary>
    /// 定位 Test 项目下 Fixtures/biz-schema。
    /// </summary>
    private static string ResolveOutputDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, "SimpX.Net.Test", "Fixtures", "biz-schema");
            if (Directory.Exists(Path.GetDirectoryName(candidate)!) || dir.Name == "SimpX.Net.Test")
            {
                var path = Path.Combine(dir.FullName, "Fixtures", "biz-schema");
                if (dir.Name == "SimpX.Net.Test")
                    path = Path.Combine(dir.FullName, "Fixtures", "biz-schema");
                else
                    path = candidate;
                Directory.CreateDirectory(path);
                return path;
            }

            var under = Path.Combine(dir.FullName, "SimpX.Net", "SimpX.Net.Test", "Fixtures", "biz-schema");
            if (Directory.Exists(Path.Combine(dir.FullName, "SimpX.Net", "SimpX.Net.Test")))
            {
                Directory.CreateDirectory(under);
                return under;
            }

            dir = dir.Parent;
        }

        var fallback = Path.Combine(Path.GetTempPath(), "simpx-biz-schema");
        Directory.CreateDirectory(fallback);
        return fallback;
    }
}
