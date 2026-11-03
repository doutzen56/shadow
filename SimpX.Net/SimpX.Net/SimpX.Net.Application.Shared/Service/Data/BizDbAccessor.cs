using Microsoft.Extensions.Configuration;
using SimpX.Net.Data;
using SqlSugar;
using DbType = SqlSugar.DbType;

namespace SimpX.Net.Application.Shared;

/// <summary>
/// 业务库访问。可选第二连接 ConfigId=BetWin30Biz；未配置时退回主库。
/// Sys* 仍用主库，避免 BetWin30 缺 SysUser 把宿主打挂。
/// </summary>
public interface IBizDbAccessor
{
    /// <summary>业务表用的 SqlSugar 客户端。</summary>
    ISqlSugarClient Db { get; }

    /// <summary>是否已接到独立业务库。</summary>
    bool IsExternalBiz { get; }
}

/// <summary>
/// 默认实现：独立挂 BetWin30Biz，与主库 Sqlite 隔离。
/// 按请求作用域建独立客户端，禁止多请求共用同一 SqlSugarClient，否则 SqlServer 会报 DataReader/MARS 冲突。
/// </summary>
public sealed class BizDbAccessor : IScoped, IBizDbAccessor
{
    /// <summary>业务库 ConfigId。</summary>
    public const string BizConfigId = "BetWin30Biz";

    private static readonly object SharedBizGate = new();
    private static DbConnectionConfig SharedBizConfig;
    private static bool SharedBizAsserted;

    private readonly ISqlSugarClient _db;
    private readonly bool _external;

    /// <summary>
    /// 注入根客户端与配置。
    /// </summary>
    /// <param name="db">当前注入的 SqlSugar（可能已切到租户主库）</param>
    /// <param name="configuration">配置（兜底探测）</param>
    public BizDbAccessor(ISqlSugarClient db, IConfiguration configuration)
    {
        var all = LoadConnectionConfigs(configuration);
        var bizCfg = all.FirstOrDefault(IsBizConfig)
            ?? all.FirstOrDefault(c => c.DbType == DbType.SqlServer); // 兜底：唯一 SqlServer 当业务库

        if (bizCfg != null)
        {
            // 不用多库根上的 GetConnectionScope：元数据会显示 SqlServer，实际 Queryable 仍可能落到主库 Sqlite。
            // 配置进程内复用；真正干活的客户端每个作用域新建，跟主库、跟其它请求彻底隔离。
            var biz = CreateScopedBizClient(bizCfg);
            _db = biz;
            _external = true;
            return;
        }

        Log.Information(
            "BizDbAccessor 未命中业务库，退回主库。已探测：" +
            string.Join(" | ", all.Select(c => $"{c.ConfigId}:{c.DbType}")));
        _db = db;
        _external = false;
    }

    /// <summary>
    /// 首次记下业务库配置并校验一次；之后每个作用域 new 独立客户端，避免并发抢同一条 SqlConnection。
    /// </summary>
    /// <param name="bizCfg">业务库配置</param>
    /// <returns>本作用域专用客户端</returns>
    private static SqlSugarClient CreateScopedBizClient(DbConnectionConfig bizCfg)
    {
        EnsureSharedBizConfig(bizCfg);

        // 配置进程内复用；客户端按作用域新建，连接与 DataReader 互不抢。
        return new SqlSugarClient(CloneBizConfig(SharedBizConfig));
    }

    /// <summary>
    /// 进程内只初始化一次业务库配置，并做一次 ADO 类型断言。
    /// </summary>
    /// <param name="bizCfg">原始业务库配置</param>
    private static void EnsureSharedBizConfig(DbConnectionConfig bizCfg)
    {
        if (SharedBizConfig != null && SharedBizAsserted)
            return;

        lock (SharedBizGate)
        {
            if (SharedBizConfig == null)
            {
                // 必须拷贝：多库根里的同一份 ConnectionConfig 会被主库切库逻辑改写 DbType/连接
                var isolated = CloneBizConfig(bizCfg);
                SqlSugarSetup.SetDbConfig(isolated);
                SharedBizConfig = isolated;
                Log.Information(
                    $"BizDbAccessor 已登记业务库 ConfigId={isolated.ConfigId} DbType={isolated.DbType}");
            }

            if (!SharedBizAsserted)
            {
                // 用临时客户端断言，断言完立刻关掉，不留给业务请求共用
                var probe = new SqlSugarClient(CloneBizConfig(SharedBizConfig));
                try
                {
                    AssertBizClient(probe, SharedBizConfig);
                    SharedBizAsserted = true;
                }
                finally
                {
                    try
                    {
                        probe.Ado?.Close();
                    }
                    catch
                    {
                        // 断言收尾关连接失败不影响后续；下次锁内会再试 Assert
                    }
                }
            }
        }
    }

    /// <inheritdoc />
    public ISqlSugarClient Db => _db;

    /// <inheritdoc />
    public bool IsExternalBiz => _external;

