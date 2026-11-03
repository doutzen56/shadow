namespace SimpX.Net.Application.Admin;

/// <summary>
/// 事件订阅
/// </summary>
public class AppEventSubscriber : IEventSubscriber, ISingleton
{
    /// <summary>
    /// 增加异常日志。告警邮件由 Core 的 <see cref="ErrorAlertMailSubscriber"/> 统一处理，这里不再重复发送。
    /// </summary>
    /// <param name="context">事件上下文，内容是 <see cref="SysLogEx"/></param>
    /// <returns>异步任务</returns>
    [EventSubscribe(CommonConst.AddExLog)]
    public async Task CreateExLog(EventHandlerExecutingContext context)
    {
        // 切换日志独立数据库
        var db = SqlSugarSetup.ITenant.IsAnyConnection(SqlSugarConst.LogConfigId)
            ? SqlSugarSetup.ITenant.GetConnectionScope(SqlSugarConst.LogConfigId)
            : SqlSugarSetup.ITenant.GetConnectionScope(SqlSugarConst.MainConfigId);
        await db.CopyNew().Insertable((SysLogEx)context.Source.Payload).ExecuteCommandAsync();
    }
}