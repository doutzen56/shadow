namespace SimpX.Net.Application.Admin;

public class LogVisOutput
{
    /// <summary>
    /// 登录地点
    /// </summary>
    public string Location { get; set; }

    /// <summary>
    /// 经度
    /// </summary>
    public double? Longitude { get; set; }

    /// <summary>
    /// 维度
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// 真实姓名
    /// </summary>
    public string RealName { get; set; }

    /// <summary>
    /// 日志时间
    /// </summary>
    public DateTime? LogDateTime { get; set; }
}