namespace SimpX.Net.Data;

/// <summary>
/// 数据变更（差异）日志的落库方式。SqlSugar 差异日志拦截只负责整理出 <see cref="DataChangeLog"/>，存到哪由实现决定。
/// 宿主没注册实现时不记差异日志。
/// </summary>
public interface IDataChangeLogWriter
{
    /// <summary>
    /// 写入一条数据变更日志。
    /// </summary>
    /// <param name="log">变更日志</param>
    /// <returns>异步任务</returns>
    Task WriteAsync(DataChangeLog log);
}

/// <summary>
/// 一次数据变更（insert / update / delete）的差异记录。
/// </summary>
public sealed class DataChangeLog
{
    /// <summary>差异数据：表名、表描述、各列变更前后的值（JSON）。</summary>
    public string DiffData { get; set; }

    /// <summary>业务对象（JSON），没有时为首个变更表名。</summary>
    public string BusinessData { get; set; }

    /// <summary>变更类型：insert、update、delete。</summary>
    public string DiffType { get; set; }

    /// <summary>执行的 SQL。</summary>
    public string Sql { get; set; }

    /// <summary>SQL 参数（JSON）。</summary>
    public string Parameters { get; set; }

    /// <summary>耗时（毫秒）。</summary>
    public long Elapsed { get; set; }
}
