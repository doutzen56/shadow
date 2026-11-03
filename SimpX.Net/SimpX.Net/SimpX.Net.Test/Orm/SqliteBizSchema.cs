using System.Text;
using System.Text.RegularExpressions;
using SqlSugar;

namespace SimpX.Net.Test;

/// <summary>
/// 从 BetWin30 拉列定义，一比一生成 SQLite 业务表（测试用；不对宿主 CodeFirst）。
/// </summary>
internal static class SqliteBizSchema
{
    /// <summary>本波要还原并测 CRUD/查询的业务表。</summary>
    public static readonly string[] DefaultTables =
    [
        "site_Domain",
        "usr_Group",
        "usr_IdCard",
        "usr_LoginLog",
        "game_RewardLog",
        "site_Host",
        "site_News",
        "site_NewsColumn",
        "site_Help",
        "site_HelpColumn",
        "usr_Invite",
        "usr_Domain",
        "usr_GameConfig",
        "site_Config_BlackIP",
        "site_Event",
        "site_EventColumn",
        "game_Setting",
        "site_ExchangeRate",
        "site_Currency",
        "usr_CurrencyExchange",
        "sys_Exchange",
        "Site",
        "Users",
        "site_Language",
        "usr_BlackKyc",
        "site_Payment",
        "site_PaymentCategory"
    ];

    /// <summary>视图当表建（只读测）。</summary>
    public static readonly string[] DefaultViewsAsTables =
    [
        "rpt_UserDateGameView"
    ];

    /// <summary>
    /// 列定义一行。
    /// </summary>
    public sealed class ColumnDef
    {
        /// <summary>列名。</summary>
        public string COLUMN_NAME { get; set; }

        /// <summary>SQL Server 类型名。</summary>
        public string DATA_TYPE { get; set; }

        /// <summary>字符长度。</summary>
        public int? CHARACTER_MAXIMUM_LENGTH { get; set; }

        /// <summary>YES/NO。</summary>
        public string IS_NULLABLE { get; set; }

        /// <summary>是否自增。</summary>
        public int IS_IDENTITY { get; set; }

        /// <summary>顺序。</summary>
        public int ORDINAL_POSITION { get; set; }
    }

    /// <summary>
    /// 从 BetWin30 读表列并拼 SQLite DDL。
    /// </summary>
    /// <param name="sqlServer">BetWin30 连接</param>
    /// <param name="tableName">表或视图名</param>
    /// <param name="relaxNotNull">测试内存库放宽可空，避免实体未映射列插不进；导出 DDL 保持源库可空性</param>
    /// <returns>DROP+CREATE 脚本</returns>
    public static string BuildCreateSql(ISqlSugarClient sqlServer, string tableName, bool relaxNotNull = false)
    {
        var cols = sqlServer.Ado.SqlQuery<ColumnDef>(
            """
            SELECT c.COLUMN_NAME, c.DATA_TYPE, c.CHARACTER_MAXIMUM_LENGTH, c.IS_NULLABLE, c.ORDINAL_POSITION,
              ISNULL(COLUMNPROPERTY(OBJECT_ID(QUOTENAME(c.TABLE_SCHEMA)+'.'+QUOTENAME(c.TABLE_NAME)), c.COLUMN_NAME, 'IsIdentity'), 0) AS IS_IDENTITY
            FROM INFORMATION_SCHEMA.COLUMNS c
            WHERE c.TABLE_NAME = @tableName
            ORDER BY c.ORDINAL_POSITION
            """,
            new { tableName });

        if (cols == null || cols.Count == 0)
            throw new InvalidOperationException($"BetWin30 找不到表/视图列：{tableName}");

        var lines = new List<string>();
        foreach (var c in cols)
        {
            var typ = MapSqliteType(c.DATA_TYPE);
            var nullSql = "";
            if (!relaxNotNull && !string.Equals(c.IS_NULLABLE, "YES", StringComparison.OrdinalIgnoreCase))
                nullSql = " NOT NULL";
            var idSql = "";
            if (c.IS_IDENTITY == 1)
            {
                typ = "INTEGER";
                nullSql = "";
                idSql = " PRIMARY KEY AUTOINCREMENT";
            }

            lines.Add($"  [{c.COLUMN_NAME}] {typ}{idSql}{nullSql}");
        }

        var sb = new StringBuilder();
        sb.AppendLine($"DROP TABLE IF EXISTS [{tableName}];");
        sb.AppendLine($"CREATE TABLE [{tableName}] (");
        sb.AppendLine(string.Join(",\n", lines));
        sb.AppendLine(");");
        return sb.ToString();
    }

