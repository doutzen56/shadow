namespace SimpX.Net.Application.Shared;

/// <summary>
/// 会员积分加减。对齐旧 PointsAgent.AddPoint（活动路径）。
/// </summary>
public interface IUserPointService
{
    /// <summary>
    /// 增加积分并写 usr_PointLog；同时累加 Users.Coin。
    /// </summary>
    /// <param name="siteId">站点</param>
    /// <param name="userId">会员</param>
    /// <param name="point">积分（正数）</param>
    /// <param name="pointType">积分类型（旧 PointItem.Event=18）</param>
    /// <param name="content">说明</param>
    /// <param name="eventType">活动类型编号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <param name="createAt">记账时间；空则用本机现在（单测可注入业务日）</param>
    Task AddPointAsync(
        int siteId,
        int userId,
        decimal point,
        int pointType,
        string content,
        int eventType = 0,
        CancellationToken cancellationToken = default,
        DateTime? createAt = null);
}
