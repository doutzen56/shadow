namespace SimpX.Net.Application.Shared;

/// <summary>
/// 游戏推送路由：按 gameType 选适配；无人认领则 505。
/// </summary>
public sealed class GamePushService : ITransient
{
    private readonly IEnumerable<IGamePushHandler> _handlers;

    /// <summary>
    /// 注入全部推送适配。
    /// </summary>
    /// <param name="handlers">适配列表</param>
    public GamePushService(IEnumerable<IGamePushHandler> handlers) =>
        _handlers = handlers ?? Array.Empty<IGamePushHandler>();

    /// <summary>
    /// 处理推送。
    /// </summary>
    /// <param name="gameType">路径类型</param>
    /// <param name="rawBody">正文</param>
    /// <param name="form">表单/查询</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>结果</returns>
    public Task<GamePushOutcome> HandleAsync(
        string gameType,
        string rawBody,
        Dictionary<string, string> form,
        CancellationToken cancellationToken = default)
    {
        var handler = _handlers.FirstOrDefault(h => h.Supports(gameType));
        if (handler == null)
        {
            return Task.FromResult(new GamePushOutcome
            {
                Ok = false,
                StatusCode = 505,
                Body = "game push not implemented"
            });
        }

        return handler.HandleAsync(gameType, rawBody, form, cancellationToken);
    }
}