    /// <summary>
    /// 导出 DDL 到目录（UTF-8 无 BOM）。
    /// </summary>
    /// <param name="outputDirectory">输出目录</param>
    /// <param name="tables">表名</param>
    /// <param name="viewsAsTables">当表建的视图</param>
    /// <returns>写出的文件数</returns>
    public static int ExportToDirectory(
        string outputDirectory,
        IEnumerable<string> tables = null,
        IEnumerable<string> viewsAsTables = null)
    {
        Directory.CreateDirectory(outputDirectory);
        using var sqlServer = OrmConnection.CreateClient();
        var all = new StringBuilder();
        var count = 0;
        foreach (var t in tables ?? DefaultTables)
        {
            var ddl = BuildCreateSql(sqlServer, t, relaxNotNull: false);
            var path = Path.Combine(outputDirectory, t + ".sql");
            File.WriteAllText(path, ddl, new UTF8Encoding(false));
            all.AppendLine(ddl).AppendLine();
            count++;
        }

        foreach (var v in viewsAsTables ?? DefaultViewsAsTables)
        {
            try
            {
                var ddl = BuildCreateSql(sqlServer, v, relaxNotNull: false);
                var path = Path.Combine(outputDirectory, v + ".sql");
                File.WriteAllText(path, ddl, new UTF8Encoding(false));
                all.AppendLine(ddl).AppendLine();
                count++;
            }
            catch (InvalidOperationException)
            {
                // 视图可能不在当前库
            }
        }

        File.WriteAllText(Path.Combine(outputDirectory, "_all.sql"), all.ToString(), new UTF8Encoding(false));
        return count;
    }

    /// <summary>
    /// 建内存 SQLite，并按 BetWin30 列一比一建表。
    /// </summary>
    /// <param name="tables">要建的表</param>
    /// <param name="viewsAsTables">当表建的视图</param>
    /// <returns>已建好的 Sqlite 客户端</returns>
    public static SqlSugarClient CreateMemoryDb(
        IEnumerable<string> tables = null,
        IEnumerable<string> viewsAsTables = null)
    {
        using var sqlServer = OrmConnection.CreateClient();
        var sqlite = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = "DataSource=:memory:",
            DbType = DbType.Sqlite,
            IsAutoCloseConnection = false,
            InitKeyType = InitKeyType.Attribute
        });
        sqlite.Ado.Open();
        sqlite.Ado.ExecuteCommand("pragma foreign_keys=off;");

        // 内存测库：列名/类型一比一；可空性放宽，方便实体局部插入
        foreach (var t in tables ?? DefaultTables)
            sqlite.Ado.ExecuteCommand(BuildCreateSql(sqlServer, t, relaxNotNull: true));

        foreach (var v in viewsAsTables ?? DefaultViewsAsTables)
        {
            try
            {
                sqlite.Ado.ExecuteCommand(BuildCreateSql(sqlServer, v, relaxNotNull: true));
            }
            catch (InvalidOperationException)
            {
            }
        }

        return sqlite;
    }

    /// <summary>
    /// SQL Server → SQLite 类型映射（列名不变）。
    /// </summary>
    private static string MapSqliteType(string sqlServerType)
    {
        var t = (sqlServerType ?? "nvarchar").Trim().ToLowerInvariant();
        return t switch
        {
            "bit" or "tinyint" or "smallint" or "int" or "bigint" => "INTEGER",
            "decimal" or "numeric" or "money" or "smallmoney" or "float" or "real" => "REAL",
            "datetime" or "datetime2" or "date" or "time" or "datetimeoffset" or "smalldatetime" => "TEXT",
            "uniqueidentifier" => "TEXT",
            "varbinary" or "binary" or "image" or "rowversion" or "timestamp" => "BLOB",
            _ => "TEXT"
        };
    }

    /// <summary>
    /// 自检：SQLite 表列名集合必须等于 SQL Server 列名集合（一比一）。
    /// </summary>
    /// <param name="sqlServer">源库</param>
    /// <param name="sqlite">目标库</param>
    /// <param name="tableName">表名</param>
    public static void AssertColumnNamesMatch(ISqlSugarClient sqlServer, ISqlSugarClient sqlite, string tableName)
    {
        var src = sqlServer.Ado.SqlQuery<string>(
            "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME=@tableName ORDER BY ORDINAL_POSITION",
            new { tableName });
        var dstCols = sqlite.Ado.GetDataTable($"PRAGMA table_info([{tableName}])");
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (System.Data.DataRow row in dstCols.Rows)
            names.Add(Convert.ToString(row["name"]));

        var missing = src.Where(c => !names.Contains(c)).ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException($"{tableName} SQLite 缺列：{string.Join(",", missing)}");

        var extra = names.Where(n => !src.Any(s => string.Equals(s, n, StringComparison.OrdinalIgnoreCase))).ToList();
        if (extra.Count > 0)
            throw new InvalidOperationException($"{tableName} SQLite 多列：{string.Join(",", extra)}");
    }
}