    /// <summary>
    /// 是否业务库段（按 ConfigId 或库昵称）。
    /// </summary>
    private static bool IsBizConfig(DbConnectionConfig c) =>
        c != null && (
            string.Equals(c.ConfigId?.ToString(), BizConfigId, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(c.DbNickName, "BetWin30业务库", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// 拷贝业务库连接配置，避免与主库 Scope 共用同一对象被改写。
    /// </summary>
    private static DbConnectionConfig CloneBizConfig(DbConnectionConfig source)
    {
        return new DbConnectionConfig
        {
            ConfigId = source.ConfigId?.ToString() ?? BizConfigId,
            DbType = source.DbType,
            ConnectionString = source.ConnectionString,
            DbNickName = source.DbNickName,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute,
            DbSettings = new DbSettings
            {
                EnableInitDb = false,
                EnableInitView = false,
                EnableDiffLog = false,
                EnableUnderLine = source.DbSettings?.EnableUnderLine ?? false,
                EnableConnStringEncrypt = false,
            },
            TableSettings = new TableSettings { EnableInitTable = false, EnableIncreTable = false },
            SeedSettings = new SeedSettings { EnableInitSeed = false, EnableIncreSeed = false },
            MoreSettings = new ConnMoreSettings
            {
                IsAutoDeleteQueryFilter = true,
                IsAutoUpdateQueryFilter = true,
                SqlServerCodeFirstNvarchar = true,
            },
        };
    }

    /// <summary>
    /// 校验客户端确实落在业务库上（看元数据 + 真实 ADO 连接类型）。
    /// </summary>
    private static void AssertBizClient(ISqlSugarClient biz, DbConnectionConfig expected)
    {
        var cfg = biz?.CurrentConnectionConfig
            ?? throw new InvalidOperationException("业务库客户端无 CurrentConnectionConfig。");

        // 打开一次才能看到真实 ADO 类型（未 Open 时 Connection 可能还是空）
        biz.Ado.Open();
        string adoType;
        try
        {
            adoType = biz.Ado.Connection?.GetType().FullName ?? "(null)";
        }
        finally
        {
            if (cfg.IsAutoCloseConnection)
                biz.Ado.Close();
        }

        var connPreview = MaskConn(cfg.ConnectionString);
        Log.Information(
            $"BizDbAccessor Assert ConfigId={cfg.ConfigId} DbType={cfg.DbType} Ado={adoType} Conn={connPreview}");

        if (cfg.DbType == DbType.Sqlite ||
            adoType.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"业务库切库后仍是 Sqlite（ConfigId={cfg.ConfigId}, Ado={adoType}, Conn={connPreview}），拒绝继续以免打错库。");
        }

        if (expected.DbType != DbType.Sqlite && cfg.DbType != expected.DbType)
        {
            throw new InvalidOperationException(
                $"业务库 DbType 期望 {expected.DbType}，实际 {cfg.DbType}（ConfigId={cfg.ConfigId}）。");
        }
    }

    /// <summary>
    /// 连接串只留前缀，去掉账号密码。
    /// </summary>
    private static string MaskConn(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return "(empty)";
        var s = connectionString;
        foreach (var key in new[] { "Password=", "Pwd=", "User Id=", "User ID=", "UID=" })
        {
            var i = s.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (i < 0)
                continue;
            var start = i + key.Length;
            var end = s.IndexOf(';', start);
            if (end < 0)
                end = s.Length;
            s = s.Substring(0, start) + "****" + s.Substring(end);
        }

        return s.Length <= 80 ? s : s.Substring(0, 80) + "...";
    }

    /// <summary>
    /// 与 AddSqlSugar 同路读库配置，并合并 IConfiguration 兜底。
    /// </summary>
    private static List<DbConnectionConfig> LoadConnectionConfigs(IConfiguration configuration)
    {
        var list = new List<DbConnectionConfig>();
        try
        {
            var opts = App.GetConfig<DbConnectionOptions>("DbConnection", true);
            if (opts?.ConnectionConfigs != null)
                list.AddRange(opts.ConnectionConfigs.Where(c => c != null));
        }
        catch (Exception ex)
        {
            Log.Warning($"BizDbAccessor GetConfig(DbConnection) 失败：{ex.Message}");
        }

        if (list.Any(IsBizConfig) || list.Any(c => c.DbType == DbType.SqlServer))
            return list;

        // GetConfig 没带业务库时，从 IConfiguration 手工拼
        try
        {
            var configs = configuration ?? App.Configuration;
            foreach (var child in configs.GetSection("DbConnection:ConnectionConfigs").GetChildren())
            {
                var id = child["ConfigId"];
                if (string.IsNullOrWhiteSpace(id))
                    continue;
                if (!list.Any(c => string.Equals(c.ConfigId?.ToString(), id, StringComparison.OrdinalIgnoreCase)))
                {
                    Enum.TryParse<DbType>(child["DbType"], true, out var dbType);
                    list.Add(new DbConnectionConfig
                    {
                        ConfigId = id,
                        DbType = dbType,
                        ConnectionString = child["ConnectionString"],
                        DbNickName = child["DbNickName"],
                        DbSettings = new DbSettings { EnableInitDb = false },
                        TableSettings = new TableSettings { EnableInitTable = false },
                        SeedSettings = new SeedSettings { EnableInitSeed = false },
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning($"BizDbAccessor 从 IConfiguration 读 ConnectionConfigs 失败：{ex.Message}");
        }

        return list;
    }

}
