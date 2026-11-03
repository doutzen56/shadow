using SimpX.Net.Core;
using SimpX.Net.Feature.Auth;
namespace SimpX.Net.Hosting;

/// <summary>
/// 全局规范化结果
/// </summary>
[UnifyModel(typeof(AdminResult<>))]
public class AdminResultProvider : IUnifyResultProvider
{
    /// <summary>
    /// JWT 授权异常返回值
    /// </summary>
    /// <param name="context"></param>
    /// <param name="metadata"></param>
    /// <returns></returns>
    public IActionResult OnAuthorizeException(DefaultHttpContext context, ExceptionMetadata metadata)
    {
        return new JsonResult(RESTfulResult(metadata.StatusCode, data: metadata.Data, msg: metadata.Errors), UnifyContext.GetSerializerSettings(context));
    }

    /// <summary>
    /// 异常返回值
    /// </summary>
    /// <param name="context"></param>
    /// <param name="metadata"></param>
    /// <returns></returns>
    public IActionResult OnException(ExceptionContext context, ExceptionMetadata metadata)
    {
        return new JsonResult(RESTfulResult(metadata.StatusCode, data: metadata.Data, msg: metadata.Errors), UnifyContext.GetSerializerSettings(context));
    }

    /// <summary>
    /// 成功返回值
    /// </summary>
    /// <param name="context"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public IActionResult OnSucceeded(ActionExecutedContext context, object data)
    {
        return new JsonResult(RESTfulResult(StatusCodes.Status200OK, true, data), UnifyContext.GetSerializerSettings(context));
    }

    /// <summary>
    /// 验证失败返回值
    /// </summary>
    /// <param name="context"></param>
    /// <param name="metadata"></param>
    /// <returns></returns>
    public IActionResult OnValidateFailed(ActionExecutingContext context, ValidationMetadata metadata)
    {
        return new JsonResult(RESTfulResult(metadata.StatusCode ?? StatusCodes.Status400BadRequest, data: metadata.Data, msg: metadata.ValidationResult), UnifyContext.GetSerializerSettings(context));
    }

    /// <summary>
    /// 特定状态码返回值
    /// </summary>
    /// <param name="context"></param>
    /// <param name="statusCode"></param>
    /// <param name="unifyResultSettings"></param>
    /// <returns></returns>
    public async Task OnResponseStatusCodes(HttpContext context, int statusCode, UnifyResultSettingsOptions unifyResultSettings)
    {
        // SignalR 协商/长连接必须拿到真实 HTTP 状态码；包成统一 JSON 且改成 200 后，
        // 前端会报 “None of the transports supported”，在线用户永远连不上。
        if (context.Request.Path.StartsWithSegments("/hubs"))
            return;

        // 设置响应状态码
        UnifyContext.SetResponseStatusCodes(context, statusCode, unifyResultSettings);

        switch (statusCode)
        {
            // 处理 401 状态码
            case StatusCodes.Status401Unauthorized:
                var msg = "401 登录已过期，请重新登录";
                // 若存在身份验证失败消息，则返回消息内容
                if (context.Items.TryGetValue(SignatureAuthenticationDefaults.AuthenticateFailMsgKey, out var authFailMsg))
                    msg = authFailMsg + "";
                await context.Response.WriteAsJsonAsync(RESTfulResult(statusCode, msg: msg),
                    App.GetOptions<JsonOptions>()?.JsonSerializerOptions);
                break;
            // 处理 403 状态码
            case StatusCodes.Status403Forbidden:
                await context.Response.WriteAsJsonAsync(RESTfulResult(statusCode, msg: "403 禁止访问，没有权限"),
                    App.GetOptions<JsonOptions>()?.JsonSerializerOptions);
                break;
            // 处理 302 状态码
            case StatusCodes.Status302Found:
                if (context.Response.Headers.TryGetValue("Location", out var redirectUrl))
                {
                    context.Response.Redirect(redirectUrl);
                }
                else
                {
                    var errorMessage = "302 跳转失败，没有提供 Location 头信息";
                    await context.Response.WriteAsJsonAsync(RESTfulResult(statusCode, msg: errorMessage),
                        App.GetOptions<JsonOptions>()?.JsonSerializerOptions);
                }
                break;
        }
    }

    /// <summary>
    /// 返回成功结果集
    /// </summary>
    /// <param name="message"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public static AdminResult<object> Ok(string message, object data = default)
    {
        return RESTfulResult(StatusCodes.Status200OK, true, data, message);
    }

    /// <summary>
    /// 返回失败结果集
    /// </summary>
    /// <param name="message"></param>
    /// <param name="code"></param>
    /// <param name="data"></param>
    /// <returns></returns>
    public static AdminResult<object> Error(string message, int code = StatusCodes.Status400BadRequest, object data = default)
    {
        return RESTfulResult(code, false, data, message);
    }

    /// <summary>
    /// 返回 RESTful 风格结果集
    /// </summary>
    /// <param name="statusCode"></param>
    /// <param name="succeeded"></param>
    /// <param name="data"></param>
    /// <param name="msg"></param>
    /// <returns></returns>
    private static AdminResult<object> RESTfulResult(int statusCode, bool succeeded = default, object data = default, object msg = default)
    {
        //// 统一返回值脱敏处理
        //if (data?.GetType() == typeof(String))
        //{
        //    data = App.GetRequiredService<ISensitiveDetectionProvider>().ReplaceAsync(data.ToString(), '*').GetAwaiter().GetResult();
        //}
        //else if (data?.GetType() == typeof(JsonResult))
        //{
        //    data = App.GetRequiredService<ISensitiveDetectionProvider>().ReplaceAsync(JSON.Serialize(data), '*').GetAwaiter().GetResult();
        //}

        return new AdminResult<object>
        {
            Code = statusCode,
            Message = msg is null or string ? (msg + "") : JSON.Serialize(msg),
            Result = data,
            Type = succeeded ? "success" : "error",
            Extras = UnifyContext.Take(),
            Time = DateTime.Now
        };
    }
}
