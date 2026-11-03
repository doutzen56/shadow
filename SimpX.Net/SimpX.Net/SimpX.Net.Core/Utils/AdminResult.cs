namespace SimpX.Net.Core;

/// <summary>
/// 统一接口返回体（各宿主 HTTP 响应外壳）。
/// </summary>
/// <typeparam name="T">业务数据类型</typeparam>
public class AdminResult<T>
{
    /// <summary>
    /// 状态码
    /// </summary>
    public int Code { get; set; }

    /// <summary>
    /// 类型：success、warning、error
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// 数据
    /// </summary>
    public T Result { get; set; }

    /// <summary>
    /// 附加数据
    /// </summary>
    public object Extras { get; set; }

    /// <summary>
    /// 时间
    /// </summary>
    public DateTime Time { get; set; }
}
