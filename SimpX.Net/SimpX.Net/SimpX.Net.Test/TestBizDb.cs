using SimpX.Net.Application.Shared;
using SqlSugar;

namespace SimpX.Net.Test;

/// <summary>
/// 单测用：把内存库包装成 IBizDbAccessor。
/// </summary>
internal sealed class TestBizDb : IBizDbAccessor
{
    /// <summary>包装已有客户端。</summary>
    public TestBizDb(ISqlSugarClient db) => Db = db;

    /// <inheritdoc />
    public ISqlSugarClient Db { get; }

    /// <inheritdoc />
    public bool IsExternalBiz => false;
}
