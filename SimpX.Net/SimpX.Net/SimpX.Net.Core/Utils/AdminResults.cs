namespace SimpX.Net.Core;

/// <summary>
/// 统一返回体的静态构造（给应用层写 JsonResult 用；统一结果包装在 Hosting.AdminResultProvider）。
/// </summary>
public static class AdminResults
{
    /// <summary>
    /// 返回成功结果。
    /// </summary>
    /// <param name="message">提示信息</param>
    /// <param name="data">业务数据</param>
    /// <returns>统一返回体</returns>
    public static AdminResult<object> Ok(string message, object data = default)
    {
        return Build(200, true, data, message);
    }

    /// <summary>
    /// 返回失败结果。
    /// </summary>
    /// <param name="message">错误信息</param>
    /// <param name="code">状态码，默认 400</param>
    /// <param name="data">附加数据</param>
    /// <returns>统一返回体</returns>
    public static AdminResult<object> Error(string message, int code = 400, object data = default)
    {
        return Build(code, false, data, message);
    }

    /// <summary>
    /// 组装统一返回体。
    /// </summary>
    private static AdminResult<object> Build(int statusCode, bool succeeded, object data, object msg)
    {
        return new AdminResult<object>
        {
            Code = statusCode,
            Message = msg is null or string ? (msg + "") : msg.ToString(),
            Result = data,
            Type = succeeded ? "success" : "error",
            Time = DateTime.Now
        };
    }
}
